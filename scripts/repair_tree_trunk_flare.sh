#!/usr/bin/env bash
set -euo pipefail

repair_flare() {
  local target="$1"
  local source_x="$2"
  local source_width="$3"
  local flare_x="$4"
  local flare_width="$5"
  local work_dir="$6"

  magick "$target" \
    -crop "310x366+0+0" +repage \
    -gravity north -background none -extent 310x376 \
    "$work_dir/lifted.png"

  magick "$target" \
    -crop "${source_width}x5+${source_x}+360" +repage \
    -resize "${flare_width}x12!" \
    "$work_dir/flare-texture.png"

  local shoulder=$((flare_width * 28 / 100))
  local far_shoulder=$((flare_width - shoulder))
  local midpoint=$((flare_width / 2))
  local right=$((flare_width - 1))
  magick -size "${flare_width}x12" xc:none -fill white \
    -draw "path 'M ${shoulder},0 C ${shoulder},3 3,6 0,8 C 3,9 6,11 $((midpoint - 5)),9 C $((midpoint - 2)),8 $((midpoint - 2)),11 ${midpoint},11 C $((midpoint + 2)),11 $((midpoint + 2)),8 $((midpoint + 5)),9 C $((right - 6)),11 $((right - 3)),9 ${right},8 C $((right - 3)),6 ${far_shoulder},3 ${far_shoulder},0 Z'" \
    "$work_dir/flare-mask.png"

  magick "$work_dir/flare-texture.png" "$work_dir/flare-mask.png" \
    -alpha off -compose CopyOpacity -composite \
    "$work_dir/flare.png"

  magick "$work_dir/lifted.png" "$work_dir/flare.png" \
    -geometry "+${flare_x}+364" -compose Over -composite \
    "$target"
}

if [[ $# -ne 5 ]]; then
  echo "usage: $0 TARGET SOURCE_X SOURCE_WIDTH FLARE_X FLARE_WIDTH" >&2
  exit 2
fi

task_work_dir="$(mktemp -d)"
trap 'rm -rf "$task_work_dir"' EXIT
repair_flare "$1" "$2" "$3" "$4" "$5" "$task_work_dir"
