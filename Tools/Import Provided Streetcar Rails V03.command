#!/bin/zsh
set -euo pipefail

PROJECT_DIR="${0:A:h:h}"
TRACK_DIR="$PROJECT_DIR/Assets/CityForgeV3/Resources/CityForgeV3/Railroad/StreetcarTrackV01"
SOURCE_DIR="$TRACK_DIR/Source/ProvidedV03"
TEMP_DIR=$(mktemp -d)
trap 'rm -rf "$TEMP_DIR"' EXIT

# The provided art uses opaque black outside its painted brick-and-rail strip.
# Key only near-black pixels so the supplied shading and metal detail survive.
magick "$SOURCE_DIR/streetcar-rail-straight.png" -alpha on -fuzz 4% -transparent black \
  "$TRACK_DIR/straight.png"
magick "$SOURCE_DIR/streetcar-rail-curve.png" -alpha on -fuzz 4% -transparent black \
  "$TEMP_DIR/curve-alpha.png"

# Register the asymmetrical supplied curve to the centered straight. The source
# crosses south near radius 809 and east near radius 866; production ports use
# radius 627. The radial scale also matches the straight artwork's port width.
magick "$TEMP_DIR/curve-alpha.png" -channel RGBA \
  -fx 'dx=abs(i-1254); dy=abs(j-1254); rr=hypot(dx,dy); dd=182+57*dy/(dx+dy+0.0001); rs=627+dd+0.853*(rr-627); rr==0 ? u.p{i,j} : u.p{1254+(i-1254)*rs/rr,1254+(j-1254)*rs/rr}' \
  "$TRACK_DIR/curve.png"
