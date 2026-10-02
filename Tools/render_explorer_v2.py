"""Render the actual second-generation explorer sources for art review in Blender.

Original Rivet Reach tooling, copyright (c) 2026 Starbugstone. See LICENSE.md.
blender --background --factory-startup --python <abs path> -- [--preview] [--preset N] [--out PREFIX]
       [--views front,back,quarter,face,hands,fp,waist,boot,...] [--use-cycles]

Reads ArtSource/Characters/V2/Explorer{Male,Female}.blend (or Logs/AvatarV2/*-preview.blend
with --preview). The review material reproduces the runtime layering: base map x region tint,
with the packed surface map and normal map when they exist.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
ARGS = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT_DIR = ROOT / 'Assets/RivetReach/Resources/Characters/V2'


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


PALETTE = json.loads((OUT_DIR / 'AppearancePalette.json').read_text())


def srgb_to_linear(c):
    return c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4


def hex_rgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def tint_image(preset):
    grid = PALETTE['grid']
    im = bpy.data.images.new('ReviewTint', grid, grid, alpha=False)
    im.colorspace_settings.name = 'sRGB'
    px = []
    for k, layer in enumerate(PALETTE['cells']):
        rgb = (1, 1, 1) if layer == 'fixed' else hex_rgb(preset[layer])
        px.extend([*rgb, 1])
    im.pixels[:] = px
    im.pack()
    return im


def review_material(preset, model_name):
    mat = bpy.data.materials.new('Explorer v2 review')
    mat.use_nodes = True
    # Match Unity: back faces are culled, so inverted parts show up in review.
    mat.use_backface_culling = True
    nt = mat.node_tree
    nodes, links = nt.nodes, nt.links
    bsdf = nodes['Principled BSDF']
    uv = nodes.new('ShaderNodeUVMap')
    uv.uv_map = 'SkinUV'
    tint = nodes.new('ShaderNodeTexImage')
    tint.image = tint_image(preset)
    tint.interpolation = 'Closest'
    tint.extension = 'EXTEND'
    links.new(uv.outputs['UV'], tint.inputs['Vector'])
    base_path = OUT_DIR / 'SkinBase.png'
    colour = tint.outputs['Color']
    if base_path.exists():
        base = nodes.new('ShaderNodeTexImage')
        base.image = bpy.data.images.load(str(base_path), check_existing=True)
        links.new(uv.outputs['UV'], base.inputs['Vector'])
        mix = nodes.new('ShaderNodeMix')
        mix.data_type = 'RGBA'
        mix.blend_type = 'MULTIPLY'
        mix.inputs['Factor'].default_value = 1
        links.new(base.outputs['Color'], mix.inputs['A'])
        links.new(tint.outputs['Color'], mix.inputs['B'])
        colour = mix.outputs['Result']
    surface_path = OUT_DIR / 'SkinSurface.png'
    if surface_path.exists():
        surf = nodes.new('ShaderNodeTexImage')
        surf.image = bpy.data.images.load(str(surface_path), check_existing=True)
        surf.image.colorspace_settings.name = 'Non-Color'
        links.new(uv.outputs['UV'], surf.inputs['Vector'])
        sep = nodes.new('ShaderNodeSeparateColor')
        links.new(surf.outputs['Color'], sep.inputs['Color'])
        links.new(sep.outputs['Red'], bsdf.inputs['Metallic'])
        links.new(sep.outputs['Green'], bsdf.inputs['Roughness'])
    else:
        bsdf.inputs['Roughness'].default_value = .7
    # Baked per-model occlusion lives in the 'Occlusion' colour attribute; the runtime
    # applies a partial cavity multiply plus full ambient occlusion.
    ao = nodes.new('ShaderNodeVertexColor')
    ao.layer_name = 'Occlusion'
    cav = nodes.new('ShaderNodeMix')
    cav.data_type = 'RGBA'
    cav.blend_type = 'MULTIPLY'
    cav.inputs['Factor'].default_value = .55
    links.new(colour, cav.inputs['A'])
    links.new(ao.outputs['Color'], cav.inputs['B'])
    colour = cav.outputs['Result']
    normal_path = OUT_DIR / 'SkinNormal.png'
    if normal_path.exists():
        nm = nodes.new('ShaderNodeTexImage')
        nm.image = bpy.data.images.load(str(normal_path), check_existing=True)
        nm.image.colorspace_settings.name = 'Non-Color'
        links.new(uv.outputs['UV'], nm.inputs['Vector'])
        nmap = nodes.new('ShaderNodeNormalMap')
        nmap.uv_map = 'SkinUV'
        links.new(nm.outputs['Color'], nmap.inputs['Color'])
        links.new(nmap.outputs['Normal'], bsdf.inputs['Normal'])
    links.new(colour, bsdf.inputs['Base Color'])
    return mat


def load(name, preview):
    path = (ROOT / 'Logs/AvatarV2' / (name + 'Body-preview.blend')) if preview else (ROOT / 'ArtSource/Characters/V2' / (name + '.blend'))
    with bpy.data.libraries.load(str(path), link=False) as (a, b):
        b.objects = a.objects
        b.actions = a.actions
    objs = [o for o in b.objects if o is not None]
    for o in objs:
        bpy.context.collection.objects.link(o)
    rig = next(o for o in objs if o.type == 'ARMATURE')
    body = next(o for o in objs if o.type == 'MESH')
    return rig, body


def use_action(rig, action):
    rig.animation_data.action = action
    if action is not None and hasattr(rig.animation_data, 'action_slot') and len(action.slots):
        rig.animation_data.action_slot = action.slots[0]


def aim(o, target):
    o.rotation_euler = (Vector(target) - o.location).to_track_quat('-Z', 'Y').to_euler()


def main():
    preview = '--preview' in ARGS
    preset = PALETTE['presets'][int(arg('--preset', '0'))]
    prefix = arg('--out', 'Logs/AvatarV2/v2')
    views = arg('--views', 'front,back,quarter,face,hands,fp').split(',')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    actors = {}
    for name, x in [('ExplorerMale', -.42), ('ExplorerFemale', .42)]:
        try:
            rig, body = load(name, preview)
        except (OSError, StopIteration):
            continue
        rig.location.x = x
        body.data.materials.clear()
        body.data.materials.append(review_material(preset, name))
        rig.animation_data_create()
        use_action(rig, bpy.data.actions.get(arg('--pose', 'Idle')))
        actors[name] = (rig, body)
    scene.frame_set(1)
    if '--debug' in ARGS:
        for name, (rig, body) in actors.items():
            m = body.data.materials[0]
            print('DBG', name, len(body.data.materials), [(l.from_node.name, l.to_socket.name) for l in m.node_tree.links])
            im = bpy.data.images['ReviewTint']
            print('DBG PX', list(im.pixels[:8]), body.data.uv_layers.active.name, len(body.data.polygons), set(p.material_index for p in body.data.polygons))
            print('DBG UV', [tuple(round(c, 3) for c in body.data.uv_layers.active.data[i].uv) for i in range(0, len(body.data.loops), 30000)])
    floor_mat = bpy.data.materials.new('Floor')
    floor_mat.diffuse_color = (.30, .32, .33, 1)
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, -.002))
    bpy.context.object.data.materials.append(floor_mat)
    world = bpy.data.worlds.new('Review sky')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs[0].default_value = (.55, .62, .72, 1)
    world.node_tree.nodes['Background'].inputs[1].default_value = .55
    scene.world = world
    for name, loc, power in [('Key', (-3, -4, 5), 650), ('Fill', (3.5, -2.5, 2.5), 220), ('Rim', (1.5, 4, 4), 520)]:
        light = bpy.data.lights.new(name, 'AREA')
        light.energy = power
        light.size = 3
        o = bpy.data.objects.new(name, light)
        scene.collection.objects.link(o)
        o.location = loc
        aim(o, (0, 0, 1))
    cam_data = bpy.data.cameras.new('Review')
    cam = bpy.data.objects.new('Review', cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    if '--use-cycles' in ARGS:
        scene.render.engine = 'CYCLES'
        scene.cycles.samples = 48
        scene.cycles.use_denoising = True
    else:
        scene.render.engine = 'BLENDER_EEVEE'
    scene.view_settings.view_transform = 'AgX'
    scene.render.image_settings.file_format = 'PNG'

    def shot(label, loc, target, ortho=None, lens=50, res=(1400, 1200), vfov=None):
        scene.render.resolution_x, scene.render.resolution_y = res
        cam.location = loc
        aim(cam, target)
        if ortho:
            cam_data.type = 'ORTHO'
            cam_data.ortho_scale = ortho
        else:
            cam_data.type = 'PERSP'
            cam_data.sensor_fit = 'AUTO'
            cam_data.lens = lens
            if vfov:
                cam_data.sensor_fit = 'VERTICAL'
                cam_data.angle_y = math.radians(vfov)
        scene.render.filepath = str(ROOT / (prefix + '-' + label + '.png'))
        bpy.ops.render.render(write_still=True)

    if 'front' in views:
        shot('front', (0, -6, 1.0), (0, 0, .95), ortho=2.25)
    if 'back' in views:
        shot('back', (0, 6, 1.0), (0, 0, .95), ortho=2.25)
    if 'quarter' in views:
        shot('quarter', (-3.2, -4.6, 1.6), (0, 0, .95), lens=62)
    if 'face' in views:
        for name, (rig, body) in actors.items():
            x = rig.location.x
            shot('face-' + name, (x + .32, -1.05, 1.68), (x, 0, 1.62), lens=85, res=(900, 900))
    for view, offset, target, lens in [('waist', (-.55, -.75, 1.05), (0, 0, 1.0), 60), ('waistside', (-.95, -.25, 1.05), (-.15, 0, 1.0), 60),
                                       ('boot', (-.55, -.85, .30), (-.14, -.05, .14), 55), ('bootside', (-.95, -.12, .22), (-.14, -.06, .13), 55), ('shoulder', (-.65, -.6, 1.52), (-.2, 0, 1.38), 60),
                                       ('backwaist', (.4, .9, 1.1), (0, 0, 1.0), 60), ('backhead', (.35, .95, 1.75), (0, 0, 1.66), 70),
                                       ('sidehead', (-1.0, -.15, 1.70), (0, 0, 1.66), 70)]:
        if view in views:
            for name, (rig, body) in actors.items():
                x = rig.location.x
                shot(view + '-' + name, (x + offset[0], offset[1], offset[2]), (x + target[0], target[1], target[2]), lens=lens, res=(900, 900))
    if 'hands' in views:
        for name, (rig, body) in actors.items():
            x = rig.location.x
            shot('hands-' + name, (x - .75, -.95, 1.02), (x - .30, 0, .98), lens=70, res=(900, 900))
    if 'fp' in views:
        # First-person review: show only arm-dominant geometry with a held-tool pose.
        for name, (rig, body) in actors.items():
            for other, (r2, b2) in actors.items():
                b2.hide_render = other != name
            groups = {g.index: g.name for g in body.vertex_groups}
            mask = body.modifiers.new('FP review mask', 'MASK')
            for clip, label in [('FP_HoldTwoHandTool', 'fp-pickaxe'), ('FP_RestTool', 'fp-rest'), ('FP_Idle', 'fp-empty'), ('FP_HoldBlock', 'fp-block')]:
                action = bpy.data.actions.get(clip)
                if action is None:
                    continue
                group = body.vertex_groups.new(name='FP ' + label)
                keep = []
                for v in body.data.vertices:
                    own = [g for g in v.groups if g.group in groups]
                    if not own:
                        continue
                    dom = groups[max(own, key=lambda g: g.weight).group]
                    if any(k in dom for k in ('Arm', 'Forearm', 'Hand', 'Finger', 'Thumb')) and (dom.endswith('R') or 'TwoHand' in clip):
                        keep.append(v.index)
                group.add(keep, 1, 'REPLACE')
                mask.vertex_group = group.name
                use_action(rig, action)
                scene.frame_set(1)
                x = rig.location.x
                shot(label + '-' + name, (x, -.02, 1.50), (x, -1, 1.50), res=(1280, 720), vfov=78)
            body.modifiers.remove(mask)
            use_action(rig, bpy.data.actions.get('Idle'))
            scene.frame_set(1)
        for name, (rig, body) in actors.items():
            body.hide_render = False


main()
