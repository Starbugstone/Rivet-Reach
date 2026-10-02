"""Geometry, UV-cell and skinning helpers for the second-generation explorer models.

Original Rivet Reach artwork tooling, copyright (c) 2026 Starbugstone. See LICENSE.md.
Imported by create_explorer_v2.py inside Blender 5.2; no third-party inputs.

Conventions: Blender +Z up, -Y forward, metres. Character left is +X. UVs live in one
2048 px atlas split into an 8x8 grid of tint cells (four cells per legacy 4x4 tile).
"""
import math
import bpy
import bmesh
from mathutils import Vector

GRID = 8
ATLAS = 2048
PAD = 6 / ATLAS


def cell(cx, cy, pad=PAD):
    s = 1 / GRID
    return (cx * s + pad, cy * s + pad, (cx + 1) * s - pad, (cy + 1) * s - pad)


def cells(cx0, cy0, cx1, cy1, pad=PAD):
    """Inclusive block of cells."""
    s = 1 / GRID
    return (cx0 * s + pad, cy0 * s + pad, (cx1 + 1) * s - pad, (cy1 + 1) * s - pad)


def sub(rect, a0, b0, a1, b1):
    u0, v0, u1, v1 = rect
    return (u0 + (u1 - u0) * a0, v0 + (v1 - v0) * b0, u0 + (u1 - u0) * a1, v0 + (v1 - v0) * b1)


def at(rect, s, t):
    return (rect[0] + (rect[2] - rect[0]) * s, rect[1] + (rect[3] - rect[1]) * t)


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def catmull(points, t):
    """Uniform Catmull-Rom through a list of equal-length tuples, t in [0, 1]."""
    n = len(points) - 1
    x = max(0.0, min(1.0, t)) * n
    i = min(int(x), n - 1)
    f = x - i
    p0, p1, p2, p3 = points[max(i - 1, 0)], points[i], points[i + 1], points[min(i + 2, n)]
    return tuple(.5 * ((2 * b) + (-a + c) * f + (2 * a - 5 * b + 4 * c - d) * f * f + (-a + 3 * b - 3 * c + d) * f ** 3)
                 for a, b, c, d in zip(p0, p1, p2, p3))


def resample(points, count, metric=None):
    """Resample a polyline to count points evenly by (optionally mapped) arc length."""
    pts = [tuple(p) for p in points]
    mapped = [Vector(metric(p)) if metric else Vector(p) for p in pts]
    lengths = [0.0]
    for a, b in zip(mapped, mapped[1:]):
        lengths.append(lengths[-1] + (b - a).length)
    total = lengths[-1] or 1.0
    out = []
    j = 0
    for k in range(count):
        target = total * k / (count - 1)
        while j < len(lengths) - 2 and lengths[j + 1] < target:
            j += 1
        span = lengths[j + 1] - lengths[j] or 1.0
        f = (target - lengths[j]) / span
        out.append(tuple(a + (b - a) * f for a, b in zip(pts[j], pts[j + 1])))
    return out


def densify(points, per_segment=8):
    out = []
    n = len(points)
    for i in range(n - 1):
        for k in range(per_segment):
            t = (i + k / per_segment) / (n - 1)
            out.append(catmull(points, t))
    out.append(tuple(points[-1]))
    return out


class Part:
    """Geometry accumulator: vertices with skin weights, faces with per-corner UVs."""

    def __init__(self, name, hard=False, smooth_angle=None):
        self.name = name
        self.v = []
        self.w = []
        self.f = []
        self.uv = []
        self.hard = hard
        self.smooth_angle = smooth_angle
        self.flat = set()

    def vert(self, co, weights):
        self.v.append(Vector(co))
        self.w.append(dict(weights))
        return len(self.v) - 1

    def face(self, idx, uvs, flat=False):
        self.f.append(tuple(idx))
        self.uv.append([tuple(u) for u in uvs])
        if flat:
            self.flat.add(len(self.f) - 1)

    def extend(self, other):
        base = len(self.v)
        self.v.extend(other.v)
        self.w.extend(other.w)
        offset = len(self.f)
        for f, uv in zip(other.f, other.uv):
            self.f.append(tuple(i + base for i in f))
            self.uv.append(uv)
        self.flat.update(i + offset for i in other.flat)


