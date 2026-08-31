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

# T junction: preserve the exact straight-piece width and form a clean union
# of the east/west through-road with the south branch. This replaces the old
# narrow substituted cobble artwork without scaling or resampling the approved
# classic brick field.
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 990 \
  -draw "path 'M -10,627 L 1264,627 M 627,627 L 627,1264'" \
  "$LAYER_DIR/t-junction-interior-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 1050 \
  -draw "path 'M -10,627 L 1264,627 M 627,627 L 627,1264'" \
  "$LAYER_DIR/t-junction-outer-mask.png"

magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 990 \
  -draw "path 'M 627,1264 L 627,1254 C 627,907.7 346.3,627 0,627 L -10,627'" \
  "$LAYER_DIR/corner-interior-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 1050 \
  -draw "path 'M 627,1264 L 627,1254 C 627,907.7 346.3,627 0,627 L -10,627'" \
  "$LAYER_DIR/corner-outer-mask.png"

# The primary 45-degree cell is the exact intersection of a constant-width
# diagonal road band with a grid square. It crosses the complete square from
# corner to corner; it is not a west-to-south arrow or a curved turn.
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 990 \
  -draw "path 'M -10,1264 L 1264,-10'" \
  "$LAYER_DIR/diagonal-interior-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 1050 \
  -draw "path 'M -10,1264 L 1264,-10'" \
  "$LAYER_DIR/diagonal-outer-mask.png"

# The complementary staircase cell is the neighboring square's intersection
# with that same infinite band. For the base northeast/southwest diagonal it
# is the exact top-left corner triangle. Use polygons rather than a clipped
# stroke so the mask includes both shared tile edges, not merely the tip.
magick -size ${SIZE}x${SIZE} xc:black -fill white -stroke none \
  -draw "polygon 0,0 700,0 0,700" \
  "$LAYER_DIR/straight-to-diagonal-interior-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill white -stroke none \
  -draw "polygon 0,0 742,0 0,742" \
  "$LAYER_DIR/straight-to-diagonal-outer-mask.png"

# Handed endpoint transitions. The base right-hand piece enters from the south
# at the exact straight-road centerline and eases into the northeast diagonal.
# The left-hand piece is its geometric mirror (south to northwest). Rotations
# cover all eight cardinal-to-diagonal approach combinations without flipping
# the approved brick texture at runtime.
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 990 \
  -draw "path 'M 627,1264 L 627,1000 C 627,750 723,531 900,354 L 1264,-10'" \
  "$LAYER_DIR/diagonal-transition-right-interior-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 1050 \
  -draw "path 'M 627,1264 L 627,1000 C 627,750 723,531 900,354 L 1264,-10'" \
  "$LAYER_DIR/diagonal-transition-right-outer-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 990 \
  -draw "path 'M 627,1264 L 627,1000 C 627,750 531,531 354,354 L -10,-10'" \
  "$LAYER_DIR/diagonal-transition-left-interior-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 1050 \
  -draw "path 'M 627,1264 L 627,1000 C 627,750 531,531 354,354 L -10,-10'" \
  "$LAYER_DIR/diagonal-transition-left-outer-mask.png"

# Through-road diagonal junctions. These retain the complete north/south road
# while a handed branch peels toward a diagonal corner. They are used only
# when an S-planned diagonal begins directly on an existing straight tile.
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 990 \
  -draw "path 'M 627,-10 L 627,1264 M 627,627 C 735,560 815,455 930,324 L 1264,-10'" \
  "$LAYER_DIR/diagonal-t-junction-right-interior-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 1050 \
  -draw "path 'M 627,-10 L 627,1264 M 627,627 C 735,560 815,455 930,324 L 1264,-10'" \
  "$LAYER_DIR/diagonal-t-junction-right-outer-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 990 \
  -draw "path 'M 627,-10 L 627,1264 M 627,627 C 519,560 439,455 324,324 L -10,-10'" \
  "$LAYER_DIR/diagonal-t-junction-left-interior-mask.png"
magick -size ${SIZE}x${SIZE} xc:black -fill none -stroke white -strokewidth 1050 \
  -draw "path 'M 627,-10 L 627,1264 M 627,627 C 519,560 439,455 324,324 L -10,-10'" \
  "$LAYER_DIR/diagonal-t-junction-left-outer-mask.png"

# Curb masks are the outer silhouette minus the road interior.
magick "$LAYER_DIR/straight-outer-mask.png" "$LAYER_DIR/straight-interior-mask.png" \
  -compose MinusSrc -composite "$LAYER_DIR/straight-curb-mask.png"
magick "$LAYER_DIR/t-junction-outer-mask.png" "$LAYER_DIR/t-junction-interior-mask.png" \
  -compose MinusSrc -composite "$LAYER_DIR/t-junction-curb-mask.png"
magick "$LAYER_DIR/corner-outer-mask.png" "$LAYER_DIR/corner-interior-mask.png" \
  -compose MinusSrc -composite "$LAYER_DIR/corner-curb-mask.png"
magick "$LAYER_DIR/diagonal-outer-mask.png" "$LAYER_DIR/diagonal-interior-mask.png" \
  -compose MinusSrc -composite "$LAYER_DIR/diagonal-curb-mask.png"
magick "$LAYER_DIR/straight-to-diagonal-outer-mask.png" \
  "$LAYER_DIR/straight-to-diagonal-interior-mask.png" \
  -compose MinusSrc -composite "$LAYER_DIR/straight-to-diagonal-curb-mask.png"
for HAND in right left; do
  magick "$LAYER_DIR/diagonal-transition-${HAND}-outer-mask.png" \
    "$LAYER_DIR/diagonal-transition-${HAND}-interior-mask.png" \
    -compose MinusSrc -composite \
    "$LAYER_DIR/diagonal-transition-${HAND}-curb-mask.png"
done
for HAND in right left; do
  magick "$LAYER_DIR/diagonal-t-junction-${HAND}-outer-mask.png" \
    "$LAYER_DIR/diagonal-t-junction-${HAND}-interior-mask.png" \
    -compose MinusSrc -composite \
    "$LAYER_DIR/diagonal-t-junction-${HAND}-curb-mask.png"
done

# Build reusable surface fields. Replacing brick-fill.png is enough to reskin the road.
magick -size ${SIZE}x${SIZE} tile:"$LAYER_DIR/brick-fill.png" "$LAYER_DIR/brick-field.png"
magick -seed 20260830 -size ${SIZE}x${SIZE} xc:'#b8ada4' -attenuate 0.10 +noise Gaussian \
  "$LAYER_DIR/curb-field.png"

for PIECE in straight corner t-junction diagonal straight-to-diagonal \
  diagonal-transition-right diagonal-transition-left \
  diagonal-t-junction-right diagonal-t-junction-left; do
  magick "$LAYER_DIR/brick-field.png" "$LAYER_DIR/${PIECE}-interior-mask.png" \
    -alpha off -compose CopyOpacity -composite "$LAYER_DIR/${PIECE}-interior.png"
  magick "$LAYER_DIR/curb-field.png" "$LAYER_DIR/${PIECE}-curb-mask.png" \
    -alpha off -compose CopyOpacity -composite "$LAYER_DIR/${PIECE}-curb-cutout.png"
  magick "$LAYER_DIR/${PIECE}-interior.png" "$LAYER_DIR/${PIECE}-curb-cutout.png" \
    -compose Over -composite "$ROAD_DIR/${PIECE}.png"
done
