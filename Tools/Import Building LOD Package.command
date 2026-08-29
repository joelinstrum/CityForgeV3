#!/bin/zsh
set -euo pipefail

project_dir="${0:A:h:h}"
if (( $# < 2 )); then
  echo "Usage: ${0:t} /path/to/source /path/to/UnityPackageDestination [options]" >&2
  exit 2
fi

source_dir="$1"
destination_dir="$2"
shift 2
exec /usr/bin/env python3 "$project_dir/scripts/building_lod_intake.py" \
  "$source_dir" "$destination_dir" "$@"
