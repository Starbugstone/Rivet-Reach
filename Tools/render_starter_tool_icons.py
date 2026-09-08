"""Render inventory icons from existing original grip props without rewriting their sources.
Copyright (c) 2026 Starbugstone. Run with Blender --background --python.
"""
from pathlib import Path
import bpy
from mathutils import Vector

root=Path(__file__).resolve().parents[1]
for source,icon in [('GripSword','StarterDaggerIcon'),('GripPickaxe','StarterPickaxeIcon')]:
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Characters'/(source+'.blend')))
    scene=bpy.context.scene
    model=bpy.data.objects[source]
    points=[model.matrix_world@Vector(corner) for corner in model.bound_box]
    centre=sum(points,Vector())/8
    span=max(max(p.x for p in points)-min(p.x for p in points),max(p.z for p in points)-min(p.z for p in points))
    bpy.ops.object.camera_add(location=centre+Vector((.10,-2,.10)))
    camera=bpy.context.object;camera.rotation_euler=(centre-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO';camera.data.ortho_scale=span*1.15;scene.camera=camera
    for offset,power in [((-.5,-1,1.3),130),((1,.5,.8),95)]:
        bpy.ops.object.light_add(type='AREA',location=centre+Vector(offset))
        light=bpy.context.object;light.data.energy=power;light.data.size=1.2
        light.rotation_euler=(centre-light.location).to_track_quat('-Z','Y').to_euler()
    scene.world.color=(.18,.18,.18)
    scene.render.engine='CYCLES';scene.cycles.samples=32
    scene.render.resolution_x=64;scene.render.resolution_y=64;scene.render.resolution_percentage=100
    scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(root/'Assets/RivetReach/Resources/Tools'/(icon+'.png'))
    bpy.ops.render.render(write_still=True)
