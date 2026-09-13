"""Original electric furnace, matching Rivet Reach's iron/copper Workshop kit."""
import ast, bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Industry'; SOURCE=ROOT/'ArtSource/ElectricFurnace'
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
# One-cell insulated oven, copper frame and visible electric heating elements.
box('Cast base',(.5,.5,.08),(.92,.9,.16),0,.025)
box('Copper base trim',(.5,.5,.17),(.88,.86,.035),3,.008)
box('Insulated cabinet',(.5,.45,.55),(.80,.71,.73),1,.035)
box('Upper cap',(.5,.47,.935),(.90,.82,.08),0,.02)
for x in [.13,.87]:
    box('Copper corner', (x,.79,.55),(.055,.045,.72),3,.009)
    for z in [.25,.87]:bolt((x,.82,z),'Y',.022)
box('Oven surround',(.5,.826,.55),(.66,.07,.53),2,.018)
box('Ceramic door',(.5,.870,.55),(.58,.035,.45),7,.015)
box('Dark chamber',(.5,.897,.58),(.46,.025,.29),8,.01)
for z in [.49,.57,.65]:
    box('Heater rail',(.5,.915,z),(.39,.015,.017),6,.003)
for x in [.30,.70]:
    cyl('Door handle mount',(x,.928,.34),.025,.07,0,'Y',12)
box('Door handle',(.5,.966,.34),(.43,.035,.04),3,.009)
for z in [.32,.40,.48,.56,.64,.72]:
    box('Side cooling fin',(.915,.44,z),(.025,.49,.025),0,.005)
cyl('Rear power terminal',(.5,.079,.51),.095,.045,3,'Y',16)
cyl('Rear insulator',(.5,.049,.51),.06,.02,8,'Y',12)
box('Control panel',(.5,.832,.84),(.37,.025,.085),0,.008)
body=merge('Electric furnace cabinet');parts=[]
box('StatusLight',(.61,.853,.84),(.065,.012,.032),6,.004)
status=merge('StatusLight');parts=[]
objects=[body,status]
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.export_scene.fbx(filepath=str(OUT/'electric_furnace.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
report={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects),'parts':2,'materials':1,'footprint':'one metre cell; lower corner origin; +Y front becomes Unity -Z'}
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.world.color=(.16,.18,.20);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(2.3,3.4,2));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.5
camera.rotation_euler=(Vector((.5,.47,.5))-camera.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,3,5),650,4),((-3,2,2),400,3),((1,-3,3),700,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.5,.5,.5))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'Icons/174.png');bpy.ops.render.render(write_still=True)
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.film_transparent=False
scene.render.filepath=str(SOURCE/'electric-furnace-review.png');bpy.ops.render.render(write_still=True)
for im in bpy.data.images:
    if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ElectricFurnace.blend'))
(SOURCE/'geometry-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('ELECTRIC_FURNACE_ASSETS_COMPLETE',report)
