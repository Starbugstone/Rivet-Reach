"""Author the second-generation Rivet Reach explorers on the existing shared rig.

Original Rivet Reach artwork, copyright (c) 2026 Starbugstone. See LICENSE.md.
Blender 5.2: blender --background --factory-startup --python <abs path> -- [options]

Inputs (read only): ArtSource/Characters/Explorer{Male,Female}.blend supply the 50-bone
rig, the complete 50-clip animation library and the sculpted forearm/hand skin whose
grip contacts are already verified. Everything else is rebuilt here: machinist workwear,
gloves, boots, head, hair and goggles in the machinery palette.

Outputs: ArtSource/Characters/V2/*.blend and Assets/RivetReach/Resources/Characters/V2/.
The first-generation sources and runtime assets are left untouched for reverting.

Options: --male-only / --female-only, --preview (skip bake/export, render review).
"""
import bpy
import bmesh
import math
import json
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
sys.dont_write_bytecode = True
sys.path.insert(0, str(ROOT / 'Tools'))
from explorer_v2_geometry import (Part, grid, ring, tube, strap, dome, at, sub, cell, cells, catmull, resample,
                                  smooth, blend, build_object, weld, Rig, torso_weights, arm_weights,
                                  leg_weights, foot_weights, helper_cube, helper_cylinder, from_object, remove,
                                  frame_from)

V1 = ROOT / 'ArtSource/Characters'
SOURCE = ROOT / 'ArtSource/Characters/V2'
OUT = ROOT / 'Assets/RivetReach/Resources/Characters/V2'
ARGS = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
TAU = math.tau
PI = math.pi

# Semantic 4x4 tiles retain the first generation's meaning; each splits into four tint cells.
T_HEAD = cells(0, 0, 1, 1)
T_SHIRT = cells(2, 0, 3, 1)
T_SLEEVE = {'L': cells(4, 0, 5, 1), 'R': cells(6, 0, 7, 1)}
T_TROUSER = {'L': cells(4, 2, 5, 3), 'R': cells(6, 2, 7, 3)}
C_GLOVE = {'L': cell(0, 4), 'R': cell(1, 4)}
C_STRAPS = cell(0, 5)
C_POUCH = cell(1, 5)
T_HAIR = cells(2, 4, 3, 5)
C_EYE_WHITE = cell(4, 4)
C_LENS = cell(5, 4)
C_BOOT = {'L': cell(6, 4), 'R': cell(7, 4)}
C_SOLE = cell(6, 5)
C_DARK = cell(7, 5)
T_BRASS = cells(0, 6, 1, 7)
T_VEST = cells(2, 6, 3, 7)
C_IRIS = cell(4, 6)
C_ACCENT = cell(5, 6)
T_COPPER = cells(6, 6, 7, 7)
# Named sub-rectangles: strap columns run lengthwise in v; copper parts keep separate islands.
U_BELT = sub(C_STRAPS, .01, .01, .12, .99)
U_LOOPS = sub(C_STRAPS, .13, .01, .18, .99)
U_CINCH = {1: sub(C_STRAPS, .19, .01, .27, .49), -1: sub(C_STRAPS, .19, .51, .27, .99)}
U_TAB = {'L': sub(C_STRAPS, .28, .01, .33, .99), 'R': sub(C_STRAPS, .34, .01, .39, .99)}
U_BOOT_STRAP = {'L': sub(C_STRAPS, .40, .01, .50, .99), 'R': sub(C_STRAPS, .51, .01, .61, .99)}
U_GOGGLE_STRAP = sub(C_STRAPS, .62, .01, .74, .99)
U_TONGUE = {'L': sub(C_STRAPS, .75, .01, .81, .99), 'R': sub(C_STRAPS, .82, .01, .88, .99)}
U_HAIR_TIE = sub(C_STRAPS, .89, .01, .99, .99)
U_PIPING = sub(T_COPPER, .02, .02, .98, .30)
U_CUPS = sub(T_COPPER, .02, .32, .48, .60)
U_RIVETS = sub(T_COPPER, .52, .32, .98, .60)
U_TOE = {'L': sub(T_COPPER, .02, .62, .48, .98), 'R': sub(T_COPPER, .52, .62, .98, .98)}
U_SOLE = {'L': sub(C_SOLE, .02, .30, .48, .98), 'R': sub(C_SOLE, .52, .30, .98, .98)}
U_LOCKS = sub(T_HAIR, .02, .52, .98, .98)
U_BROWS = sub(T_HAIR, .90, .02, .98, .20)
U_CAP = sub(T_HAIR, .02, .02, .86, .48)
HEAD_Z0, HEAD_Z1 = 1.430, 1.808


def dims(female):
    if female:
        return {
            'female': True, 'n': 2.25,
            'side': [(.170, .945), (.166, .985), (.145, 1.06), (.146, 1.14), (.166, 1.22), (.186, 1.29), (.191, 1.35),
                     (.188, 1.395), (.174, 1.425), (.142, 1.446), (.100, 1.459), (.072, 1.466), (.060, 1.470)],
            'front': [(-.112, .945), (-.111, .985), (-.102, 1.06), (-.104, 1.14), (-.120, 1.22), (-.134, 1.29),
                      (-.130, 1.35), (-.116, 1.395), (-.098, 1.425), (-.081, 1.446), (-.067, 1.459), (-.058, 1.466), (-.053, 1.470)],
            'back': [(.106, .945), (.105, .985), (.097, 1.06), (.099, 1.14), (.108, 1.22), (.116, 1.29), (.119, 1.35),
                     (.115, 1.395), (.102, 1.425), (.084, 1.446), (.070, 1.459), (.061, 1.466), (.057, 1.470)],
            'bust': .010,
            'waist': (.152, .100), 'hip': (.182, .110), 'seat': (.180, .108),
            'thigh': .104, 'thighY': .110, 'sleeve': .93, 'boot': .95, 'thighLeg': (.094, .092, .104), 'legScale': 1.0,
        }
    return {
        'female': False, 'n': 2.35,
        'side': [(.178, .945), (.174, .985), (.166, 1.06), (.172, 1.14), (.194, 1.22), (.212, 1.29), (.220, 1.35),
                 (.217, 1.395), (.200, 1.423), (.162, 1.443), (.112, 1.457), (.078, 1.465), (.064, 1.470)],
        'front': [(-.117, .945), (-.116, .985), (-.113, 1.06), (-.117, 1.14), (-.128, 1.22), (-.134, 1.29),
                  (-.131, 1.35), (-.120, 1.395), (-.102, 1.425), (-.084, 1.446), (-.069, 1.459), (-.060, 1.466), (-.055, 1.470)],
        'back': [(.111, .945), (.110, .985), (.106, 1.06), (.110, 1.14), (.120, 1.22), (.127, 1.29), (.129, 1.35),
                 (.123, 1.395), (.108, 1.425), (.088, 1.446), (.073, 1.459), (.064, 1.466), (.060, 1.470)],
        'bust': 0.0,
        'waist': (.164, .104), 'hip': (.173, .108), 'seat': (.171, .106),
        'thigh': .102, 'thighY': .108, 'sleeve': 1.0, 'boot': 1.0, 'thighLeg': (.090, .086, .101), 'legScale': 1.0,
    }


class Torso:
    """Polar superellipse torso surface. theta is the true angle around +Z (0 = +X/left,
    -pi/2 = front); t in [0,1] runs up the shared profile rows to the neck base."""

    def __init__(self, D, inflate=0.0, hem_flare=0.0):
        self.D = D
        self.L = len(D['side']) - 1
        self.inflate = inflate
        self.hem_flare = hem_flare

    def raw(self, th, t):
        D = self.D
        xs, zs = catmull(D['side'], t)
        yf, zf = catmull(D['front'], t)
        yb, zb = catmull(D['back'], t)
        c, s = math.cos(th), math.sin(th)
        depth = -yf if s < 0 else yb
        zf_ = zf if s < 0 else zb
        w = s * s
        z = zs * (1 - w) + zf_ * w
        flare = self.hem_flare * smooth((2.3 / self.L - t) / (1.3 / self.L)) if self.hem_flare else 0.0
        a = xs + self.inflate + flare * .6
        b = depth + self.inflate + flare
        n = D['n']
        r = (abs(c / a) ** n + abs(s / b) ** n) ** (-1 / n)
        x, y = r * c, r * s
        if D['bust'] and s < 0:
            # Modest bust volume with soft cleavage plane, under the fitted vest.
            for side in (-1, 1):
                d = ((th + PI / 2 - side * .42) / .38) ** 2 + ((z - 1.285) / .055) ** 2
                y -= D['bust'] * math.exp(-d)
        return Vector((x, y, z))

    def point(self, th, idx, off=0.0):
        t = idx / self.L
        p = self.raw(th, t)
        if off:
            p += self.normal(th, idx) * off
        return p

    def normal(self, th, idx):
        t = idx / self.L
        e = 1e-4
        dth = self.raw(th + e, t) - self.raw(th - e, t)
        dt = self.raw(th, min(1, t + e)) - self.raw(th, max(0, t - e))
        n = dth.cross(dt)
        return n.normalized()


def coons(bottom, top, left, right):
    ns, nt = len(bottom), len(left)
    B, T, L, R = [Vector(p) for p in bottom], [Vector(p) for p in top], [Vector(p) for p in left], [Vector(p) for p in right]
    rows = []
    for j in range(nt):
        t = j / (nt - 1)
        row = []
        for i in range(ns):
            s = i / (ns - 1)
            p = (1 - t) * B[i] + t * T[i] + (1 - s) * L[j] + s * R[j] - (
                (1 - s) * (1 - t) * B[0] + s * (1 - t) * B[-1] + (1 - s) * t * T[0] + s * t * T[-1])
            row.append((p.x, p.y))
        rows.append(row)
    return rows


def segment(T, keys, count):
    pts = [catmull(keys, k / 40) for k in range(41)] if len(keys) > 2 else [keys[0], keys[1]]
    if len(keys) == 2:
        pts = [tuple(a + (b - a) * k / 20 for a, b in zip(keys[0], keys[1])) for k in range(21)]
    return resample(pts, count + 1, metric=lambda q: T.point(q[0], q[1]))


def chain(T, segments):
    out = []
    for keys, count in segments:
        pts = segment(T, keys, count)
        out.extend(pts if not out else pts[1:])
    return out


def rim(part, T_or_none, boundary, interior, rect_uvs, weights, depth=.0045, roll=.0016, flip=False, normals=None):
    """Turned hem along a boundary row: rounded outer edge folding under the garment."""
    rows = [boundary]
    pos = [part.v[i] for i in boundary]
    inner = [part.v[i] for i in interior]
    ids1, ids2, ids3 = [], [], []
    for k, (p, q) in enumerate(zip(pos, inner)):
        n = normals[k] if normals else Vector((0, 0, 1))
        d = (p - q)
        d = (d - n * d.dot(n)).normalized()
        ids1.append(part.vert(p + d * roll * .9 - n * roll * .55, weights(p)))
        ids2.append(part.vert(p + d * roll * .35 - n * roll * 1.6, weights(p)))
        ids3.append(part.vert(p - d * depth * 1.8 - n * roll * 2.0, weights(p)))
    rows = [boundary, ids1, ids2, ids3]
    uv0 = rect_uvs
    for r in range(3):
        for k in range(len(boundary) - 1):
            a, b, c, d = rows[r][k], rows[r][k + 1], rows[r + 1][k + 1], rows[r + 1][k]
            uvs = [uv0[k], uv0[k + 1], uv0[k + 1], uv0[k]]
            if flip:
                part.face((a, d, c, b), [uvs[0], uvs[3], uvs[2], uvs[1]])
            else:
                part.face((a, b, c, d), uvs)


