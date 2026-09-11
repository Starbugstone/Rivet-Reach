"""Original Azure ore: fractured stone with branching crystalline seams.

Run in Blender --background --python. Owns only AzureOre.blend, azure_ore.fbx,
icon 120 and the terrain face render. Uses the existing workshop atlas/material.
"""
import bpy
import json
import math
import random
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/RivetReach/Resources/Industry'
SOURCE = ROOT / 'ArtSource/Industry'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
mat = bpy.data.materials.new('WorkshopAtlas')
mat.use_nodes = True
bs = mat.node_tree.nodes.get('Principled BSDF')
bs.inputs['Metallic'].default_value = .25
bs.inputs['Roughness'].default_value = .65
for filename, socket in [('Atlas.png', 'Base Color'), ('Emission.png', 'Emission Color')]:
    node = mat.node_tree.nodes.new('ShaderNodeTexImage')
    node.image = bpy.data.images.load(str(OUT / filename))
    mat.node_tree.links.new(node.outputs['Color'], bs.inputs[socket])
bs.inputs['Emission Strength'].default_value = .6
vertices, faces, colours = [], [], []


def polygon(points, colour):
    start = len(vertices)
    vertices.extend(points)
    faces.append(tuple(range(start, len(vertices))))
    colours.append(colour)


def clip(poly, normal, limit):
    result = []
    for a, b in zip(poly, poly[1:] + poly[:1]):
        da, db = a.dot(normal) - limit, b.dot(normal) - limit
        if da <= 0:
            result.append(a)
        if (da <= 0) != (db <= 0):
            result.append(a + (b - a) * (da / (da - db)))
    return result


# Local face axes have outward winding; all surface relief stays within one cell.
for face_index, (normal, u, v) in enumerate([
    ((0, 0, 1), (1, 0, 0), (0, 1, 0)),
    ((0, 0, -1), (1, 0, 0), (0, -1, 0)),
    ((1, 0, 0), (0, 1, 0), (0, 0, 1)),
    ((-1, 0, 0), (0, -1, 0), (0, 0, 1)),
    ((0, 1, 0), (-1, 0, 0), (0, 0, 1)),
    ((0, -1, 0), (1, 0, 0), (0, 0, 1)),
]):
    normal, u, v = Vector(normal), Vector(u), Vector(v)
    rng = random.Random(12090 + face_index)
    seeds = []
    for _ in range(300):
        candidate = Vector((rng.random(), rng.random()))
        if all((candidate - s).length > .13 for s in seeds):
            seeds.append(candidate)
        if len(seeds) == 24:
            break

    def point(p, height):
        return Vector((.5, .5, .5)) + normal * height + u * ((p.x - .5) * .94) + v * ((p.y - .5) * .94)

    # Closed inner rock prevents see-through corners between the six relief surfaces.
    polygon([Vector((.5, .5, .5)) + normal * .452 + u * a * .452 + v * b * .452
             for a, b in [(-1, -1), (1, -1), (1, 1), (-1, 1)]], 0)

    for seed in seeds:
        cell = [Vector((0, 0)), Vector((1, 0)), Vector((1, 1)), Vector((0, 1))]
        for other in seeds:
            if other != seed:
                delta = other - seed
                cell = clip(cell, delta, (other.length_squared - seed.length_squared) / 2)
        centre = sum(cell, Vector((0, 0))) / len(cell)
        # Blue mineral under each fracture, with a narrow luminous ridge in its centre.
        polygon([point(p, .455) for p in cell], 0)
        inset = [centre + (p - centre) * rng.uniform(.86, .94) for p in cell]
        crown = [centre + (p - centre) * rng.uniform(.65, .82) for p in cell]
        height = rng.uniform(.473, .493)
        stone = rng.choice([0, 1, 12, 12])
        pocket = rng.random() < .14
        for i, a in enumerate(cell):
            j = (i + 1) % len(cell)
            b = cell[j]
            # Uneven cyan ridges break up the broad blue channels into crystal facets.
            mid = (a + b) / 2
            ridge = mid + (centre - mid) * rng.uniform(.04, .20)
            if rng.random() < .73:
                inner_a = centre + (a - centre) * .83
                inner_b = centre + (b - centre) * .83
                polygon([point(a, .456), point(b, .456), point(inner_b, .456), point(inner_a, .456)], 5)
                polygon([point(a, .457), point(b, .457), point(ridge, .465)], rng.choice([5, 6, 6]))
            if pocket:
                polygon([point(inset[i], .46), point(inset[j], .46), point(centre, .49)], rng.choice([5, 6]))
                continue
            polygon([point(inset[i], .457), point(inset[j], .457), point(crown[j], height), point(crown[i], height)], 0)
            polygon([point(crown[i], height), point(crown[j], height), point(centre, height + .006)], stone if i % 3 else 1)

