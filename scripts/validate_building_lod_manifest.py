#!/usr/bin/env python3
"""Validate a CityForge 3D-building authoring manifest before Unity import."""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

LEVELS = (0, 1, 2, 3)
ANGLES = tuple(range(0, 360, 45))
REDUCTION_RANGES = {1: (50.0, 70.0), 2: (20.0, 40.0), 3: (8.0, 18.0)}


def vector(value: object, label: str) -> tuple[float, float, float]:
    if not isinstance(value, list) or len(value) != 3:
        raise ValueError(f"{label} must contain exactly three numbers")
    try:
        result = tuple(float(item) for item in value)
    except (TypeError, ValueError) as error:
        raise ValueError(f"{label} contains a non-numeric value") from error
    if not all(math.isfinite(item) for item in result):
        raise ValueError(f"{label} contains a non-finite value")
    return result  # type: ignore[return-value]


def delta(left: tuple[float, ...], right: tuple[float, ...]) -> float:
    return max(abs(a - b) for a, b in zip(left, right))


def has_basename(root: Path, name: str) -> bool:
    return any(path.is_file() for path in root.rglob(Path(name).name))


def validate(manifest_path: Path, package_root: Path | None,
             bounds_tolerance: float) -> dict[str, object]:
    data = json.loads(manifest_path.read_text(encoding="utf-8"))
    errors: list[str] = []
    warnings: list[str] = []

    if data.get("coordinateSystem") != "blender-metric-origin-centered":
        errors.append("coordinateSystem must be blender-metric-origin-centered")
    if data.get("originPolicy") != "foundation-center-ground":
        errors.append("originPolicy must be foundation-center-ground")
    try:
        if delta(vector(data.get("rotationAnchor"), "rotationAnchor"),
                 (0.0, 0.0, 0.0)) > 1e-6:
            errors.append("rotationAnchor must be [0, 0, 0]")
    except ValueError as error:
        errors.append(str(error))

    lod_values = data.get("lods")
    lods = lod_values if isinstance(lod_values, list) else []
    by_level = {item.get("level"): item for item in lods
                if isinstance(item, dict)}
    missing = [level for level in LEVELS if level not in by_level]
    if missing:
        errors.append("missing LOD levels: " + ", ".join(map(str, missing)))

    expected_bounds: dict[str, object] | None = None
    reference = by_level.get(0)
    if isinstance(reference, dict):
        try:
            ref_dimensions = vector(reference.get("boundsDimensions"),
                                    "LOD0.boundsDimensions")
            ref_center = vector(reference.get("boundsCenter"),
                                "LOD0.boundsCenter")
            ref_origin = vector(reference.get("origin"), "LOD0.origin")
            ref_rotation = vector(reference.get("rotationEulerRadians"),
                                  "LOD0.rotationEulerRadians")
            ref_scale = vector(reference.get("scale"), "LOD0.scale")
            ref_materials = reference.get("materials")
            ref_triangles = int(reference.get("triangles", 0))
            if ref_triangles <= 0:
                errors.append("LOD0.triangles must be positive")
            if min(ref_dimensions) <= 0:
                errors.append("LOD0 bounds dimensions must be positive")
            expected_bounds = {
                "dimensionsMeters": list(ref_dimensions),
                "centerMeters": list(ref_center),
                "originMeters": list(ref_origin),
                "unityScalePolicy": "derive imported scale from expected bounds",
            }
            for level in LEVELS[1:]:
                item = by_level.get(level)
                if not isinstance(item, dict):
                    continue
                dimensions = vector(item.get("boundsDimensions"),
                                    f"LOD{level}.boundsDimensions")
                center = vector(item.get("boundsCenter"),
                                f"LOD{level}.boundsCenter")
                origin = vector(item.get("origin"), f"LOD{level}.origin")
                rotation = vector(item.get("rotationEulerRadians"),
                                  f"LOD{level}.rotationEulerRadians")
                scale = vector(item.get("scale"), f"LOD{level}.scale")
                drift = delta(dimensions, ref_dimensions) / max(ref_dimensions)
                if drift > bounds_tolerance:
                    errors.append(f"LOD{level} exterior bounds differ from LOD0 by {drift:.3%}")
                if delta(center, ref_center) > 0.02:
                    errors.append(f"LOD{level} bounds center drifts more than 2 cm")
                if delta(origin, ref_origin) > 1e-5:
                    errors.append(f"LOD{level} origin differs from LOD0")
                if delta(rotation, ref_rotation) > 1e-5:
                    errors.append(f"LOD{level} rotation differs from LOD0")
                if delta(scale, ref_scale) > 1e-5:
                    errors.append(f"LOD{level} scale differs from LOD0")
                if item.get("materials") != ref_materials:
                    errors.append(f"LOD{level} material-slot ordering differs from LOD0")
                triangles = int(item.get("triangles", 0))
                percent = triangles * 100.0 / ref_triangles if ref_triangles else 0.0
                low, high = REDUCTION_RANGES[level]
                if not low <= percent <= high:
                    warnings.append(f"LOD{level} is {percent:.1f}% of LOD0; expected {low:.0f}-{high:.0f}%")
        except (TypeError, ValueError) as error:
            errors.append(str(error))
    else:
        errors.append("LOD0 record is missing")

    export = data.get("export") if isinstance(data.get("export"), dict) else {}
    for key, expected in (("forward", "-Z"), ("up", "Y"),
                          ("identicalPathForAllLods", True)):
        if export.get(key) != expected:
            errors.append(f"export.{key} must be {expected!r}")

    billboards = data.get("billboards")
    names = billboards if isinstance(billboards, list) else []
    for angle in ANGLES:
        if not any(f"{angle:03d}" in str(name) for name in names):
            errors.append(f"missing billboard yaw {angle:03d}")
    qa = data.get("qa") if isinstance(data.get("qa"), dict) else {}
    if qa.get("cameraElevationDegrees") != 20:
        errors.append("qa.cameraElevationDegrees must be 20")
    if qa.get("transparentBackground") is not True:
        errors.append("billboards must use transparent backgrounds")
    if qa.get("bakedGroundShadow") is not False:
        errors.append("billboards must not contain a baked ground shadow")

    if package_root:
        for group in ("textures", "billboards"):
            values = data.get(group)
            for name in values if isinstance(values, list) else []:
                if not has_basename(package_root, str(name)):
                    errors.append(f"referenced {group[:-1]} is missing: {name}")
        for level, item in by_level.items():
            if isinstance(item, dict) and item.get("fbx") and not has_basename(
                    package_root, str(item["fbx"])):
                errors.append(f"referenced LOD{level} FBX is missing: {item['fbx']}")

    return {
        "schema": "cityforge.unity-building-intake-contract.v1",
        "assetId": data.get("assetId"),
        "sourceManifest": str(manifest_path.resolve()),
        "valid": not errors,
        "errors": errors,
        "warnings": warnings,
        "expectedBounds": expected_bounds,
        "requiredUnityChecks": [
            "derive one uniform package scale from imported LOD0 versus expectedBounds",
            "apply that scale once to the shared Representations root",
            "recalculate LODGroup bounds after scaling",
            "compare every imported renderer bounds and ground plane numerically",
            "resolve all materials and textures",
            "add to current lot, select immediately, and preserve lot lighting",
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("--package-root", type=Path)
    parser.add_argument("--bounds-tolerance", type=float, default=0.01)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    try:
        report = validate(args.manifest, args.package_root, args.bounds_tolerance)
        rendered = json.dumps(report, indent=2) + "\n"
        if args.output:
            args.output.parent.mkdir(parents=True, exist_ok=True)
            args.output.write_text(rendered, encoding="utf-8")
        print(rendered, end="")
        return 0 if report["valid"] else 2
    except (OSError, ValueError, json.JSONDecodeError) as error:
        print(f"validate_building_lod_manifest: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