def panel(part, T, bottom, top, left, right, rect, weights, flip=False, mirror=False):
    rows2 = coons(bottom, top, left, right)
    rows3 = []
    for row in rows2:
        pts = []
        for th, idx in row:
            p = T.point(th, idx)
            if mirror:
                p = Vector((-p.x, p.y, p.z))
            pts.append(p)
        rows3.append(pts)
    ids = grid(part, rows3, rect, weights, flip=flip)
    normals = []
    for row in rows2:
        nr = []
        for th, idx in row:
            n = T.normal(th, idx)
            if mirror:
                n = Vector((-n.x, n.y, n.z))
            nr.append(n)
        normals.append(nr)
    return ids, rows2, normals


def uv_of(part, vid):
    for f, uvs in zip(part.f, part.uv):
        if vid in f:
            return uvs[f.index(vid)]
    return (0, 0)


def vest(D, T):
    part = Part('Machinist vest')
    P = PI
    notch, point, hem_side, hem_back = 2.0, 1.55, 1.86, 1.9
    armpit, armtop, neck_side = 5.0, 9.3, 11.3
    v_bottom = 4.35
    bottom = segment(T, [(-P / 2, notch), (-1.42, 1.72), (-1.24, point), (-.98, 1.70), (-.50, 1.84), (0, hem_side)], 12)
    side = chain(T, [([(0, hem_side), (0, armpit)], 7),
                     ([(0, armpit), (-.30, 5.6), (-.43, 6.6), (-.37, 7.8), (-.20, 8.8), (0, armtop)], 14),
                     ([(0, armtop), (0, neck_side)], 5)])
    front = chain(T, [([(-P / 2, notch), (-P / 2, v_bottom)], 7),
                      ([(-P / 2, v_bottom), (-1.37, 6.2), (-1.19, 8.2), (-1.05, 9.9), (-.96, 11.2)], 19)])
    top = segment(T, [(-.96, 11.2), (-.62, 11.31), (-.3, 11.33), (0, neck_side)], 12)
    tile = T_VEST
    rect_l = sub(tile, .02, .52, .48, .99)
    rect_r = sub(tile, .52, .52, .98, .99)
    rect_b = sub(tile, .02, .01, .98, .48)
    ids_l, rows_l, n_l = panel(part, T, bottom, top, front, side, rect_l, torso_weights)
    ids_r, rows_r, n_r = panel(part, T, bottom, top, front, side, rect_r, torso_weights, flip=True, mirror=True)
    b_bottom = segment(T, [(0, hem_side), (P / 4, 1.88), (P / 2, hem_back), (3 * P / 4, 1.88), (P, hem_side)], 24)
    b_top = segment(T, [(0, neck_side), (P / 4, 11.37), (P / 2, 11.40), (3 * P / 4, 11.37), (P, neck_side)], 24)
    b_left = chain(T, [([(0, hem_side), (0, armpit)], 7),
                       ([(0, armpit), (.32, 5.7), (.45, 6.8), (.38, 8.0), (.21, 8.9), (0, armtop)], 14),
                       ([(0, armtop), (0, neck_side)], 5)])
    b_right = [(P - th, idx) for th, idx in b_left]
    ids_b, rows_b, n_b = panel(part, T, b_bottom, b_top, b_left, b_right, rect_b, torso_weights)

    def edge(ids, normals, which):
        nt = len(ids)
        if which == 'bottom':
            return ids[0], ids[1], normals[0]
        if which == 'top':
            return ids[-1], ids[-2], normals[-1]
        if which == 'left':
            return [ids[j][0] for j in range(nt)], [ids[j][1] for j in range(nt)], [normals[j][0] for j in range(nt)]
        return [ids[j][-1] for j in range(nt)], [ids[j][-2] for j in range(nt)], [normals[j][-1] for j in range(nt)]

    def mirror_n(ns):
        return [Vector((-n.x, n.y, n.z)) for n in ns]

    hems = []
    arm = (7, 7 + 14 + 1)
    for ids, normals, flip, mirrored in [(ids_l, n_l, False, False), (ids_r, n_r, True, True)]:
        for which, rng in [('bottom', None), ('left', None), ('top', None), ('right', arm)]:
            b, i, ns = edge(ids, normals, which)
            if rng:
                b, i, ns = b[rng[0]:rng[1]], i[rng[0]:rng[1]], ns[rng[0]:rng[1]]
            if mirrored:
                ns = mirror_n(ns)
            uvs = [uv_of(part, v) for v in b]
            f = (which in ('bottom', 'right')) != flip
            rim(part, T, b, i, uvs, torso_weights, flip=not f, normals=ns)
            hems.append((which, b, ns, mirrored))
    for which, rng in [('bottom', None), ('top', None), ('left', arm), ('right', arm)]:
        b, i, ns = edge(ids_b, n_b, which)
        if rng:
            b, i, ns = b[rng[0]:rng[1]], i[rng[0]:rng[1]], ns[rng[0]:rng[1]]
        uvs = [uv_of(part, v) for v in b]
        rim(part, T, b, i, uvs, torso_weights, flip=which in ('top', 'left'), normals=ns)
        hems.append(('back' + which, b, ns, False))
    return part, hems, (ids_l, ids_r, ids_b, rows_l, n_l)


def vest_welded(vp):
    return weld_part(vp)


def piping(part, vest_part, hems):
    """Copper piping on the front opening, neckline and hem, echoing machine panel trim."""
    pipe = Part('Copper piping')
    for which, ids, normals, mirrored in hems:
        if which not in ('left', 'top', 'bottom', 'backbottom', 'backtop'):
            continue
        pts = []
        for k, vid in enumerate(ids):
            p = vest_part.v[vid]
            n = normals[k]
            pts.append(p + n * .0012)
        path = pts
        tube(pipe, path, [.0021] * len(path), U_PIPING, torso_weights, sides=6, cap=False,
             hint=(0, 0, 1))
    return pipe


def shirt(D, T):
    part = Part('Work shirt')
    inner = Torso(D, inflate=-.0055)
    nth = 56
    rows = []
    idxs = [2.6 + (12 - 2.6) * k / 22 for k in range(23)]
    for idx in idxs:
        rows.append([inner.point(-PI / 2 + TAU * i / nth, idx) for i in range(nth)])
    grid(part, rows, sub(T_SHIRT, .01, .01, .99, .72), torso_weights, closed=True)
    # Band collar, open at the throat, with a turned top edge.
    collar = []
    zs = [1.452, 1.468, 1.486, 1.502, 1.509]
    for k, z in enumerate(zs):
        rx = .064 - k * .0016 if not D['female'] else .058 - k * .0014
        ry = .061 - k * .0012 if not D['female'] else .056 - k * .0012
        row = []
        for i in range(37):
            a = -PI / 2 + .26 + (TAU - .52) * i / 36
            row.append(Vector((rx * math.cos(a), .008 + ry * math.sin(a), z + .004 * math.sin(a) ** 2)))
        collar.append(row)
    lining = [[p + (Vector((0, .008, p.z)) - p).normalized() * .0035 * Vector((1, 1, 0)).length for p in row] for row in collar]
    for row, inner_row in zip(collar, lining):
        for k, p in enumerate(inner_row):
            d = Vector((p.x, p.y - .008, 0)).normalized()
            inner_row[k] = Vector((row[k].x - d.x * .0035, row[k].y - d.y * .0035, row[k].z))
    crect = sub(T_SHIRT, .01, .76, .99, .99)
    grid(part, collar, crect, torso_weights, flip=True)
    grid(part, [collar[-1], lining[-1]], sub(crect, 0, .9, 1, 1), torso_weights, flip=True)
    grid(part, list(reversed(lining)), sub(crect, 0, 0, 1, .1), torso_weights, flip=True)
    # Placket down the open throat.
    path = [T.point(-PI / 2, idx, -.0035) for idx in [4.5, 6.5, 8.5, 10.4, 11.6]]
    normals = [T.normal(-PI / 2, idx) for idx in [4.5, 6.5, 8.5, 10.4, 11.6]]
    strap(part, path, normals, .021, .003, sub(T_SHIRT, .45, .74, .55, .76), torso_weights)
    buttons = []
    for idx in [6.8, 9.2]:
        p = T.point(-PI / 2, idx, -.0035 + .0016)
        buttons.append((p, T.normal(-PI / 2, idx)))
    return part, buttons


def vest_details(D, T, brass, copper, vest_part):
    """Brass rivet buttons, flap pockets with riveted corners, rear cinch strap."""
    leather = Part('Vest leather')
    for idx in [2.25, 2.95, 3.62, 4.18]:
        th = -PI / 2
        p = T.point(th, idx, .0012)
        n = T.normal(th, idx)
        dome(brass, p, n, .0078, .0046, sub(T_BRASS, .05, .05, .45, .45), torso_weights, sides=12, rim=.0012)
    flap = Part('Pocket flaps')
    for side in (1, -1):
        for (th0, th1, idx0, idx1) in [(-1.30, -.86, 2.45, 3.05)] + ([(-1.30, -.92, 6.9, 7.35)] if side == 1 else []):
            rows = []
            for j in range(4):
                idx = idx0 + (idx1 - idx0) * j / 3
                row = []
                for i in range(9):
                    th = th0 + (th1 - th0) * i / 8
                    point_shape = .22 * (1 - abs(i - 4) / 4) if j == 0 else 0
                    p = T.point(th, idx - point_shape, .0034 - .0012 * (j == 3))
                    row.append(Vector((p.x * side, p.y, p.z)))
                rows.append(row)
            rect = sub(T_VEST, .9, .49, .99, .51)
            ids = grid(flap, rows, rect, torso_weights, flip=side < 0)
            edge = ids[0]
            under = []
            for vid in edge:
                p = flap.v[vid]
                under.append(flap.vert(p + (p - Vector((0, 0, p.z))).normalized() * -.003, torso_weights(p)))
            for k in range(len(edge) - 1):
                a, b, c, d = edge[k], edge[k + 1], under[k + 1], under[k]
                f = (a, b, c, d) if side > 0 else (a, d, c, b)
                flap.face(f, [at(rect, .5, .5)] * 4)
            for th in (th0 + .05, th1 - .05):
                idx = idx0 + .18
                p = T.point(th, idx, .0048)
                n = T.normal(th, idx)
                dome(copper, Vector((p.x * side, p.y, p.z)), Vector((n.x * side, n.y, n.z)), .0042, .0028,
                     U_RIVETS, torso_weights, sides=8)
    for side in (1, -1):
        path = []
        normals = []
        for k in range(6):
            th = PI / 2 - side * (.95 - .9 * k / 5)
            idx = 2.65
            path.append(T.point(th, idx, .0025))
            normals.append(T.normal(th, idx))
        strap(leather, path, normals, .024, .0035, U_CINCH[side], torso_weights)
    p = T.point(PI / 2, 2.65, .006)
    n = T.normal(PI / 2, 2.65)
    buckle(brass, p, n, Vector((1, 0, 0)), .034, .03, torso_weights)
    return leather, flap


