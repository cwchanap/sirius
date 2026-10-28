"""Halve a generated GLB's oversized textures before tools/import_character.py, keeping JPEG as JPEG and PNG as PNG.

/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/shrink_glb_textures.py -- IN.glb OUT.glb [MAX_SIZE]

Tripo bakes 4096 px maps; a prop never fills enough of the screen to use them, and every map lands in the repository three
times (the GLB, assets/art/NAME-import/ and Godot's extracted copy). Geometry, UVs, normals and materials pass through.
The import presets (import_tripo_patrol.py, import_tripo_drone.py) call shrink() on the scene they have loaded.
"""
import os
import sys
import tempfile

import bpy


def shrink(limit=2048):
    stage = tempfile.mkdtemp()
    for image in list(bpy.data.images):
        if max(image.size) <= limit:
            continue
        jpeg = bytes(image.packed_file.data[:2]) == b'\xff\xd8'
        width, height = (max(1, round(s * limit / max(image.size))) for s in image.size)
        image.scale(width, height)
        image.file_format = 'JPEG' if jpeg else 'PNG'
        path = os.path.join(stage, image.name + ('.jpg' if jpeg else '.png'))
        # Image.save writes the stored pixels; save_render would push the normal and ORM data through the view transform.
        image.save(filepath=path, quality=92)
        # Loading the written file, rather than repacking the scaled pixels, is what keeps the original encoding.
        small = bpy.data.images.load(path)
        small.name, small.colorspace_settings.name = image.name, image.colorspace_settings.name
        small.pack()
        image.user_remap(small)
        print(f'SHRINK {image.name} -> {width}x{height} {"JPEG" if jpeg else "PNG"}')


if __name__ == '__main__':
    source, target, *rest = sys.argv[sys.argv.index('--') + 1:]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.abspath(source))
    shrink(int(rest[0]) if rest else 2048)
    bpy.ops.export_scene.gltf(filepath=os.path.abspath(target), export_format='GLB', export_image_format='AUTO')
    print('SHRINK_OK')
