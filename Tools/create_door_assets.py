"""Original two-metre timber door using Rivet Reach's shared workshop atlas."""
import ast, bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Industry'; SOURCE=ROOT/'ArtSource/Doors'
SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.25;bs.inputs['Roughness'].default_value=.48
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
parts=[];motion={}
for n in ast.parse((ROOT/'Tools/create_industry_assets.py').read_text()).body:
    if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt']:
        exec(compile(ast.Module(body=[n],type_ignores=[]),'<original workshop helpers>','exec'))
def merge(name,pivot=(0,0,0)):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:
        o.data.uv_layers.active.name='UVMap';o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    scene.cursor.location=pivot;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');return o
# Closed plane near +Y (Unity front); opening swings into the same reserved footprint.
for x in [.045,.955]:
    box('Solid oak jamb',(x,.85,1),(.09,.20,2),14,.01)
    for z in [.15,1.85]:box('Iron corner binding',(x,.85,z),(.09,.208,.12),1,.006)
box('Oak lintel',(.5,.85,1.955),(.82,.20,.09),14,.01)
box('Threshold',(.5,.85,.025),(.82,.20,.05),1,.005)
for z in [.42,1.58]:cyl('Hinge barrel',(.15,.89,z),.035,.20,2,'Z',12)
# A keyed blue receiver at the base, connected logically on all six lower-cell faces.
box('Blue receiver plate',(.95,.963,.32),(.082,.022,.17),0,.005)
box('Azure inset',(.95,.980,.32),(.047,.013,.095),5,.003)
frame=merge('Door frame');parts=[]
for i in range(5):
    x=.18+i*.16
    box('Vertical timber plank',(x,.84,1),(.153,.10,1.80),4 if i%2 else 14,.008)
for z in [.43,1.57]:
    for y in [.778,.902]:
        box('Forged strap',(.5,y,z),(.76,.024,.072),1,.006)
        for x in [.19,.50,.81]:bolt((x,y+(.017 if y>.8 else -.017),z),'Y',.014)
for y in [.756,.924]:
    box('Handle escutcheon',(.77,y,1),(.075,.025,.17),2,.006)
    cyl('Handle boss',(.77,y+(.023 if y>.8 else -.023),1),.031,.05,3,'Y',12)
    box('Brass latch handle',(.715,y+(.04 if y>.8 else -.04),1),(.13,.026,.028),3,.006)
leaf=merge('MotionDoor',(.15,.84,1));parts=[]
objects=[frame,leaf]
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=frame
bpy.ops.export_scene.fbx(filepath=str(OUT/'wooden_door.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
report={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects),'parts':2,'materials':1,'footprint':[1,2,1]}
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.world.color=(.18,.20,.22);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(3,5,3));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=2.8
camera.rotation_euler=(Vector((.5,.5,1))-camera.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,4,5),900,4),((-3,2,2),500,3),((1,-3,4),800,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.5,.5,1))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'Icons/171.png');bpy.ops.render.render(write_still=True)
scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.film_transparent=False
scene.render.filepath=str(SOURCE/'door-closed.png');bpy.ops.render.render(write_still=True)
leaf.rotation_euler[2]=-math.pi/2
scene.render.filepath=str(SOURCE/'door-open.png');bpy.ops.render.render(write_still=True)
leaf.rotation_euler[2]=0
for im in bpy.data.images:
    if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'WoodenDoor.blend'))
(SOURCE/'geometry-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('DOOR_ASSETS_COMPLETE',report)