def buckle(part, centre, normal, across, w, h, weights, bar=.0045, depth=.004):
    """Square brass buckle frame with a centre prong, sitting on a strap."""
    n = Vector(normal).normalized()
    x = (Vector(across) - n * Vector(across).dot(n)).normalized()
    y = n.cross(x).normalized()
    c = Vector(centre)
    rect = sub(T_BRASS, .55, .05, .95, .45)
    bars = [((0, h / 2 - bar / 2), (w, bar)), ((0, -h / 2 + bar / 2), (w, bar)),
            ((-w / 2 + bar / 2, 0), (bar, h)), ((w / 2 - bar / 2, 0), (bar, h)), ((0, 0), (bar * .55, h * .9))]
    for (ox, oy), (sx, sy) in bars:
        o = helper_cube((0, 0, 0), (sx, sy, depth), bevel=min(sx, sy, depth) * .3, segments=1)
        m = o.data
        for v in m.vertices:
            v.co = c + x * (v.co.x + ox) + y * (v.co.y + oy) + n * (v.co.z + depth * .5)
        from_object(part, o, rect, weights)
        remove(o)


def sleeves(D, R, parts):
    out = []
    k = D['sleeve']
    for side, s in (('L', 1), ('R', -1)):
        sx = R.head('UpperArm' + side).x
        ex = abs(R.head('Forearm' + side).x)
        w = arm_weights(side, R)
        part = Part('Rolled shirt sleeve ' + side)
        spec = [(-.066, .010, 1.441, .024, .028), (-.046, .009, 1.437, .046, .049), (-.024, .007, 1.423, .061, .062),
                (-.007, .005, 1.398, .070, .069), (.004, .003, 1.362, .072, .070), (.012, .0, 1.320, .069, .067),
                (.021, -.004, 1.272, .065, .064), (.031, -.008, 1.228, .063, .062)]
        rows = []
        for dx, y, z, rx, ry in spec:
            rows.append((sx + s * dx * k, y, z, rx * k, ry * k))
        for y, z, rx, ry in [(-.012, 1.188, .063, .062), (-.014, 1.170, .064, .062), (-.016, 1.155, .064, .061)]:
            rows.append((s * (ex + .005), y, z, rx * k, ry * k))
        nth = 28
        pts = []
        for j, (cx, cy, z, rx, ry) in enumerate(rows):
            row = []
            for i in range(nth):
                a = PI / 2 + s * TAU * i / nth
                r_scale = 1.0
                crook = -PI / 2 - s * .55
                da = math.atan2(math.sin(a - crook), math.cos(a - crook))
                for zk, amp in [(1.196, .0045), (1.218, .0035), (1.243, .0025)]:
                    r_scale += amp / rx * math.exp(-((z - zk + .006 * math.cos(a)) / .0075) ** 2) * math.exp(-(da / .7) ** 2) * 6
                r_scale += .03 * math.exp(-((z - 1.17) / .012) ** 2) * (.6 + .4 * math.cos(3 * a + 1.3))
                row.append(Vector((cx + rx * r_scale * math.cos(a), cy + ry * r_scale * math.sin(a), z)))
            pts.append(row)
        ids = grid(part, pts, sub(T_SLEEVE[side], .01, .30, .99, .99), w, closed=True, flip=s < 0)
        top = sum((Vector(q) for q in pts[0]), Vector()) / nth
        cap = part.vert(top + Vector((0, 0, .004)), w(top))
        mid = at(T_SLEEVE[side], .5, .995)
        for i in range(nth):
            j = (i + 1) % nth
            part.face((ids[0][j], ids[0][i], cap) if s > 0 else (ids[0][i], ids[0][j], cap), [mid] * 3)
        # Roll: two flat turned bands with crisp fold edges, tucked to the forearm.
        roll_rect = sub(T_SLEEVE[side], .01, .01, .99, .27)
        prof = [(1.093, .059), (1.095, .066), (1.099, .072), (1.106, .074), (1.122, .074), (1.127, .070),
                (1.131, .068), (1.135, .074), (1.140, .077), (1.157, .077), (1.163, .074), (1.166, .069), (1.163, .062)]
        rrows = []
        cx = s * (ex + .005)
        for z, r in prof:
            row = []
            for i in range(nth):
                a = PI / 2 + s * TAU * i / nth
                wob = 1 + .018 * math.cos(3 * a + .7) + .010 * math.cos(5 * a + 2.1)
                row.append(Vector((cx + r * k * wob * math.cos(a), -.016 + r * k * .96 * wob * math.sin(a), z)))
            rrows.append(row)
        grid(part, rrows, roll_rect, w, closed=True, flip=s < 0)
        out.append(part)
        tab = Part('Sleeve tab ' + side)
        path = []
        normals = []
        for z in [1.168, 1.158, 1.147, 1.136, 1.126]:
            r = .0795 * k if z < 1.162 else .074 * k
            path.append(Vector((cx + s * r, -.016, z)))
            normals.append(Vector((s, 0, 0)))
        strap(tab, path, normals, .016, .0028, U_TAB[side], w)
        out.append(tab)
        parts['brass_tabs'].append((Vector((cx + s * (.0795 * k + .0016), -.016, 1.139)), Vector((s, 0, 0)), w))
    return out


