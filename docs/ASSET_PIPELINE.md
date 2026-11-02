# Asset pipeline

## Asset classes
Terrain/heightmaps; roads/bridges/tunnels/barriers; buildings/interiors; props/signage/lights; NPC portraits/appearance; phone UI; ambient audio.

## Source of truth
- Blender projects: assets/source/blender/
- GLB/GLTF exports: assets/game/
- Textures: assets/source/textures/
- UI: assets/ui/
- Metadata: assets/manifests/

## Asset IDs
SIME_<PROVINCE>_<CATEGORY>_<NAME>_<VARIANT>

Never reuse IDs. Record source, creator, license, attribution, dimensions, polygon count, texture sizes and target location.

## Pipeline
Survey coordinates -> greybox -> validate world bounds/colliders/navigation -> Blender final mesh -> GLB/GLTF export -> S1MAPI load -> material/collider validation -> performance capture -> stable-ID manifest.

## Licensing
Do not copy game assets into the repository. Do not redistribute third-party mod assets unless license permits it. Keep unlicensed/proprietary assets out of public commits.
