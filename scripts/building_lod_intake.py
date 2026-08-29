#!/usr/bin/env python3
"""Safely stage authored building LOD archives for CityForge Unity import.

This script does not modify meshes, textures, or Unity metadata. Unity-specific
package/prefab construction remains in Building3DPackageEditor.cs.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path, PurePosixPath

LOD_PATTERN = re.compile(r"^(?P<name>.+)-LOD(?P<level>[0-3])\.zip$", re.IGNORECASE)
PACKAGE_FOLDERS = (
    "Source", "LOD0", "LOD1", "LOD2", "LOD3", "Impostor",
    "Materials", "Textures", "Prefabs",
)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def discover(source: Path) -> tuple[str, dict[int, Path]]:
    archives: dict[int, Path] = {}
    names: set[str] = set()
    for path in sorted(source.glob("*.zip")):
        match = LOD_PATTERN.match(path.name)
        if not match:
            continue
        level = int(match.group("level"))
        if level in archives:
            raise ValueError(f"Duplicate LOD{level} archive: {path.name}")
        archives[level] = path
        names.add(match.group("name"))
    missing = sorted(set(range(4)) - archives.keys())
    if missing:
        raise ValueError("Missing authored archives: " +
                         ", ".join(f"LOD{level}" for level in missing))
    if len(names) != 1:
        raise ValueError(f"LOD archive basenames disagree: {sorted(names)}")
    return names.pop(), archives


def safe_members(archive: zipfile.ZipFile) -> list[zipfile.ZipInfo]:
    members = archive.infolist()
    for member in members:
        path = PurePosixPath(member.filename)
        if path.is_absolute() or ".." in path.parts:
            raise ValueError(f"Unsafe ZIP member: {member.filename}")
    return members


def inspect_archive(path: Path) -> dict[str, object]:
    with zipfile.ZipFile(path) as archive:
        members = safe_members(archive)
        files = [member.filename for member in members if not member.is_dir()]
    fbx = [name for name in files if name.lower().endswith(".fbx")]
    textures = [name for name in files if name.lower().endswith(
        (".png", ".jpg", ".jpeg", ".tif", ".tiff"))]
    if len(fbx) != 1:
        raise ValueError(f"{path.name} must contain exactly one FBX; found {len(fbx)}")
    return {"fbx": fbx[0], "textures": textures, "files": files}


def apply_progress(manifest: dict[str, object], updates: list[str]) -> None:
    progress = manifest["progress"]
    assert isinstance(progress, dict)
    allowed = {"pending", "in_progress", "complete", "blocked"}
    for update in updates:
        if "=" not in update:
            raise ValueError(f"Progress update must be KEY=STATUS: {update}")
        key, value = update.split("=", 1)
        if key not in progress:
            raise ValueError(f"Unknown progress key: {key}")
        if value not in allowed:
            raise ValueError(f"Invalid progress status for {key}: {value}")
        progress[key] = value


def stage(source: Path, destination: Path, *, force: bool,
          progress_updates: list[str]) -> dict[str, object]:
    building_name, archives = discover(source)
    if destination.exists() and any(destination.iterdir()) and not force:
        raise FileExistsError(
            f"Destination is not empty: {destination} (pass --force to update it)")
    for folder in PACKAGE_FOLDERS:
        (destination / folder).mkdir(parents=True, exist_ok=True)

    records = []
    for level, archive_path in sorted(archives.items()):
        inspection = inspect_archive(archive_path)
        copied_archive = destination / "Source" / archive_path.name
        shutil.copy2(archive_path, copied_archive)
        with zipfile.ZipFile(archive_path) as archive:
            members = safe_members(archive)
            archive.extractall(destination / f"LOD{level}", members=members)
        records.append({
            "level": level,
            "archive": archive_path.name,
            "sha256": sha256(archive_path),
            "fbx": inspection["fbx"],
            "textures": inspection["textures"],
        })

    for reference in sorted(source.iterdir()):
        if reference.is_file() and reference.suffix.lower() in {
            ".png", ".jpg", ".jpeg", ".webp"
        }:
            shutil.copy2(reference, destination / "Source" / reference.name)

    manifest = {
        "schema": "cityforge.building-lod-intake.v1",
        "buildingName": building_name,
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "sourceDirectory": str(source.resolve()),
        "sourceMutation": "none",
        "automaticMeshReduction": False,
        "lods": records,
        "progress": {
            "sourceIntake": "complete",
            "unityImport": "pending",
            "packagePrefab": "pending",
            "boundsValidation": "pending",
            "materialValidation": "pending",
            "transitionQa": "pending",
            "impostor": "pending",
            "normalLotQa": "pending",
            "performanceQa": "pending",
        },
    }
    apply_progress(manifest, progress_updates)
    manifest_path = destination / "building-lod-intake.json"
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return manifest


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path,
                        help="Directory containing Building-LOD0.zip through LOD3.zip")
    parser.add_argument("destination", type=Path,
                        help="Unity package destination directory")
    parser.add_argument("--force", action="store_true",
                        help="Update an existing package without deleting unrelated files")
    parser.add_argument("--inspect", action="store_true",
                        help="Validate and report source archives without writing")
    parser.add_argument("--progress", action="append", default=[], metavar="KEY=STATUS",
                        help="Set a progress checkpoint (repeatable)")
    args = parser.parse_args()
    try:
        name, archives = discover(args.source)
        if args.inspect:
            report = {f"LOD{level}": {
                "archive": path.name,
                "sha256": sha256(path),
                **inspect_archive(path),
            } for level, path in sorted(archives.items())}
            print(json.dumps({"buildingName": name, "lods": report}, indent=2))
            return 0
        manifest = stage(args.source, args.destination, force=args.force,
                         progress_updates=args.progress)
        print(json.dumps(manifest, indent=2))
        return 0
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        print(f"building_lod_intake: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
