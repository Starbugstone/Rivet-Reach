"""Original Rivet Reach meshes and skin atlases. Run with Blender --background --python.
Copyright (c) 2026 Starbugstone. See LICENSE.md. No third-party asset inputs.
"""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/RivetReach/Resources/Characters'
SOURCE = ROOT / 'ArtSource/Characters'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)

def atlas(name, dark=False):
    palette = ([
        (.48,.27,.16),(.82,.62,.27),(.86,.66,.31),(.86,.66,.31),
        (.48,.27,.16),(.48,.27,.16),(.20,.29,.37),(.20,.29,.37),
        (.20,.13,.09),(.095,.065,.045),(.92,.91,.83),(.07,.06,.05),
        (.65,.43,.15),(.31,.36,.32),(.30,.11,.075),(.47,.48,.42)] if dark else [
        (.72,.46,.30),(.80,.76,.61),(.84,.79,.64),(.84,.79,.64),
        (.72,.46,.30),(.72,.46,.30),(.23,.25,.27),(.23,.25,.27),
        (.29,.18,.105),(.15,.09,.055),(.94,.92,.83),(.065,.055,.04),
        (.67,.47,.21),(.25,.40,.39),(.43,.21,.14),(.47,.48,.42)])
    image=bpy.data.images.new(name,width=256,height=256,alpha=True)
    pixels=[]
    for y in range(256):
        for x in range(256):
            tile=(y//64)*4+x//64; c=palette[tile]
            noise=(((x//4)*17+(y//4)*31)%13-6)*.002
            edge=.91 if x%64 in (1,2,61,62) else 1
            pixels.extend([max(0,min(1,v*edge+noise)) for v in c]+[1])
    image.pixels=pixels;image.filepath_raw=str(OUT/(name+'.png'));image.file_format='PNG';image.save()

def build(female):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    name='ExplorerFemale' if female else 'ExplorerMale'
    arm=bpy.data.armatures.new(name+'Rig');rig=bpy.data.objects.new(name+'Rig',arm);bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    defs=[('Root',(0,0,0),(0,0,.1),None),('Hips',(0,0,.94),(0,0,1.06),'Root'),
          ('Spine',(0,0,1.06),(0,0,1.28),'Hips'),('Chest',(0,0,1.28),(0,0,1.49),'Spine'),
          ('Head',(0,0,1.49),(0,0,1.79),'Chest')]
    shoulder=.225 if female else .25
    for suffix,sign in [('L',1),('R',-1)]:
        defs += [('UpperArm'+suffix,(sign*shoulder,0,1.44),(sign*.32,0,1.18),'Chest'),
                 ('Forearm'+suffix,(sign*.32,0,1.18),(sign*.37,-.025,.97),'UpperArm'+suffix),
                 ('Hand'+suffix,(sign*.37,-.025,.97),(sign*.38,-.025,.88),'Forearm'+suffix),
                 ('Thigh'+suffix,(sign*.105,0,.97),(sign*.11,0,.54),'Hips'),
                 ('Shin'+suffix,(sign*.11,0,.54),(sign*.115,0,.17),'Thigh'+suffix),
                 ('Foot'+suffix,(sign*.115,0,.17),(sign*.115,-.17,.08),'Shin'+suffix)]
    for n,h,t,parent in defs:
        b=arm.edit_bones.new(n);b.head=h;b.tail=t
        if parent:b.parent=arm.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
    parts=[]
    def finish(obj,tile,bone):
        bpy.context.view_layer.objects.active=obj
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        if not obj.data.uv_layers:obj.data.uv_layers.new(name='SkinUV')
        uv=obj.data.uv_layers.active.data
        # Distinct atlas regions preserve left/right editing. UV coordinates remain inside each tile.
        for poly in obj.data.polygons:
            for idx,li in enumerate(poly.loop_indices):
                corner=[(.08,.08),(.92,.08),(.92,.92),(.08,.92)][idx%4]
                uv[li].uv=((tile%4+corner[0])/4,(tile//4+corner[1])/4)
            poly.use_smooth=False
        g=obj.vertex_groups.new(name=bone);g.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        parts.append(obj)
        return obj
    def blob(n,loc,scale,tile,bone,sub=1):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc)
        o=bpy.context.object;o.name=n;o.scale=scale;return finish(o,tile,bone)
    def box(n,loc,scale,tile,bone,bevel=.025):
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=n;o.scale=scale
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        if bevel:
            mod=o.modifiers.new('Soft edges','BEVEL');mod.width=bevel;mod.segments=1
            bpy.ops.object.modifier_apply(modifier=mod.name)
        return finish(o,tile,bone)
    def segment(n,a,b,r1,r2,tile,bone,verts=8):
        a,b=Vector(a),Vector(b);mid=(a+b)/2
        bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r2,radius2=r1,depth=(b-a).length,location=mid)
        o=bpy.context.object;o.name=n;o.rotation_euler=(a-b).to_track_quat('Z','Y').to_euler()
        return finish(o,tile,bone)
    # Layered cloth silhouette with restrained differences, and broad paintable surfaces.
    segment('Shirt',(0,0,1.48),(0,0,1.02),.225 if not female else .205,.175,1,'Chest',8)
    vest=segment('Waistcoat',(0,-.012,1.44),(0,-.012,1.06),.240 if not female else .220,.181 if not female else .171,13,'Chest',8)
    vest.scale.y=1.04
    blob('Hip cloth',(0,0,.98),(.205 if female else .19,.145,.17),6,'Hips',2)
    box('Belt',(0,-.01,1.045),(.355,.26,.055),8,'Hips',.014)
    box('Buckle',(0,-.147,1.045),(.065,.02,.048),12,'Hips',.006)
    for z in [1.14,1.24,1.34]:blob('Button',(0,-.168,z),(.015,.009,.015),12,'Chest')
    segment('Neck',(0,0,1.61),(0,0,1.47),.065,.07,0,'Head')
    blob('Head',(0,-.008,1.674),(.116 if female else .124,.107,.14),0,'Head',2)
    blob('Nose',(0,-.112,1.66),(.021,.037,.034),0,'Head')
    for sign in (-1,1):
        blob('Ear',(sign*.118,0,1.665),(.023,.025,.039),0,'Head')
        box('Eye',(sign*.049,-.102,1.705),(.047,.014,.021),10,'Head',.003)
        box('Pupil',(sign*.049,-.111,1.704),(.017,.005,.019),11,'Head',.002)
        box('Brow',(sign*.049,-.104,1.737),(.054,.014,.014),9,'Head',.003)
    box('Mouth',(0,-.102,1.626),(.054,.013,.012),14,'Head',.003)
    blob('Hair cap',(0,.006,1.77),(.128,.118,.071),9,'Head',2)
    for i in range(4):
        blob('Hair sweep',(-.09+i*.055,-.055,1.774+(i%2)*.012),(.05,.085,.047),9,'Head')
    if female:
        for sign in (-1,1):blob('Long side hair',(sign*.111,.018,1.695),(.028,.103,.10),9,'Head',2)
        blob('Ponytail upper',(0,.13,1.66),(.065,.075,.13),9,'Head',2)
        blob('Ponytail lower',(0,.17,1.49),(.055,.065,.13),9,'Head',1)
    for suffix,sign in [('L',1),('R',-1)]:
        upper='UpperArm'+suffix;fore='Forearm'+suffix;hand='Hand'+suffix
        sleeve=2 if sign==1 else 3;skin=4 if sign==1 else 5;trouser=6 if sign==1 else 7
        a=(sign*shoulder,0,1.44);b=(sign*.32,0,1.18);c=(sign*.37,-.025,.97)
        blob('Shoulder',a,(.09,.092,.10),sleeve,upper,2)
        segment('Sleeve',a,b,.09,.077,sleeve,upper)
        segment('Rolled cuff',(sign*.313,0,1.205),(sign*.326,0,1.16),.085,.085,sleeve,fore)
        segment('Forearm',b,c,.066,.045,skin,fore)
        box('Palm',(sign*.378,-.026,.93),(.088,.071,.093),skin,hand,.019)
        blob('Thumb',(sign*.344,-.063,.946),(.028,.031,.039),skin,hand,1)
        for i in range(4):blob('Knuckle',(sign*(.345+i*.021),-.064,.912),(.014,.018,.022),skin,hand)
        hip=(sign*.105,0,.96);knee=(sign*.11,0,.54);ankle=(sign*.115,0,.20)
        segment('Trouser thigh',hip,knee,.116 if female else .112,.086,trouser,'Thigh'+suffix)
        blob('Knee',knee,(.085,.085,.085),trouser,'Shin'+suffix)
        segment('Trouser shin',knee,ankle,.085,.070,trouser,'Shin'+suffix)
        box('Boot',(sign*.115,-.064,.102),(.17,.30,.17),8,'Foot'+suffix,.025)
        segment('Boot cuff',(sign*.115,0,.26),(sign*.115,0,.18),.083,.082,8,'Shin'+suffix)
        box('Sole',(sign*.115,-.067,.034),(.178,.306,.043),11,'Foot'+suffix,.008)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();mesh=bpy.context.object;mesh.name=name+'Body'
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    mat=bpy.data.materials.new('PlayerSkin');mat.use_nodes=True
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'SkinField.png'));tex.image.filepath=bpy.path.relpath(str(OUT/'SkinField.png'),start=str(SOURCE));tex.interpolation='Closest'
    mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
    mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.85
    mesh.data.materials.clear();mesh.data.materials.append(mat)
    mod=mesh.modifiers.new('Shared rig','ARMATURE');mod.object=rig;mesh.parent=rig
    rig.select_set(True);mesh.select_set(True)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_armature_deform_only=False)
    mesh.data.calc_loop_triangles()
    return {'model':name,'triangles':len(mesh.data.loop_triangles),'bones':len(arm.bones),'materials':len(mesh.data.materials)}

atlas('SkinField');atlas('SkinOchre',True)
report=[build(False),build(True)]
(SOURCE/'asset-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('RIVET_ASSETS '+json.dumps(report))
