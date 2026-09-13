"""Original Floater, actions and collectible rock. Copyright 2026 Starbugstone.
Run Blender --background --python Tools/create_floater_assets.py.
"""
import bpy, bmesh, math, json, sys, random
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import create_mob_assets as art
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/RivetReach/Resources/Mobs'
LOOT=ROOT/'Assets/RivetReach/Resources/MobLoot'
SOURCE=ROOT/'ArtSource/Mobs'
REVIEW=ROOT/'.docs/verification/floater-2026-09-13'
for p in (OUT,LOOT,SOURCE,REVIEW): p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1;scene.render.fps=30
art.parts=[]
image=bpy.data.images.load(str(OUT/'CreaturePalette.png'));image.name='CreaturePalette'
art.material=bpy.data.materials.new('Floater stone palette');art.material.use_nodes=True
nodes=art.material.node_tree.nodes;tex=nodes.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest'
bsdf=nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.85
art.material.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])

def rock(name,center,scale,bone,seed,subdivisions=2):
    bm=bmesh.new();bmesh.ops.create_icosphere(bm,subdivisions=subdivisions,radius=1)
    rng=random.Random(seed);verts=[]
    for v in bm.verts:
        p=v.co*(.91+rng.random()*.13)
        # Chisel the face plane into the round boulder rather than adding a separate head.
        if name=='Weathered sphere':p.y=min(p.y,.83)
        verts.append(tuple(Vector(center)+Vector((p.x*scale[0],p.y*scale[1],p.z*scale[2]))))
    bm.verts.ensure_lookup_table();bm.verts.index_update()
    faces=[tuple(v.index for v in f.verts) for f in bm.faces];bm.free()
    obj=art.mesh_part(name,verts,faces,5,bone)
    uv=obj.data.uv_layers.active
    for face in obj.data.polygons:
        color=4 if face.index%9==0 else 13 if face.index%19==0 else 5
        for li in face.loop_indices:uv.data[li].uv=((color%4+.45)/4,(color//4+.45)/4)
    return obj

rock('Weathered sphere',(0,0,.55),(.51,.48,.51),'Body',27,3)
# Inset angular sockets with a stone brow and tiny warm mineral pupils.
for s in (-1,1):
    art.loft('Carved eye recess',[(.39,.66,.145,.092),(.436,.66,.127,.075)],11,'Head',8)
    socket=art.parts[-1]
    for v in socket.data.vertices:v.co.x+=s*.20
    art.gem('Pale mineral eye',(s*.20,.448,.661),(.080,.018,.040),15,'Head')
    art.gem('Amber pupil',(s*.185,.466,.659),(.022,.009,.030),9,'Head')
    # Asymmetric raised brow gives an angry cut-stone expression.
    art.tube('Brow ridge',[(s*.06,.424,.719),(s*.22,.431,.775),(s*.36,.345,.746)],[.060,.075,.035],4,'Head',5)
art.mesh_part('Chiselled scowl',[(-.23,.423,.44),(-.10,.469,.465),(.06,.477,.458),(.23,.427,.43),(.18,.439,.385),(-.14,.455,.399)],[(0,1,2,3,4,5)],11,'Head')
art.tube('Lower jaw ledge',[(-.18,.393,.354),(0,.440,.335),(.20,.386,.345)],[.04,.055,.028],4,'Head',5)
bones=[('Root',(0,0,0),(0,0,.2),None),('Body',(0,0,.55),(0,0,.8),'Root'),('Head',(0,.20,.59),(0,.44,.59),'Body')]
for s,label in [(-1,'L'),(1,'R')]:
    upper='Arm'+label;fore='Forearm'+label
    shoulder=(s*.42,0,.66);elbow=(s*.72,.035,.39);wrist=(s*.91,.27,.48)
    bones.extend([(upper,shoulder,elbow,'Body'),(fore,elbow,wrist,upper)])
    art.tube('Continuous stone upper arm',[shoulder,(s*.57,.012,.58),elbow],[.165,.14,.12],5,upper,7)
    rock('Elbow joint',elbow,(.14,.14,.14),fore,40+s)
    art.tube('Heavy tapered forearm',[elbow,(s*.83,.16,.43),wrist],[.12,.18,.20],5,fore,7)
    rock('Clenched stone palm',(s*.96,.32,.51),(.235,.22,.205),fore,54+s,2)
    for i in range(3):
        # Shaped knuckles and folded fingers remain separated by readable narrow creases.
        x=s*(.81+i*.13)
        art.tube('Folded finger',[(x,.34,.64),(x,.49,.60),(x,.49,.47),(x,.38,.435)],[.069,.073,.060,.044],4 if i==1 else 5,fore,6)
    rock('Folded thumb',(s*.77,.37,.48),(.095,.13,.105),fore,64+s,1)

bpy.ops.object.select_all(action='DESELECT')
for p in art.parts:p.select_set(True)
bpy.context.view_layer.objects.active=art.parts[0];bpy.ops.object.join();model=bpy.context.object;model.name='FloaterMesh'
bm=bmesh.new();bm.from_mesh(model.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(model.data);bm.free()
arm=bpy.data.armatures.new('FloaterSkeleton');rig=bpy.data.objects.new('Floater',arm);bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);model.select_set(False);bpy.ops.object.mode_set(mode='EDIT')
for name,head,tail,parent in bones:
    b=arm.edit_bones.new(name);b.head=head;b.tail=tail
    if parent:b.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');model.parent=rig;mod=model.modifiers.new('Floater rig','ARMATURE');mod.object=rig
rig.animation_data_create()
for name,length in [('Idle',60),('Walk',40),('Attack',30),('Death',42)]:
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    for frame in range(1,length+2):
        t=(frame-1)/length;wave=math.sin(t*math.tau)
        for b in rig.pose.bones:
            b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0)
            if b.name=='Body':
                # Bone local Y follows Blender vertical Z: breathing stays within body bounds.
                b.location.y=.025*wave if name in ('Idle','Walk') else 0
                if name=='Walk':b.rotation_euler.x=.06
                if name=='Attack':b.rotation_euler.x=-.18*math.sin(math.pi*t)
                if name=='Death':b.location.y=-.5*min(t*1.5,1);b.rotation_euler.z=.7*t
            if b.name.startswith('Arm'):
                side=1 if b.name.endswith('R') else -1
                b.rotation_euler.z=.06*wave*side
                if name=='Attack':b.rotation_euler.x=-.65*math.sin(math.pi*t);b.rotation_euler.z=side*.35*math.sin(math.pi*t)
                if name=='Death':b.rotation_euler.z=side*.75*t
            if b.name.startswith('Forearm') and name=='Attack':b.rotation_euler.x=.95*math.sin(math.pi*t)
            b.keyframe_insert(data_path='location',frame=frame,group=b.name);b.keyframe_insert(data_path='rotation_euler',frame=frame,group=b.name)
    action.frame_end=length+1
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1)
image.filepath='//../../Assets/RivetReach/Resources/Mobs/CreaturePalette.png'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Floater.blend'))
bpy.ops.object.select_all(action='DESELECT');model.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'Floater.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
model.data.calc_loop_triangles();report={'triangles':len(model.data.loop_triangles),'bones':len(bones),'materials':len(model.data.materials),'bounds_m':list(model.dimensions)}
# Studio source evidence: put the complete rig above a real floor to show the intended hover.
rig.location.z=.6
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world.color=(.17,.17,.17)
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Review floor'
mat=bpy.data.materials.new('Studio slate');mat.diffuse_color=(.12,.15,.17,1);floor.data.materials.append(mat)
bpy.ops.object.camera_add();camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=3.2
for loc,energy,size in [((2,3,5),700,4),((-3,1,3),400,3),((1,-3,4),650,3)]:
    bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=energy;light.data.shape='DISK';light.data.size=size;light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat('-Z','Y').to_euler()
