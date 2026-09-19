"""Original cage spawner. Copyright 2026 Starbugstone; all rights reserved.
Blender --background --python Tools/create_spawner_assets.py [-- --output-root PATH].
The optional root supports asset review without importing into an occupied Editor.
"""
import bpy, bmesh, json, math, sys
from pathlib import Path
from mathutils import Vector

PROJECT=Path(__file__).resolve().parents[1]
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
ROOT=Path(args[args.index('--output-root')+1]) if '--output-root' in args else PROJECT
OUT=ROOT/'Assets/RivetReach/Resources/Spawners'
SOURCE=ROOT/'ArtSource/Spawners'
REVIEW=ROOT/'.docs/verification/alpha-playtest-2026-09-19'
for directory in (OUT,SOURCE,REVIEW):directory.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
palette=[(.075,.095,.105,1),(.19,.23,.25,1),(.35,.21,.115,1),(.56,.36,.17,1),
         (.11,.14,.15,1),(.28,.31,.30,1),(.13,.19,.22,1),(.49,.58,.56,1)]
image=bpy.data.images.new('Spawner palette',width=4,height=2)
image.pixels=[component for color in palette for component in color]
image.filepath_raw=str(OUT/'Palette.png');image.file_format='PNG';image.save()
material=bpy.data.materials.new('SpawnerPalette');material.use_nodes=True
bsdf=material.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Metallic'].default_value=.65;bsdf.inputs['Roughness'].default_value=.6
texture=material.node_tree.nodes.new('ShaderNodeTexImage');texture.image=image;texture.interpolation='Closest'
material.node_tree.links.new(texture.outputs['Color'],bsdf.inputs['Base Color'])
parts=[]
def finish(obj,color):
    obj.data.materials.clear();obj.data.materials.append(material)
    uv=obj.data.uv_layers.active or obj.data.uv_layers.new(name='PaletteUV');uv.name='PaletteUV'
    for loop in uv.data:loop.uv=((color%4+.5)/4,(color//4+.5)/2)
    parts.append(obj);return obj
def beam(name,center,size,color,bevel=.008):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center);obj=bpy.context.object;obj.name=name;obj.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=obj.modifiers.new('Forged edges','BEVEL');mod.width=bevel;mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj,color)
def rivet(center,axis):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8,ring_count=4,radius=1,location=center)
    obj=bpy.context.object;obj.name='Square-headed bronze fastening';obj.scale=(.018,.018,.018)
    obj.scale[axis]=.01;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(obj,3)
# Open floor/roof frames, visible corner shoes and slender wrought cage bars.
for z in (.065,.895):
    for side in (-1,1):
        beam('Framed perimeter',(0,side*.425,z),(.94,.09,.09),1)
        beam('Framed perimeter',(side*.425,0,z),(.09,.78,.09),1)
for x in (-.425,.425):
    for y in (-.425,.425):
        beam('Corner post',(x,y,.48),(.075,.075,.78),0)
        beam('Riveted corner shoe',(x,y,.115),(.105,.105,.16),2)
        beam('Corner crown',(x,y,.91),(.10,.10,.11),2)
        for z in (.085,.15,.88,.94):
            rivet((x+math.copysign(.055,x),y,z),0)
            rivet((x,y+math.copysign(.055,y),z),1)
for side in (-1,1):
    for a in (-.22,.22):
        beam('Cage vertical',(a,side*.425,.48),(.034,.038,.75),0,.003)
        beam('Cage vertical',(side*.425,a,.48),(.038,.034,.75),0,.003)
    # Central iron belt keeps the broad opening readable at normal distance.
    beam('Mid-height belt',(0,side*.425,.36),(.80,.035,.035),2,.003)
    beam('Mid-height belt',(side*.425,0,.36),(.035,.80,.035),2,.003)
for a in (-.22,0,.22):
    beam('Roof grille',(a,0,.895),(.032,.80,.025),0,.002)
    beam('Base grille',(0,a,.068),(.80,.032,.025),0,.002)
beam('Central pedestal',(0,0,.10),(.29,.29,.09),1)
beam('Mineral cradle',(0,0,.15),(.20,.20,.04),3)
# Four small inset blue-gray plates identify the cage without an emissive light.
for side in (-1,1):
    beam('Identification plate',(0,side*.448,.79),(.14,.012,.09),6,.004)
    beam('Identification plate',(side*.448,0,.79),(.012,.14,.09),6,.004)
bpy.ops.object.select_all(action='DESELECT')
for part in parts:part.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();cage=bpy.context.object;cage.name='Cage'
scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bm=bmesh.new();bm.from_mesh(cage.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(cage.data);bm.free()
bpy.ops.export_scene.fbx(filepath=str(OUT/'Cage.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False)
cage.data.calc_loop_triangles()
report={'triangles':len(cage.data.loop_triangles),'materials':len(cage.data.materials),'bounds_m':list(cage.dimensions),'origin':'bottom centre; Unity adds voxel (.5,0,.5)'}
image.filepath='//../../Assets/RivetReach/Resources/Spawners/Palette.png'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Cage.blend'))
# Existing original species art is a visual reference only; no new creature source.
with bpy.data.libraries.load(str(PROJECT/'ArtSource/Mobs/Floater.blend'),link=False) as (available,data):
    data.objects=[name for name in available.objects if name in ('Floater','FloaterMesh')]
for obj in data.objects:
    if obj is not None:scene.collection.objects.link(obj)
rig=bpy.data.objects.get('Floater')
if rig:
    rig.scale=(.32,.32,.32);rig.location=(0,0,.27);rig.rotation_euler.z=math.pi
    rig.animation_data_clear()
    for child in rig.children:
        for mat in child.data.materials:
            if mat and mat.use_nodes:
                for node in mat.node_tree.nodes:
                    if node.type=='TEX_IMAGE' and node.image:
                        node.image.filepath=str(PROJECT/'Assets/RivetReach/Resources/Mobs/CreaturePalette.png');node.image.reload()
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.world.color=(.18,.18,.18)
bpy.ops.object.camera_add(location=(1.65,-2.65,1.6));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.55
camera.rotation_euler=(Vector((0,0,.48))-camera.location).to_track_quat('-Z','Y').to_euler()
for location,power,size in [((2,-3,4),500,3),((-3,-1,2),280,2),((0,3,4),550,2)]:
    bpy.ops.object.light_add(type='AREA',location=location);light=bpy.context.object;light.data.energy=power;light.data.size=size
    light.rotation_euler=(Vector((0,0,.5))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.film_transparent=True;scene.render.filepath=str(OUT/'38.png');bpy.ops.render.render(write_still=True)
scene.render.film_transparent=False
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.01));floor=bpy.context.object
floor_mat=bpy.data.materials.new('Studio slate');floor_mat.diffuse_color=(.11,.13,.15,1);floor.data.materials.append(floor_mat)
scene.render.filepath=str(REVIEW/'spawner-blender.png');bpy.ops.render.render(write_still=True)
(SOURCE/'geometry.json').write_text(json.dumps(report,indent=2)+'\n')
print('SPAWNER_REPORT '+json.dumps(report))