def grid(part, rows, rect, weights, closed=False, flip=False, u_range=(0, 1), v_values=None):
    """Quads over rows of equal-length 3D point lists. Rows advance in v, columns in u.

    closed joins the last column back to the first with a UV seam at u=1.
    """
    nr = len(rows)
    nc = len(rows[0])
    ids = [[part.vert(p, weights(Vector(p))) for p in row] for row in rows]
    if v_values is None:
        lengths = [0.0]
        for a, b in zip(rows, rows[1:]):
            lengths.append(lengths[-1] + sum((Vector(p) - Vector(q)).length for p, q in zip(a, b)) / nc)
        total = lengths[-1] or 1.0
        v_values = [l / total for l in lengths]
    cols = nc if closed else nc - 1
    # Column u by average arc length along the rows.
    seg = []
    for i in range(cols):
        j = (i + 1) % nc
        seg.append(sum((Vector(r[j]) - Vector(r[i])).length for r in rows) / nr)
    total = sum(seg) or 1.0
    us = [0.0]
    for s in seg:
        us.append(us[-1] + s / total)
    us = [u_range[0] + (u_range[1] - u_range[0]) * u for u in us]
    for r in range(nr - 1):
        for i in range(cols):
            j = (i + 1) % nc
            a, b, c, d = ids[r][i], ids[r][j], ids[r + 1][j], ids[r + 1][i]
            uvs = [at(rect, us[i], v_values[r]), at(rect, us[i + 1], v_values[r]),
                   at(rect, us[i + 1], v_values[r + 1]), at(rect, us[i], v_values[r + 1])]
            if flip:
                part.face((a, d, c, b), [uvs[0], uvs[3], uvs[2], uvs[1]])
            else:
                part.face((a, b, c, d), uvs)
    return ids


def ring(center, xaxis, yaxis, rx, ry, count, exponent=2.0, phase=0.0, shape=None):
    """Superellipse ring; shape(angle) returns a radial scale multiplier."""
    pts = []
    for i in range(count):
        a = phase + math.tau * i / count
        c, s = math.cos(a), math.sin(a)
        ex = math.copysign(abs(c) ** (2 / exponent), c)
        ey = math.copysign(abs(s) ** (2 / exponent), s)
        k = shape(a) if shape else 1.0
        pts.append(Vector(center) + Vector(xaxis) * rx * ex * k + Vector(yaxis) * ry * ey * k)
    return pts


def frame_from(direction, hint=(0, -1, 0)):
    d = Vector(direction).normalized()
    h = Vector(hint)
    x = (h - d * h.dot(d))
    if x.length < 1e-6:
        x = Vector((1, 0, 0)) - d * d.x
    x.normalize()
    y = d.cross(x).normalized()
    return x, y, d


def tube(part, path, radii, rect, weights, sides=8, cap=True, hint=(0, 0, 1), flat=False, radii_y=None):
    """Swept circular/elliptical tube along a polyline."""
    pts = [Vector(p) for p in path]
    rows = []
    for j, p in enumerate(pts):
        t = (pts[min(j + 1, len(pts) - 1)] - pts[max(j - 1, 0)])
        if t.length < 1e-9:
            t = Vector((0, 0, 1))
        t.normalize()
        h = Vector(hint)
        u = h - t * h.dot(t)
        if u.length < 1e-5:
            u = Vector((1, 0, 0)) - t * t.x
        u.normalize()
        v = t.cross(u).normalized()
        ry = radii_y[j] if radii_y else radii[j]
        rows.append([p + u * math.cos(math.tau * i / sides) * radii[j] + v * math.sin(math.tau * i / sides) * ry for i in range(sides)])
    ids = grid(part, rows, rect, weights, closed=True)
    if cap:
        c0 = sum((Vector(q) for q in rows[0]), Vector()) / sides
        c1 = sum((Vector(q) for q in rows[-1]), Vector()) / sides
        mid = at(rect, .5, .5)
        a = part.vert(c0, weights(c0))
        b = part.vert(c1, weights(c1))
        for i in range(sides):
            j = (i + 1) % sides
            part.face((ids[0][j], ids[0][i], a), [mid, mid, mid])
            part.face((ids[-1][i], ids[-1][j], b), [mid, mid, mid])
    return ids


