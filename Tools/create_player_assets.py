"""Author the original Rivet Reach explorers, weighted rigs and animation library.
Run with Blender 5.2 --background --python <absolute path>.
Copyright (c) 2026 Starbugstone. See LICENSE.md. No third-party asset inputs.
"""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/RivetReach/Resources/Characters'
SOURCE = ROOT / 'ArtSource/Characters'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)


def atlas(name, alternate=False):
    # Shared 4x4 semantic regions; vertical bands supply restrained painted facets.
    colours = ([
        (.49,.285,.18),(.78,.57,.25),(.82,.61,.29),(.82,.61,.29),
        (.49,.285,.18),(.49,.285,.18),(.19,.255,.30),(.19,.255,.30),
        (.24,.135,.07),(.085,.056,.04),(.88,.84,.73),(.035,.029,.026),
        (.70,.46,.16),(.22,.31,.33),(.32,.13,.085),(.48,.44,.37)] if alternate else [
        (.69,.405,.225),(.80,.737,.595),(.80,.737,.595),(.80,.737,.595),
        (.69,.405,.225),(.69,.405,.225),(.235,.218,.205),(.235,.218,.205),
        (.28,.155,.075),(.105,.075,.056),(.92,.88,.77),(.036,.030,.025),
        (.72,.49,.20),(.155,.285,.295),(.39,.19,.115),(.48,.44,.37)])
    im=bpy.data.images.new(name,width=256,height=256,alpha=True)
    pixels=[]
    for y in range(256):
        for x in range(256):
            tile=y//64*4+x//64
            shade=.88+(y%64)/63*.24
            grain=(((x*13+y*7)%17)-8)*.00028
            pixels.extend([max(0,min(1,c*shade+grain)) for c in colours[tile]]+[1])
    im.pixels=pixels;im.filepath_raw=str(OUT/(name+'.png'));im.file_format='PNG';im.save()


