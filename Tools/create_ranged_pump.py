"""Original ranged liquid pump, matching Rivet Reach's iron/copper Workshop kit."""
import ast, bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Industry'; SOURCE=ROOT/'ArtSource/RangedPump'
SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.65;bs.inputs['Roughness'].default_value=.34
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
parts=[];motion={}
for n in ast.parse((ROOT/'Tools/create_industry_assets.py').read_text()).body:
    if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt']:
        exec(compile(ast.Module(body=[n],type_ignores=[]),'<original workshop helpers>','exec'))
def merge(name,pivot=(0,0,0)):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    scene.cursor.location=pivot;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');return o
# Floater-powered collector: suspended mineral core in a copper induction cradle.
box('Cast plinth',(.5,.5,.075),(.9,.88,.15),0,.025)
box('Copper deck',(.5,.5,.16),(.86,.84,.04),3,.01)
cyl('Collector base',(.5,.5,.265),.31,.17,2,'Z',24)
ring('Intake collar',(.5,.5,.35),.29,.025,3)
for x in [.19,.81]:
    box('Cradle upright',(x,.5,.59),(.065,.09,.48),0,.012)
    for z in [.39,.79]:bolt((x,.558,z),'Y',.022)
ring('Field hoop',(.5,.5,.62),.31,.024,3,'Y')
ring('Lower field coil',(.5,.5,.43),.22,.023,3)
cyl('Right outlet',(.867,.5,.29),.08,.22,2,'X',16)
ring('Outlet rim',(.976,.5,.29),.074,.013,3,'X')
cyl('Outlet bore',(.989,.5,.29),.050,.003,8,'X',16)
box('Control face',(.5,.785,.27),(.33,.045,.12),0,.01)
for x in [.11,.89]:
    for y in [.12,.88]:bolt((x,y,.19))
body=merge('Ranged pump cradle');parts=[]
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=(.5,.5,.64))
o=bpy.context.object;o.scale=(.195,.175,.185);finish(o,'Suspended Floater Rock',15)
# Pale mineral band echoes the collectible's diagonal vein.
vein=box('Pale mineral seam',(.5,.665,.645),(.34,.018,.027),7,.006)
vein.rotation_euler[1]=-.5
core=merge('MotionBob',(.5,.5,.64));parts=[]
box('StatusLight',(.5,.815,.27),(.13,.015,.035),6,.004)
status=merge('StatusLight');parts=[]
objects=[body,core,status]
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.export_scene.fbx(filepath=str(OUT/'ranged_liquid_pump.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
report={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects),'parts':3,'materials':1,'footprint':'one metre cell; lower corner origin; +Y front becomes Unity -Z'}
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.world.color=(.16,.18,.20);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(2.3,3.4,2));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.5
camera.rotation_euler=(Vector((.5,.47,.5))-camera.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,3,5),650,4),((-3,2,2),400,3),((1,-3,3),700,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.5,.5,.5))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'Icons/180.png');bpy.ops.render.render(write_still=True)
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.film_transparent=False
scene.render.filepath=str(SOURCE/'ranged-pump-review.png');bpy.ops.render.render(write_still=True)
for im in bpy.data.images:
    if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'RangedPump.blend'))
(SOURCE/'geometry-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('RANGED_PUMP_ASSETS_COMPLETE',report)