def strap(part, path, normals, width, thickness, rect, weights, flat=True):
    """Rectangular band laid along path with given outward normals (e.g. belts, laces)."""
    pts = [Vector(p) for p in path]
    rows = []
    for j, p in enumerate(pts):
        t = (pts[min(j + 1, len(pts) - 1)] - pts[max(j - 1, 0)]).normalized()
        n = Vector(normals[j]).normalized()
        side = t.cross(n).normalized()
        n = side.cross(t).normalized()
        w = width[j] if isinstance(width, (list, tuple)) else width
        rows.append([p + side * (-w / 2) - n * thickness * .5, p + side * (-w / 2) + n * thickness * .5,
                     p + side * (w / 2) + n * thickness * .5, p + side * (w / 2) - n * thickness * .5])
    nr = len(rows)
    ids = [[part.vert(q, weights(q)) for q in row] for row in rows]
    lengths = [0.0]
    for a, b in zip(pts, pts[1:]):
        lengths.append(lengths[-1] + (b - a).length)
    total = lengths[-1] or 1
    # u across the visible top face (columns 1-2), sides squeezed at the rect edges.
    us = [0.0, .08, .92, 1.0]
    for r in range(nr - 1):
        v0, v1 = lengths[r] / total, lengths[r + 1] / total
        for i in range(4):
            j = (i + 1) % 4
            u0, u1 = us[i], (us[j] if j else 1.0)
            if i == 3:
                u0, u1 = .96, 1.0
            part.face((ids[r][i], ids[r][j], ids[r + 1][j], ids[r + 1][i]),
                      [at(rect, u0, v0), at(rect, u1, v0), at(rect, u1, v1), at(rect, u0, v1)], flat=flat)
    mid = at(rect, .5, .02)
    part.face(tuple(reversed(ids[0])), [mid] * 4, flat=True)
    part.face(tuple(ids[-1]), [at(rect, .5, .98)] * 4, flat=True)
    return ids