def build(female):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    name='ExplorerFemale' if female else 'ExplorerMale'
    scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1;scene.render.fps=30
    shoulder=.225 if female else .254
    elbow=shoulder+.063;wrist=shoulder+.095
    chest=.211 if female else .235;waist=.145 if female else .167
    arm=bpy.data.armatures.new(name+'Skeleton');rig=bpy.data.objects.new(name+'Rig',arm);bpy.context.collection.objects.link(rig)
    rig.show_in_front=True;arm.display_type='OCTAHEDRAL';rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    defs=[('Root',(0,0,0),(0,0,.12),None),('Hips',(0,0,.96),(0,0,1.07),'Root'),
          ('Spine',(0,0,1.07),(0,0,1.26),'Hips'),('Chest',(0,0,1.26),(0,0,1.44),'Spine'),
          ('Neck',(0,0,1.44),(0,0,1.53),'Chest'),('Head',(0,0,1.53),(0,0,1.79),'Neck')]
    for side,s in [('L',1),('R',-1)]:
        defs.extend([
            ('Clavicle'+side,(s*.045,0,1.415),(s*shoulder,0,1.415),'Chest'),
            ('UpperArm'+side,(s*shoulder,0,1.415),(s*elbow,-.014,1.177),'Clavicle'+side),
            ('Forearm'+side,(s*elbow,-.014,1.177),(s*wrist,-.035,.947),'UpperArm'+side),
            ('Hand'+side,(s*wrist,-.035,.947),(s*wrist,-.04,.862),'Forearm'+side),
            ('Thigh'+side,(s*.101,0,.97),(s*.126,-.004,.555),'Hips'),
            ('Shin'+side,(s*.126,-.004,.555),(s*.139,0,.155),'Thigh'+side),
            ('Foot'+side,(s*.139,0,.155),(s*.139,-.155,.075),'Shin'+side)])
        for f in range(4):
            fx=s*(wrist-.034+f*.023)
            length=[.068,.077,.071,.057][f]
            defs.extend([('Finger%dA%s'%(f,side),(fx,-.039,.873),(fx,-.05,.873-length*.55),'Hand'+side),
                         ('Finger%dB%s'%(f,side),(fx,-.05,.873-length*.55),(fx,-.070,.873-length),'Finger%dA%s'%(f,side))])
        defs.extend([('ThumbA'+side,(s*(wrist-.044),-.042,.921),(s*(wrist-.065),-.067,.895),'Hand'+side),
                     ('ThumbB'+side,(s*(wrist-.065),-.067,.895),(s*(wrist-.060),-.084,.868),'ThumbA'+side)])
    for n,h,t,parent in defs:
        b=arm.edit_bones.new(n);b.head=h;b.tail=t
        if parent:b.parent=arm.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
    parts=[]

    def mesh(n,verts,faces,tile,weights,facets=False):
        data=bpy.data.meshes.new(n);data.from_pydata(verts,[],faces);data.update()
        o=bpy.data.objects.new(n,data);bpy.context.collection.objects.link(o)
        uv=data.uv_layers.new(name='SkinUV')
        for p in data.polygons:
            shade=((p.index*37+len(n)*7)%11-5)*.031 if facets else 0
            for j,li in enumerate(p.loop_indices):
                u,v=[(.42,.48),(.58,.48),(.58,.52),(.42,.52)][j%4]
                uv.data[li].uv=((tile%4+u)/4,(tile//4+v+shade)/4)
        for v in data.vertices:
            ws={weights:1} if isinstance(weights,str) else weights(v.co)
            for bone,w in ws.items():
                if w>0:
                    g=o.vertex_groups.get(bone) or o.vertex_groups.new(name=bone);g.add([v.index],w,'REPLACE')
        parts.append(o);return o

    def rings(n,profile,tile,weights,sides=12,facets=False,phase=0,cap=True):
        vs=[]
        for j,(x,y,z,rx,ry) in enumerate(profile):
            for i in range(sides):
                a=math.tau*i/sides+phase
                variation=1+(math.sin(i*7+j*11)*.045 if facets and 0<j<len(profile)-1 else 0)
                vs.append((x+rx*math.cos(a)*variation,y+ry*math.sin(a)*variation,z))
        fs=[tuple(reversed(range(sides)))] if cap else []
        for j in range(len(profile)-1):
            for i in range(sides):
                a=j*sides+i;b=j*sides+(i+1)%sides;c=b+sides;d=a+sides
                if facets and (i+j)%3!=0:fs.extend([(a,b,d),(b,c,d)])
                else:fs.append((a,b,c,d))
        if cap:fs.append(tuple((len(profile)-1)*sides+i for i in range(sides)))
        return mesh(n,vs,fs,tile,weights,facets)

    def panel(n,points,tile,weights,thickness=.003):
        vs=list(points)+[(x,y+thickness,z) for x,y,z in points];c=len(points)
        fs=[tuple(reversed(range(c))),tuple(range(c,c*2))]+[(i,(i+1)%c,(i+1)%c+c,i+c) for i in range(c)]
        return mesh(n,vs,fs,tile,weights)

    def bevel(n,loc,size,tile,bone,width=.005):
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=n;o.scale=size
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        if width:
            m=o.modifiers.new('Edge planes','BEVEL');m.width=width;m.segments=1;bpy.ops.object.modifier_apply(modifier=m.name)
        vs=[v.co.copy() for v in o.data.vertices];fs=[tuple(p.vertices) for p in o.data.polygons]
        bpy.data.objects.remove(o,do_unlink=True)
        return mesh(n,vs,fs,tile,bone)

    def sweep(n,path,widths,depths,tile,bone,sides=8):
        vs=[]
        for j,p in enumerate(path):
            v=Vector(p);t=(Vector(path[min(j+1,len(path)-1)])-Vector(path[max(0,j-1)])).normalized()
            u=Vector((1,0,0));u=(u-t*u.dot(t)).normalized();d=t.cross(u).normalized()
            for i in range(sides):
                a=math.tau*i/sides;vs.append(v+u*math.cos(a)*widths[j]+d*math.sin(a)*depths[j])
        fs=[tuple(reversed(range(sides)))]
        for j in range(len(path)-1):
            for i in range(sides):fs.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
        fs.append(tuple((len(path)-1)*sides+i for i in range(sides)))
        return mesh(n,vs,fs,tile,bone,True)

    def torso_weights(v):
        z=v.z
        if z<1.08:return {'Hips':1}
        if z<1.25:
            a=(z-1.08)/.17;return {'Spine':1-a,'Chest':a}
        return {'Chest':1}

    # The shirt and vest are shaped shells around the same torso profile.
    rings('Linen shirt',[(0,0,1.03,waist,.097),(0,0,1.17,waist+.016,.106),
        (0,0,1.32,chest-.005,.121),(0,.005,1.415,chest-.016,.101),(0,0,1.465,.058,.063)],1,torso_weights,16,True)
    profile=[(1.035,waist+.014,.111),(1.13,waist+.012,.117),(1.255,chest-.012,.145),(1.35,chest+.012,.148),(1.44,chest-.023,.117)]
    vs=[];sides=16
    for j,(z,rx,ry) in enumerate(profile):
        for i in range(sides):
            a=math.tau*i/sides;x=rx*math.cos(a);y=ry*math.sin(a)
            front=max(0,-math.sin(a))
            if j==0:zv=z-.035*front*(1-abs(math.cos(a))*.5)+(.046 if i==12 else 0)
            elif j==4:zv=z-.135*(front**10)
            elif j==3:zv=z-.043*(front**12)
            else:zv=z
            vs.append((x,y,zv))
    fs=[]
    for j in range(4):
        for i in range(sides):
            a=j*sides+i;b=j*sides+(i+1)%sides;c=b+sides;d=a+sides
            fs.extend([(a,b,d),(b,c,d)])
    mesh('Tailored waistcoat shell',vs,fs,13,torso_weights,True)
    for s in [-1,1]:
        panel('Waistcoat neckline facing',[(s*.004,-.155,1.302),(s*.079,-.114,1.436),(s*.094,-.120,1.428),(s*.025,-.158,1.315)],13,torso_weights)
        panel('Open linen collar',[(s*.013,-.070,1.471),(s*.055,-.049,1.488),(s*.088,-.103,1.433),(s*.059,-.116,1.411)],1,'Chest')
        panel('Welt pocket',[(s*.074,-.115,1.095),(s*.14,-.082,1.103),(s*.14,-.084,1.116),(s*.074,-.117,1.108)],13,torso_weights)
        bevel('Waistcoat back adjustment',(s*.057,.117,1.093),(.105,.012,.021),13,'Spine',.004)
    for z,y in [(1.094,-.128),(1.173,-.145),(1.252,-.153)]:
        sweep('Domed brass button',[(.008,y+.002,z),(.008,y-.008,z),(.008,y-.01,z)],[.013,.012,.008],[.013,.012,.008],12,torso_weights,8)
    bevel('Back strap clasp',(0,.128,1.094),(.027,.009,.025),12,'Spine',.003)
    rings('Neck',[(0,.004,1.44,.055,.059),(0,.006,1.55,.059,.063),(0,0,1.575,.066,.067)],0,'Neck',12)
    # Deliberate facial planes, with broad cheeks, a tapered jaw and flatter front.
    hw=.101 if female else .109
    head=[(1.543,.060 if female else .073,.063,-.009),(1.568,.083 if female else .098,.079,-.006),
          (1.612,hw,.090,0),(1.654,hw+.006,.095,.005),(1.694,hw+.004,.092,.008),
          (1.734,hw-.003,.086,.015),(1.765,.078,.066,.018),(1.78,.033,.033,.02)]
    vs=[]
    # Front has a flat centre and cheek wings. Back remains round.
    section=[(1,0),(.94,-.53),(.68,-.88),(.35,-1),(0,-1.03),(-.35,-1),(-.68,-.88),(-.94,-.53),(-1,0),(-.87,.62),(-.5,.93),(0,1),(.5,.93),(.87,.62)]
    for z,rx,ry,cy in head:
        vs.extend([(x*rx,cy+y*ry,z) for x,y in section])
    n=len(section);fs=[tuple(reversed(range(n)))]
    for j in range(len(head)-1):
        for i in range(n):
            a=j*n+i;b=j*n+(i+1)%n;c=b+n;d=a+n
            fs.extend([(a,b,c),(a,c,d)]) if j in [1,2] and i in [0,1,6,7] else fs.append((a,b,c,d))
    fs.append(tuple((len(head)-1)*n+i for i in range(n)))
    mesh('Face jaw and cranium',vs,fs,0,'Head',True)
    for s in [-1,1]:
        cx=s*.044;z=1.675
        # Eyes follow the face plane; dark upper lid and minimal white below iris.
        points=[(cx-.026,-.089,z),(cx-.016,-.096,z+.012),(cx+.013,-.096,z+.012),(cx+.026,-.087,z+.001),(cx+.013,-.095,z-.008),(cx-.013,-.095,z-.007)]
        panel('Eye recess',points,9,'Head',.001)
        panel('Almond eye',[(cx-.022,-.093,z),(cx-.013,-.099,z+.009),(cx+.012,-.099,z+.009),(cx+.022,-.092,z),(cx+.012,-.099,z-.006),(cx-.012,-.099,z-.005)],10,'Head',.001)
        bevel('Warm iris',(cx+s*.002,-.102,z),(.016,.003,.016),8,'Head',.004)
        bevel('Pupil',(cx+s*.002,-.104,z+.001),(.008,.002,.012),11,'Head',.002)
        bevel('Eye catchlight',(cx-.003,-.106,z+.004),(.003,.002,.003),10,'Head',0)
        panel('Upper eyelid',[(cx-.027,-.09,z+.001),(cx-.016,-.10,z+.013),(cx+.014,-.10,z+.013),(cx+.027,-.088,z+.002),(cx+.016,-.099,z+.008),(cx-.014,-.099,z+.009)],9,'Head',.001)
        panel('Sculpted brow',[(cx-.029,-.083,z+.022),(cx-.018,-.098,z+.031),(cx+.022,-.091,z+.028),(cx+.028,-.081,z+.018),(cx+.008,-.097,z+.023)],9,'Head')
        rings('Ear',[(s*(hw+.002),.004,1.605,.014,.014),(s*(hw+.011),.001,1.633,.023,.018),(s*(hw+.004),.004,1.657,.017,.017)],0,'Head',8)
        panel('Ear inner plane',[(s*(hw+.007),-.012,1.618),(s*(hw+.023),-.014,1.637),(s*(hw+.008),-.014,1.645)],14,'Head')
    mesh('Nose planes',[(-.012,-.093,1.695),(.012,-.093,1.695),(-.014,-.113,1.644),(.014,-.113,1.644),(0,-.132,1.638),(-.022,-.103,1.630),(.022,-.103,1.630),(0,-.106,1.622)],[(0,1,3,2),(2,3,4),(0,2,5),(1,6,3),(2,4,5),(3,6,4),(4,7,5),(4,6,7),(5,7,6)],0,'Head')
    panel('Relaxed mouth',[(-.027,-.098,1.596),(-.009,-.104,1.594),(.011,-.104,1.595),(.027,-.098,1.6),(.016,-.104,1.591),(-.012,-.105,1.59)],14,'Head',.001)
    # Hair cap is cut to a hairline; swept tapered locks define the silhouette.
    hp=[]
    for j in range(4):
        for i in range(16):
            a=math.tau*i/16
            front=max(0,-math.sin(a));back=max(0,math.sin(a))
            z=[1.686+front*.033-back*.024,1.759,1.793,1.813][j]
            rx=[hw+.009,hw+.021,.089,.028][j];ry=[.094,.102,.076,.028][j]
            hp.append((rx*math.cos(a),.022+ry*math.sin(a),z))
    fs=[]
    for j in range(3):
        for i in range(16):fs.append((j*16+i,j*16+(i+1)%16,(j+1)*16+(i+1)%16,(j+1)*16+i))
    fs.append(tuple(range(48,64)));mesh('Fitted hair mass',hp,fs,9,'Head',True)
    if female:
        for i in range(5):
            x=.073-i*.035
            sweep('Side swept fringe',[(x,.038,1.781),(x-.020,-.019,1.797),(x-.044,-.083,1.749),(x-.066,-.089,1.699-i*.006)], [.023,.029,.024,.001],[.015,.020,.016,.001],9,'Head',6)
        for s in [-1,1]:
            sweep('Tucked temple lock',[(s*.102,.004,1.746),(s*.12,-.008,1.701),(s*.111,-.012,1.647),(s*.091,-.024,1.614)],[.023,.024,.017,.001],[.023,.018,.012,.001],9,'Head',6)
        sweep('Low gathered ponytail',[(0,.094,1.691),(-.022,.142,1.622),(-.040,.18,1.555),(-.055,.195,1.484),(-.033,.18,1.423)],[.042,.066,.055,.042,.003],[.035,.05,.043,.028,.003],9,'Head',10)
        rings('Leather hair tie',[(0,.123,1.627,.043,.027),(0,.122,1.644,.045,.03)],8,'Head',10)
        for i in range(3):
            x=-.05+i*.036
            sweep('Ponytail crease',[(x,.165,1.622),(x-.019,.215,1.54),(x-.025,.208,1.472)],[.016,.019,.001],[.009,.012,.001],9,'Head',6)
    else:
        for i,(x,y,z,tx,ty,tz,w) in enumerate([
            (-.066,.032,1.77,.038,-.108,1.752,.035),
            (-.028,.028,1.791,.080,-.089,1.733,.037),
            (.013,.037,1.792,.111,-.044,1.725,.034),
            (-.088,.014,1.75,-.039,-.112,1.716,.033),
            (-.048,.073,1.758,.083,.045,1.768,.034),
            (-.094,.06,1.725,-.081,-.053,1.69,.027)]):
            sweep('Swept sculpted hair lock',[(x,y,z),(x+(tx-x)*.35,y+(ty-y)*.35,z+.019),(tx,ty,tz)], [w,w*.92,.001],[.017,.021,.001],9,'Head',6)
        for s in [-1,1]:
            for j in range(2):
                sweep('Temple hair',[(s*.098,.025+j*.036,1.75),(s*.119,.013+j*.03,1.714),(s*.111,-.014+j*.028,1.668)],[.026,.022,.002],[.018,.015,.001],9,'Head',6)
    for side,s in [('L',1),('R',-1)]:
        for j in range(3):
            sweep('Combed rear hair',[(s*(.035+j*.028),.035,1.785-j*.014),(s*(.053+j*.018),.092,1.738-j*.014),(s*(.041+j*.021),.102,1.667+j*.006)],[.024,.027,.002],[.013,.015,.001],9,'Head',6)
    # Pelvis, trousers and articulated limbs.
    rings('Belt',[(0,0,1.015,waist+.01,.111),(0,0,1.04,waist+.01,.111)],8,'Hips',16)
    bevel('Belt buckle',(0,-.116,1.028),(.039,.011,.03),12,'Hips',.003)
    for side,s in [('L',1),('R',-1)]:
        sx=s*shoulder;ex=s*elbow;wx=s*wrist;skin=4 if s==1 else 5;sleeve=2 if s==1 else 3;trouser=6 if s==1 else 7
        def arm_weights(v):
            t=max(0,min(1,(v.z-1.135)/.083));return {'UpperArm'+side:t,'Forearm'+side:1-t}
        def fore_weights(v):
            t=max(0,min(1,(v.z-.947)/.035));return {'Forearm'+side:t,'Hand'+side:1-t}
        rings('Rolled linen sleeve '+side,[(ex,-.014,1.15,.078,.074),(ex,-.012,1.196,.087,.08),
            (s*(shoulder+.045),0,1.251,.103,.088),(sx,.002,1.348,.103,.093),(sx-s*.018,.01,1.42,.079,.081),
            (sx-s*.032,.012,1.442,.035,.04)],sleeve,arm_weights,10,True)
        rings('Folded cuff '+side,[(ex,-.014,1.15,.085,.080),(ex,-.014,1.16,.096,.089),(ex,-.012,1.19,.096,.09),(ex,-.012,1.20,.082,.077)],sleeve,arm_weights,10,True)
        rings('Forearm anatomy '+side,[(wx,-.035,.947,.039,.036),(wx,-.03,.992,.044,.041),
            (s*(elbow+.023),-.017,1.07,.067,.059),(ex,-.014,1.132,.076,.068),(ex,-.014,1.169,.065,.061)],skin,fore_weights,10,True)
        rings('Palm '+side,[(wx,-.039,.871,.047,.028),(wx,-.031,.897,.050,.032),(wx,-.035,.937,.043,.032),(wx,-.035,.953,.037,.033)],skin,'Hand'+side,10)
        for f in range(4):
            a=arm.bones['Finger%dA%s'%(f,side)];b=arm.bones['Finger%dB%s'%(f,side)]
            path=[a.head_local, a.head_local.lerp(a.tail_local,.55), a.tail_local, b.tail_local]
            def fw(v,a=a,b=b):
                t=max(0,min(1,(a.tail_local.z+.007-v.z)/.014));return {a.name:1-t,b.name:t}
            sweep('Finger %d %s'%(f,side),path,[.0105,.011,.010,.007],[.013,.014,.013,.008],skin,fw,6)
        a=arm.bones['ThumbA'+side];b=arm.bones['ThumbB'+side]
        sweep('Opposable thumb '+side,[a.head_local,a.tail_local,b.tail_local],[.019,.016,.012],[.018,.016,.011],skin,lambda v:{a.name:1} if v.z>.89 else {b.name:1},8)
        hx=s*.101;kx=s*.126;ax=s*.139
        def leg_weights(v):
            if v.z>.9:
                t=min(1,(v.z-.9)/.09);return {'Thigh'+side:1-t,'Hips':t}
            t=max(0,min(1,(v.z-.505)/.10));return {'Thigh'+side:t,'Shin'+side:1-t}
        leg=rings('Tailored trouser leg '+side,[(ax,0,.265,.070,.072),(ax,.007,.34,.081,.08),(s*.135,.012,.438,.087,.087),
            (kx,-.005,.53,.081,.084),(kx,-.014,.586,.088,.09),(s*.118,0,.685,.102,.102),
            (s*.109,.002,.82,.12 if female else .115,.108),(hx,0,.95,.104,.108)],trouser,leg_weights,12,True)
        # Open the hip end for the shared pelvis shell.
        import bmesh
        bm=bmesh.new();bm.from_mesh(leg.data);bm.faces.ensure_lookup_table();bm.faces.remove(bm.faces[-1]);bm.to_mesh(leg.data);bm.free()
        panel('Back trouser pocket '+side,[(hx-.037,.112,.945),(hx+.038,.112,.945),(hx+.034,.119,.886),(hx,.125,.874),(hx-.034,.119,.886)],trouser,'Hips')
        # Boots have one continuous vamp outline, a raised cuff and a thin welt/sole.
        boot=[(ax,-.071,.025,.089,.151),(ax,-.072,.049,.091,.154),(ax,-.072,.061,.087,.150),
              (ax,-.083,.103,.084,.132),(ax,-.060,.141,.076,.111),(ax,-.009,.18,.073,.079),(ax,0,.259,.079,.08)]
        def boot_weights(v):
            t=max(0,min(1,(v.z-.14)/.08));return {'Foot'+side:1-t,'Shin'+side:t}
        rings('Shaped boot '+side,boot,8,boot_weights,12,True)
        rings('Boot sole '+side,[(ax,-.073,.005,.089,.151),(ax,-.073,.024,.093,.157),(ax,-.073,.031,.09,.154)],11,'Foot'+side,12)
        rings('Stitched welt '+side,[(ax,-.073,.032,.092,.155),(ax,-.073,.043,.092,.155)],8,'Foot'+side,12)
        rings('Folded leather cuff '+side,[(ax,0,.244,.092,.089),(ax,0,.258,.102,.096),(ax,0,.289,.1,.095),(ax,0,.301,.09,.087)],8,'Shin'+side,12,True)
        for z,y in [(.115,-.173),(.153,-.12),(.19,-.083),(.223,-.081)]:
            panel('Boot leather strap '+side,[(ax-.057,y+.007,z-.008),(ax+.057,y+.007,z-.008),(ax+.057,y,z+.008),(ax-.057,y,z+.008)],8,boot_weights)
        bevel('Boot side buckle '+side,(ax+s*.077,-.025,.209),(.012,.027,.022),12,'Foot'+side,.002)
    boundary=[]
    for sign,ids in [(1,list(range(9,12))+list(range(0,4))),(-1,list(range(3,10)))]:
        for i in ids:
            a=math.tau*i/12;boundary.append((sign*.101+.104*math.cos(a),.108*math.sin(a),.95))
    vs=list(boundary);n=len(boundary)
    for x,y,z in boundary:
        a=math.atan2(y/.108,x/.20);vs.append(((waist+.006)*math.cos(a),.105*math.sin(a),1.042))
    fs=[]
    for i in range(n):fs.append((i,(i+1)%n,(i+1)%n+n,i+n))
    # Close the small inner saddle between the two leg openings.
    for sign,ids in [(1,list(range(3,10))),(-1,list(range(9,12))+list(range(0,4)))]:
        start=len(vs)
        for i in ids:
            a=math.tau*i/12;vs.append((sign*.101+.104*math.cos(a),.108*math.sin(a),.95))
        centre=len(vs);vs.append((0,0,.895))
        for i in range(len(ids)-1):fs.append((start+i,start+i+1,centre))
    def pelvis_weights(v):
        if v.z>=1.0:return {'Hips':1}
        return {'Hips':.56,'ThighL' if v.x>0 else 'ThighR':.44}
    mesh('Continuous trouser pelvis',vs,fs,6,pelvis_weights,True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();model=bpy.context.object;model.name=name+'Body'
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.00005);bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    mat=bpy.data.materials.new('PlayerSkin');mat.use_nodes=True
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'SkinField.png'));tex.image.filepath=bpy.path.relpath(str(OUT/'SkinField.png'),start=str(SOURCE));tex.interpolation='Linear'
    mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes['Principled BSDF'].inputs['Base Color']);mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.82
    model.data.materials.clear();model.data.materials.append(mat)
    mod=model.modifiers.new('Weighted explorer skeleton','ARMATURE');mod.object=rig;model.parent=rig
    clips=animate(rig,shoulder,female)
    rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1)
    rig.select_set(True);model.select_set(True);bpy.context.view_layer.objects.active=rig
    rig['authoring_notes']='Blender +Z up, -Y forward. In-place clips; gameplay owns movement. FP clips use the same hands with articulated fingers and controlled wrist roll.'
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_armature_deform_only=False)
    model.data.calc_loop_triangles()
    weights=[sum(g.weight for g in v.groups) for v in model.data.vertices]
    assert all(abs(w-1)<.001 for w in weights),'Unnormalized vertex weights'
    assert len(model.data.uv_layers)==1
    return {'model':name,'triangles':len(model.data.loop_triangles),'bones':len(arm.bones),'materials':len(model.data.materials),'maxInfluences':max(len(v.groups) for v in model.data.vertices),'clips':clips}


