#!/bin/zsh
set -euo pipefail

PROJECT_DIR="${0:A:h:h}"
TRACK_DIR="$PROJECT_DIR/Assets/CityForgeV3/Resources/CityForgeV3/Railroad/StreetcarTrackV01"
# Two embedded streetcar rails only: no sleepers, ballast, or surrounding fill.
# Their 104 px outside envelope is about 12.1% of the production road width,
# comfortably below the requested one-fifth maximum.

magick -size 1024x1024 xc:none \
  -fill none -stroke '#090b0d' -strokewidth 14 -draw 'line 467,0 467,1024' -draw 'line 557,0 557,1024' \
  -stroke '#30353a' -strokewidth 8 -draw 'line 467,0 467,1024' -draw 'line 557,0 557,1024' \
  -stroke '#5b6268' -strokewidth 2 -draw 'line 464,0 464,1024' -draw 'line 554,0 554,1024' \
  "PNG32:$TRACK_DIR/straight.png"

magick -size 1024x1024 xc:none \
  -fill none -stroke '#090b0d' -strokewidth 14 -draw 'arc 467,467 1581,1581 180,270' -draw 'arc 557,557 1491,1491 180,270' \
  -stroke '#30353a' -strokewidth 8 -draw 'arc 467,467 1581,1581 180,270' -draw 'arc 557,557 1491,1491 180,270' \
  -stroke '#5b6268' -strokewidth 2 -draw 'arc 464,464 1584,1584 180,270' -draw 'arc 554,554 1494,1494 180,270' \
  "PNG32:$TRACK_DIR/curve.png"
