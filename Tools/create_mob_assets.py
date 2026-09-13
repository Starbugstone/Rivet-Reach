"""Original Rivet Reach native creatures, rig, actions and FBX exports.
Copyright (c) 2026 Starbugstone. All rights reserved.
Run using Blender 5.2 --background --python Tools/create_mob_assets.py.
"""
import bpy, bmesh, math, json, sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/Mobs'
OUT = ROOT / 'Assets/RivetReach/Resources/Mobs'
REVIEW = ROOT / '.docs/verification'
for directory in (SOURCE, OUT, REVIEW): directory.mkdir(parents=True, exist_ok=True)
PALETTE = [(0.24, .085, .035), (.61, .245, .075), (.88, .49, .18), (.10, .105, .095),
           (.25, .29, .32), (.42, .46, .48), (.085, .13, .175), (.20, .29, .35),
           (.48, .65, .66), (.91, .75, .43), (.96, .32, .08), (.035, .045, .05),
           (.40, .31, .25), (.70, .63, .46), (.73, .41, .23), (.82, .88, .82)]

def mesh_part(name, verts, faces, colour, bone):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces); mesh.update()
    obj = bpy.data.objects.new(name, mesh); bpy.context.collection.objects.link(obj)
    uv = mesh.uv_layers.new(name='CreatureUV')
    for face in mesh.polygons:
        # Small tonal variation within a shared palette tile; one material per creature.
        for li in face.loop_indices:
            uv.data[li].uv = ((colour % 4 + .35 + (face.index % 3)*.12)/4,
                             (colour // 4 + .35 + (face.index % 2)*.2)/4)
    obj.data.materials.append(material)
    obj.vertex_groups.new(name=bone).add(list(range(len(verts))), 1, 'REPLACE')
    parts.append(obj)
    return obj

def loft(name, rings, colour, bone, sides=12):
    """Shaped cross sections along forward Y, not scaled primitive placeholders."""
    verts=[]
    for y, z, rx, rz in rings:
        for i in range(sides):
            a=2*math.pi*i/sides
            verts.append((math.cos(a)*rx, y, z+math.sin(a)*rz))
    faces=[tuple(range(sides-1,-1,-1))]
    for j in range(len(rings)-1):
        for i in range(sides):
            k=j*sides+i; n=j*sides+(i+1)%sides
            faces.append((k,n,n+sides,k+sides))
    faces.append(tuple(range((len(rings)-1)*sides,len(rings)*sides)))
    return mesh_part(name,verts,faces,colour,bone)

def tube(name, points, radii, colour, bone, sides=8):
    verts=[]
    for j, point in enumerate(points):
        axis=(Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])).normalized()
        u=axis.cross(Vector((0,0,1)))
        if u.length<.01: u=axis.cross(Vector((0,1,0)))
        u.normalize();v=axis.cross(u).normalized()
        for i in range(sides):
            a=2*math.pi*i/sides
            verts.append(Vector(point)+(u*math.cos(a)+v*math.sin(a))*radii[j])
    faces=[tuple(range(sides-1,-1,-1))]
    for j in range(len(points)-1):
        for i in range(sides): faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
    faces.append(tuple(range((len(points)-1)*sides,len(points)*sides)))
    return mesh_part(name,verts,faces,colour,bone)

def gem(name, center, size, colour, bone):
    x,y,z=center;rx,ry,rz=size
    return mesh_part(name,[(x-rx,y,z),(x,y-ry,z),(x+rx,y,z),(x,y+ry,z),(x,y,z+rz),(x,y,z-rz)],
                     [(0,1,4),(1,2,4),(2,3,4),(3,0,4),(1,0,5),(2,1,5),(3,2,5),(0,3,5)],colour,bone)

