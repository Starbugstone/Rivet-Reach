"""Original Rivet Reach timber-and-linen bed. Copyright 2026 Starbugstone.
Blender --background --python Tools/create_bed_assets.py [-- --output-root PATH].
"""
import ast,bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
BASE=Path(sys.argv[sys.argv.index('--output-root')+1]) if '--output-root' in sys.argv else ROOT
OUT=BASE/'Assets/RivetReach/Resources/Industry';SOURCE=BASE/'ArtSource/Beds'
for p in (OUT/'Icons',SOURCE):p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.05;bs.inputs['Roughness'].default_value=.8
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/'Assets/RivetReach/Resources/Industry/Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
parts=[];motion={}
for n in ast.parse((ROOT/'Tools/create_industry_assets.py').read_text()).body:
 if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt']:
  exec(compile(ast.Module(body=[n],type_ignores=[]),'<original workshop helpers>','exec'))
# Source +Y points toward the pillow; Unity normalization preserves +Z.
for x in (.09,.91):
 for y in (.10,1.90):
  box('Carved timber post',(x,y,.48),(.13,.15,.96),14,.02)
  box('Post collar',(x,y,.12),(.145,.16,.07),1,.007)
 box('Long timber rail',(x,1,.42),(.12,1.82,.25),4,.018)
 for y in (.23,1.77):bolt((x+.066 if x>.5 else x-.066,y,.46),'X',.025)
for y in (.12,1.88):box('End rail',(.5,y,.51),(.82,.10,.28),14,.02)
for y in (.3,.6,.9,1.2,1.5,1.7):box('Mattress support slat',(.5,y,.52),(.83,.11,.06),4,.008)
box('Linen mattress',(.5,1,.665),(.81,1.67,.24),7,.055)
box('Woven teal quilt',(.5,.77,.80),(.83,1.25,.12),5,.032)
box('Folded quilt edge',(.5,1.32,.85),(.84,.16,.12),7,.025)
for x in (.16,.84):box('Quilt woven border',(x,.77,.869),(.028,1.14,.008),7,.004)
for y in (.29,.51,.73,.95,1.17):
 box('Quilt stitched seam',(.5,y,.869),(.68,.009,.006),6,.002)
box('Soft linen pillow',(.5,1.61,.84),(.62,.37,.22),7,.075)
for x in (.12,.88):box('Headboard upright',(x,1.91,.68),(.08,.09,.48),14,.012)
box('Headboard top',(.5,1.91,.91),(.79,.09,.09),14,.015)
for x in (.29,.5,.71):box('Headboard slat',(x,1.91,.72),(.12,.065,.29),4,.01)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.data.uv_layers.active.name='UVMap';o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();bed=bpy.context.object;bed.name='Timber and linen bed'
scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.export_scene.fbx(filepath=str(OUT/'bed.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False)
report={'triangles':sum(len(p.vertices)-2 for p in bed.data.polygons),'renderers':1,'materials':1,'footprint':[1,1,2]}
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.world.color=(.18,.20,.22)
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(3,-3,3));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=2.65
camera.rotation_euler=(Vector((.5,1,.5))-camera.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,-3,5),900,4),((-3,2,3),600,3),((2,4,4),800,3)]:
 bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.5,1,.5))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'Icons/254.png');bpy.ops.render.render(write_still=True)
scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.film_transparent=False
for name,pos in [('bed-front',(3,-3,3)),('bed-back',(-3,4,3))]:
 camera.location=pos;camera.rotation_euler=(Vector((.5,1,.5))-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(SOURCE/(name+'.png'));bpy.ops.render.render(write_still=True)
for im in bpy.data.images:
 if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(ROOT/"ArtSource/Beds"))
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Bed.blend'))
(SOURCE/'geometry.json').write_text(json.dumps(report,indent=2)+'\n');print('BED_ASSETS_COMPLETE',report)
