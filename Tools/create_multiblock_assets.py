"""Original tank shell kit and independent pipe channel fittings, authored in Blender.
Uses the existing original Workshop atlas and modeling helpers without regenerating earlier art.
"""
import bpy, math, json, ast
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/RivetReach/Resources/Industry';SOURCE=ROOT/'ArtSource/Multiblocks'
SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Metallic'].default_value=.65;bs.inputs['Roughness'].default_value=.34
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
glass=bpy.data.materials.new('TankGlass');glass.use_nodes=True;g=glass.node_tree.nodes.get('Principled BSDF');g.inputs['Base Color'].default_value=(.28,.58,.67,1);g.inputs['Transmission Weight'].default_value=.96;g.inputs['Roughness'].default_value=.1;g.inputs['IOR'].default_value=1.08
parts=[];motion={}
# Reuse only original geometry helpers, never execute the old kit generator.
source=ast.parse((ROOT/'Tools/create_industry_assets.py').read_text())
for n in source.body:
    if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt','tube','gauge']:
        exec(compile(ast.Module(body=[n],type_ignores=[]),'<workshop helpers>','exec'))

def merge(objects,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');return o
# Transform a front panel into a world face. Blender front +Y = game front -Z.
rotations=[Matrix.Rotation(-math.pi/2,4,'Z'),Matrix.Rotation(math.pi/2,4,'Z'),Matrix.Rotation(math.pi/2,4,'X'),Matrix.Rotation(-math.pi/2,4,'X'),Matrix.Rotation(math.pi,4,'Z'),Matrix.Identity(4)]
# Local trim sides in world direction for the face's tangent axes.
tangents={}
for f,r in enumerate(rotations):
    tangents[f]=[]
    for v in [Vector((1,0,0)),Vector((-1,0,0)),Vector((0,0,1)),Vector((0,0,-1))]:
        b=r.to_3x3()@v;w=Vector((b.x,b.z,-b.y));axis=max(range(3),key=lambda i:abs(w[i]));tangents[f].append([0,2,4][axis]+(1 if w[axis]<0 else 0))

def rotate_group(objects,f):
    matrix=Matrix.Translation((.5,.5,.5))@rotations[f]@Matrix.Translation((-.5,-.5,-.5))
    for o in objects:o.matrix_world=matrix@o.matrix_world

def panel(id,key):
    global parts,motion
    parts=[];motion={};groups=[]
    for f in range(6):
        first=len(parts)
        if id==162:
            mesh=bpy.data.meshes.new('Continuous glass face');mesh.from_pydata([(0,.975,0),(0,.975,1),(1,.975,1),(1,.975,0)],[],[(0,1,2,3)]);mesh.update()
            o=bpy.data.objects.new('Clear reinforced pane',mesh);scene.collection.objects.link(o);finish(o,'Clear reinforced pane',13);o.data.materials.clear();o.data.materials.append(glass)
            rotate_group(parts[first:],f);groups.append(merge(parts[first:],'Glass'+str(f)))
        else:
            box('Rolled iron skin',(.5,.948,.5),(.998,.08,.998),0,.008)
            box('Inset brushed panel',(.5,.993,.5),(.76,.008,.76),1,.005)
            # Tiny scored horizontal tooling lines, restrained industrial wear.
            for z in [.26,.74]:box('Panel scribe',(.5,.998,z),(.55,.002,.003),12,0)
            rotate_group(parts[first:],f);groups.append(merge(parts[first:],('Skin' if id==160 else 'Face')+str(f)))
        for side in range(4):
            first=len(parts);vertical=side<2
            x=(.947 if side==0 else .053) if vertical else .5;z=.5 if vertical else (.947 if side==2 else .053)
            box('Iron edge rail',(x,.98,z),(.105,.045,1) if vertical else (1,.045,.105),1,.009)
            box('Brass edge reveal',(x,1.006,z),(.028,.012,1) if vertical else (1,.012,.028),2,.003)
            for t in [.18,.82]:
                bx=x if vertical else t;bz=t if vertical else z
                box('Brass saddle',(bx,1.008,bz),(.085,.018,.085),3,.006);bolt((bx,1.023,bz),'Y',.019)
            rotate_group(parts[first:],f);groups.append(merge(parts[first:],f'Trim{f}_{tangents[f][side]}'))
    first=len(parts)
    if id==163:
        box('Controller housing',(.5,1.013,.53),(.69,.03,.77),0,.025)
        gauge(.5,1.04,.66,.19)
        for x in [.34,.66]:
            box('Square status bezel',(x,1.04,.28),(.17,.032,.13),3,.01)
        ring('Recovery union',(.66,1.052,.28),.055,.015,3,'Y')
        cyl('Recovery bore',(.66,1.053,.28),.037,.009,8,'Y')
        for z in [.44,.50,.56]:box('Side vent',(.999,.43,z),(.009,.28,.02),8,.002)
    if id in [164,166]:
        box('Nozzle mount',(.5,1.008,.5),(.65,.035,.65),2,.025)
        cyl('Deep throat',(.5,1.035,.5),.235,.025,8,'Y',16)
        ring('Machined brass union',(.5,1.03,.5),.26,.035,3,'Y')
        for i in range(8):
            a=i*math.pi/4;bolt((.5+math.cos(a)*.285,1.046,.5+math.sin(a)*.285),'Y',.018)
        for z in [.16,.84]:box('Flow direction mark',(.5,1.03,z),(.12,.015,.025),7,.003)
    if id==165:
        box('Hatch gasket',(.5,1.005,.5),(.69,.035,.70),8,.025);box('Brass hatch door',(.5,1.028,.5),(.61,.024,.61),2,.02)
        box('Raised door inset',(.5,1.043,.5),(.43,.007,.43),3,.013)
        for z in [.28,.72]:box('Hatch hinge',(.21,1.05,z),(.075,.035,.1),10,.006)
        box('Handle shadow',(.5,1.055,.48),(.25,.024,.095),8,.005);box('Pull grip',(.5,1.068,.5),(.23,.022,.06),10,.008)
    if id in [166,167]:
        box('Signal terminal',(.84,1.013,.20),(.20,.04,.20),0,.016)
        box('Square blue key',(.84,1.04,.20),(.11,.027,.11),5,.008)
        for x in [.77,.91]:bolt((x,1.04,.20),'Y',.014)
    if id==167:
        box('Level gauge housing',(.5,1.018,.5),(.3,.028,.81),3,.015)
        box('Level dark slot',(.5,1.036,.5),(.16,.012,.68),8,.008)
        for z in [.23,.33,.43,.53,.63,.73]:box('Level tick',(.64,1.036,z),(.05,.013,.009),7,.001)
    if len(parts)>first:groups.append(merge(parts[first:],'Detail'))
    if id in [163,167]:
        first=len(parts)
        if id==163:box('Status pilot',(.34,1.061,.28),(.11,.008,.06),6,.005)
        else:box('Level strip',(.5,1.05,.5),(.095,.009,.59),6,.002)
        groups.append(merge(parts[first:],'StatusLight'))
    export(key,groups);return groups

def export(key,groups):
    bpy.ops.object.select_all(action='DESELECT')
    for o in groups:o.select_set(True)
    bpy.context.view_layer.objects.active=groups[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)

def addon(key,power):
    global parts
    parts=[];groups=[]
    for f in range(6):
        first=len(parts)
        # Offset external insulated leads preserve separate visible pipe/channel paths.
        x=.69 if power else .32;z=.69 if power else .32
        tube('Offset lead shoulder',[(.5,.5,.5),(x,.52,z)],.028 if power else .018,8 if power else 5)
        cyl('Insulated lead',(x,.76,z),.028 if power else .018,.48,8 if power else 5,'Y',12)
        ring('Brass ferrule',(x,.96,z),.036 if power else .026,.01,3,'Y')
        if power:
            for dz in [-.008,.008]:cyl('Copper contact',(x,.992,z+dz),.007,.014,2,'Y',8)
        else:box('Blue keyed tip',(x,.99,z),(.045,.018,.045),6,.004)
        rotate_group(parts[first:],f);groups.append(merge(parts[first:],'Arm'+str(f)))
    first=len(parts)
    box('Fitted channel terminal',(.68 if power else .32,.5,.68 if power else .32),(.10,.16,.10),8 if power else 5,.008)
    groups.append(merge(parts[first:],'Body'))
    export(key,groups);return groups

assets=[];report={}
for id,key in [(160,'tank_frame'),(161,'tank_wall'),(162,'tank_glass'),(163,'tank_controller'),(164,'tank_port'),(165,'tank_hatch'),(166,'tank_valve'),(167,'tank_sensor')]:
    groups=panel(id,key);assets.append((id,key,groups));report[key]={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in groups),'sourceParts':len(groups)}
    for o in groups:o.hide_render=True