for name,loc in [('front',(2.2,5,2.5)),('back',(-2.2,-5,2.5))]:
    camera.location=loc;camera.rotation_euler=(Vector((0,0,1))-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(REVIEW/('blender-'+name+'.png'));bpy.ops.render.render(write_still=True)
# Matching collectible: a small fragment of the same gray mineral with a warm exposed vein.
model.hide_render=True;rig.hide_render=True;art.parts=[]
loot=rock('Floater Rock',(0,0,0),(.46,.39,.42),'Body',91,2)
art.tube('Exposed mineral vein',[(-.26,.22,.22),(-.08,.34,.21),(.08,.35,.10),(.24,.29,-.04)],[.033,.025,.036,.018],13,'Body',5)
bpy.ops.object.select_all(action='DESELECT')
for p in art.parts:p.select_set(True)
bpy.context.view_layer.objects.active=loot;bpy.ops.object.join();loot.name='FloaterRock'
loot.vertex_groups.clear()
bpy.ops.export_scene.fbx(filepath=str(LOOT/'FloaterRock.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False)
loot.data.calc_loop_triangles();report['rock_triangles']=len(loot.data.loop_triangles)
# Store an independently editable item source without the hidden creature rig.
for obj in list(bpy.data.objects):
    if obj.type in ('MESH','ARMATURE') and obj!=loot:bpy.data.objects.remove(obj,do_unlink=True)
image.filepath='//../../Assets/RivetReach/Resources/Mobs/CreaturePalette.png'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'FloaterRock.blend'))
scene.render.film_transparent=True;camera.location=(1.8,3,1.7);camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=1.25
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(LOOT/'FloaterRockIcon.png');bpy.ops.render.render(write_still=True)
(SOURCE/'floater-asset-report.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