def animate(rig,shoulder,female):
    scene=bpy.context.scene;bones=rig.pose.bones
    rig.animation_data_create()
    def reset():
        for b in bones:b.rotation_mode='QUATERNION';b.matrix_basis=Matrix.Identity(4)
    def rotate(n,x=0,y=0,z=0):
        # Rotate in armature axes, then convert through the bone's rest basis.
        from mathutils import Euler
        b=bones[n];q=Euler(tuple(math.radians(a) for a in (x,y,z)),'XYZ').to_quaternion()
        basis=b.bone.matrix_local.to_quaternion();b.rotation_quaternion=basis.inverted() @ q @ basis
    def curl(side,strength):
        for f in range(4):
            rotate('Finger%dA%s'%(f,side),strength*(76-f*2))
            rotate('Finger%dB%s'%(f,side),strength*86)
        b=bones['ThumbA'+side];basis=b.bone.matrix_local.to_quaternion()
        direction=b.bone.tail_local-b.bone.head_local
        q=direction.rotation_difference(Vector(((1 if side=='L' else -1)*.014,.025,-.024)))
        b.rotation_quaternion=basis.inverted() @ Quaternion().slerp(q,strength) @ basis
        rotate('ThumbB'+side,strength*48)
    def aim(n,target):
        b=bones[n];scene.view_layers[0].update();m=b.matrix.copy();q=(b.tail-b.head).rotation_difference(Vector(target)-b.head);b.matrix=Matrix.Translation(b.head) @ q.to_matrix().to_4x4() @ m.to_3x3().to_4x4()
        scene.view_layers[0].update()
    def arm_ik(side,target,pole,hand_direction,palm_normal):
        # Two-bone analytic IK, baked into the exported action. Roll is explicitly fixed at the wrist.
        u=bones['UpperArm'+side];f=bones['Forearm'+side];h=bones['Hand'+side]
        scene.view_layers[0].update();root=u.head.copy();target=Vector(target)
        d=target-root;distance=min(d.length,u.length+f.length-.001);direction=d.normalized()
        along=(u.length*u.length-f.length*f.length+distance*distance)/(2*distance)
        bend=(Vector(pole)-root);bend=(bend-direction*bend.dot(direction)).normalized()
        joint=root+direction*along+bend*math.sqrt(max(0,u.length*u.length-along*along))
        aim(u.name,joint);aim(f.name,target)
        y=Vector(hand_direction).normalized();z=Vector(palm_normal);z=(z-y*z.dot(y)).normalized();x=y.cross(z).normalized()
        h.matrix=Matrix.Translation(h.head) @ Matrix((x,y,z)).transposed().to_4x4();scene.view_layers[0].update()
    specs=[('Idle',90),('Walk',30),('Run',24),('Airborne',30),('Mine',18),('FP_Idle',90),('FP_Walk',30),('FP_Mine',18)]
    for name,length in specs:
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        scene.frame_start=1;scene.frame_end=length+1
        for frame in range(1,length+2):
            t=(frame-1)/length;a=t*math.tau;reset()
            for side in ['L','R']:curl(side,.26)
            if name in ['Idle','Walk','Run','Airborne','Mine']:
                rotate('Chest',0,.5*math.sin(a),.7*math.sin(a))
                rotate('Head',0,0,-1+.6*math.sin(a))
                if name in ['Walk','Run']:
                    run=name=='Run';swing=math.sin(a)*(34 if run else 23)
                    bones['Hips'].location.z=.012*(1-math.cos(a*2))
                    rotate('Spine',5 if run else 1.5,0,math.sin(a)*2)
                    for side,s in [('L',1),('R',-1)]:
                        phase=a if s==1 else a+math.pi
                        rotate('Thigh'+side,s*swing);rotate('Shin'+side,-max(0,math.sin(phase))*(64 if run else 39))
                        rotate('Foot'+side,-s*swing+max(0,math.sin(phase))*(44 if run else 29))
                        rotate('UpperArm'+side,-s*swing*.8);rotate('Forearm'+side,(-48 if run else -10)-max(0,s*swing)*.25)
                        curl(side,.45 if run else .18)
                elif name=='Airborne':
                    rotate('ThighL',-16);rotate('ThighR',12);rotate('ShinL',-32);rotate('ShinR',-20)
                    rotate('UpperArmL',-18,8);rotate('UpperArmR',-18,-8);rotate('ForearmL',-24);rotate('ForearmR',-24)
                elif name=='Mine':
                    hit=math.sin(math.pi*min(1,t/.45)) if t<.45 else 0
                    rotate('Chest',-4+hit*9,0,hit*-8)
                    arm_ik('R',(-.17,-.30-hit*.14,1.32+hit*.045),(-.45,-.1,1.15),(0,-1,.12),(0,0,-1));curl('R',1)
                    rotate('ForearmL',-12)
            else:
                hit=(math.sin(math.pi*min(1,t/.48))**1.5) if name=='FP_Mine' and t<.48 else 0
                bob=math.sin(a)*.008 if name=='FP_Walk' else math.sin(a)*.002
                for side,s in [('L',1),('R',-1)]:
                    strike=hit if side=='R' else 0
                    # Hands sit at the lower corners; knuckles face forwards, thumb side up/inwards.
                    target=(s*(.235-strike*.075),-.34-strike*.15,1.265+bob+strike*.06)
                    arm_ik(side,target,(s*.46,.03,1.08),(-s*.15,-.95,.26),(s*.30,-.28,-.94))
                    curl(side,1)
            for b in bones:
                b.keyframe_insert(data_path='location',frame=frame,group=b.name)
                b.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=b.name)
                b.keyframe_insert(data_path='scale',frame=frame,group=b.name)
        action['loop']=name not in ['Mine','FP_Mine']
    scene.frame_start=1;scene.frame_end=91
    return [{'name':n,'seconds':f/30,'loop':n not in ['Mine','FP_Mine']} for n,f in specs]


atlas('SkinField');atlas('SkinOchre',True)
report=[build(False),build(True)]
(SOURCE/'asset-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('RIVET_ASSETS '+json.dumps(report))
