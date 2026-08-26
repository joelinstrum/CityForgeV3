#!/bin/zsh
set -euo pipefail

PROJECT_DIR="${0:A:h:h}"
TRACK_DIR="$PROJECT_DIR/Assets/CityForgeV3/Resources/CityForgeV3/Railroad/StreetcarTrackV01"
SOURCE="$TRACK_DIR/Source/curve-authored-offset.png"
OUTPUT="$TRACK_DIR/curve.png"

# The supplied curve crosses its east edge around pixel 648 and its south edge
# around pixel 629, while the production straight crosses at pixel 513. Move
# the authored arc outward around the southeast curve center, interpolating the
# correction from 135 px at east to 115.5 px at south. This keeps its gauge and
# material width intact while registering both ports to the straight artwork.
magick "$SOURCE" -channel RGBA \
  -fx 'dx=abs(i-1024); dy=abs(j-1024); rr=hypot(dx,dy); dd=115.5+19.5*dy/(dx+dy+0.0001); rr==0 ? u.p{i,j} : u.p{1024+(i-1024)*(rr-dd)/rr,1024+(j-1024)*(rr-dd)/rr}' \
  "$OUTPUT"