def trousers(D, R):
    """Continuous work trousers: pelvis rings split into two legs through a shared crotch."""
    N = 40
    M = N // 2 + 2
    wa, wb = D['waist']
    ha, hb = D['hip']
    sa, sb = D['seat']
    pel = [(1.046, wa, wb, 0, 0), (1.005, (wa + ha) / 2 + .002, (wb + hb) / 2, 0, 0), (.962, ha, hb, 0, .002),
           (.928, sa, sb, .018, .004)]
    n = 2.2

    def pring(z, a, b, dip, yoff):
        pts = []
        for k in range(N):
            th = -PI / 2 + TAU * k / N
            c, s = math.cos(th), math.sin(th)
            r = (abs(c / a) ** n + abs(s / b) ** n) ** (-1 / n)
            zz = z + dip * (abs(s) ** 6)
            pts.append(Vector((r * c, yoff + r * s, zz)))
        return pts
    rings_ = [pring(*p) for p in pel]
    crotch = Vector((0, .008, .905))
    cx0, rx0, ry0 = D['thighLeg']
    f = D['legScale']
    spec = [(.880, cx0, -.003, rx0, ry0 + .003), (.80, cx0 + .007, -.002, rx0 - .006 * f, ry0 - .005 * f),
            (.70, cx0 + .016, -.002, rx0 - .014 * f, ry0 - .017 * f), (.62, cx0 + .023, -.006, rx0 - .020 * f, ry0 - .025 * f),
            (.555, cx0 + .028, -.010, rx0 - .023 * f, ry0 - .029 * f), (.49, cx0 + .032, -.006, rx0 - .022 * f, ry0 - .029 * f),
            (.42, cx0 + .037, -.002, rx0 - .020 * f, ry0 - .029 * f), (.36, cx0 + .042, .002, rx0 - .018 * f, ry0 - .029 * f),
            (.334, .135, .002, .078, .082), (.314, .137, .001, .085, .088), (.296, .138, .0, .079, .082),
            (.272, .139, .0, .071, .074)]
    part = Part('Work trousers')

    def seat(p, side):
        if p.z > 1.0:
            return {'Hips': 1}
        t = smooth((1.0 - p.z) / .09)
        return {'Hips': 1 - .45 * t, 'Thigh' + side: .45 * t}
    for side, s in (('L', 1), ('R', -1)):
        lw = leg_weights(side, R)
        w = lambda p, side=side, lw=lw: seat(p, side) if p.z > .90 else lw(p)
        top = rings_[-1]
        if s > 0:
            arc = [top[k] for k in range(0, N // 2 + 1)]
        else:
            arc = [top[(N - k) % N] for k in range(0, N // 2 + 1)]
        ring0 = arc + [crotch]
        cxa0 = s * spec[0][1]
        angles = [math.atan2(p.y, s * (p.x - cxa0)) for p in ring0]
        mono = [angles[0]]
        for a in angles[1:]:
            while a < mono[-1]:
                a += TAU
            mono.append(a)
        rows = [ring0]
        for j, (z, cxa, cy, rx, ry) in enumerate(spec):
            blend_ = min(1.0, j / 3)
            row = []
            for i in range(M):
                a = mono[i] * (1 - blend_) + (mono[0] + TAU * i / M) * blend_
                ca, sa_ = math.cos(a), math.sin(a)
                rr = 1.0
                rr += .010 * math.exp(-((a + PI / 2) / .25) ** 2) * (1 if z < .8 else 0)
                rr += .025 * math.exp(-((z - .54) / .05) ** 2) * max(0, -sa_)
                row.append(Vector((s * (cxa + rx * rr * ca), cy + ry * rr * sa_, z)))
            rows.append(row)
        rect = sub(T_TROUSER[side], .01, .01, .99, .80)
        grid(part, rows, rect, w, closed=True, flip=s > 0)
    for side, s in (('L', 1), ('R', -1)):
        rect = sub(T_TROUSER[side], .01, .82, .99, .99)
        rows = []
        for rg in rings_:
            rows.append([rg[k] for k in range(0, N // 2 + 1)] if s > 0 else [rg[(N - k) % N] for k in range(0, N // 2 + 1)])
        rows = list(reversed(rows))

        grid(part, rows, sub(rect, 0, 0, .62, 1), lambda p, side=side: seat(p, side), flip=s < 0)
    # Riveted double-knee reinforcement on each leg.
    knee = Part('Knee patches')
    for side, s in (('L', 1), ('R', -1)):
        w = leg_weights(side, R)
        rows = []
        for z in [.49, .52, .555, .59, .625]:
            sp = min(spec, key=lambda q: abs(q[0] - z))
            j = spec.index(sp)
            lo = spec[max(j - 1, 0)] if spec[j][0] < z else spec[j]
            hi = spec[j] if spec[j][0] < z else spec[min(j + 1, len(spec) - 1)]
            hi, lo = (spec[j], spec[min(j + 1, len(spec) - 1)]) if spec[j][0] >= z else (spec[max(j - 1, 0)], spec[j])
            t = 0 if hi[0] == lo[0] else (z - lo[0]) / (hi[0] - lo[0])
            cxa = lo[1] + (hi[1] - lo[1]) * t
            cy = lo[2] + (hi[2] - lo[2]) * t
            rx = lo[3] + (hi[3] - lo[3]) * t
            ry = lo[4] + (hi[4] - lo[4]) * t
            row = []
            for i in range(9):
                a = -PI / 2 + (i / 8 - .5) * 1.9
                ca, sa_ = math.cos(a), math.sin(a)
                rr = 1.0 + .010 * math.exp(-((a + PI / 2) / .25) ** 2) + .025 * math.exp(-((z - .54) / .05) ** 2) * max(0, -sa_)
                row.append(Vector((s * (cxa + (rx * rr + .0028) * ca), cy + (ry * rr + .0028) * sa_, z)))
            rows.append(row)
        ids = grid(knee, rows, sub(T_TROUSER[side], .82, .83, .99, .99), w, flip=s > 0)
    return weld_part(part), knee


def belt(D, brass, R):
    part = Part('Tool belt')
    wa, wb = D['waist']
    a, b = wa + .009, wb + .009
    n = 2.2
    path = []
    normals = []
    for k in range(65):
        th = -PI / 2 + .07 + (TAU - .14) * k / 64
        c, s = math.cos(th), math.sin(th)
        r = (abs(c / a) ** n + abs(s / b) ** n) ** (-1 / n)
        path.append(Vector((r * c, r * s, 1.012)))
        normals.append(Vector((c, s, 0)))
    strap(part, path, normals, .042, .0055, U_BELT, torso_weights)
    buckle(brass, Vector((0, -b - .004, 1.012)), Vector((0, -1, 0)), Vector((1, 0, 0)), .052, .048, torso_weights, bar=.006, depth=.005)
    for th in [-PI / 2 + .62, -PI / 2 - .62, 0, PI, PI / 2 - .45, PI / 2 + .45]:
        c, s = math.cos(th), math.sin(th)
        r = (abs(c / a) ** n + abs(s / b) ** n) ** (-1 / n) + .0045
        p0 = Vector((r * c, r * s, 1.031))
        p1 = Vector((r * c, r * s, .993))
        strap(part, [p0, (p0 + p1) / 2, p1], [Vector((c, s, 0))] * 3, .012, .0028, U_LOOPS, torso_weights)
    pouch = Part('Belt pouches')
    for side in (1, -1):
        th = PI / 2 - side * 1.05
        c, s = math.cos(th), math.sin(th)
        r = (abs(c / a) ** n + abs(s / b) ** n) ** (-1 / n) + .02
        centre = Vector((r * c, r * s, .975))
        normal = Vector((c, s, 0))
        across = Vector((-s, c, 0))
        sd = 'L' if side > 0 else 'R'
        o = helper_cube((0, 0, 0), (.066, .036, .074), bevel=.009, segments=3)
        for v in o.data.vertices:
            co = v.co.copy()
            v.co = centre + across * co.x + normal * (co.y + (.003 if co.y > 0 else 0)) + Vector((0, 0, co.z))
        from_object(pouch, o, sub(C_POUCH, .02, .02, .98, .6), lambda p, sd=sd: blend('Hips', 'Thigh' + sd, (1.0 - p.z) / .25 * .5), flat=False)
        remove(o)
        flap = helper_cube((0, 0, 0), (.071, .041, .03), bevel=.007, segments=3)
        for v in flap.data.vertices:
            co = v.co.copy()
            v.co = centre + across * co.x + normal * (co.y + .004) + Vector((0, 0, co.z + .028))
        from_object(pouch, flap, sub(C_POUCH, .02, .64, .98, .98), lambda p: {'Hips': 1}, flat=False)
        remove(flap)
        dome(brass, centre + normal * .0245 + Vector((0, 0, .02)), normal, .0058, .0035, sub(T_BRASS, .05, .55, .45, .95),
             lambda p: {'Hips': 1}, sides=10, rim=.001)
    return part, pouch


def boots(D, R, brass, copper):
    """Work boots: vertical shaft, D-section foot loft, welted sole, copper toe cap and laces."""
    out = []
    k = D['boot']
    stations = [(.076, .058, .092), (.063, .071, .120), (.040, .079, .138), (.008, .082, .140), (-.040, .085, .110),
                (-.080, .089, .086), (-.120, .092, .068), (-.155, .091, .056), (-.186, .087, .048), (-.207, .077, .042),
                (-.220, .058, .034), (-.228, .030, .024)]
    sole_top = .033
    for side, s in (('L', 1), ('R', -1)):
        ax = abs(R.head('Foot' + side).x)
        w = foot_weights(side, R)
        cx = s * ax
        part = Part('Work boot ' + side)
        nth = 30
        # Shaft
        shaft = []
        for z, cy, rx, ry in [(.300, .0, .074, .077), (.262, .0, .072, .075), (.215, -.002, .069, .073), (.175, -.006, .070, .076),
                              (.140, -.010, .072, .080), (.095, -.016, .070, .080)]:
            row = []
            for i in range(nth):
                a = PI / 2 + s * TAU * i / nth
                row.append(Vector((cx + rx * k * math.cos(a), cy + ry * k * math.sin(a), z)))
            shaft.append(row)
        grid(part, shaft, sub(C_BOOT[side], .01, .55, .99, .80), w, closed=True, flip=s < 0)
        # Foot: D-shaped sections from heel to toe in the x-z plane.
        nf = 26
        foot = []
        for y, wd, h in stations:
            row = []
            for i in range(nf):
                a = TAU * i / nf
                ca, sa = math.cos(a), math.sin(a)
                ex = math.copysign(abs(ca) ** (2 / 2.25), ca)
                if sa >= 0:
                    zz = sole_top + h * k * (sa ** .72)
                else:
                    zz = sole_top - .004 * (-sa)
                row.append(Vector((cx + s * wd * k * ex, y * k, zz)))
            foot.append(row)
        ids = grid(part, foot, sub(C_BOOT[side], .01, .01, .99, .52), w, closed=True, flip=s < 0)
        for end, rowi, sign in [(0, 0, 1), (1, -1, -1)]:
            row = foot[rowi]
            c = sum((Vector(q) for q in row), Vector()) / nf + Vector((0, sign * (.002 if end == 0 else -.007), 0))
            ci = part.vert(c, w(c))
            mid = at(C_BOOT[side], .5, .02 if end == 0 else .5)
            for i in range(nf):
                j = (i + 1) % nf
                f = (ids[rowi][i], ids[rowi][j], ci)
                if (sign > 0) == (s > 0):
                    f = (ids[rowi][j], ids[rowi][i], ci)
                part.face(f, [mid] * 3)
        # Padded collar at the top of the shaft.
        cuff = []
        for z, r in [(.272, .076), (.278, .083), (.296, .086), (.309, .083), (.313, .077), (.305, .072)]:
            row = []
            for i in range(nth):
                a = PI / 2 + s * TAU * i / nth
                row.append(Vector((cx + r * k * math.cos(a), r * k * 1.03 * math.sin(a), z)))
            cuff.append(row)
        grid(part, cuff, sub(C_BOOT[side], .01, .82, .99, .99), w, closed=True, flip=s < 0)
        out.append(part)
        # Sole: welted outline with a heel block and slight toe spring.
        sole = Part('Boot sole ' + side)
        outline = []
        for y, wd, h in stations:
            outline.append((y * k, wd * k + .006))
        poly = [Vector((cx + s * x_, y, 0)) for y, x_ in outline] + [Vector((cx - s * x_, y, 0)) for y, x_ in reversed(outline)]
        levels = [(0.0, -.003), (.004, .0), (.030, .0), (sole_top + .004, -.002)]
        srows = []
        for z, grow in levels:
            row = []
            for p in poly:
                spring = .012 * smooth((-p.y - .17) / .06)
                d = Vector((p.x - cx, 0, 0))
                row.append(Vector((p.x + (grow if p.x > cx else -grow), p.y, z + spring)))
            srows.append(row)
        sids = grid(sole, srows, U_SOLE[side], w, closed=True, flip=s > 0)
        for rowi, up in [(0, False), (-1, True)]:
            c = sum((Vector(q) for q in srows[rowi]), Vector()) / len(poly)
            ci = sole.vert(c, w(c))
            mid = at(C_SOLE, .5, .12)
            for i in range(len(poly)):
                j = (i + 1) % len(poly)
                f = (sids[rowi][i], sids[rowi][j], ci)
                if up == (s > 0):
                    f = (sids[rowi][j], sids[rowi][i], ci)
                sole.face(f, [mid] * 3)
        out.append(sole)
        # Copper toe cap over the toe box, closed at the front.
        cap = Part('Copper toe cap ' + side)
        crow = []
        toe = stations[7:]
        for y, wd, h in toe:
            row = []
            for i in range(15):
                a = PI * i / 14
                ca, sa = math.cos(a), math.sin(a)
                ex = math.copysign(abs(ca) ** (2 / 2.25), ca)
                row.append(Vector((cx + s * (wd * k + .0027) * ex, y * k, sole_top + .0015 + (h * k + .0027) * (sa ** .72))))
            crow.append(row)
        tip = Vector((cx, (stations[-1][0] - .004) * k, sole_top + .0015))
        crow.append([tip + (q - tip) * .25 + Vector((0, -.0015, 0)) for q in crow[-1]])
        cids = grid(cap, crow, U_TOE[side], w, flip=s < 0)
        # Rolled back edge of the cap.
        back = cids[0]
        under = [cap.vert(cap.v[i] + Vector((0, .0035, -.0012)), w(cap.v[i])) for i in back]
        for j in range(len(back) - 1):
            f = (back[j], back[j + 1], under[j + 1], under[j])
            cap.face(f if s < 0 else tuple(reversed(f)), [at(U_TOE[side], .5, .02)] * 4)
        out.append(cap)
        # Laces up the instep over a tongue.
        surf = Surface(part)
        lace = Part('Boot laces ' + side)
        line = [(-.112, .104, .8), (-.098, .116, .75), (-.089, .133, .55), (-.085, .152, .3), (-.082, .174, .1), (-.079, .198, 0)]

        def cast(x_, y, z, up):
            n0 = Vector((0, -1, up)).normalized()
            loc, n = surf.hit(Vector((x_, y * k, z)) + n0 * .05, -n0, .12)
            return (loc, n) if loc is not None else (Vector((x_, y * k, z)), n0)
        eye = []
        for j, (y, z, up) in enumerate(line[1:]):
            row = []
            for e in (-1, 1):
                half = .015 + .005 * j / 6
                loc, n = cast(cx + e * half, y, z, up)
                dome(brass, loc, n, .0034, .0015, sub(T_BRASS, .55, .55, .95, .95), w, sides=8, rim=.0007)
                row.append((loc, n))
            eye.append(row)
        tongue_path, tongue_n = [], []
        for y, z, up in line:
            loc, n = cast(cx, y, z, up)
            tongue_path.append(loc + n * .0014)
            tongue_n.append(n)
        tongue_n = [Vector((0, -1, .6 if i < 3 else .2)).normalized() for i in range(len(tongue_n))]
        strap(lace, tongue_path, tongue_n, .024, .0018, U_TONGUE[side], w, flat=False)
        for j in range(len(eye) - 1):
            (a1, n1), (b1, m1) = eye[j]
            (a2, n2), (b2, m2) = eye[j + 1]
            for (p_, pn), (q, qn) in [((a1, n1), (b2, m2)), ((b1, m1), (a2, n2))]:
                mid = (p_ + q) / 2 + (pn + qn).normalized() * .0042
                tube(lace, [p_ + pn * .0022, mid, q + qn * .0022], [.0021] * 3, sub(C_DARK, .1, .1, .3, .9), w, sides=6)
        out.append(lace)
        st = Part('Boot strap ' + side)
        path, normals = surf.ring((cx, -.002), .222, 29, offset=.0024)
        strap(st, path, normals, .019, .0032, U_BOOT_STRAP[side], w)
        out.append(st)
        loc, n = surf.hit(Vector((cx + s * .2, -.002, .222)), Vector((-s, 0, 0)), .4)
        buckle(brass, loc + n * .004, n, Vector((0, 1, 0)), .024, .024, w, bar=.0038, depth=.0036)
    return out


def import_v1(old, rig):
    """Forearm/hand skin and nails from the first generation, plus a temporary head."""
    me = old.data
    uv = me.uv_layers.active.data
    names = {g.index: g.name for g in old.vertex_groups}
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    # Connected components to separate the skin from v1 glove shells.
    comp = {}
    cid = 0
    for f in bm.faces:
        if f.index in comp:
            continue
        stack = [f]
        comp[f.index] = cid
        while stack:
            g = stack.pop()
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in comp:
                        comp[h.index] = cid
                        stack.append(h)
        cid += 1
    sizes = {}
    for k, c in comp.items():
        sizes[c] = sizes.get(c, 0) + 1
    hands = Part('Sculpted forearms and hands')
    head = Part('First-generation head (placeholder)')
    remap = {}

    def tile_of(co):
        return int(co[0] * 4) + 4 * int(co[1] * 4), (co[0] * 4) % 1, (co[1] * 4) % 1
    largest = {}
    for f in me.polygons:
        t, lu, lv = tile_of(uv[f.loop_start].uv)
        if t in (4, 5):
            c = comp[f.index]
            if sizes[c] > largest.get(t, (0, 0))[1]:
                largest[t] = (c, sizes[c])
    for f in me.polygons:
        co0 = uv[f.loop_start].uv
        t, lu, lv = tile_of(co0)
        zs = [me.vertices[i].co.z for i in f.vertices]
        target = None
        if t in (4, 5):
            c = comp[f.index]
            nail = lu < .05 and lv < .05
            if c == largest[t][0] or nail:
                target = hands
        elif min(zs) > 1.44 and t in (0, 8, 9, 10, 11, 14):
            target = head
        if target is None:
            continue
        ids = []
        for vi in f.vertices:
            key = (id(target), vi)
            if key not in remap:
                v = me.vertices[vi]
                remap[key] = target.vert(v.co.copy(), {names[g.group]: g.weight for g in v.groups if g.weight > 1e-6})
            ids.append(remap[key])
        uvs = []
        for li in f.loop_indices:
            u = tuple(uv[li].uv)
            tt, lu, lv = tile_of(u)
            if target is hands:
                if lu >= .953:
                    # First-generation glove lining: restore the continuous cylindrical skin mapping.
                    side = 'L' if tt == 4 else 'R'
                    co = me.vertices[me.loops[li].vertex_index].co
                    wx = rig.data.bones['Hand' + side].head_local.x
                    uu = .5 + math.atan2(co.y + .035, co.x - wx) / TAU * .88
                    u = ((int(side == 'R') + uu) / 4, (1 + .13 + (co.z - .79) / .4 * .75) / 4)
            else:
                if t == 8:
                    u = at(C_IRIS, .5, .5)
                elif t == 10:
                    u = at(C_EYE_WHITE, .5, .5)
                elif t == 11:
                    u = at(C_DARK, .5, .5)
                elif t == 14:
                    u = at(T_HEAD, .975, .03)
                elif t == 9:
                    u = at(T_HAIR, .5, .5)
            uvs.append(u)
        target.face(ids, uvs)
    bm.free()
    return hands, head


def palm_dominant(weights):
    if not weights:
        return False
    bone = max(weights, key=weights.get)
    return bone.startswith(('Hand', 'Forearm'))


def gloves(hands, R, brass):
    """Fingerless riveted leather work gloves fitted over the reused hand skin."""
    out = []
    for side in ('L', 'R'):
        s = 1 if side == 'L' else -1
        wx = R.head('Hand' + side).x
        cy = R.head('Hand' + side).y
        dorsal = -(R.b['Hand' + side].matrix_local.to_3x3().col[2]).normalized()
        sel = Part('glove source')
        idmap = {}
        for f, uvs in zip(hands.f, hands.uv):
            vs = [hands.v[i] for i in f]
            if not all(.86 < v.z < 1.02 for v in vs):
                continue
            if not all((v.x > 0) == (s > 0) for v in vs):
                continue
            # Fingerless with a thumb hole: digits keep their verified grip-contact skin.
            if not all(palm_dominant(hands.w[i]) for i in f):
                continue
            new = []
            for i in f:
                if i not in idmap:
                    idmap[i] = sel.vert(hands.v[i], hands.w[i])
                new.append(idmap[i])
            sel.face(new, uvs)
        mat = bpy.data.materials.get('tmp') or bpy.data.materials.new('tmp')
        o = build_object('glove tmp', [sel], mat)
        # Leather needs far fewer facets than the sculpted skin it covers.
        bpy.ops.object.select_all(action='DESELECT')
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        dec = o.modifiers.new('Glove density', 'DECIMATE')
        dec.ratio = .34
        bpy.ops.object.modifier_apply(modifier=dec.name)
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bm.normal_update()
        for v in bm.verts:
            flare = .0018 * smooth((v.co.z - .972) / .02)
            back = max(0.0, v.normal.dot(dorsal))
            v.co += v.normal * (.0006 + .0016 * back + flare)
        geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
        bmesh.ops.bisect_plane(bm, geom=geom, dist=1e-6, plane_co=(0, 0, .8795), plane_no=(0, 0, 1), clear_inner=True)
        geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
        bmesh.ops.bisect_plane(bm, geom=geom, dist=1e-6, plane_co=(0, 0, .992), plane_no=(0, 0, 1), clear_outer=True)
        bm.normal_update()
        boundary = [e for e in bm.edges if e.is_boundary]
        first_rim = len(bm.verts)
        res = bmesh.ops.extrude_edge_only(bm, edges=boundary)
        newv = [g for g in res['geom'] if isinstance(g, bmesh.types.BMVert)]
        axis = Vector((wx, cy, 0))
        for v in newv:
            inward = Vector((axis.x - v.co.x, axis.y - v.co.y, 0))
            inward = inward.normalized() if inward.length > 1e-6 else -v.normal
            # Turned leather lip: fold toward the limb and back into the glove.
            v.co += inward * .0027 + Vector((0, 0, -.0030 if v.co.z > .95 else .0024))
        bm.verts.index_update()
        bm.to_mesh(o.data)
        bm.free()
        o.data.update()
        part = Part('Leather glove ' + side)
        rect = C_GLOVE[side]
        groups = {g.index: g.name for g in o.vertex_groups}
        ids = []
        for v in o.data.vertices:
            ids.append(part.vert(v.co.copy(), {groups[g.group]: g.weight for g in v.groups if g.weight > 1e-6}))
        lip = sub(rect, .54, .64, .96, .94)
        for poly in o.data.polygons:
            if any(vi >= first_rim for vi in poly.vertices):
                part.face([ids[i] for i in poly.vertices], [at(lip, .5 + .4 * math.sin(k), .5 + .4 * math.cos(k)) for k in range(len(poly.vertices))])
                continue
            uvs = []
            for vi in poly.vertices:
                p = o.data.vertices[vi].co
                ang = math.atan2(p.y - cy + .002, s * (p.x - wx))
                u = (ang / TAU) % 1.0
                v = (p.z - .875) / .12
                uvs.append([u, v])
            us = [q[0] for q in uvs]
            if max(us) - min(us) > .5:
                uvs = [[q[0] + 1 if q[0] < .5 else q[0], q[1]] for q in uvs]
            uvs = [at(rect, .02 + .48 * min(q[0], 1.0), .03 + .30 * q[1]) for q in uvs]
            part.face([ids[i] for i in poly.vertices], uvs)
        remove(o)
        out.append(part)
        # Wrist strap with a brass buckle on the back of the wrist.
        surf = Surface(part)
        centre = (wx + dorsal.x * -.004, cy)
        strap_part = Part('Glove straps ' + side)
        hand_w = lambda p, side=side: blend('Hand' + side, 'Forearm' + side, (p.z - .952) / .035)
        path, normals = surf.ring(centre, .962, 31, offset=.0019)
        if len(path) > 10:
            path.append(path[0])
            normals.append(normals[0])
            strap(strap_part, path, normals, .015, .0028, sub(rect, .52, .05, .98, .30), hand_w)
            o_ = Vector((wx, cy, .962)) + dorsal * .2
            loc, n = surf.hit(o_, -dorsal, .4)
            if loc is not None:
                buckle(brass, loc + n * .0042, n, Vector((0, 0, 1)).cross(n), .021, .02, hand_w, bar=.0034, depth=.0032)
        # Padded knuckle guard across the back of the hand, riveted at the ends.
        base = math.atan2(dorsal.y, dorsal.x)
        kpath, kn = surf.ring((wx, cy), .8935, 13, arc=(base - .85, base + .85), offset=.0019)
        keep = [(p_, n_) for p_, n_ in zip(kpath, kn) if (Vector((p_.x - wx, p_.y - cy, 0))).length < .052]
        kpath, kn = [q[0] for q in keep], [q[1] for q in keep]
        if len(kpath) > 4:
            strap(strap_part, kpath, kn, .0125, .0034, sub(rect, .52, .34, .98, .58), lambda p, side=side: {'Hand' + side: 1})
            for idx in (1, len(kpath) // 2, len(kpath) - 2):
                dome(brass, kpath[idx] + kn[idx] * .0017, kn[idx], .0028, .0016, sub(T_BRASS, .55, .55, .95, .95),
                     lambda p, side=side: {'Hand' + side: 1}, sides=8)
        out.append(strap_part)
    return out


def weld_part(part, eps=1e-6):
    """Merge coincident vertices inside a part, averaging their skin weights so seams
    cannot open under deformation."""
    key = lambda v: (round(v.x / eps), round(v.y / eps), round(v.z / eps))
    index = {}
    groups = []
    remap = []
    for i, v in enumerate(part.v):
        k = key(v)
        if k not in index:
            index[k] = len(groups)
            groups.append([i])
        else:
            groups[index[k]].append(i)
        remap.append(index[k])
    out = Part(part.name, part.hard, part.smooth_angle)
    for members in groups:
        w = {}
        for i in members:
            for bone, x in part.w[i].items():
                w[bone] = w.get(bone, 0) + x / len(members)
        out.vert(part.v[members[0]], w)
    for k, (f, uv) in enumerate(zip(part.f, part.uv)):
        ids = [remap[i] for i in f]
        if len(set(ids)) < 3:
            continue
        out.face(ids, uv, flat=k in part.flat)
    return out


def orient_components(obj, rig):
    """Unity culls back faces: make every connected piece face outward.
    Closed pieces use signed volume; open shells vote against the nearest bone segment
    and are only flipped when the vote is decisive."""
    import numpy as np
    segs = [(Vector(b.head_local), Vector(b.tail_local)) for b in rig.data.bones if b.use_deform]
    A = np.array([tuple(h) for h, t in segs])
    B = np.array([tuple(t) for h, t in segs])
    AB = B - A
    L2 = np.maximum((AB * AB).sum(1), 1e-12)

    def nearest(p):
        q = np.array(tuple(p))
        t = np.clip(((q - A) * AB).sum(1) / L2, 0, 1)
        c = A + AB * t[:, None]
        d = ((c - q) ** 2).sum(1)
        return Vector(tuple(c[int(np.argmin(d))]))
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    seen = set()
    flipped = closed_flips = 0
    for f0 in bm.faces:
        if f0.index in seen:
            continue
        comp = []
        stack = [f0]
        seen.add(f0.index)
        while stack:
            f = stack.pop()
            comp.append(f)
            for e in f.edges:
                for g in e.link_faces:
                    if g.index not in seen:
                        seen.add(g.index)
                        stack.append(g)
        edges = {e for f in comp for e in f.edges}
        closed = all(len(e.link_faces) == 2 for e in edges)
        flip = False
        if closed:
            vol = 0.0
            for f in comp:
                vs = [v.co for v in f.verts]
                for k in range(1, len(vs) - 1):
                    vol += vs[0].dot(vs[k].cross(vs[k + 1]))
            flip = vol < 0
            closed_flips += flip
        else:
            vote = total = 0.0
            for f in comp:
                c = f.calc_center_median()
                out = c - nearest(c)
                if out.length < 1e-6:
                    continue
                area = f.calc_area()
                vote += area * f.normal.dot(out.normalized())
                total += area
            flip = total > 0 and vote < -.25 * total
        if flip:
            bmesh.ops.reverse_faces(bm, faces=comp, flip_multires=False)
            flipped += 1
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    print('V2_ORIENTATION', obj.name, 'flipped components', flipped, 'closed', closed_flips, flush=True)


def compact(part, keep):
    out = Part(part.name, part.hard, part.smooth_angle)
    remap = {}
    for f, uv in zip(part.f, part.uv):
        if not keep(f):
            continue
        ids = []
        for i in f:
            if i not in remap:
                remap[i] = out.vert(part.v[i], part.w[i])
            ids.append(remap[i])
        out.face(ids, uv)
    return out


def assemble(female):
    name = 'ExplorerFemale' if female else 'ExplorerMale'
    bpy.ops.wm.open_mainfile(filepath=str(V1 / (name + '.blend')))
    rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    old = next(o for o in bpy.data.objects if o.type == 'MESH')
    rig.data.pose_position = 'REST'
    R = Rig(rig)
    D = dims(female)
    T = Torso(D, hem_flare=.0065)
    extra = {'brass_tabs': []}
    brass = Part('Brass hardware', smooth_angle=40)
    copper = Part('Copper hardware', smooth_angle=40)
    hands, _legacy_head = import_v1(old, rig)
    vp, hems, vinfo = vest(D, T)
    pipe = piping(None, vp, hems)
    vp = vest_welded(vp)
    sh, shirt_buttons = shirt(D, T)
    for p, n in shirt_buttons:
        dome(brass, p, n, .0055, .0028, sub(T_BRASS, .05, .55, .45, .95), torso_weights, sides=10)
    vleather, flaps = vest_details(D, T, brass, copper, vp)
    sl = sleeves(D, R, extra)
    for p, n, w in extra['brass_tabs']:
        dome(brass, p, n, .0055, .003, sub(T_BRASS, .05, .55, .45, .95), w, sides=10, rim=.0008)
    tr, knees = trousers(D, R)
    bl, pouches = belt(D, brass, R)
    bt = boots(D, R, brass, copper)
    gl = gloves(hands, R, brass)
    # Skin fully enclosed by the new gloves is never visible; drop it to fund the new detail.
    hands = compact(hands, lambda f: not (all(.8825 < hands.v[i].z < .9895 for i in f) and all(palm_dominant(hands.w[i]) for i in f)))
    hd, H = head(D, R, brass, copper)
    parts = hd + [hands, vp, pipe, sh, vleather, flaps, tr, knees, bl, pouches, brass, copper] + sl + bt + gl
    mat = old.data.materials[0]
    remove(old)
    if '--breakdown' in ARGS:
        rows = sorted(((sum(len(f) - 2 for f in pt.f), pt.name) for pt in parts), reverse=True)
        print('V2_BREAKDOWN', json.dumps(rows[:30]), flush=True)
    obj = build_object(name + 'Body', parts, mat)
    orient_components(obj, rig)
    obj.parent = rig
    mod = obj.modifiers.new('Weighted explorer skeleton', 'ARMATURE')
    mod.object = rig
    rig.data.pose_position = 'POSE'
    return rig, obj, D


class Surface:
    """Ray queries against Part geometry (rest pose) for placing straps and fittings."""

    def __init__(self, *parts):
        from mathutils.bvhtree import BVHTree
        verts = []
        polys = []
        for p in parts:
            base = len(verts)
            verts.extend(p.v)
            polys.extend([tuple(i + base for i in f) for f in p.f])
        self.tree = BVHTree.FromPolygons(verts, polys, all_triangles=False)

    def hit(self, origin, direction, distance=.5):
        loc, n, i, d = self.tree.ray_cast(Vector(origin), Vector(direction).normalized(), distance)
        return loc, n

    def ring(self, centre, z, count, phase=0.0, radius=.25, arc=None, offset=0.0):
        pts, ns = [], []
        c = Vector((centre[0], centre[1], z))
        a0, a1 = arc if arc else (phase, phase + TAU)
        n_ = count if not arc else count - 1
        for i in range(count):
            a = a0 + (a1 - a0) * i / n_
            d = Vector((math.cos(a), math.sin(a), 0))
            loc, n = self.hit(c + d * radius, -d, radius * 2)
            if loc is None:
                continue
            pts.append(loc + n * offset)
            ns.append(n)
        return pts, ns


def head_uv(th, z):
    """Shared cylindrical face mapping: identical for both bodies so one skin fits both.
    theta runs from -3pi/2 (back seam, u=0) through the face (-pi/2, u=.5) to the seam again."""
    u = max(0.0, min(1.0, (th + PI / 2) / TAU + .5))
    return at(T_HEAD, .03 + .94 * u, .03 + .78 * (z - HEAD_Z0) / (HEAD_Z1 - HEAD_Z0))


class Head:
    """Lofted head and neck: Catmull-Rom sections plus facial relief fields."""

    def __init__(self, female):
        self.female = female
        f = female
        k = .945 if f else 1.0
        # z, half width, front depth, back depth, centre y
        rows = [(1.430, .054 if f else .058, .050 if f else .054, .052 if f else .056, .010),
                (1.470, .049 if f else .055, .046 if f else .051, .049 if f else .054, .010),
                (1.500, .051 if f else .057, .048 if f else .053, .052 if f else .056, .010),
                (1.522, .058 if f else .065, .062 if f else .064, .056 if f else .059, .009),
                (1.538, .066 if f else .077, .074 if f else .078, .060 if f else .063, .008),
                (1.553, .076 if f else .088, .084 if f else .088, .065, .008), (1.572, .086 if f else .095, .090 if f else .093, .071, .008),
                (1.596, .092 if f else .099, .094, .078, .009), (1.625, .096 if f else .101, .096, .088, .010),
                (1.655, .097 if f else .102, .096, .097, .011), (1.685, .098 if f else .102, .094, .103, .012),
                (1.715, .097 if f else .101, .091, .106, .013), (1.742, .092 if f else .096, .084, .104, .015),
                (1.765, .084 if f else .087, .073, .096, .016), (1.783, .068 if f else .070, .058, .081, .017),
                (1.797, .045 if f else .047, .038, .055, .018), (1.805, .017, .014, .021, .018), (1.808, .002, .002, .002, .018)]
        self.rows = rows
        self.zs = [r[0] for r in rows]

    def section(self, z):
        rows = self.rows
        zs = self.zs
        if z <= zs[0]:
            return rows[0][1:]
        if z >= zs[-1]:
            return rows[-1][1:]
        i = max(k for k in range(len(zs) - 1) if zs[k] <= z)
        t = (z - zs[i]) / (zs[i + 1] - zs[i])
        p0, p1, p2, p3 = rows[max(i - 1, 0)], rows[i], rows[i + 1], rows[min(i + 2, len(rows) - 1)]
        out = []
        for c in range(1, 5):
            a, b, cc, d = p0[c], p1[c], p2[c], p3[c]
            out.append(.5 * ((2 * b) + (-a + cc) * t + (2 * a - 5 * b + 4 * cc - d) * t * t + (-a + 3 * b - 3 * cc + d) * t ** 3))
        return tuple(out)

    def relief(self, x, z):
        """Forward (-y) displacement of the face for a point at lateral x and height z."""
        f = self.female
        g = lambda dx, sx, dz, sz: math.exp(-(dx / sx) ** 2 - (dz / sz) ** 2)
        ax = abs(x)
        d = 0.0
        # Eye sockets, brow ridge, temples
        d -= .010 * g(ax - .040, .021, z - 1.667, .012)
        d += (.004 if f else .006) * g(ax - .036, .028, z - 1.6935, .0075)
        d -= .004 * g(ax - .078, .014, z - 1.700, .02)
        # Nose: bridge rising to a rounded tip, septum, wings
        bridge = .0035 + .0125 * smooth((1.690 - z) / .055)
        if z >= 1.627:
            nose = bridge * g(x, .0075 + .0035 * smooth((1.68 - z) / .05), 0, 1)
        else:
            tip = (.0140 if f else .0175) * smooth((z - 1.607) / .020)
            nose = tip * g(x, .0105, 0, 1)
        d += nose * (1 if z > 1.603 else 0)
        d += (.0045 if f else .0055) * g(x, .010, z - 1.6285, .0075)
        d += .0060 * g(ax - .0125, .0060, z - 1.6155, .0055)
        # Cheekbones and mouth area
        d += .005 * g(ax - .050, .024, z - 1.638, .016)
        d += .0045 * g(x, .015, z - 1.599, .0055)
        lip_w = smooth((.0265 - ax) / .007)
        d += (.0055 if f else .0042) * g(x, .019, z - 1.5945, .0032) * lip_w
        d -= .0030 * g(0, 1, z - 1.5890, .0015) * lip_w
        d += (.0060 if f else .0048) * g(x, .017, z - 1.5825, .0038) * lip_w
        d -= .0035 * g(x, .020, z - 1.5735, .0035)
        d += (.0085 if f else .0115) * g(x, .018 if f else .024, z - 1.5575, .0105)
        return d

    def point(self, th, z):
        rx, ryf, ryb, cy = self.section(z)
        c, s = math.cos(th), math.sin(th)
        ry = ryf if s < 0 else ryb
        n = 2.25 if z < 1.52 else (2.0 if z < 1.73 else 2.12)
        r = (abs(c / rx) ** n + abs(s / ry) ** n) ** (-1 / n)
        x, y = r * c, cy + r * s
        front = max(0.0, -s) ** 1.5
        if front > 0 and 1.53 < z < 1.73:
            y -= self.relief(x, z) * front
        if not self.female:
            x += math.copysign(.0045, x) * math.exp(-((z - 1.552) / .013) ** 2) * abs(c) ** 2
        return Vector((x, y, z))


def head(D, R, brass, copper):
    female = D['female']
    H = Head(female)
    parts = []
    skin = Part('Sculpted head and neck')
    nth = 84
    ths = []
    for i in range(nth):
        w_ = 2 * i / nth - 1
        ths.append(-PI / 2 + PI * math.copysign(abs(w_) ** 1.35, w_))
    zs = [1.430, 1.452, 1.474, 1.495, 1.512, 1.526, 1.538, 1.548]
    z = 1.548
    while z < 1.715:
        z += .0036 if 1.570 < z < 1.700 else .0058
        zs.append(round(z, 5))
    for z in [1.726, 1.740, 1.753, 1.765, 1.776, 1.786, 1.794, 1.800, 1.804, 1.807]:
        zs.append(z)
    weights = lambda p: torso_weights(p) if p.z < 1.5 else blend('Neck', 'Head', (p.z - 1.505) / .04)
    ids = []
    for z in zs:
        row = []
        for th in ths:
            p = H.point(th, z)
            row.append(skin.vert(p, weights(p)))
        ids.append(row)
    for r in range(len(zs) - 1):
        for i in range(nth):
            j = (i + 1) % nth
            t0, t1 = ths[i], (ths[j] if j else ths[0] + TAU)
            uv = [head_uv(t0, zs[r]), head_uv(t1, zs[r]), head_uv(t1, zs[r + 1]), head_uv(t0, zs[r + 1])]
            skin.face((ids[r][i], ids[r][j], ids[r + 1][j], ids[r + 1][i]), uv)
    # Close the crown.
    top = skin.vert(H.point(0, 1.808), {'Head': 1})
    for i in range(nth):
        j = (i + 1) % nth
        skin.face((ids[-1][i], ids[-1][j], top), [head_uv(ths[i], 1.807)] * 3)
    parts.append(skin)
    surf = Surface(skin)

    def on_face(x, z, lift=0.0):
        loc, n = surf.hit(Vector((x, -.3, z)), Vector((0, 1, 0)), .5)
        return loc + n * lift, n

    hw = lambda p: {'Head': 1}
    # Eyes: recessed almond sclera, iris (eye colour layer), pupil, catchlight, lids and lashes.
    eye = Part('Eyes')
    lash = Part('Lashes and lids')
    brows = Part('Brows')
    for side in (-1, 1):
        cx, cz = side * .040, 1.667
        hwid, top, bot = (.0206, .0114, .0080) if female else (.0190, .0098, .0072)
        outline = []
        for i in range(28):
            a = TAU * i / 28
            x = cx + hwid * math.cos(a)
            zz = cz + (top if math.sin(a) > 0 else bot) * math.sin(a) + side * (x - cx) * .05
            p, n = on_face(x, zz)
            outline.append(p - n * .0010)
        cp, cn = on_face(cx, cz)
        base_c = cp + cn * .0016
        rings_ = [[base_c.lerp(q, f) + cn * .0019 * (1 - f * f) for q in outline] for f in (.35, .7, 1.0)]
        centre = base_c + cn * .0019
        ev = [centre] + [q for r in rings_ for q in r]
        base = len(eye.v)
        for q in ev:
            eye.vert(q, {'Head': 1})
        white = C_EYE_WHITE
        for i in range(28):
            j = (i + 1) % 28
            eye.face((base, base + 1 + i, base + 1 + j), [at(white, .5, .5)] * 3)
            for r in range(2):
                a0 = base + 1 + r * 28
                eye.face((a0 + i, a0 + 28 + i, a0 + 28 + j, a0 + j), [at(white, .5, .5)] * 4)
        # Iris, pupil and catchlight discs riding the sclera dome.
        look = Vector((0, -1, 0))
        for name, rad, lift, rect, off in [('iris', .0094 if female else .0090, .0006, C_IRIS, (0, 0)),
                                             ('limbal', .0102 if female else .0098, .0003, C_DARK, (0, 0)),
                                             ('pupil', .0043, .0010, C_DARK, (0, 0)), ('catch', .0016, .0014, C_EYE_WHITE, (-.0028 * side, .0030))]:
            c0 = centre + Vector((off[0], 0, off[1] - .0004)) + cn * .0003
            ex_, ey_, _ = frame_from(cn, (0, 0, 1))
            vs = [c0 + cn * lift]
            for i in range(18):
                a = TAU * i / 18
                q = c0 + cn * (lift - .0011 * (rad / .0095) ** 2) + ex_ * rad * math.cos(a) + ey_ * rad * math.sin(a) * (.96 if name != 'catch' else 1)
                vs.append(q)
            b = len(eye.v)
            for q in vs:
                eye.vert(q, {'Head': 1})
            for i in range(18):
                j = (i + 1) % 18
                if name == 'iris':
                    uvs = [at(rect, .5, .5), at(rect, .5 + .46 * math.cos(TAU * j / 18), .5 + .46 * math.sin(TAU * j / 18)),
                           at(rect, .5 + .46 * math.cos(TAU * i / 18), .5 + .46 * math.sin(TAU * i / 18))]
                else:
                    uvs = [at(rect, .5, .5)] * 3
                eye.face((b, b + 1 + j, b + 1 + i), uvs)
        # Upper lid fold with a dark lash line, thin lower lid.
        top_pts = outline[:15]
        bot_pts = outline[14:] + [outline[0]]
        lid_path = [q + Vector((0, -.0018, .0007)) for q in top_pts]
        tube(lash, lid_path, [.0011 + .0016 * math.sin(math.pi * k / 14) for k in range(15)], sub(T_HEAD, .975, .05, .985, .07), hw, sides=7,
             radii_y=[.0014 + .0019 * math.sin(math.pi * k / 14) for k in range(15)])
        lash_pts = [q + Vector((0, -.0026, .0004)) for q in top_pts[1:-1]]
        if female:
            outer = lash_pts[0] if side * (lash_pts[0].x - cx) > 0 else lash_pts[-1]
            flick = outer + Vector((side * .0035, -.0005, .0022))
            lash_pts = (lash_pts + [flick]) if outer is lash_pts[-1] else ([flick] + lash_pts)
        tube(lash, lash_pts, [(.0007 if not female else .0012) * math.sin(math.pi * (k + 1) / (len(lash_pts) + 1)) + .0003 for k in range(len(lash_pts))],
             sub(C_DARK, .1, .1, .3, .3), hw, sides=6)
        tube(lash, [q + Vector((0, -.0010, -.0002)) for q in bot_pts], [.0007] * len(bot_pts), sub(T_HEAD, .975, .05, .985, .07), hw, sides=6)
        # Brows on the hair layer
        bp = []
        bw = []
        for k in range(7):
            f = k / 6
            x = cx - side * .026 + side * .056 * f
            zz = 1.691 + (.007 if not female else .010) * math.sin(math.pi * (f * .9 + .05)) + (.002 if not female else .005) * (f - .3)
            p, n = on_face(x, zz, .0015)
            bp.append(p)
            bw.append((.0013 if female else .0028) * (1 - .55 * f) + .0005 if k not in (0, 6) else .0004)
        tube(brows, bp, bw, sub(U_BROWS, 0 if side < 0 else .5, 0, .5 if side < 0 else 1, 1), hw, sides=6, radii_y=[w_ * .55 for w_ in bw])
    parts += [eye, lash, brows]
    # Ears: sculpted rim and lobe with a recessed bowl.
    ear = Part('Ears')
    for side in (-1, 1):
        rect = sub(T_HEAD, .0 if side < 0 else .86, .84, .14 if side < 0 else 1.0, .99)
        rx_, _, _, cyy = H.section(1.635)
        base_x = side * (rx_ - .006)
        rows = []
        prof = [(1.596, .010, .007), (1.608, .016, .011), (1.626, .020, .013), (1.646, .021, .013), (1.660, .018, .011), (1.668, .010, .006)]
        for z, wd, th_ in prof:
            row = []
            for i in range(14):
                a = TAU * i / 14
                row.append(Vector((base_x + side * (th_ * (1 + math.cos(a)) * .7), cyy + .012 + wd * math.sin(a) * .9 - .002, z)))
            rows.append(row)
        ids_e = grid(ear, rows, rect, hw, closed=True, flip=side < 0)
        top = sum((Vector(q) for q in rows[-1]), Vector()) / 14
        ti = ear.vert(top, {'Head': 1})
        for i in range(14):
            j = (i + 1) % 14
            ear.face((ids_e[-1][i], ids_e[-1][j], ti) if side > 0 else (ids_e[-1][j], ids_e[-1][i], ti), [at(rect, .5, .98)] * 3)
        bot = sum((Vector(q) for q in rows[0]), Vector()) / 14
        bi = ear.vert(bot, {'Head': 1})
        for i in range(14):
            j = (i + 1) % 14
            ear.face((ids_e[0][j], ids_e[0][i], bi) if side > 0 else (ids_e[0][i], ids_e[0][j], bi), [at(rect, .5, .02)] * 3)
        # Helix rim
        path = []
        for k in range(12):
            t = k / 11
            a = -PI * .35 + PI * 1.45 * t
            z = 1.630 + .030 * math.sin(a)
            yy = cyy + .012 + .019 * math.cos(a) * -1
            path.append(Vector((base_x + side * (.020 + .004 * math.sin(a)), yy, z)))
        tube(ear, path, [.0035 * (1 - .4 * abs(2 * k / 11 - 1)) + .001 for k in range(12)], sub(rect, .1, .1, .9, .3), hw, sides=7)
    parts.append(ear)
    hair_parts = hair(D, H, surf, skin)
    parts += hair_parts
    # The strap rides on the hair where it crosses it, and on skin at the forehead.
    parts += goggles(D, H, Surface(skin, hair_parts[0]), brass, copper)
    return parts, H


def hair(D, H, surf, skin):
    female = D['female']
    hw = lambda p: {'Head': 1}
    cap = Part('Hair cap')
    nth = 64
    rows = []

    def hairline(th):
        s = math.sin(th)
        front = max(0, -s)
        if female:
            return 1.704 + .020 * front ** 2 - .132 * max(0, s) ** .6
        return 1.708 + .022 * front ** 2 - .140 * max(0, s) ** .6

    def lift_at(th, f):
        front = max(0, -math.sin(th))
        base = (.0060 if not female else .0052) * smooth(f / .22)
        volume = (.010 if not female else .007) * smooth((f - .1) / .5) * (1 - f ** 3)
        quiff = (.017 if not female else .004) * front ** 1.5 * smooth((f - .05) / .35) * (1 - smooth((f - .62) / .38))
        return .0007 + base + volume + quiff
    for k in range(13):
        f = k / 12
        row = []
        for i in range(nth):
            th = -PI / 2 + TAU * i / nth
            z0 = hairline(th)
            z = z0 + (1.809 - z0) * (1 - (1 - f) ** 1.5)
            p = H.point(th, min(z, 1.806))
            out = Vector((p.x, p.y - .016, 0))
            lift = lift_at(th, f)
            q = p + out.normalized() * lift * (1 - f ** 4) + Vector((0, 0, lift * (f ** 2) * 1.2))
            row.append(q)
        rows.append(row)
    grid(cap, rows, U_CAP, hw, closed=True)
    parts = [cap]
    locks = Part('Hair locks')
    counter = [0]

    def lock(path, widths, depths, span=1):
        pts = [tuple(q) for q in path]
        n = len(pts) * 4
        curve = [Vector(catmull(pts, k / n)) for k in range(n + 1)]
        ws = [catmull([(w_,) for w_ in widths], k / n)[0] for k in range(n + 1)]
        ds = [catmull([(d_,) for d_ in depths], k / n)[0] for k in range(n + 1)]
        # Ribbon-like clumps: wide across the scalp, thin away from it.
        rows_ = []
        for j, p in enumerate(curve):
            t = (curve[min(j + 1, n)] - curve[max(j - 1, 0)]).normalized()
            out = Vector((p.x, p.y - .016, p.z - 1.66)).normalized()
            side = t.cross(out).normalized()
            up = side.cross(t).normalized()
            ring_ = []
            for i in range(8):
                a = -TAU * i / 8
                ring_.append(p + side * math.cos(a) * ws[j] + up * (math.sin(a) * ds[j] + ds[j] * .35 * math.cos(2 * a)))
            rows_.append(ring_)
        k_ = counter[0]
        counter[0] += span
        rect_ = sub(U_LOCKS, k_ / 12 + .003, 0, (k_ + span) / 12 - .003, 1)
        ids = grid(locks, rows_, rect_, hw, closed=True)
        c0 = locks.vert(curve[0], hw(curve[0]))
        c1 = locks.vert(curve[-1], hw(curve[-1]))
        for i in range(8):
            j = (i + 1) % 8
            locks.face((ids[0][j], ids[0][i], c0), [at(rect_, .5, .01)] * 3)
            locks.face((ids[-1][i], ids[-1][j], c1), [at(rect_, .5, .99)] * 3)

    def scalp(th, z, lift):
        p = H.point(th, min(z, 1.806))
        out = Vector((p.x, p.y - .016, 0)).normalized()
        return p + out * lift + Vector((0, 0, lift * .8 if z > 1.775 else 0))
    if not female:
        # Swept-back quiff: broad clumps rising off the forehead and flowing over the crown.
        for i, (th, rise, wd) in enumerate([(-1.90, .010, .020), (-1.66, .015, .023), (-1.44, .018, .024), (-1.22, .015, .022),
                                            (-1.00, .010, .019)]):
            back = PI / 2 + (th + PI / 2) * .55
            path = [scalp(th, 1.726, .005), scalp(th, 1.744, .016 + rise * .7), scalp(th * .8 - .25, 1.772, .022 + rise),
                    scalp(th * .5 - .75, 1.798, .019 + rise * .6), scalp(back, 1.792, .012), scalp(back, 1.766, .005)]
            lock(path, [wd * .55, wd, wd * 1.05, wd, wd * .7, .003], [.0035, .0065, .0075, .0065, .0045, .001], span=2)
    else:
        for i in range(6):
            th = -1.98 + .27 * i
            path = [scalp(PI / 2 - .35 + i * .07, 1.802, .008), scalp(-PI / 2 + .95 - .06 * i, 1.797, .013), scalp(th + .30, 1.776, .016),
                    scalp(th + .05, 1.752, .019), scalp(th - .18, 1.741, .016), scalp(th - .40, 1.738, .008)]
            lock(path, [.011, .018, .021, .020, .015, .003], [.004, .006, .007, .007, .005, .001])
        for side in (-1, 1):
            th = 0 if side > 0 else PI
            path = [scalp(th - side * .3, 1.768, .009), scalp(th - side * .15, 1.728, .010), scalp(th, 1.692, .008), scalp(th + side * .1, 1.662, .003)]
            lock(path, [.017, .021, .016, .003], [.005, .007, .006, .001])
        for side in (-1, 1):
            th = -PI / 2 + side * 1.02
            path = [scalp(th - side * .10, 1.742, .010), scalp(th, 1.712, .012), scalp(th + side * .06, 1.672, .011),
                    scalp(th + side * .08, 1.632, .010), scalp(th + side * .06, 1.598, .005)]
            lock(path, [.011, .014, .013, .011, .002], [.004, .006, .006, .005, .001])
        path = [Vector((0, .097, 1.702)), Vector((0, .130, 1.658)), Vector((-.008, .150, 1.603)), Vector((-.010, .158, 1.543)),
                Vector((-.004, .150, 1.483)), Vector((.004, .138, 1.443))]
        lock(path, [.030, .044, .046, .040, .026, .003], [.022, .030, .032, .027, .018, .002], span=2)
        tube(locks, [Vector((0, .106, 1.683)), Vector((0, .118, 1.667))], [.031, .032], U_HAIR_TIE, hw, sides=12, radii_y=[.025, .026])
    parts.append(locks)
    return parts


def goggles(D, H, surf, brass, copper):
    """Brass-rimmed workshop goggles resting on the forehead, strap over the hair."""
    hw = lambda p: {'Head': 1}
    out = Part('Goggle cups')
    lens = Part('Goggle lenses')
    strap_p = Part('Goggle strap')
    z = 1.719 if not D['female'] else 1.721
    centres = []
    for side in (-1, 1):
        th = -PI / 2 + side * .40
        p = H.point(th, z)
        ctr = Vector((0, .012, p.z))
        n = (p - ctr)
        n.z = 0
        n = (n.normalized() + Vector((0, 0, .30))).normalized()
        c = p + n * .0155
        centres.append((c, n))
        x_, y_, _ = frame_from(n, (0, 0, 1))
        rows = []
        for rr, dd in [(.0190, -.0150), (.0212, -.0070), (.0218, .0), (.0206, .0016)]:
            rows.append([c + n * dd + (x_ * math.cos(TAU * i / 20) + y_ * math.sin(TAU * i / 20)) * rr for i in range(20)])
        grid(out, rows, sub(U_CUPS, 0 if side < 0 else .5, 0, .5 if side < 0 else 1, 1), hw, closed=True)
        ring_rows = []
        for rr, dd in [(.0222, .0008), (.0230, .0027), (.0216, .0048), (.0172, .0044), (.0163, .0024)]:
            ring_rows.append([c + n * dd + (x_ * math.cos(TAU * i / 20) + y_ * math.sin(TAU * i / 20)) * rr for i in range(20)])
        grid(brass, ring_rows, sub(T_BRASS, .55, .05, .95, .45), hw, closed=True)
        b = len(lens.v)
        lc = c + n * .0030
        lens.vert(lc + n * .0011, hw(lc))
        for i in range(20):
            lens.vert(lc + (x_ * math.cos(TAU * i / 20) + y_ * math.sin(TAU * i / 20)) * .0166, hw(lc))
        for i in range(20):
            j = (i + 1) % 20
            lens.face((b, b + 1 + i, b + 1 + j), [at(C_LENS, .5, .5)] * 3)
        for a in (.6, .6 + PI):
            d_ = x_ * math.cos(a) + y_ * math.sin(a)
            dome(brass, c + n * .0018 + d_ * .0232, d_, .0024, .0015, sub(T_BRASS, .55, .55, .95, .95), hw, sides=6)
    (c0, n0), (c1, n1) = centres
    across = (c1 - c0).normalized()
    tube(brass, [c0 + n0 * .003 + across * .021, (c0 + c1) / 2 + (n0 + n1).normalized() * .006, c1 + n1 * .003 - across * .021],
         [.0024] * 3, sub(T_BRASS, .55, .05, .95, .45), hw, sides=6)
    path = []
    normals = []
    for k in range(43):
        th = -PI / 2 + .66 + (TAU - 1.32) * k / 42
        zz = z + .018 * math.sin(math.pi * k / 42) ** 1.5
        p = surf_point = None
        loc, n = surf.hit(Vector((0, .012, zz)) + Vector((math.cos(th), math.sin(th), 0)) * .3, -Vector((math.cos(th), math.sin(th), 0)), .4)
        if loc is None:
            continue
        o = Vector((math.cos(th), math.sin(th), 0))
        path.append(loc + n * .0026)
        normals.append(o)
    strap(strap_p, path, normals, .018, .0030, U_GOGGLE_STRAP, hw, flat=False)
    return [out, lens, strap_p]


def bake_pose(rig, spread):
    """Arms and legs moved apart so occlusion captures creases, not limb contact."""
    from mathutils import Euler
    rig.animation_data.action = None
    for b in rig.pose.bones:
        b.rotation_mode = 'QUATERNION'
        b.rotation_quaternion = (1, 0, 0, 0)
        b.location = (0, 0, 0)
    if not spread:
        return
    for name, deg in [('UpperArmL', -38), ('UpperArmR', 38), ('ThighL', -9), ('ThighR', 9)]:
        b = rig.pose.bones[name]
        basis = b.bone.matrix_local.to_quaternion()
        q = Euler((0, math.radians(deg), 0), 'XYZ').to_quaternion()
        b.rotation_quaternion = basis.inverted() @ q @ basis
    bpy.context.view_layer.update()


def bake_occlusion(rig, obj):
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 96
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Occlusion')
    scene.world.light_settings.distance = .055
    bake_pose(rig, True)
    hand = rig.matrix_world @ rig.pose.bones['HandL'].head
    attr = obj.data.color_attributes.get('Occlusion') or obj.data.color_attributes.new('Occlusion', 'FLOAT_COLOR', 'POINT')
    obj.data.color_attributes.active_color = attr
    obj.data.color_attributes.render_color_index = obj.data.color_attributes.active_color_index
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.bake(type='AO', target='VERTEX_COLORS', use_clear=True)
    bake_pose(rig, False)
    values = [attr.data[i].color[0] for i in range(len(attr.data))]
    print('V2_OCCLUSION', obj.name, 'spread hand', tuple(round(c, 3) for c in hand), 'min', round(min(values), 3), 'mean', round(sum(values) / len(values), 3), flush=True)


def finalize(rig, obj, D, name):
    import explorer_v2_textures  # noqa: F401  (ensures module availability for reports)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='WEIGHT_PAINT')
    bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL', limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL', lock_active=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    bake_occlusion(rig, obj)
    # Source preview material: base x tint is runtime-only; show the template base map here.
    mat = obj.data.materials[0]
    mat.name = 'ExplorerV2Skin'
    nodes = mat.node_tree.nodes
    for node in list(nodes):
        if node.type in ('TEX_IMAGE', 'SEPARATE_COLOR', 'NORMAL_MAP', 'MATH', 'MIX_RGB', 'TEX_COORD', 'SEPARATE_XYZ', 'MIX'):
            nodes.remove(node)
    bsdf = nodes.get('Principled BSDF')
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(str(OUT / 'SkinBase.png'), check_existing=True)
    tex.image.filepath = bpy.path.relpath(str(OUT / 'SkinBase.png'), start=str(SOURCE))
    mat.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    for im in list(bpy.data.images):
        if im.users == 0:
            bpy.data.images.remove(im)
    use = bpy.data.actions.get('Idle')
    rig.animation_data.action = use
    if hasattr(rig.animation_data, 'action_slot') and use is not None and len(use.slots):
        rig.animation_data.action_slot = use.slots[0]
    bpy.context.scene.frame_set(1)
    bpy.context.scene.render.engine = 'BLENDER_EEVEE'
    rig['authoring_notes'] = ('Second-generation explorer (machinist workwear) on the shared first-generation rig and clips. '
                              'Blender +Z up, -Y forward. Generated by Tools/create_explorer_v2.py.')
    obj['explorer_generation'] = 2
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / (name + '.blend')), relative_remap=False)
    bpy.ops.export_scene.fbx(filepath=str(OUT / (name + '.fbx')), use_selection=True, object_types={'ARMATURE', 'MESH'},
                             add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                             bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
                             use_armature_deform_only=False, colors_type='LINEAR', prioritize_active_color=True)
    obj.data.calc_loop_triangles()
    clips = [{'name': a.name, 'seconds': round((a.frame_range[1] - a.frame_range[0]) / 30, 4), 'loop': bool(a.get('loop', False))}
             for a in bpy.data.actions]
    return {'model': name, 'generation': 2, 'triangles': len(obj.data.loop_triangles), 'vertices': len(obj.data.vertices),
            'bones': len(rig.data.bones), 'deformBones': sum(b.use_deform for b in rig.data.bones),
            'materials': len(obj.data.materials), 'uvLayers': [l.name for l in obj.data.uv_layers],
            'colorAttributes': [a.name for a in obj.data.color_attributes],
            'maxInfluences': max(sum(g.weight > 1e-6 for g in v.groups) for v in obj.data.vertices),
            'sockets': ['BlockSocket', 'ToolSocket'], 'clips': clips}


if __name__ == '__main__':
    targets = [False, True]
    if '--male-only' in ARGS:
        targets = [False]
    if '--female-only' in ARGS:
        targets = [True]
    SOURCE.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    preview = '--preview' in ARGS
    if not preview or '--textures' in ARGS:
        import explorer_v2_textures
        explorer_v2_textures.paint(sys.modules[__name__], OUT)
        print('V2_TEXTURES_PAINTED', flush=True)
    report = []
    for female in targets:
        rig, obj, D = assemble(female)
        name = 'ExplorerFemale' if female else 'ExplorerMale'
        obj.data.calc_loop_triangles()
        print('V2_TRIANGLES', obj.name, len(obj.data.loop_triangles), flush=True)
        if preview:
            bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Logs/AvatarV2' / (obj.name + '-preview.blend')))
            continue
        report.append(finalize(rig, obj, D, name))
    if not preview:
        path = SOURCE / 'asset-report.json'
        existing = json.loads(path.read_text()) if path.exists() else []
        merged = {e['model']: e for e in existing}
        merged.update({e['model']: e for e in report})
        path.write_text(json.dumps([merged[k] for k in sorted(merged, reverse=True)], indent=2) + '\n')
        print('EXPLORER_V2_EXPORT_PASS', json.dumps([{k: e[k] for k in ('model', 'triangles', 'maxInfluences', 'bones')} for e in report]), flush=True)
