"""Original Rivet Reach food meshes, shared palette and rendered inventory icons.
Run with Blender --background --python Tools/create_potato_assets.py.
Copyright (c) 2026 Starbugstone. All rights reserved.
"""
import bpy
import math
import random
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/RivetReach/Resources/Food'
SOURCE = ROOT / 'ArtSource/Food'
for folder in (OUT, SOURCE):
    folder.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
# Earthy ochre skin, toasted russet shell, cream flesh, recessed eyes.
# HeldTool reserves regions 12 and 15 for metal; food UVs avoid both.
colours = ['A56E37','B68043','C18B4B','CE9A59','BC874C','D4A664',
           '724020','844825','98552A','A56432','B0743A','F1D49A',
           'F1D49A','FFE8B2','E4BC7A','D9AE68']
atlas = bpy.data.images.new('PotatoPalette', width=128, height=128)
pixels = []
for y in range(128):
    for x in range(128):
        c = colours[x//32+(y//32)*4]
        grain=.80 if (x*19+y*37+x*y*7)%113<4 else 1
        pixels.extend([int(c[i:i+2],16)/255*grain for i in (0,2,4)]+[1])
atlas.pixels[:] = pixels
atlas.filepath_raw = str(OUT/'Palette.png'); atlas.file_format='PNG'; atlas.save()
material = bpy.data.materials.new('PotatoPalette'); material.use_nodes=True
bsdf = material.node_tree.nodes.get('Principled BSDF'); bsdf.inputs['Roughness'].default_value=.86
tex = material.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=atlas; tex.interpolation='Closest'
material.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])

models=[]
for baked, key in ((False,'Potato'),(True,'BakedPotato')):
    rng=random.Random(317 if baked else 91)
    verts=[]; faces=[]; slots=[]
    segments=24; rings=17
    # Long-axis ellipsoid with uneven lobes. The upper strip of the baked skin
    # opens into a recessed, craggy flesh surface with raised browned shoulders.
    eyes=[(-.30,.55),(.12,1.2),(.30,2.45),(-.16,2.8),(.03,3.65),(.28,5.25),(-.29,5.7)]
    for j in range(rings):
        u=-math.pi/2+(j+.35)/(rings-.3)*math.pi
        x=.48*math.sin(u); radius=math.cos(u)
        for k in range(segments):
            a=k*math.tau/segments
            lobe=1+.055*math.sin(a*3+u*2)+.035*math.cos(a*5-u*3)
            y=.29*radius*math.cos(a)*lobe
            z=.265*radius*math.sin(a)*lobe
            z+=.006*math.sin(k*17+j*31)*radius
            if baked and math.sin(a)>.90 and abs(x)<.405:
                z-=.075*max(0,1-abs(y)/.18)*(1-abs(x)/.48)
                z+=rng.uniform(-.014,.014)
                y*=1.14
            if not baked:
                for ex,ea in eyes:
                    da=math.atan2(math.sin(a-ea),math.cos(a-ea))
                    d=((x-ex)/.05)**2+(da/.19)**2
                    inset=.030*math.exp(-d*1.5)
                    y-=inset*math.cos(a);z-=inset*math.sin(a)
            verts.append((x,y,z+.015*math.sin(u*3)))
    for j in range(rings-1):
        for k in range(segments):
            a=(k+.5)*math.tau/segments
            indices=(j*segments+k,j*segments+(k+1)%segments,(j+1)*segments+(k+1)%segments,(j+1)*segments+k)
            x=sum(verts[i][0] for i in indices)/4
            flesh=baked and math.sin(a)>.92 and abs(x)<.37
            tone=math.sin(x*18+a*2)*.7+math.cos(a*5-x*11)*.45+rng.uniform(-.4,.4)
            slot=rng.choice([11,11,13,13,14]) if flesh else (max(7,min(10,round(8.5+tone))) if baked else max(0,min(5,round(2.5+tone))))
            if not baked:
                for ex,ea in eyes:
                    da=math.atan2(math.sin(a-ea),math.cos(a-ea))
                    if ((x-ex)/.048)**2+(da/.19)**2<1.3:slot=6
            # Alternating diagonals avoid a mechanically uniform banded surface.
            if (j+k)%2:
                faces.extend([(indices[0],indices[1],indices[3]),(indices[1],indices[2],indices[3])])
            else:
                faces.extend([(indices[0],indices[1],indices[2]),(indices[0],indices[2],indices[3])])
            slots.extend([slot,slot if rng.random()<.8 else (11 if flesh else slot)])
    faces.extend([tuple(reversed(range(segments))),tuple((rings-1)*segments+k for k in range(segments))]);slots.extend([8 if baked else 1]*2)
    mesh=bpy.data.meshes.new(key);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(key,mesh);scene.collection.objects.link(obj);mesh.materials.append(material)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    uv=mesh.uv_layers.new(name='FoodUV')
    for p,slot in zip(mesh.polygons,slots):
        for li in p.loop_indices:
            v=mesh.vertices[mesh.loops[li].vertex_index].co
            uv.data[li].uv=((slot%4+.1+(v.x*7+v.y*3)%1*.8)/4,(slot//4+.1+(v.z*9+v.x*3)%1*.8)/4)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(key+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
    obj.select_set(False);models.append(obj)
    mesh.calc_loop_triangles();print(key, len(mesh.loop_triangles),'triangles',flush=True)

scene.render.engine='CYCLES';scene.cycles.samples=48
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.film_transparent=True
scene.world=bpy.data.worlds.new('Neutral studio');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.22,.27,.33,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
scene.view_settings.view_transform='AgX'
def aim(obj,at):obj.rotation_euler=(Vector(at)-obj.location).to_track_quat('-Z','Y').to_euler()
for name,position,power,size in [('Warm key',(-2,-3,4),330,3),('Cool fill',(2,1,3),190,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    ob=bpy.data.objects.new(name,data);scene.collection.objects.link(ob);ob.location=position;aim(ob,(0,0,0))
data=bpy.data.cameras.new('Item review');camera=bpy.data.objects.new('Item review',data);scene.collection.objects.link(camera);scene.camera=camera
data.type='ORTHO';data.ortho_scale=1.17;camera.location=(.9,-1.8,1.35);aim(camera,(0,0,0))
scene.render.resolution_x=scene.render.resolution_y=256;scene.render.resolution_percentage=100
for obj in models:
    for other in models:other.hide_render=other!=obj
    scene.render.filepath=str(OUT/(obj.name+'Icon.png'));bpy.ops.render.render(write_still=True)
for i,obj in enumerate(models):obj.hide_render=False;obj.location.x=(i-.5)*1.2
camera.location=(.7,-3.4,2.4);aim(camera,(0,0,0));data.ortho_scale=2.65
scene.render.resolution_x=1200;scene.render.resolution_y=650
scene.render.filepath=str(SOURCE/'potato-review.png');bpy.ops.render.render(write_still=True)
atlas.filepath='//../../Assets/RivetReach/Resources/Food/Palette.png'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Potatoes.blend'))
print('POTATO_ASSETS_COMPLETE',flush=True)
