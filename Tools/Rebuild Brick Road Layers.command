#!/bin/zsh
set -euo pipefail

PROJECT_DIR="${0:A:h:h}"
ROAD_DIR="$PROJECT_DIR/Assets/CityForgeV3/Resources/CityForgeV3/Roads/BrickRoadV1"
LAYER_DIR="$ROAD_DIR/Source/Layered"
SIZE=1254
CENTER=627

mkdir -p "$LAYER_DIR"

# Canonical masks. Both ports terminate at the exact center of a tile edge.
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 990 \
  -draw "path 'M 627,-10 L 627,1264'" "$LAYER_DIR/straight-interior-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 1050 \
  -draw "path 'M 627,-10 L 627,1264'" "$LAYER_DIR/straight-outer-mask.png"

magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 990 \
  -draw "path 'M 627,1264 L 627,1254 C 627,907.7 346.3,627 0,627 L -10,627'" \
  "$LAYER_DIR/corner-interior-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 1050 \
  -draw "path 'M 627,1264 L 627,1254 C 627,907.7 346.3,627 0,627 L -10,627'" \
  "$LAYER_DIR/corner-outer-mask.png"

# Curb masks are the outer silhouette minus the road interior.
magick "$LAYER_DIR/straight-outer-mask.png" "$LAYER_DIR/straight-interior-mask.png" \
  -compose MinusSrc -composite "$LAYER_DIR/straight-curb-mask.png"
magick "$LAYER_DIR/corner-outer-mask.png" "$LAYER_DIR/corner-interior-mask.png" \
  -compose MinusSrc -composite "$LAYER_DIR/corner-curb-mask.png"

# Build reusable surface fields. Replacing brick-fill.png is enough to reskin the road.
magick -size ${SIZE}x${SIZE} tile:"$LAYER_DIR/brick-fill.png" "$LAYER_DIR/brick-field.png"
magick -size ${SIZE}x${SIZE} xc:'#b8ada4' -attenuate 0.10 +noise Gaussian \
  "$LAYER_DIR/curb-field.png"

for PIECE in straight corner; do
  magick "$LAYER_DIR/brick-field.png" "$LAYER_DIR/${PIECE}-interior-mask.png" \
    -alpha off -compose CopyOpacity -composite "$LAYER_DIR/${PIECE}-interior.png"
  magick "$LAYER_DIR/curb-field.png" "$LAYER_DIR/${PIECE}-curb-mask.png" \
    -alpha off -compose CopyOpacity -composite "$LAYER_DIR/${PIECE}-curb-cutout.png"
  magick "$LAYER_DIR/${PIECE}-interior.png" "$LAYER_DIR/${PIECE}-curb-cutout.png" \
    -compose Over -composite "$ROAD_DIR/${PIECE}.png"
done