def from_object(part, obj, rect, weights, project='box', flat=True, centre=None, scale=None):
    """Copy an evaluated helper object's polygons into part with projected UVs."""
    me = obj.data
    pts = [obj.matrix_world @ v.co for v in me.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    size = hi - lo
    ids = [part.vert(p, weights(p)) for p in pts]
    for poly in me.polygons:
        n = obj.matrix_world.to_3x3() @ poly.normal
        ax = max(range(3), key=lambda k: abs(n[k]))
        a, b = [k for k in range(3) if k != ax]
        uvs = []
        for vi in poly.vertices:
            p = pts[vi]
            s = (p[a] - lo[a]) / (size[a] or 1)
            t = (p[b] - lo[b]) / (size[b] or 1)
            uvs.append(at(rect, .1 + .8 * s, .1 + .8 * t))
        part.face([ids[i] for i in poly.vertices], uvs, flat=flat)
    return ids


def helper_cube(location, size, bevel=0.0, segments=2):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    o = bpy.context.object
    o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if bevel:
        m = o.modifiers.new('bevel', 'BEVEL')
        m.width = bevel
        m.segments = segments
        m.limit_method = 'NONE'
        bpy.ops.object.modifier_apply(modifier=m.name)
    return o


def helper_cylinder(location, radius, depth, vertices=12, bevel=0.0, segments=2, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    o = bpy.context.object
    if bevel:
        m = o.modifiers.new('bevel', 'BEVEL')
        m.width = bevel
        m.segments = segments
        m.limit_method = 'ANGLE'
        bpy.ops.object.modifier_apply(modifier=m.name)
    return o


def remove(obj):
    bpy.data.objects.remove(obj, do_unlink=True)


def dome(part, centre, normal, radius, height, rect, weights, sides=10, rings_=3, rim=0.0):
    """Domed rivet/button head sitting on a surface: centre on surface, normal outward."""
    n = Vector(normal).normalized()
    x, y, _ = frame_from(n, (0, 0, 1) if abs(n.z) < .9 else (1, 0, 0))
    c = Vector(centre)
    rows = []
    profile = []
    if rim:
        profile.append((radius + rim, 0.0))
        profile.append((radius + rim, height * .25))
    profile.append((radius, height * .3 if rim else 0.0))
    for k in range(1, rings_ + 1):
        a = (k / (rings_ + 1)) * math.pi / 2
        profile.append((radius * math.cos(a), height * (.3 if rim else 0) + height * (.7 if rim else 1) * math.sin(a)))
    for r, h in profile:
        rows.append([c + n * h + (x * math.cos(math.tau * i / sides) + y * math.sin(math.tau * i / sides)) * r for i in range(sides)])
    ids = grid(part, rows, rect, weights, closed=True)
    top = part.vert(c + n * (height + (0 if rim else 0)), weights(c))
    mid = at(rect, .5, .95)
    for i in range(sides):
        j = (i + 1) % sides
        part.face((ids[-1][i], ids[-1][j], top), [mid] * 3)
    return ids


def build_object(name, parts, material):
    """Join parts into one mesh object with the SkinUV layer and vertex groups."""
    verts = []
    faces = []
    uvs = []
    weights = []
    flat = []
    for p in parts:
        base = len(verts)
        verts.extend(p.v)
        weights.extend(p.w)
        for k, (f, uv) in enumerate(zip(p.f, p.uv)):
            faces.append(tuple(i + base for i in f))
            uvs.append(uv)
            flat.append(p.hard or k in p.flat)
    me = bpy.data.meshes.new(name + 'Mesh')
    me.from_pydata([tuple(v) for v in verts], [], faces)
    me.update()
    layer = me.uv_layers.new(name='SkinUV')
    for poly, uv in zip(me.polygons, uvs):
        for li, co in zip(poly.loop_indices, uv):
            layer.data[li].uv = co
        poly.use_smooth = True
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    groups = {}
    for i, ws in enumerate(weights):
        total = sum(w for w in ws.values() if w > 0) or 1.0
        for bone, w in ws.items():
            if w <= 0:
                continue
            g = groups.get(bone)
            if g is None:
                g = groups[bone] = obj.vertex_groups.new(name=bone)
            g.add([i], w / total, 'REPLACE')
    # Hard-surface faces get split normals through sharp edges between flat faces.
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    for face, is_flat in zip(bm.faces, flat):
        face.smooth = not is_flat
    bm.to_mesh(me)
    bm.free()
    me.materials.append(material)
    return obj


def weld(obj, distance=1e-5):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=distance)
    bm.to_mesh(obj.data)
    bm.free()


class Rig:
    """Rest-pose bone joints read from the shared explorer armature."""

    def __init__(self, armature):
        self.b = {b.name: b for b in armature.data.bones}

    def head(self, n):
        return Vector(self.b[n].head_local)

    def tail(self, n):
        return Vector(self.b[n].tail_local)


def blend(a, b, t):
    t = smooth(t)
    out = {}
    if 1 - t > 0:
        out[a] = 1 - t
    if t > 0:
        out[b] = out.get(b, 0) + t
    return out


def torso_weights(p):
    z = p.z
    if z < 1.10:
        return blend('Hips', 'Spine', (z - 1.04) / .09)
    if z < 1.33:
        return blend('Spine', 'Chest', (z - 1.20) / .12)
    if z < 1.50:
        return blend('Chest', 'Neck', (z - 1.455) / .05)
    return blend('Neck', 'Head', (z - 1.51) / .04)


def arm_weights(side, rig):
    elbow = rig.head('Forearm' + side).z
    wrist = rig.head('Hand' + side).z

    def f(p):
        z = p.z
        if z > elbow - .02:
            return blend('Forearm' + side, 'UpperArm' + side, (z - (elbow - .045)) / .09)
        if z > wrist + .03:
            return {'Forearm' + side: 1}
        return blend('Hand' + side, 'Forearm' + side, (z - (wrist - .005)) / .035)
    return f


def leg_weights(side, rig):
    knee = rig.head('Shin' + side).z
    ankle = rig.head('Foot' + side).z

    def f(p):
        z = p.z
        if z > .90:
            return blend('Thigh' + side, 'Hips', (z - .92) / .10)
        if z > knee - .06:
            return blend('Shin' + side, 'Thigh' + side, (z - (knee - .05)) / .10)
        if z > ankle + .02:
            return blend('Foot' + side, 'Shin' + side, (z - (ankle - .015)) / .085)
        return {'Foot' + side: 1}
    return f


def foot_weights(side, rig):
    ankle = rig.head('Foot' + side).z

    def f(p):
        return blend('Foot' + side, 'Shin' + side, (p.z - (ankle - .015)) / .085)
    return f
