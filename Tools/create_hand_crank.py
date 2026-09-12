"""Original hand generator, matching Rivet Reach's iron/copper Workshop kit."""
import ast, bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Industry'; SOURCE=ROOT/'ArtSource/HandCrank'
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
# Rear mounting plate lies on the battery face. +Y points toward the operator.
box('Cast mounting plate',(.5,.055,.5),(.7,.10,.76),0,.025)
for x in [.22,.78]:
    for z in [.18,.82]:bolt((x,.115,z),'Y',.025)
cyl('Rear electrical plug',(.5,.016,.5),.115,.032,8,'Y',16)
ring('Rear brass collar',(.5,.025,.5),.095,.016,3,'Y')
cyl('Generator barrel',(.5,.33,.5),.235,.43,0,'Y',24)
for y in [.15,.24,.33,.42,.51]:ring('Copper winding',(.5,y,.5),.235,.023,3,'Y')
for x in [.32,.68]:box('Cage brace',(x,.33,.68),(.045,.44,.045),1,.008)
cyl('Front bearing',(.5,.57,.5),.16,.08,1,'Y',20)
ring('Bearing rim',(.5,.615,.5),.135,.018,3,'Y')
box('Maker plate',(.5,.623,.43),(.13,.018,.065),7,.006)
body=merge('Crank housing');parts=[]
# A single mesh pivots around the axle: wheel, offset arm and wooden turning grip.
cyl('Axle',(.5,.66,.5),.055,.14,10,'Y',12)
ring('Flywheel',(.5,.66,.5),.205,.025,2,'Y')
for a in range(0,360,90):
    rad=math.radians(a);o=box('Wheel spoke',(.5+math.sin(rad)*.1,.66,.5+math.cos(rad)*.1),(.026,.035,.20),1,.004);o.rotation_euler[1]=rad
box('Offset crank arm',(.5,.715,.64),(.065,.055,.30),3,.012)
cyl('Grip pin',(.5,.80,.79),.032,.19,10,'Y',12)
cyl('Turned wooden handle',(.5,.85,.79),.056,.21,4,'Y',16)
for y in [.755,.94]:ring('Handle ferrule',(.5,y,.79),.05,.008,2,'Y')
crank=merge('MotionSpinCrank',(.5,.66,.5));parts=[]
objects=[body,crank]
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.export_scene.fbx(filepath=str(OUT/'hand_crank.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
report={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects),'parts':2,'materials':1}
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.world.color=(.16,.18,.20);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(2.3,3.4,2));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.45
camera.rotation_euler=(Vector((.5,.47,.5))-camera.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,3,5),650,4),((-3,2,2),400,3),((1,-3,3),700,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.5,.5,.5))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'Icons/170.png');bpy.ops.render.render(write_still=True)
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.film_transparent=False
scene.render.filepath=str(SOURCE/'hand-crank-review.png');bpy.ops.render.render(write_still=True)
# Editable source animation; runtime drives the exported named pivot from paid strokes.
crank.rotation_euler=(0,0,0);crank.keyframe_insert(data_path='rotation_euler',frame=1)
crank.rotation_euler[1]=2*math.pi;crank.keyframe_insert(data_path='rotation_euler',frame=13)
scene.frame_end=13;scene.frame_set(1)
for im in bpy.data.images:
    if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'HandCrank.blend'))
(SOURCE/'geometry-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('HAND_CRANK_ASSETS_COMPLETE',report)