mesh = bpy.data.meshes.new('Fractured Azure ore')
mesh.from_pydata(vertices, [], faces)
mesh.materials.append(mat)
uv = mesh.uv_layers.new(name='UVMap')
for poly, colour in zip(mesh.polygons, colours):
    for li in poly.loop_indices:
        co = mesh.vertices[mesh.loops[li].vertex_index].co
        uv.data[li].uv = ((colour % 4 + .15 + (co.x * 3 + co.y * 2) % 1 * .7) / 4,
                          (colour // 4 + .15 + (co.z * 3 + co.y) % 1 * .7) / 4)
ore = bpy.data.objects.new('Body', mesh)
scene.collection.objects.link(ore)
root = bpy.data.objects.new('azure_ore', None)
scene.collection.objects.link(root)
ore.parent = root
ore.select_set(True)
root.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.fbx(filepath=str(OUT / 'azure_ore.fbx'), use_selection=True,
    object_types={'MESH', 'EMPTY'}, apply_unit_scale=True, axis_forward='-Z', axis_up='Y',
    add_leaf_bones=False, bake_anim=False)

scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.cycles.use_denoising = True
scene.world.color = (.12, .12, .12)
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = True
scene.view_settings.view_transform = 'AgX'
bpy.ops.object.camera_add(location=(2, 2.5, 1.8))
camera = bpy.context.object
scene.camera = camera
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 1.6
camera.rotation_euler = (Vector((.5, .5, .5)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
for pos, power in [((1, 3, 4), 250), ((-3, 1, 2), 160), ((1, -3, 3), 300)]:
    bpy.ops.object.light_add(type='AREA', location=pos)
    lamp = bpy.context.object
    lamp.data.energy = power
    lamp.data.shape = 'DISK'
    lamp.data.size = 3
    lamp.rotation_euler = (Vector((.5, .5, .5)) - lamp.location).to_track_quat('-Z', 'Y').to_euler()
scene.render.resolution_percentage = 100
scene.render.resolution_x = scene.render.resolution_y = 256
scene.render.filepath = str(OUT / 'Icons/120.png')
bpy.ops.render.render(write_still=True)
scene.render.resolution_x = scene.render.resolution_y = 1000
scene.render.filepath = str(SOURCE / 'azure-ore-review.png')
bpy.ops.render.render(write_still=True)
# Orthographic face derived from the same actual mesh for the chunk-meshed ore.
camera.location = (.5, .5, 3)
camera.rotation_euler = (0, 0, 0)
camera.data.ortho_scale = .938
scene.render.resolution_x = scene.render.resolution_y = 64
scene.render.filepath = str(OUT / 'AzureOreTile.png')
bpy.ops.render.render(write_still=True)
camera.location = (2, 2.5, 1.8)
camera.rotation_euler = (Vector((.5, .5, .5)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.ortho_scale = 1.6
scene.render.resolution_x = scene.render.resolution_y = 1000
for im in bpy.data.images:
    if im.filepath:
        im.filepath = bpy.path.relpath(im.filepath, start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'AzureOre.blend'))
report = dict(triangles=sum(len(p.vertices) - 2 for p in mesh.polygons), meshParts=1,
              materials=1, boundsMin=[min(v[i] for v in vertices) for i in range(3)],
              boundsMax=[max(v[i] for v in vertices) for i in range(3)])
(SOURCE / 'azure-ore-geometry.json').write_text(json.dumps(report, indent=2) + '\n')
print('AZURE_ORE_COMPLETE', json.dumps(report))
