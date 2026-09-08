"""Original grip-review models, also used by the starter dagger and pickaxe.
Copyright (c) 2026 Starbugstone. Run in background Blender.
"""
import bpy, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
out=root/'Assets/RivetReach/Resources/Characters';source=root/'ArtSource/Characters'
def build(name):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    parts=[]
    def finish(o,tile,bevel=0):
        bpy.context.view_layer.objects.active=o
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        if bevel:
            m=o.modifiers.new('Rounded manufactured edges','BEVEL');m.width=bevel;m.segments=3;bpy.ops.object.modifier_apply(modifier=m.name)
        uv=o.data.uv_layers.active or o.data.uv_layers.new();uv.name='SkinUV'
        for p in o.data.polygons:
            for j,li in enumerate(p.loop_indices):uv.data[li].uv=((tile%4+.45+(j%2)*.10)/4,(tile//4+.46+(j//2%2)*.08)/4)
        parts.append(o);return o
    def cylinder(n,z,length,radius,tile):
        bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=radius,depth=length,location=(0,0,z));o=bpy.context.object;o.name=n;return finish(o,tile,.0015)
    def box(n,loc,dimensions,tile,bevel):
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=n;o.dimensions=dimensions;return finish(o,tile,bevel)
    if name=='GripPickaxe':
        cylinder('Shaped ash shaft',.055,.52,.018,8)
        cylinder('Butt cap',-.205,.028,.021,12)
        for z in [-.075,-.045,-.015,.015,.045]:cylinder('Leather grip binding',z,.022,.0188,9)
        cylinder('Head ferrule',.28,.065,.026,12)
        # Continuous forged head, broad at the eye, curved taper toward both points.
        profile=[(-.35,.37,.002,.003),(-.27,.45,.013,.018),(-.15,.50,.023,.025),(0,.505,.032,.039),(.15,.50,.024,.024),(.27,.45,.013,.015),(.35,.37,.002,.003)]
        vs=[]
        for x,z,depth,height in [(x*.78,z-.15,d,h) for x,z,d,h in profile]:vs.extend([(x,-depth,z-height),(x,depth,z-height),(x,depth,z+height),(x,-depth,z+height)])
        faces=[(3,2,1,0)]
        for i in range(len(profile)-1):
            for j in range(4):faces.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
        faces.append(tuple(range(len(vs)-4,len(vs))))
        mesh=bpy.data.meshes.new('Forged pick');mesh.from_pydata(vs,[],faces);mesh.update();o=bpy.data.objects.new('Forged pick',mesh);bpy.context.collection.objects.link(o);finish(o,15,.002)
    else:
        cylinder('Wrapped handle',0,.18,.018,9)
        for z in [-.073,-.049,-.025,0,.025,.049,.073]:cylinder('Diagonal leather binding',z,.008,.0192,8)
        cylinder('Pommel',-.105,.035,.028,12)
        box('Curved brass guard',(0,0,.108),(.21,.044,.034),12,.012)
        cylinder('Blade collar',.14,.04,.024,15)
        vs=[]
        for z,w in [(.15,.033),(.47,.027),(.60,.001)]:vs.extend([(-w,0,z),(0,-.009,z),(w,0,z),(0,.009,z)])
        faces=[(3,2,1,0)]
        for i in range(2):
            for j in range(4):faces.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
        mesh=bpy.data.meshes.new('Diamond section blade');mesh.from_pydata(vs,[],faces);mesh.update();o=bpy.data.objects.new('Tempered blade',mesh);bpy.context.collection.objects.link(o);finish(o,15)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();model=bpy.context.object;model.name=name
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    mat=bpy.data.materials.new('Explorer tool palette');mat.use_nodes=True
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(out/'SkinField.png'));tex.image.filepath='//../../Assets/RivetReach/Resources/Characters/SkinField.png'
    mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes['Principled BSDF'].inputs['Base Color']);mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.48;model.data.materials.clear();model.data.materials.append(mat)
    bpy.ops.wm.save_as_mainfile(filepath=str(source/(name+'.blend')))
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
    model.data.calc_loop_triangles();print(name+' triangles='+str(len(model.data.loop_triangles)))
for name in ['GripSword','GripPickaxe']:build(name)
