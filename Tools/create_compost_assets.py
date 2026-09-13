"""Original wooden compost bin and loose compost, using Rivet Reach's authored atlas."""
import bpy,math,ast,json,random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/RivetReach/Resources/Industry';SRC=ROOT/'ArtSource/Compost';SRC.mkdir(parents=True,exist_ok=True)
report=[]
for key,item in [('compost_bin',242),('compost',236)]:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
    mat=bpy.data.materials.new('WorkshopAtlas');mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.82
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Atlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    parts=[];motion={}
    for n in ast.parse((ROOT/'Tools/create_industry_assets.py').read_text()).body:
        if isinstance(n,ast.FunctionDef) and n.name in ['finish','box','cyl','ring','bolt']:exec(compile(ast.Module(body=[n],type_ignores=[]),'<workshop helpers>','exec'))
    def join(name,objects):
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects:o.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');return o
    if key=='compost_bin':
        for x in [.1,.9]:
            for y in [.1,.9]:box('Corner post',(x,y,.47),(.12,.12,.92),4,.012)
        for z in [.18,.37,.56,.75]:
            for y in [.09,.91]:box('Horizontal wood plank',(.5,y,z),(.83,.08,.155),14 if z==.37 else 4,.01)
            for x in [.09,.91]:box('Side wood plank',(x,.5,z),(.08,.79,.155),4,.01)
        for x in [.25,.5,.75]:box('Floor board',(x,.5,.12),(.24,.78,.07),4,.008)
        for y in [.07,.93]:box('Top rim',(.5,y,.93),(.98,.14,.065),14,.012)
        for x in [.07,.93]:box('Side rim',(x,.5,.93),(.14,.8,.065),14,.012)
        # A small carved leaf sign identifies the bin without powered-looking controls.
        box('Front sign',(.5,.963,.63),(.3,.03,.22),7,.008)
        leaf=box('Leaf emblem',(.5,.984,.64),(.08,.008,.13),11,.02);leaf.rotation_euler[1]=-.45
        box('Leaf stem',(.49,.989,.58),(.015,.009,.08),4,.002)
        for x in [.15,.85]:
            for z in [.2,.76]:cyl('Wood peg',(x,.957,z),.014,.009,14,'Y',8)
        body=join('Wooden compost bin',parts[:]);parts=[]
        box('Organic bed',(.5,.5,.73),(.74,.72,.08),4,.02)
        rng=random.Random(77)
        for i in range(12):
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=rng.uniform(.045,.1),location=(rng.uniform(.22,.78),rng.uniform(.22,.78),.8));o=bpy.context.object;o.scale=(1,.8,.45);finish(o,'Organic scraps',11 if i%3==0 else 14)
        fill=join('CompostFill',parts[:]);objects=[body,fill]
    else:
        rng=random.Random(83)
        for i in range(14):
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=rng.uniform(.1,.18),location=(rng.uniform(.3,.7),rng.uniform(.3,.7),rng.uniform(.15,.28)));o=bpy.context.object;o.scale=(1,.9,.65);finish(o,'Rich compost clod',4 if i%3 else 14)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.21,location=(.5,.5,.28));o=bpy.context.object;o.scale=(1.4,1.2,.8);finish(o,'Top compost clod',4)
        for i in range(5):
            o=box('Plant fibre',(rng.uniform(.43,.57),rng.uniform(.43,.57),.365),(.11,.022,.025),7,.002);o.rotation_euler[2]=rng.uniform(0,math.pi)
        objects=[join('Compost',parts[:])]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    report.append({'asset':key,'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects),'meshes':len(objects)})
    scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.world.color=(.16,.18,.20);scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
    bpy.ops.object.camera_add(location=(2.3,3.4,2.6));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.48 if item==242 else .85;target=Vector((.5,.5,.48 if item==242 else .23));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    for pos,power in [((1,3,5),600),((-3,2,2),350),((1,-3,3),500)]:
        bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=4;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100;scene.render.filepath=str(OUT/('Icons/'+str(item)+'.png'));bpy.ops.render.render(write_still=True)
    scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.film_transparent=False;scene.render.filepath=str(SRC/(key+'-review.png'));bpy.ops.render.render(write_still=True)
    for im in bpy.data.images:
        if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=str(SRC))
    bpy.ops.wm.save_as_mainfile(filepath=str(SRC/(key+'.blend')))
(SRC/'geometry.json').write_text(json.dumps(report,indent=2)+'\n')
print('COMPOST_ASSETS_COMPLETE')
