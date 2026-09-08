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
    elbow=shoulder+.045;wrist=shoulder+.072
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
            shade=((p.index*37+len(n)*7)%11-5)*.007 if facets else 0
            p.use_smooth=True
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
                variation=1+(math.sin(i*2+j*1.7)*.022 if facets and 0<j<len(profile)-1 else 0)
                vs.append((x+rx*math.cos(a)*variation,y+ry*math.sin(a)*variation,z))
        fs=[tuple(reversed(range(sides)))] if cap else []
        for j in range(len(profile)-1):
            for i in range(sides):
                a=j*sides+i;b=j*sides+(i+1)%sides;c=b+sides;d=a+sides
                fs.append((a,b,c,d))
        if cap:fs.append(tuple((len(profile)-1)*sides+i for i in range(sides)))
        return mesh(n,vs,fs,tile,weights,facets)

    def panel(n,points,tile,weights,thickness=.003):
        vs=list(points)+[(x,y+thickness,z) for x,y,z in points];c=len(points)
        fs=[tuple(reversed(range(c))),tuple(range(c,c*2))]+[(i,(i+1)%c,(i+1)%c+c,i+c) for i in range(c)]
        o=mesh(n,vs,fs,tile,weights)
        for face in o.data.polygons:face.use_smooth=False
        return o

    def bevel(n,loc,size,tile,bone,width=.005):
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=n;o.scale=size
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        if width:
            m=o.modifiers.new('Edge planes','BEVEL');m.width=width;m.segments=3;bpy.ops.object.modifier_apply(modifier=m.name)
        vs=[v.co.copy() for v in o.data.vertices];fs=[tuple(p.vertices) for p in o.data.polygons]
        bpy.data.objects.remove(o,do_unlink=True)
        o=mesh(n,vs,fs,tile,bone)
        for face in o.data.polygons:face.use_smooth=False
        return o

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

    # Fitted garments: rounded support loops preserve the tailored hem and armholes.
    shirt=rings('Linen shirt',[(0,0,1.032,waist,.094),(0,0,1.12,waist+.004,.102),
        (0,0,1.26,chest-.024,.119),(0,.002,1.365,chest-.015,.109),
        (0,.004,1.424,chest-.020,.098),(0,.002,1.442,.113,.078),(0,.002,1.464,.058,.056)],1,torso_weights,24,cap=False)
    for v in shirt.data.vertices:
        if v.co.z>1.441 and v.co.y<0:
            v.co.z-=.058*max(0,1-abs(v.co.x)/.085)**2
    profile=[(1.038,waist+.012,.110),(1.05,waist+.015,.113),(1.13,waist+.010,.115),
             (1.255,chest-.022,.133),(1.35,chest-.004,.137),(1.438,chest-.018,.111)]
    vs=[];sides=32
    for j,(z,rx,ry) in enumerate(profile):
        for i in range(sides):
            a=math.tau*i/sides;x=rx*math.cos(a);y=ry*math.sin(a);front=max(0,-math.sin(a))
            # Two pointed front panels with a deliberate centre opening.
            hem=-.032*front+.035*front**24-.023*math.exp(-((abs(x)-.065)/.04)**2)*front
            if j<2:zv=z+hem
            elif j==5:
                zv=z-.138*front**12-.095*abs(math.cos(a))**14
            elif j==4:zv=z-.048*front**16
            else:zv=z
            vs.append((x,y,zv))
    fs=[]
    for j in range(len(profile)-1):
        for i in range(sides):fs.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
    vest=mesh('Tailored waistcoat shell',vs,fs,13,torso_weights)
    for side in [-1,1]:
        panel('Waistcoat neckline facing',[(side*.003,-.137,1.300),(side*.079,-.105,1.426),(side*.091,-.110,1.419),(side*.023,-.140,1.305)],13,torso_weights,.005)
        panel('Open linen collar',[(side*.012,-.056,1.477),(side*.052,-.043,1.484),(side*.092,-.098,1.426),(side*.058,-.111,1.405),(side*.035,-.082,1.45)],1,'Chest',.005)
        panel('Welt pocket',[(side*.066,-.109,1.086),(side*.137,-.082,1.094),(side*.138,-.083,1.107),(side*.066,-.111,1.099)],13,torso_weights,.006)
        bevel('Waistcoat back adjustment',(side*.053,.114,1.089),(.1,.012,.018),13,'Spine',.003)
        sweep('Waistcoat side seam',[(side*.128,-.078,1.047),(side*.143,-.081,1.15),(side*.174,-.078,1.285),(side*.183,-.078,1.365)],[.0015]*4,[.0015]*4,13,torso_weights,6)
    for z,y in [(1.083,-.12),(1.161,-.126),(1.24,-.138)]:
        sweep('Domed brass button',[(.007,y+.002,z),(.007,y-.006,z),(.007,y-.009,z)],[.011,.011,.007],[.011,.011,.007],12,torso_weights,12)
    bevel('Back strap clasp',(0,.125,1.09),(.025,.009,.021),12,'Spine',.003)
    rings('Neck',[(0,-.002,1.393,.048,.087),(0,.007,1.444,.053,.052),(0,.005,1.48,.052,.055),(0,.009,1.526,.058,.059),(0,.012,1.556,.065,.063)],0,'Neck',24)
    # Dense facial loops shape the cheek, orbit, nose, muzzle and jaw as one surface.
    hw=.098 if female else .109
    head=[(1.518,.056 if female else .067,.062,-.002),(1.529,.069 if female else .080,.072,-.002),
          (1.548,.081 if female else .095,.080,.0),(1.573,hw-.003,.088,.003),
          (1.606,hw+.003,.093,.006),(1.633,hw+.005,.098,.010),
          (1.660,hw+.003,.096,.013),(1.69,hw,.094,.014),(1.722,hw-.001,.091,.018),
          (1.749,hw-.010,.082,.019),(1.769,.075,.066,.018),(1.783,.038,.035,.019),(1.787,.006,.006,.019)]
    def face_profile(z):
        for low,high in zip(head,head[1:]):
            if low[0]<=z<=high[0]:
                t=(z-low[0])/(high[0]-low[0]);return tuple(low[k]*(1-t)+high[k]*t for k in [1,2,3])
        return head[-1][1:]
    def face_y(x,z):
        rx,ry,cy=face_profile(z);u=min(.999,abs(x)/rx)
        y=cy-ry*math.sqrt(1-u*u)
        # Eye sockets sit behind the bridge and the cheekbones.
        y+=.009*math.exp(-((abs(x)-.043)/.027)**2-((z-1.665)/.020)**2)
        y-=.009*math.exp(-((abs(x)-.063)/.034)**2-((z-1.63)/.022)**2)
        y-=.012*math.exp(-(x/.027)**2-((z-1.603)/.025)**2)
        y-=.022*math.exp(-(x/.013)**2-((z-1.659)/.036)**2)
        y-=.034*math.exp(-(x/.019)**2-((z-1.629)/.013)**2)
        return y
    levels=sorted(set([a[0] for a in head]+[1.53+i*.007 for i in range(36)]))
    vs=[];n=64
    for z in levels:
        rx,ry,cy=face_profile(z)
        for i in range(n):
            a=math.tau*i/n;x=rx*math.cos(a);y=cy+ry*math.sin(a)
            if math.sin(a)<0:y=face_y(x,z)
            vs.append((x,y,z))
    fs=[tuple(reversed(range(n)))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(levels)-1) for i in range(n)]
    fs.append(tuple((len(levels)-1)*n+i for i in range(n)))
    mesh('Sculpted facial topology',vs,fs,0,'Head')
    def disc(n,cx,y,z,rx,rz,tile):
        vs=[(cx,y-.001,z)]+[(cx+rx*math.cos(a*math.tau/20),y,z+rz*math.sin(a*math.tau/20)) for a in range(20)]
        return mesh(n,vs,[(0,i+1,(i+1)%20+1) for i in range(20)],tile,'Head')
    for side in [-1,1]:
        cx=side*.043;z=1.666
        # Convex almond eye, bounded by skin lids. Iris and pupil follow its dome.
        outline=[]
        for i in range(24):
            a=i*math.tau/24;x=cx+.026*math.cos(a);zz=z+(.009 if math.sin(a)>0 else .006)*math.sin(a)+side*(x-cx)*.04
            outline.append((x,face_y(x,zz)-.002,zz))
        eye_y=face_y(cx,z)-.003
        ev=[(cx,eye_y,z)]
        for scale in [.45,.8,1]:
            for x,y,zz in outline:ev.append((cx+(x-cx)*scale,eye_y*(1-scale*scale)+y*scale*scale,z+(zz-z)*scale))
        ef=[(0,1+i,1+(i+1)%24) for i in range(24)]
        for j in range(2):
            for i in range(24):ef.append((1+j*24+i,1+j*24+(i+1)%24,1+(j+1)*24+(i+1)%24,1+(j+1)*24+i))
        mesh('Almond sclera',ev,ef,10,'Head')
        disc('Iris limbal rim',cx,eye_y-.001,z,.0105,.010,9)
        disc('Amber iris',cx,eye_y-.002,z,.0088,.0088,8)
        disc('Pupil',cx,eye_y-.003,z,.005,.0068,11)
        disc('Eye catchlight',cx-.0025,eye_y-.004,z+.0035,.0016,.0018,10)
        top=outline[:13];bottom=outline[12:]+[outline[0]]
        sweep('Upper eyelid',[(x,y-.001,zz+.001) for x,y,zz in top],[.0023]*len(top),[.0028]*len(top),0,'Head',8)
        sweep('Lower eyelid',[(x,y,zz-.001) for x,y,zz in bottom],[.0014]*len(bottom),[.002]*len(bottom),0,'Head',8)
        brow=[]
        for x,zz in [(cx-side*.030,z+.020),(cx-side*.014,z+.030),(cx+side*.011,z+.028),(cx+side*.032,z+.018)]:brow.append((x,face_y(x,zz)-.006,zz))
        sweep('Expressive eyebrow',brow,[.002,.006 if female else .007,.005,.001],[.0015,.003,.0025,.001],9,'Head',8)
        rings('Sculpted ear',[(side*(hw+.002),.008,1.59,.006,.010),(side*(hw+.012),.001,1.603,.015,.018),
             (side*(hw+.021),.003,1.626,.023,.024),(side*(hw+.015),.012,1.645,.022,.025),(side*(hw+.005),.015,1.65,.009,.012)],0,'Head',16)
        panel('Ear concha',[(side*(hw+.006),-.017,1.606),(side*(hw+.023),-.020,1.624),(side*(hw+.011),-.017,1.638),(side*(hw+.006),-.019,1.623)],14,'Head',.001)

    # Broad, readable bridge/tip planes retain the concept's angular facial character.
    nose=mesh('Defined nose bridge and wings',[(-.010,-.087,1.691),(.010,-.087,1.691),(-.010,-.111,1.65),(.010,-.111,1.65),
        (-.014,-.131,1.631),(.014,-.131,1.631),(0,-.142,1.63),(-.022,-.112,1.619),(.022,-.112,1.619),(0,-.113,1.614)],
        [(0,1,3,2),(2,3,5,6,4),(0,2,4,7),(1,8,5,3),(4,6,9,7),(6,5,8,9),(7,9,8)],0,'Head')
    for face in nose.data.polygons:face.use_smooth=False
    for side in [-1,1]:disc('Nostril',side*.013,-.123,1.62,.0038,.0016,14)
    # Relaxed lips sit on the muzzle, with a slight asymmetric smile.
    lip=[(-.027,1.589),(-.016,1.586),(0,1.585),(.016,1.588),(.027,1.593)]
    sweep('Mouth crease',[(x,face_y(x,z)-.002,z) for x,z in lip],[.0008,.0013,.0014,.0011,.0006],[.001]*5,14,'Head',8)
    sweep('Lower lip volume',[(x,face_y(x,z-.003)-.002,z-.003) for x,z in lip[1:-1]],[.001,.0025,.001],[.001,.002,.001],0,'Head',8)
    # Hair is built as interlocking shaped locks above a close scalp, with a broken silhouette.
    hp=[];sides=24
    for j in range(5):
        for i in range(sides):
            a=math.tau*i/sides;front=max(0,-math.sin(a));back=max(0,math.sin(a))
            z=[1.664+front*.048-back*.018,1.729,1.769,1.786,1.790][j]
            rx=[hw+.002,hw+.009,.091,.057,.004][j];ry=[.091,.095,.082,.051,.004][j]
            hp.append((rx*math.cos(a),.02+ry*math.sin(a),z))
    fs=[(j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i) for j in range(4) for i in range(sides)]
    mesh('Fitted hair mass',hp,fs,9,'Head')
    def lock(name,path,widths,depths):
        points=[Vector(v) for v in path];curve=[];ww=[];dd=[]
        for j in range(len(points)-1):
            p0=points[max(0,j-1)];p1=points[j];p2=points[j+1];p3=points[min(j+2,len(points)-1)]
            for k in range(3):
                t=k/3;curve.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
                ww.append(widths[j]*(1-t)+widths[j+1]*t);dd.append(depths[j]*(1-t)+depths[j+1]*t)
        curve.append(points[-1]);ww.append(widths[-1]);dd.append(depths[-1])
        o=sweep(name,curve,ww,dd,9,'Head',8)
        # Split two longitudinal ridges; shade smoothly along the swept curve.
        for edge in o.data.edges:
            a,b=edge.vertices
            if a%8==b%8 and a%8 in [2,6]:edge.use_edge_sharp=True
        bpy.context.view_layer.objects.active=o
        mod=o.modifiers.new('Hair ridge normals','EDGE_SPLIT');mod.use_edge_angle=False;mod.use_edge_sharp=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if female:
        for i in range(6):
            x=.079-i*.029;top=1.79+math.sin(i*.8)*.016
            lock('Sculpted side part',[(x,.035,1.771),(x-.012,-.004,top),(x-.035,-.064,1.776-i*.003),(x-.053,-.090,1.729-i*.008),(x-.069,-.076,1.690-i*.009)],
                [.016,.024,.023,.015,.001],[.014,.020,.022,.015,.001])
        for sign in [-1,1]:
            lock('Tucked temple lock',[(sign*.087,.004,1.754),(sign*.116,-.005,1.713),(sign*.112,-.008,1.665),(sign*.100,-.021,1.618)],[.022,.023,.015,.001],[.020,.021,.016,.001])
        lock('Low gathered ponytail',[(0,.097,1.69),(-.014,.14,1.64),(-.037,.174,1.577),(-.049,.185,1.507),(-.047,.171,1.447),(-.019,.156,1.417)],
             [.035,.060,.061,.051,.029,.002],[.026,.045,.049,.038,.023,.002])
        rings('Leather hair tie',[(-.01,.12,1.63,.039,.03),(-.01,.12,1.649,.042,.032)],8,'Head',12)
        for i in range(4):
            x=-.058+i*.032
            lock('Ponytail strand',[(x,.155,1.632),(x-.016,.202,1.577),(x-.022,.216,1.515),(x-.021,.197,1.455)],[.016,.023,.018,.001],[.009,.012,.014,.001])
    else:
        for i,(x,tx,z,tip,w) in enumerate([(-.081,-.048,1.801,1.728,.031),(-.059,.010,1.825,1.736,.038),(-.025,.055,1.832,1.746,.037),(.017,.093,1.82,1.744,.033),(.051,.116,1.798,1.731,.025)]):
            lock('Swept crown lock',[(x,.038,1.756),(x-.012,.014,z-.005),(x+.009,-.021,z),(tx,-.073,1.784),(tx+.006,-.096,tip)],
                 [w*.65,w*.95,w,w*.70,.001],[.017,.023,.026,.022,.001])
        for sign in [-1,1]:
            for j in range(3):
                lock('Temple hair',[(sign*.092,.035+j*.021,1.755-j*.01),(sign*.119,.01+j*.024,1.730-j*.01),(sign*.117,-.026+j*.03,1.694-j*.011),(sign*.102,-.03+j*.03,1.674-j*.008)],
                     [.024,.023,.017,.001],[.016,.018,.016,.001])
        lock('Crown accent',[(-.025,.048,1.782),(-.016,.059,1.833),(.018,.025,1.813)],[.022,.02,.001],[.016,.012,.001])
    for sign in [-1,1]:
        for j in range(4):
            lock('Combed rear hair',[(sign*(.02+j*.025),.038,1.792-j*.009),(sign*(.028+j*.026),.087,1.763-j*.008),(sign*(.036+j*.020),.113,1.712-j*.005),(sign*(.038+j*.020),.102,1.655+j*.009)],
                 [.025,.027,.023,.001],[.016,.019,.017,.001])
    # Pelvis, trousers and articulated limbs.
    rings('Belt',[(0,0,1.015,waist+.01,.111),(0,0,1.04,waist+.01,.111)],8,'Hips',16)
    bevel('Belt buckle',(0,-.116,1.028),(.039,.011,.03),12,'Hips',.003)
    for side,s in [('L',1),('R',-1)]:
        sx=s*shoulder;ex=s*elbow;wx=s*wrist;skin=4 if s==1 else 5;sleeve=2 if s==1 else 3;trouser=6 if s==1 else 7
        def arm_weights(v):
            t=max(0,min(1,(v.z-1.135)/.083));return {'UpperArm'+side:t,'Forearm'+side:1-t}
        def fore_weights(v):
            t=max(0,min(1,(v.z-.947)/.035));return {'Forearm'+side:t,'Hand'+side:1-t}
        rings('Rolled linen sleeve '+side,[(ex,-.014,1.159,.065,.062),(ex,-.013,1.182,.070,.066),(ex,-.01,1.205,.076,.068),(ex,-.01,1.225,.078,.071),(ex,-.008,1.244,.082,.073),
            (s*(shoulder+.030),-.002,1.265,.088,.077),(sx,.005,1.349,.088,.085),(sx-s*.018,.010,1.402,.074,.074),
            (sx-s*.039,.012,1.430,.043,.047)],sleeve,arm_weights,16,True)
        rings('Folded cuff '+side,[(ex,-.014,1.151,.072,.069),(ex,-.014,1.158,.080,.076),(ex,-.012,1.184,.084,.077),(ex,-.012,1.195,.072,.068)],sleeve,arm_weights,16,True)
        forearm=rings('Forearm anatomy '+side,[(wx,-.035,.943,.035,.030),(wx,-.034,.958,.036,.031),(wx,-.030,.993,.041,.036),
            (s*(elbow+.020),-.018,1.052,.055,.048),(s*(elbow+.008),-.011,1.111,.065,.056),(ex,-.014,1.162,.061,.057)],skin,fore_weights,16)
        palm=rings('Palm '+side,[(wx,-.042,.867,.043,.023),(wx,-.040,.877,.047,.027),(wx,-.034,.896,.048,.029),(wx,-.033,.919,.043,.028),(wx,-.035,.940,.034,.028),(wx,-.035,.943,.035,.030)],skin,'Hand'+side,16)
        # Open matching wrist loops, then weld before subdivision to keep a continuous skin surface.
        import bmesh
        for obj,cap_index in [(forearm,0),(palm,-1)]:
            bm=bmesh.new();bm.from_mesh(obj.data);bm.faces.ensure_lookup_table();bm.faces.remove(bm.faces[cap_index]);bm.to_mesh(obj.data);bm.free()
        bpy.ops.object.select_all(action='DESELECT');forearm.select_set(True);palm.select_set(True)
        bpy.context.view_layer.objects.active=forearm;bpy.ops.object.join()
        parts.remove(palm)
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.0001);bpy.ops.object.mode_set(mode='OBJECT')
        for f in range(4):
            a=arm.bones['Finger%dA%s'%(f,side)];b=arm.bones['Finger%dB%s'%(f,side)]
            path=[a.head_local, a.head_local.lerp(a.tail_local,.3), a.head_local.lerp(a.tail_local,.82), a.tail_local, a.tail_local.lerp(b.tail_local,.55),b.tail_local, b.tail_local+Vector((0,0,-.003))]
            def fw(v,a=a,b=b):
                t=max(0,min(1,(a.tail_local.z+.007-v.z)/.014));return {a.name:1-t,b.name:t}
            sweep('Finger %d %s'%(f,side),path,[.0105,.0115,.0105,.011,.0095,.0075,.003],[.012,.0135,.012,.013,.011,.008,.004],skin,fw,10)
        a=arm.bones['ThumbA'+side];b=arm.bones['ThumbB'+side]
        sweep('Opposable thumb '+side,[a.head_local,a.head_local.lerp(a.tail_local,.5),a.tail_local,a.tail_local.lerp(b.tail_local,.6),b.tail_local],[.019,.019,.016,.014,.009],[.017,.018,.016,.013,.009],skin,lambda v:{a.name:1} if v.z>.89 else {b.name:1},8)
        hx=s*.101;kx=s*.126;ax=s*.139
        def leg_weights(v):
            if v.z>.9:
                t=min(1,(v.z-.9)/.09);return {'Thigh'+side:1-t,'Hips':t}
            t=max(0,min(1,(v.z-.505)/.10));return {'Thigh'+side:t,'Shin'+side:1-t}
        leg=rings('Tailored trouser leg '+side,[(ax,0,.265,.070,.072),(ax,.007,.34,.081,.08),(s*.135,.012,.438,.087,.087),
            (kx,-.005,.53,.081,.084),(kx,-.014,.586,.088,.09),(s*.118,0,.685,.102,.102),
            (s*.109,.002,.82,.12 if female else .115,.108),(hx,0,.95,.099,.108)],trouser,leg_weights,12,True)
        for v in leg.data.vertices:
            if abs(v.co.z-.95)<.0001:
                inward=max(0,-s*(v.co.x-hx)/.099);v.co.z-=.075*inward**1.5
        # Open the hip end for the shared pelvis shell.
        import bmesh
        bm=bmesh.new();bm.from_mesh(leg.data);bm.faces.ensure_lookup_table();bm.faces.remove(bm.faces[-1]);bm.to_mesh(leg.data);bm.free()
        pocket=[(-.034,.94),(.034,.94),(.030,.889),(0,.878),(-.030,.889),(-.034,.94)]
        curve=[]
        for (x1,z1),(x2,z2) in zip(pocket,pocket[1:]):
            for j in range(4):
                t=j/4;x=x1*(1-t)+x2*t;z=z1*(1-t)+z2*t
                curve.append((hx+x,.105*math.sqrt(1-(x/.108)**2)+.003,z))
        curve.append(curve[0])
        seam=sweep('Back pocket stitching '+side,curve,[.0013]*len(curve),[.0013]*len(curve),trouser,leg_weights,6)
        for uv in seam.data.uv_layers.active.data:uv.uv.y+=.025
        # Boots have one continuous vamp outline, a raised cuff and a thin welt/sole.
        boot=[(ax,-.071,.025,.089,.151),(ax,-.072,.049,.091,.154),(ax,-.072,.061,.087,.150),
              (ax,-.083,.103,.084,.132),(ax,-.060,.141,.076,.111),(ax,-.009,.18,.073,.079),(ax,0,.259,.079,.08),(ax,0,.300,.078,.080)]
        def boot_weights(v):
            t=max(0,min(1,(v.z-.14)/.08));return {'Foot'+side:1-t,'Shin'+side:t}
        rings('Shaped boot '+side,boot,8,boot_weights,12,True)
        rings('Boot sole '+side,[(ax,-.073,.005,.089,.151),(ax,-.073,.024,.093,.157),(ax,-.073,.031,.09,.154)],11,'Foot'+side,12)
        rings('Stitched welt '+side,[(ax,-.073,.032,.092,.155),(ax,-.073,.043,.092,.155)],8,'Foot'+side,12)
        rings('Folded leather cuff '+side,[(ax,0,.244,.092,.089),(ax,0,.258,.102,.096),(ax,0,.289,.1,.095),(ax,0,.301,.09,.087)],8,'Shin'+side,12,True)
        def boot_section(z):
            for lo,hi in zip(boot,boot[1:]):
                if lo[2]<=z<=hi[2]:
                    t=(z-lo[2])/(hi[2]-lo[2]);return tuple(lo[k]*(1-t)+hi[k]*t for k in [1,3,4])
            return (0,.078,.080)
        for z in [.115,.151,.188,.222]:
            vs=[];steps=12
            for zz in [z-.006,z+.006]:
                cy,rx,ry=boot_section(zz)
                for j in range(steps+1):
                    a=-math.pi*.73+j/steps*math.pi*.46
                    vs.append((ax+(rx+.003)*math.cos(a),cy+(ry+.004)*math.sin(a),zz))
            strap=mesh('Curved boot strap '+side,vs,[(j,j+1,steps+2+j,steps+1+j) for j in range(steps)],8,boot_weights)
            for uv in strap.data.uv_layers.active.data:uv.uv.y-=.024
        bevel('Boot side buckle '+side,(ax+s*.077,-.025,.209),(.012,.027,.022),12,'Foot'+side,.002)
    boundary=[]
    for sign,ids in [(1,list(range(9,12))+list(range(0,4))),(-1,list(range(3,10)))]:
        for i in ids:
            a=math.tau*i/12;boundary.append((sign*.101+.099*math.cos(a),.108*math.sin(a),.95))
    vs=list(boundary);n=len(boundary)
    for x,y,z in boundary:
        a=math.atan2(y/.108,x/.20);vs.append(((waist+.006)*math.cos(a),.105*math.sin(a),1.042))
    fs=[]
    for i in range(n):fs.append((i,(i+1)%n,(i+1)%n+n,i+n))
    # Quad saddle joins the inner thigh openings without a separate horizontal crotch flap.
    start=len(vs)
    for sign,ids in [(1,list(range(3,10))),(-1,[3,2,1,0,11,10,9])]:
        for i in ids:
            a=math.tau*i/12;x=sign*.101+.099*math.cos(a)
            vs.append((x,.108*math.sin(a),.95-.075*max(0,-sign*math.cos(a))**1.5))
    for i in range(6):fs.append((start+i,start+i+1,start+7+i+1,start+7+i))
    def pelvis_weights(v):
        if v.z>=1.0:return {'Hips':1}
        return {'Hips':.56,'ThighL' if v.x>0 else 'ThighR':.44}
    mesh('Continuous trouser pelvis',vs,fs,6,pelvis_weights,True)
    # Apply one shaped subdivision pass before skinning; the dense face and small trims already
    # have their final topology. Joining/welding the trouser shell first removes the hip seam.
    trousers=[o for o in parts if o.name.startswith(('Tailored trouser leg','Continuous trouser pelvis'))]
    bpy.ops.object.select_all(action='DESELECT')
    for o in trousers:o.select_set(True)
    bpy.context.view_layer.objects.active=trousers[0];bpy.ops.object.join();pants=bpy.context.object
    parts=[o for o in parts if o not in trousers]+[pants]
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.0001);bpy.ops.object.mode_set(mode='OBJECT')
    for o in parts:
        if o.name.startswith(('Linen shirt','Tailored waistcoat','Rolled linen','Folded cuff','Forearm anatomy','Palm ',
                              'Finger ','Opposable thumb','Tailored trouser leg','Shaped boot','Folded leather cuff',
                              'Sculpted ear')):
            bpy.context.view_layer.objects.active=o
            mod=o.modifiers.new('Sculpted support loops','SUBSURF');mod.levels=1
            bpy.ops.object.modifier_apply(modifier=mod.name)
    for o in parts:
        if o.name.startswith('Rolled linen'):
            side=1 if o.name.endswith('L') else -1
            for v in o.data.vertices:
                z=v.co.z;cx=side*(shoulder+.035*max(0,min(1,(1.40-z)/.2)))
                a=math.atan2(v.co.y,(v.co.x-cx));rad=Vector((v.co.x-cx,v.co.y,0)).normalized()
                fold=.009*math.exp(-((z-1.225-.018*math.cos(a))/.014)**2)-.006*math.exp(-((z-1.252-.020*math.cos(a))/.020)**2)
                fold+=.005*math.cos(a*3+z*23)*math.sin(max(0,min(1,(z-1.19)/.21))*math.pi)
                v.co+=rad*fold
        elif o.name.startswith('Tailored trouser leg'):
            for v in o.data.vertices:
                z=v.co.z;cx=(1 if v.co.x>0 else -1)*.132;rad=Vector((v.co.x-cx,v.co.y,0)).normalized()
                a=math.atan2(v.co.y,v.co.x-cx)
                fold=.007*math.exp(-((z-.535-.027*math.cos(a))/.025)**2)-.006*math.exp(-((z-.585-.019*math.cos(a))/.024)**2)
                fold+=.006*math.exp(-((z-.329-.016*math.sin(a))/.018)**2)
                v.co+=rad*fold
    # Explicit geometric creases keep leather soles and metal separate from soft anatomy.
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
            rotate('Finger%dA%s'%(f,side),strength*(94-f*1.5))
            rotate('Finger%dB%s'%(f,side),strength*100)
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
    def translate(n,x=0,y=0,z=0):
        b=bones[n];b.location=b.bone.matrix_local.to_quaternion().inverted() @ Vector((x,y,z))
    def leg_ik(side,target,pitch=0):
        u=bones['Thigh'+side];f=bones['Shin'+side];h=bones['Foot'+side]
        scene.view_layers[0].update();root=u.head.copy();target=Vector(target)
        delta=target-root;distance=min(delta.length,u.length+f.length-.001);direction=delta.normalized()
        along=(u.length*u.length-f.length*f.length+distance*distance)/(2*distance)
        pole=Vector((root.x,-.8,.55))-root;bend=(pole-direction*pole.dot(direction)).normalized()
        joint=root+direction*along+bend*math.sqrt(max(0,u.length*u.length-along*along))
        aim(u.name,joint);aim(f.name,target)
        # The foot keeps its world orientation through knee flexion.
        q=Quaternion((1,0,0),math.radians(pitch)) @ h.bone.matrix_local.to_quaternion()
        h.matrix=Matrix.Translation(h.head) @ q.to_matrix().to_4x4();scene.view_layers[0].update()
    def smooth(t):
        t=max(0,min(1,t));return t*t*(3-2*t)
    def punch(t):
        # Anticipation, fast extension, then a softer recovery; continuous at loop boundaries.
        if t<.16:return -.20*smooth(t/.16)
        if t<.36:return -.20+1.20*smooth((t-.16)/.20)
        return 1-smooth((t-.36)/.64)
    specs=[('Idle',90),('Walk',30),('Run',24),('Airborne',30),('Mine',18),
           ('CrouchIdle',90),('CrouchWalk',36),('Land',12),
           ('FP_Idle',90),('FP_Walk',30),('FP_Run',24),('FP_Airborne',30),('FP_Crouch',90),('FP_CrouchWalk',36),('FP_Land',12),('FP_Mine',18)]
    for name,length in specs:
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        scene.frame_start=1;scene.frame_end=length+1
        for frame in range(1,length+2):
            t=(frame-1)/length;a=t*math.tau;reset()
            for side in ['L','R']:curl(side,.32)
            if not name.startswith('FP_'):
                crouch=name.startswith('Crouch');run=name=='Run';walk=name in ['Walk','Run','CrouchWalk']
                breathe=math.sin(a)*.45 if name in ['Idle','CrouchIdle'] else 0
                dip=math.sin(math.pi*smooth(t))*.09 if name=='Land' else 0
                translate('Hips',x=math.sin(a)*(.008 if walk else .002),z=(-.56 if crouch else -.012)-dip)
                rotate('Spine',22 if crouch else 7 if run else 1.8+breathe)
                rotate('Chest',-8 if crouch else -2,math.sin(a)*(2 if walk else .4),math.sin(a)*(2.2 if walk else .5))
                rotate('Head',-12 if crouch else -2 if run else -1,0,-math.sin(a)*.5)
                for side,sign in [('L',1),('R',-1)]:
                    rotate('UpperArm'+side,0,-sign*2,sign*1)
                    rotate('Forearm'+side,-10)
                    if walk:
                        phase=(t+(0 if sign==1 else .5))%1
                        stance=.58 if not run else .46
                        stride=.30 if crouch else .57 if run else .40
                        if phase<stance:
                            q=phase/stance;y=-stride*.5+stride*q;lift=0
                            pitch=-9*max(0,1-q*5)+16*max(0,(q-.76)/.24)
                            lift=.02*max(0,(q-.8)/.2)
                        else:
                            q=(phase-stance)/(1-stance);y=stride*.5-stride*smooth(q)
                            lift=math.sin(math.pi*q)*(.14 if run else .075 if crouch else .095);pitch=-12*math.sin(math.pi*q)
                        leg_ik(side,(sign*.139,y,.155+lift),pitch)
                        swing=math.sin(a+(0 if sign==1 else math.pi))*(31 if run else 20 if not crouch else 12)
                        rotate('UpperArm'+side,-swing,-sign*2)
                        rotate('Forearm'+side,(-55 if run else -15)-max(0,swing)*.3)
                        curl(side,.6 if run else .3)
                    elif name=='Airborne':
                        rotate('Thigh'+side,-16 if sign==1 else 12);rotate('Shin'+side,-32 if sign==1 else -20)
                        rotate('UpperArm'+side,-18,sign*8);rotate('Forearm'+side,-24)
                    else:
                        leg_ik(side,(sign*.139,-.018 if crouch else 0,.155))
                if name=='Mine':
                    hit=punch(t);rotate('Chest',-2+hit*9,0,hit*-9)
                    arm_ik('R',(-.18+hit*.025,-.30-hit*.15,1.32+hit*.055),(-.43,-.08,1.15),(0,-1,.12),(0,0,-1));curl('R',1)
            else:
                hit=punch(t) if name=='FP_Mine' else 0
                run=name=='FP_Run';walk=name in ['FP_Walk','FP_Run','FP_CrouchWalk']
                landing=math.sin(math.pi*smooth(t)) if name=='FP_Land' else 0
                for side,sign in [('L',1),('R',-1)]:
                    strike=hit if side=='R' else 0
                    armphase=a+(0 if sign==1 else math.pi)
                    bob=math.sin(armphase)*(.012 if run else .007) if walk else math.sin(a)*.0018
                    sway=math.cos(armphase)*(.011 if run else .004) if walk else 0
                    up=.026 if name=='FP_Airborne' else -.016 if name in ['FP_Crouch','FP_CrouchWalk'] else 0
                    target=(sign*(.235-strike*.075)+sway,-.34-strike*.15,1.265+bob+strike*.06+up-landing*.045)
                    # Lower, outward viewmodel shoulders keep the connected sleeve below the camera.
                    translate('Clavicle'+side,x=sign*.08,y=-.07,z=-.22)
                    arm_ik(side,target,(sign*.46,.03,1.01),(-sign*.15,-.95,.26),(sign*.30,-.28,-.94))
                    curl(side,1)
            for b in bones:
                b.keyframe_insert(data_path='location',frame=frame,group=b.name)
                b.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=b.name)
                b.keyframe_insert(data_path='scale',frame=frame,group=b.name)
        action['loop']=name not in ['Mine','FP_Mine','Land','FP_Land']
    scene.frame_start=1;scene.frame_end=91
    return [{'name':n,'seconds':f/30,'loop':n not in ['Mine','FP_Mine','Land','FP_Land']} for n,f in specs]


atlas('SkinField');atlas('SkinOchre',True)
report=[build(False),build(True)]
(SOURCE/'asset-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('RIVET_ASSETS '+json.dumps(report))