if __name__ == "__main__":
    reports=[]
    for kind in ('RustbackBeetle','DuskProwler'):
        bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
        for action in list(bpy.data.actions): bpy.data.actions.remove(action)
        scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
        scene.render.fps=30;parts=[];bones=[('Root',(0,0,0),(0,0,.25),None),('Body',(0,0,.55),(0,0,.85),'Root')]
        image=bpy.data.images.get('CreaturePalette')
        if image is None:
            image=bpy.data.images.new('CreaturePalette',width=64,height=64)
            pixels=[]
            for y in range(64):
                for x in range(64):
                    c=PALETTE[(y//16)*4+x//16];shade=.91+.12*((x*7+y*11)%17)/16
                    pixels.extend((*[v*shade for v in c],1))
            image.pixels.foreach_set(pixels);image.filepath_raw=str(OUT/'CreaturePalette.png');image.file_format='PNG';image.save()
        material=bpy.data.materials.new(kind+' palette');material.use_nodes=True
        tex=material.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest'
        bsdf=material.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.8
        material.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
        if kind=='RustbackBeetle':
            loft('Segmented dark abdomen',[(-.70,.44,.12,.13),(-.55,.47,.38,.29),(0,.51,.48,.34),(.34,.49,.39,.29),(.46,.48,.19,.18)],0,'Body',16)
            # Two genuinely domed elytra, separated by a dark central seam.
            for side in (-1,1):
                verts=[]
                for y,z,w,h in [(-.70,.50,.035,.025),(-.51,.53,.34,.27),(-.13,.55,.46,.35),(.23,.53,.38,.29),(.34,.52,.25,.20)]:
                    for i in range(7):
                        a=i*math.pi/12
                        verts.append((side*(.014+w*math.sin(a)),y,z+h*math.cos(a)))
                faces=[]
                for j in range(4):
                    for i in range(6):faces.append((j*7+i,j*7+i+1,(j+1)*7+i+1,(j+1)*7+i))
                obj=mesh_part('Copper wing case',verts,faces,1,'Body')
                for j in range(3):
                    gem('Shell ridge',(side*(.15+j*.075),-.37+j*.21,.82),(.075,.15,.035),2,'Body')
            bones.append(('Head',(0,.35,.48),(0,.78,.48),'Body'))
            loft('Armoured brow',[(.30,.48,.24,.19),(.49,.48,.31,.23),(.72,.42,.25,.16),(.79,.40,.15,.10)],1,'Head')
            for s in (-1,1):
                gem('Amber compound eye',(s*.25,.66,.52),(.075,.082,.071),10,'Head')
                tube('Curved mandible',[(s*.18,.73,.38),(s*.26,.89,.34),(s*.17,1.03,.36),(s*.07,1.01,.41)],[.08,.07,.044,.003],13,'Head')
                tube('Antenna',[(s*.16,.68,.61),(s*.27,.85,.77),(s*.31,1.01,.82)],[.02,.015,.006],3,'Head',6)
                for j,y in enumerate((-.42,-.04,.32)):
                    leg='Leg'+str(j)+('L' if s<0 else 'R');start=(s*.30,y,.48);knee=(s*.62,y-.08,.35);foot=(s*.68,y+.16,.035)
                    bones.append((leg,start,knee,'Body'))
                    tube('Jointed leg',[start,knee,foot],[.085,.062,.018],3,leg)
                    gem('Knee plate',knee,(.065,.09,.06),1,leg)
        else:
            loft('Continuous prowler torso',[(-.77,.77,.11,.15),(-.61,.80,.27,.30),(-.32,.85,.31,.33),(.02,.91,.25,.32),(.35,.96,.34,.42),(.52,.96,.25,.33)],6,'Body',16)
            loft('Pale throat and chest',[(.26,.77,.24,.21),(.45,.81,.28,.29),(.56,.89,.18,.18)],8,'Body')
            bones.extend([('Head',(0,.43,1.06),(0,.91,1.08),'Body'),('Jaw',(0,.75,.91),(0,1.05,.89),'Head'),('Tail',(0,-.62,.89),(0,-1.1,.72),'Body')])
            loft('Angular feline skull',[(.39,1.11,.17,.18),(.58,1.19,.27,.28),(.82,1.13,.245,.23),(.99,1.03,.17,.13),(1.12,1.02,.11,.08)],7,'Head')
            loft('Lower jaw',[(.74,.93,.17,.10),(1.06,.90,.12,.06)],4,'Jaw')
            gem('Nose',(0,1.125,1.045),(.105,.043,.055),11,'Head')
            for s in (-1,1):
                # Swept triangular ears and cheek tufts carry the silhouette.
                mesh_part('Swept ear',[(s*.16,.54,1.37),(s*.28,.50,1.35),(s*.29,.35,1.70),(s*.17,.62,1.42),(s*.23,.44,1.42)],[(0,1,2),(0,2,3),(1,4,2),(2,4,3),(0,3,4,1)],6,'Head')
                gem('Ear inset',(s*.235,.51,1.48),(.029,.027,.105),8,'Head')
                gem('Eye socket',(s*.215,.86,1.205),(.083,.047,.07),11,'Head')
                gem('Gold slit eye',(s*.23,.886,1.21),(.056,.026,.032),9,'Head')
                gem('Vertical pupil',(s*.233,.907,1.211),(.009,.009,.028),11,'Head')
                for j in range(3):
                    tube('Cheek mane',[(s*.20,.67-j*.11,1.07),(s*(.38+j*.018),.42-j*.10,.94)],[.105,.003],5,'Head',5)
                tube('Lower fang',[(s*.115,1.035,.935),(s*.10,1.04,1.003)],[.022,.002],15,'Jaw',6)
                for j,y in enumerate((-.52,.34)):
                    leg='Leg'+str(j)+('L' if s<0 else 'R')
                    start=(s*.23,y,.87);knee=(s*.29,y+(.11 if j else -.12),.47);ankle=(s*.30,y-.02,.14)
                    bones.append((leg,start,knee,'Body'))
                    tube('Tapered articulated leg',[start,knee,ankle],[.15,.103,.066],7,leg,10)
                    paw=loft('Broad paw',[(y-.14,.085,.07,.064),(y+.02,.09,.115,.082),(y+.21,.065,.096,.055)],4,leg,10)
                    for vertex in paw.data.vertices:vertex.co.x+=s*.30
                    for toe in (-1,0,1):tube('Paw claw',[(s*.30+toe*.05,y+.17,.07),(s*.30+toe*.05,y+.24,.035)],[.018,.003],13,leg,5)
                for j in range(4):gem('Shoulder stripe',(s*(.29-j*.015),.24-j*.12,1.10),(.022,.033,.15),8,'Body')
            tube('Long balancing tail',[(0,-.62,.88),(0,-.97,.76),(.12,-1.22,.65),(.25,-1.43,.80),(.27,-1.51,.89)],[.105,.09,.075,.06,.008],7,'Tail',10)
            for j in range(5):gem('Dorsal mane',(0,.28-j*.19,1.31-j*.025),(.12,.12,.13 if j<3 else .08),5,'Body')

        # All parts share UV semantics before joining, preserving tiny eyes/claws.
        bpy.ops.object.select_all(action='DESELECT')
        for part in parts:part.select_set(True)
        bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();model=bpy.context.object;model.name=kind+'Mesh'
        bm=bmesh.new();bm.from_mesh(model.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(model.data);bm.free()
        arm=bpy.data.armatures.new(kind+'Skeleton');rig=bpy.data.objects.new(kind,arm);bpy.context.collection.objects.link(rig)
        bpy.context.view_layer.objects.active=rig;rig.select_set(True);model.select_set(False);bpy.ops.object.mode_set(mode='EDIT')
        for name,head,tail,parent in bones:
            bone=arm.edit_bones.new(name);bone.head=head;bone.tail=tail
            if parent:bone.parent=arm.edit_bones[parent]
        bpy.ops.object.mode_set(mode='OBJECT');model.parent=rig
        modifier=model.modifiers.new('Creature rig','ARMATURE');modifier.object=rig
        rig.animation_data_create()
        for name,length in [('Idle',60),('Walk',30),('Attack',30),('Death',35)]:
            action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
            for frame in range(1,length+2,2):
                t=(frame-1)/length;wave=math.sin(t*2*math.pi)
                for b in rig.pose.bones:
                    b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0)
                    if b.name=='Body':
                        b.location.y=(.012 if name=='Idle' else .025)*wave
                        if name=='Attack':b.rotation_euler.x=-.27*math.sin(math.pi*t);b.location.z=-.12*math.sin(math.pi*t)
                        if name=='Death':b.rotation_euler.z=1.3*min(1,t*2);b.location.y=-.28*min(1,t*2)
                    if b.name.startswith('Leg') and name=='Walk':
                        offset=(int(b.name[3])+int(b.name.endswith('R')))%2*math.pi
                        b.rotation_euler.x=math.sin(t*2*math.pi+offset)*.40
                        b.rotation_euler.z=math.cos(t*2*math.pi+offset)*.12
                    if b.name=='Head':b.rotation_euler.z=wave*.05 if name=='Idle' else 0
                    if b.name=='Jaw' and name=='Attack':b.rotation_euler.x=.6*math.sin(t*math.pi)
                    if b.name=='Tail':b.rotation_euler.z=wave*.18
                    b.keyframe_insert(data_path='location',frame=frame,group=b.name)
                    b.keyframe_insert(data_path='rotation_euler',frame=frame,group=b.name)
            action.frame_end=length+1
        rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1)
        model.data.calc_loop_triangles()
        report={'name':kind,'triangles':len(model.data.loop_triangles),'vertices':len(model.data.vertices),'bones':len(bones),'materials':len(model.data.materials),'bounds_m':list(model.dimensions),'actions':['Idle','Walk','Attack','Death']}
        reports.append(report)
        image.filepath='//../../Assets/RivetReach/Resources/Mobs/CreaturePalette.png'
        bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(kind+'.blend')))
        bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);model.select_set(True);bpy.context.view_layer.objects.active=rig
        bpy.ops.export_scene.fbx(filepath=str(OUT/(kind+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
        if '--skip-renders' in sys.argv: continue
        scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
        scene.world.color=(.18,.18,.18)
        bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object
        floor_mat=bpy.data.materials.new('Studio floor');floor_mat.diffuse_color=(.13,.16,.18,1);floor.data.materials.append(floor_mat)
        bpy.ops.object.camera_add(location=(2.7,3.9,2.2));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=3.4
        for location,power,diameter in [((2,3,4),650,4),((-3,1,2.6),450,3),((1,-3,3),600,2)]:
            bpy.ops.object.light_add(type='AREA',location=location);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=diameter;light.rotation_euler=(Vector((0,0,.6))-light.location).to_track_quat('-Z','Y').to_euler()
        for view,location in [('front',(2.7,3.9,2.2)),('back',(-2.7,-3.9,2.2))]:
            camera.location=location;camera.rotation_euler=(Vector((0,0,.7))-camera.location).to_track_quat('-Z','Y').to_euler()
            scene.render.image_settings.file_format='PNG';scene.render.filepath=str(REVIEW/(kind+'-'+view+'.png'));bpy.ops.render.render(write_still=True)
    (SOURCE/'mob-asset-report.json').write_text(json.dumps(reports,indent=2))
    print(json.dumps(reports))
