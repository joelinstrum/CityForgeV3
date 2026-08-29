#!/usr/bin/env python3
"""Stage City Forge 3D-building LOD archives into a Unity package root."""

from __future__ import annotations

import argparse
import json
import shutil
import zipfile
from pathlib import Path


def safe_members(archive: zipfile.ZipFile):
    for info in archive.infolist():
        path = Path(info.filename)
        if info.is_dir() or "__MACOSX" in path.parts or path.name.startswith("._"):
            continue
        if path.is_absolute() or ".." in path.parts:
            raise ValueError(f"Unsafe archive member: {info.filename}")
        yield info


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--stem", required=True,
                        help="Archive prefix, for example PlymouthStore")
    parser.add_argument("--reference", type=Path)
    parser.add_argument("--billboard-archive", type=Path)
    parser.add_argument("--lighting-source", type=Path)
    parser.add_argument("--authoring-destination", type=Path,
                        help="Required when preserving a Blender source file")
    args = parser.parse_args()

    args.destination.mkdir(parents=True, exist_ok=True)
    source_dir = args.destination / "Source"
    source_dir.mkdir(exist_ok=True)
    lod_records = []

    for level in range(4):
        archive_path = args.source / f"{args.stem}-LOD{level}.zip"
        if not archive_path.is_file():
            raise FileNotFoundError(archive_path)
        lod_dir = args.destination / f"LOD{level}"
        lod_dir.mkdir(exist_ok=True)
        with zipfile.ZipFile(archive_path) as archive:
            members = list(safe_members(archive))
            archive.extractall(lod_dir, members)
        shutil.copy2(archive_path, source_dir / archive_path.name)
        fbx_files = sorted(lod_dir.rglob("*.fbx"))
        if len(fbx_files) != 1:
            raise RuntimeError(
                f"LOD{level} must contain exactly one FBX; found {len(fbx_files)}")
        lod_records.append({
            "level": level,
            "archive": archive_path.name,
            "model": fbx_files[0].relative_to(args.destination).as_posix(),
        })

    optional_sources = []
    for path in (args.reference,):
        if path:
            if not path.is_file():
                raise FileNotFoundError(path)
            shutil.copy2(path, source_dir / path.name)
            optional_sources.append(path.name)

    if args.lighting_source:
        if not args.authoring_destination:
            raise ValueError(
                "--authoring-destination is required with --lighting-source")
        if not args.lighting_source.is_file():
            raise FileNotFoundError(args.lighting_source)
        args.authoring_destination.mkdir(parents=True, exist_ok=True)
        shutil.copy2(args.lighting_source,
                     args.authoring_destination / args.lighting_source.name)
        optional_sources.append(args.lighting_source.name)

    if args.billboard_archive:
        if not args.billboard_archive.is_file():
            raise FileNotFoundError(args.billboard_archive)
        shutil.copy2(args.billboard_archive,
                     source_dir / args.billboard_archive.name)
        billboard_dir = args.destination / "Impostor" / "Night"
        billboard_dir.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(args.billboard_archive) as archive:
            archive.extractall(billboard_dir, list(safe_members(archive)))
        optional_sources.append(args.billboard_archive.name)

    manifest = {
        "schema": "cityforge.building-lod-intake.v1",
        "asset": args.stem,
        "sourceDirectory": str(args.source),
        "lods": lod_records,
        "preservedOptionalSources": optional_sources,
    }
    (args.destination / "building-lod-intake.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