for key,power in [('pipe_signal_addition',False),('pipe_power_addition',True)]:
    groups=addon(key,power);assets.append((0,key,groups))
    for o in groups:o.hide_render=True
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.world.color=(.20,.20,.20);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(2.3,3.1,2.1));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.9
camera.rotation_euler=(Vector((.5,.5,.5))-camera.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,3,5),650,4),((-3,2,2),400,3),((1,-3,3),700,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.5,.5,.5))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
for id,key,objects in assets:
    if not id:continue
    for o in objects:o.hide_render=o.name.startswith("Skin")
    scene.render.filepath=str(OUT/'Icons'/f'{id}.png');bpy.ops.render.render(write_still=True)
    for o in objects:o.hide_render=True
for i,(id,key,objects) in enumerate(assets):
    for o in objects:o.location=((i%4)*1.45,(i//4)*1.7,0);o.hide_render=o.name.startswith("Skin")
camera.location=(9,13,10);target=Vector((2.7,2.0,.4));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=8.4
scene.render.film_transparent=False;scene.world.color=(.055,.075,.09);scene.render.resolution_x=1600;scene.render.resolution_y=1100
scene.render.filepath=str(SOURCE/'tank-kit-review.png');bpy.ops.render.render(write_still=True)
for im in bpy.data.images:
    if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'TankKit.blend'))
(SOURCE/'geometry-report.json').write_text(json.dumps(report,indent=2))
print('MULTIBLOCK_ASSETS_COMPLETE')
