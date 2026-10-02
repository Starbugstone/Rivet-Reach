"""Paint the shared second-generation explorer skin template, surface and normal maps.

Original Rivet Reach artwork, copyright (c) 2026 Starbugstone. See LICENSE.md.
Run inside Blender 5.2 (numpy is bundled); imported by create_explorer_v2.py.

Outputs (2048 px, Assets/RivetReach/Resources/Characters/V2):
  SkinBase.png    sRGB. Tinted cells hold a near-white modulation (stitches, weave, grain,
                  strands, lips); 'fixed' cells hold finished colour. Runtime albedo is
                  base x region tint, so hair, skin and cloth colours are chosen independently.
  SkinSurface.png linear. R metallic, G roughness, B skin (for the shader's light wrap).
  SkinNormal.png  linear tangent-space detail normal (OpenGL convention, +V green).
Both explorer bodies share one UV layout, so these maps serve both models.
"""
import math
import numpy as np
import bpy

S = 2048
RNG_SEED = 2026


def box(rect):
    u0, v0, u1, v1 = rect
    return int(round(v0 * S)), int(round(v1 * S)), int(round(u0 * S)), int(round(u1 * S))


def value_noise(h, w, scale, rng):
    gh, gw = int(h / scale) + 3, int(w / scale) + 3
    g = rng.random((gh, gw))
    y = np.arange(h) / scale
    x = np.arange(w) / scale
    y0 = np.floor(y).astype(int)
    x0 = np.floor(x).astype(int)
    fy = (y - y0)[:, None]
    fx = (x - x0)[None, :]
    fy = fy * fy * (3 - 2 * fy)
    fx = fx * fx * (3 - 2 * fx)
    a = g[y0][:, x0]
    b = g[y0][:, x0 + 1]
    c = g[y0 + 1][:, x0]
    d = g[y0 + 1][:, x0 + 1]
    return a * (1 - fx) * (1 - fy) + b * fx * (1 - fy) + c * (1 - fx) * fy + d * fx * fy


def fbm(h, w, scale, rng, octaves=4):
    out = np.zeros((h, w))
    amp, total = 1.0, 0.0
    for _ in range(octaves):
        out += value_noise(h, w, max(scale, 1.0), rng) * amp
        total += amp
        amp *= .5
        scale /= 2
    return out / total


class Canvas:
    def __init__(self, layout):
        self.L = layout
        self.rng = np.random.default_rng(RNG_SEED)
        self.base = np.ones((S, S, 3))
        self.height = np.zeros((S, S))
        self.metal = np.zeros((S, S))
        self.rough = np.full((S, S), .75)
        self.skin = np.zeros((S, S))
        self.flat_normal = np.zeros((S, S), dtype=bool)

    # --- region fills -------------------------------------------------------------
    def fill(self, rect, rgb=None, mod=None, height=None, rough=None, metal=None, skin=None, flat=False):
        r0, r1, c0, c1 = box(rect)
        if rgb is not None:
            self.base[r0:r1, c0:c1] = rgb
        if mod is not None:
            self.base[r0:r1, c0:c1] *= mod[..., None] if mod.ndim == 2 else mod
        if height is not None:
            self.height[r0:r1, c0:c1] += height
        if rough is not None:
            self.rough[r0:r1, c0:c1] = rough
        if metal is not None:
            self.metal[r0:r1, c0:c1] = metal
        if skin is not None:
            self.skin[r0:r1, c0:c1] = skin
        if flat:
            self.flat_normal[r0:r1, c0:c1] = True

    def shape(self, rect):
        r0, r1, c0, c1 = box(rect)
        return r1 - r0, c1 - c0

    def cloth(self, rect, shade=1.0, weave=1.0, scale=1.0):
        h, w = self.shape(rect)
        yy, xx = np.mgrid[0:h, 0:w]
        twill = .5 + .5 * np.sin((xx + yy) * (2 * math.pi / (3.2 * scale)))
        cross = .5 + .5 * np.sin((xx - yy * .35) * (2 * math.pi / (9.0 * scale)))
        mottle = fbm(h, w, 90, self.rng, 4)
        slub = value_noise(h, w, 6, self.rng)
        mod = shade * (1 - .030 * twill * weave - .012 * cross - .060 * (mottle - .5) - .018 * slub)
        self.fill(rect, mod=mod, height=(.10 * twill + .08 * slub) * weave, rough=.86)

    def leather(self, rect, shade=1.0, scale=1.0, bump=1.0):
        h, w = self.shape(rect)
        grain = value_noise(h, w, 3.0 * scale, self.rng)
        pores = value_noise(h, w, 1.2 * scale, self.rng)
        broad = fbm(h, w, 70 * scale, self.rng, 3)
        mod = shade * (1 - .030 * grain - .020 * pores - .10 * (broad - .5))
        self.fill(rect, mod=mod, height=(.10 * grain + .02 * pores) * bump, rough=.46)

    def skin_fill(self, rect):
        h, w = self.shape(rect)
        n = fbm(h, w, 60, self.rng, 4)
        f = value_noise(h, w, 2.0, self.rng)
        mod = np.ones((h, w, 3))
        mod[..., 0] = 1 - .018 * (n - .5) - .010 * f
        mod[..., 1] = 1 - .032 * (n - .5) - .014 * f
        mod[..., 2] = 1 - .040 * (n - .5) - .016 * f
        self.fill(rect, mod=mod, height=.08 * f, rough=.58, skin=1.0)

    def hair_fill(self, rect, along_v=True):
        h, w = self.shape(rect)
        rng = self.rng
        if along_v:
            strands = value_noise(1, w, 1.6, rng)[0]
            coarse = value_noise(1, w, 9, rng)[0]
            s = np.tile(.6 * strands + .4 * coarse, (h, 1))
            vary = value_noise(h, w, 40, rng) * .25
        else:
            strands = value_noise(h, 1, 1.6, rng)[:, 0]
            coarse = value_noise(h, 1, 9, rng)[:, 0]
            s = np.tile((.6 * strands + .4 * coarse)[:, None], (1, w))
            vary = value_noise(h, w, 40, rng) * .25
        mod = .78 + .22 * s - .08 * vary
        self.fill(rect, mod=mod, height=.9 * s, rough=.62)

    def metal_fill(self, rect, rough=.30):
        h, w = self.shape(rect)
        brushed = np.tile(value_noise(1, w, 1.5, self.rng)[0], (h, 1))
        broad = fbm(h, w, 50, self.rng, 3)
        mod = 1 - .04 * brushed - .10 * (broad - .5)
        self.fill(rect, mod=mod, rough=rough, metal=1.0, flat=True)

    # --- linework -----------------------------------------------------------------
    def line(self, rect, p0, p1, width=1.4, dash=None, dark=.80, raise_=.6, light=False):
        """Stitch or seam in rect-local coordinates (0..1)."""
        r0, r1, c0, c1 = box(rect)
        h, w = r1 - r0, c1 - c0
        x0, y0 = p0[0] * w, p0[1] * h
        x1, y1 = p1[0] * w, p1[1] * h
        length = max(1.0, math.hypot(x1 - x0, y1 - y0))
        pad = int(width + 3)
        xa, xb = int(max(0, min(x0, x1) - pad)), int(min(w, max(x0, x1) + pad + 1))
        ya, yb = int(max(0, min(y0, y1) - pad)), int(min(h, max(y0, y1) + pad + 1))
        if xb <= xa or yb <= ya:
            return
        yy, xx = np.mgrid[ya:yb, xa:xb]
        dx, dy = (x1 - x0) / length, (y1 - y0) / length
        t = (xx - x0) * dx + (yy - y0) * dy
        d = np.abs((xx - x0) * dy - (yy - y0) * dx)
        inside = (t >= 0) & (t <= length)
        a = np.clip(1 - (d - width * .5) / 1.0, 0, 1) * inside
        if dash:
            a *= ((t % dash) < dash * .62)
        region = self.base[r0 + ya:r0 + yb, c0 + xa:c0 + xb]
        factor = 1 + (dark - 1) * a if not light else 1 + (dark - 1) * a
        region *= factor[..., None]
        self.height[r0 + ya:r0 + yb, c0 + xa:c0 + xb] += a * raise_

    def border(self, rect, inset=.03, dash=6.0, edges='LRBT', width=1.3, dark=.80, double=0.0, aspect=1.0):
        ix, iy = inset, inset * aspect
        for e in edges:
            for k in ([0, 1] if double else [0]):
                o = k * double
                if e == 'L':
                    self.line(rect, (ix + o, 0), (ix + o, 1), width, dash, dark)
                if e == 'R':
                    self.line(rect, (1 - ix - o, 0), (1 - ix - o, 1), width, dash, dark)
                if e == 'B':
                    self.line(rect, (0, iy + o * aspect), (1, iy + o * aspect), width, dash, dark)
                if e == 'T':
                    self.line(rect, (0, 1 - iy - o * aspect), (1, 1 - iy - o * aspect), width, dash, dark)

    def blot(self, rect, cx, cy, rx, ry, rgb, softness=.5, raise_=0.0):
        """Soft elliptical tint multiply in rect-local coordinates."""
        r0, r1, c0, c1 = box(rect)
        h, w = r1 - r0, c1 - c0
        yy, xx = np.mgrid[0:h, 0:w]
        d = ((xx / w - cx) / rx) ** 2 + ((yy / h - cy) / ry) ** 2
        a = np.clip((1 - d) / max(softness, 1e-3), 0, 1)
        a = a * a * (3 - 2 * a)
        region = self.base[r0:r1, c0:c1]
        for k in range(3):
            region[..., k] *= 1 + (rgb[k] - 1) * a
        self.height[r0:r1, c0:c1] += a * raise_

    # --- outputs ------------------------------------------------------------------
    def normal(self, strength=1.2):
        h = self.height.copy()
        gy, gx = np.gradient(h)
        n = np.stack((-gx * strength, -gy * strength, np.ones_like(h)), axis=2)
        n /= np.linalg.norm(n, axis=2)[..., None]
        n[self.flat_normal] = (0, 0, 1)
        return n * .5 + .5


def save(name, data, path, linear=False):
    h, w, c = data.shape
    im = bpy.data.images.get(name) or bpy.data.images.new(name, width=w, height=h, alpha=True, float_buffer=False)
    if linear:
        im.colorspace_settings.name = 'Non-Color'
    rgba = np.ones((h, w, 4), dtype=np.float32)
    rgba[..., :c] = np.clip(data, 0, 1)
    im.pixels.foreach_set(rgba.reshape(-1))
    im.filepath_raw = str(path)
    im.file_format = 'PNG'
    im.save()
    return im


def paint(L, out_dir):
    """L is the layout module (create_explorer_v2) providing rects and the head mapping."""
    cv = Canvas(L)
    sub, at = L.sub, L.at
    # Fixed colours
    cv.fill(L.C_EYE_WHITE, rgb=(.93, .92, .88), rough=.18, flat=True)
    cv.fill(L.C_LENS, rgb=(.15, .36, .40), rough=.06, flat=True)
    cv.fill(L.C_SOLE, rgb=(.17, .15, .14), rough=.80)
    cv.fill(L.C_DARK, rgb=(.045, .040, .040), rough=.45, flat=True)
    cv.fill(L.cell(4, 5), rgb=(.5, .5, .5), flat=True)
    cv.fill(L.cell(5, 5), rgb=(.5, .5, .5), flat=True)
    cv.fill(L.cells(4, 7, 4, 7), rgb=(1, 1, 1), flat=True)
    h, w = cv.shape(L.C_SOLE)
    tread = (np.arange(h)[:, None] % 9 < 3) * 1.0
    cv.fill(L.C_SOLE, mod=1 - .25 * tread, height=tread * .8)
    # Skin
    for rect in (L.T_HEAD, L.cells(0, 2, 1, 3), L.cells(2, 2, 3, 3)):
        cv.skin_fill(rect)
    # Nails (first-generation hand UV patches) slightly pinker and glossier.
    for t in (4, 5):
        tx, ty = t % 4, t // 4
        nail = (tx / 4, ty / 4, tx / 4 + .05 / 4, ty / 4 + .05 / 4)
        cv.fill(nail, rgb=(1.0, .90, .88), rough=.32)
    # Face: lips, blush, nostrils, lid crease (shared cylindrical mapping)
    head = L.T_HEAD

    def face_rect_pt(x, z):
        th = -math.pi / 2 + x / .095
        u, v = L.head_uv(th, z)
        return (u - head[0]) / (head[2] - head[0]), (v - head[1]) / (head[3] - head[1])
    hu = (head[2] - head[0]) * S
    hv = (head[3] - head[1]) * S
    cx, cy = face_rect_pt(0, 1.5935)
    cv.blot(head, cx, cy, .026 / .095 / (2 * math.pi) * .94 * 1.05, .0045 / .378 * .78 * 1.05, (.80, .58, .56), .55)
    cx, cy = face_rect_pt(0, 1.5825)
    cv.blot(head, cx, cy, .022 / .095 / (2 * math.pi) * .94, .0052 / .378 * .78, (.84, .62, .60), .6)
    cx, cy = face_rect_pt(0, 1.5888)
    cv.blot(head, cx, cy, .028 / .095 / (2 * math.pi) * .94, .0011 / .378 * .78, (.55, .38, .36), .4)
    for side in (-1, 1):
        cx, cy = face_rect_pt(side * .052, 1.632)
        cv.blot(head, cx, cy, .026 / .095 / (2 * math.pi) * .94, .016 / .378 * .78, (1.0, .92, .90), .9)
        cx, cy = face_rect_pt(side * .0125, 1.6075)
        cv.blot(head, cx, cy, .0030 / .095 / (2 * math.pi) * .94, .0012 / .378 * .78, (.72, .56, .54), .6)
        cx, cy = face_rect_pt(side * .040, 1.682)
        cv.blot(head, cx, cy, .022 / .095 / (2 * math.pi) * .94, .0045 / .378 * .78, (.90, .84, .82), .8)
    cv.fill(sub(head, .975, .05, .985, .07), mod=np.array((.86, .74, .70)))
    for rect in (sub(head, 0, .84, .14, .99), sub(head, .86, .84, 1.0, .99)):
        cv.blot(rect, .5, .5, .4, .5, (.86, .74, .72), .9)
    # Iris: radial streaks, darker limbal ring, lighter collarette
    r0, r1, c0, c1 = box(L.C_IRIS)
    hh, ww = r1 - r0, c1 - c0
    yy, xx = np.mgrid[0:hh, 0:ww]
    dx, dy = (xx / ww - .5) / .46, (yy / hh - .5) / .46
    rr = np.sqrt(dx * dx + dy * dy)
    ang = np.arctan2(dy, dx)
    fib = .5 + .5 * np.sin(ang * 46 + np.sin(ang * 9) * 2)
    iris = .72 + .30 * np.exp(-((rr - .55) / .2) ** 2) - .32 * np.clip((rr - .78) / .22, 0, 1) + .08 * fib - .15 * np.exp(-(rr / .3) ** 2)
    cv.base[r0:r1, c0:c1] *= np.clip(iris, .25, 1.0)[..., None]
    cv.rough[r0:r1, c0:c1] = .15
    cv.flat_normal[r0:r1, c0:c1] = True
    # Cloth: shirt, sleeves, trousers, vest, accent
    cv.cloth(L.T_SHIRT, 1.0)
    for side in ('L', 'R'):
        cv.cloth(L.T_SLEEVE[side], 1.0)
        cv.cloth(L.T_TROUSER[side], 1.0, weave=1.3, scale=1.2)
    cv.cloth(L.T_VEST, 1.0, weave=1.1, scale=1.1)
    cv.cloth(L.C_ACCENT, 1.0)
    # Vest panels: topstitched edges, centre back seam and a back yoke.
    rect_l = sub(L.T_VEST, .02, .52, .48, .99)
    rect_r = sub(L.T_VEST, .52, .52, .98, .99)
    rect_b = sub(L.T_VEST, .02, .01, .98, .48)
    for rect in (rect_l, rect_r):
        cv.border(rect, inset=.035, dash=7, edges='LBT', width=1.3, dark=.78, double=.035)
        cv.border(rect, inset=.02, dash=7, edges='R', width=1.3, dark=.80)
        cv.line(rect, (.0, .0), (.0, 1.0), 2.2, None, .70, .9)
    cv.border(rect_b, inset=.02, dash=7, edges='BT', width=1.3, dark=.78, double=.03)
    cv.line(rect_b, (.5, .0), (.5, 1.0), 2.0, None, .72, 1.0)
    cv.line(rect_b, (.49, 0), (.49, 1), 1.2, 7, .80)
    cv.line(rect_b, (.51, 0), (.51, 1), 1.2, 7, .80)
    cv.line(rect_b, (.06, .66), (.94, .66), 2.0, None, .74, 1.0)
    cv.line(rect_b, (.06, .645), (.94, .645), 1.2, 7, .80)
    # Pocket flaps strip
    cv.fill(sub(L.T_VEST, .9, .49, .99, .51), mod=np.array((.94, .94, .94)))
    # Shirt collar topstitch and placket
    crect = sub(L.T_SHIRT, .01, .76, .99, .99)
    cv.border(crect, inset=.08, dash=6, edges='BT', width=1.2, dark=.82)
    # Sleeves: inner seam with felled double stitch, shoulder seam, and the rolled cuff's reverse face.
    for side in ('L', 'R'):
        srect = sub(L.T_SLEEVE[side], .01, .30, .99, .99)
        cv.fill(srect, mod=np.array((.95, .95, .95)))
        cv.line(srect, (.0, 0), (.0, 1), 2.4, None, .74, 1.0)
        cv.line(srect, (1.0, 0), (1.0, 1), 2.4, None, .74, 1.0)
        cv.line(srect, (.012, 0), (.012, 1), 1.2, 6, .80)
        cv.line(srect, (.988, 0), (.988, 1), 1.2, 6, .80)
        cv.line(srect, (0, .93), (1, .93), 2.0, None, .78, .9)
        roll = sub(L.T_SLEEVE[side], .01, .01, .99, .27)
        for v, d in [(.40, .80), (.48, .80), (.88, .82), (.12, .84)]:
            cv.line(roll, (0, v), (1, v), 3.0, None, d, -.8)
        cv.border(roll, inset=.22, dash=6, edges='T', width=1.2, dark=.82)
    # Trousers: side seams, inseam, hem, fly and knee patches.
    for side in ('L', 'R'):
        leg = sub(L.T_TROUSER[side], .01, .01, .99, .80)
        for u in (.27, .785):
            cv.line(leg, (u, 0), (u, 1), 2.2, None, .74, 1.0)
            cv.line(leg, (u + .012, 0), (u + .012, 1), 1.2, 6, .80)
        cv.line(leg, (0, .02), (1, .02), 1.2, 6, .80)
        pel = sub(L.T_TROUSER[side], .01, .82, .99, .99)
        pel = sub(pel, 0, 0, .62, 1)
        cv.line(pel, (.06, .0), (.06, .75), 1.3, 6, .78)
        cv.line(pel, (.0, .78), (1, .78), 1.3, 6, .80)
        knee = sub(L.T_TROUSER[side], .82, .83, .99, .99)
        cv.fill(knee, mod=np.array((.97, .97, .97)))
        cv.border(knee, inset=.08, dash=5, edges='LRBT', width=1.2, dark=.76)
    # Leather: gloves, straps, pouches, boots
    for side in ('L', 'R'):
        cv.leather(L.C_GLOVE[side], 1.0, bump=.45)
        body = sub(L.C_GLOVE[side], .02, .03, .50, .33)
        for u in (.125, .375, .625, .875):
            cv.line(body, (u, .0), (u, 1.0), 1.2, 5, .72)
        cv.line(body, (0, .86), (1, .86), 1.2, 5, .72)
        cv.line(body, (0, .97), (1, .97), 2.0, None, .78, .6)
        for rect in (sub(L.C_GLOVE[side], .52, .05, .98, .30), sub(L.C_GLOVE[side], .52, .34, .98, .58)):
            cv.border(rect, inset=.12, dash=5, edges='LR', width=1.1, dark=.72)
    cv.leather(L.C_STRAPS, 1.0, scale=.8, bump=.35)
    for rect in [L.U_BELT, L.U_LOOPS, L.U_CINCH[1], L.U_CINCH[-1], L.U_TAB['L'], L.U_TAB['R'], L.U_BOOT_STRAP['L'],
                 L.U_BOOT_STRAP['R'], L.U_GOGGLE_STRAP, L.U_TONGUE['L'], L.U_TONGUE['R']]:
        cv.line(rect, (.2, 0), (.2, 1), 1.1, 5, .70)
        cv.line(rect, (.8, 0), (.8, 1), 1.1, 5, .70)
        cv.line(rect, (.06, 0), (.06, 1), 1.6, None, .84, -.3)
        cv.line(rect, (.94, 0), (.94, 1), 1.6, None, .84, -.3)
    cv.leather(L.C_POUCH, 1.0, bump=.4)
    for rect in (sub(L.C_POUCH, .02, .02, .98, .6), sub(L.C_POUCH, .02, .64, .98, .98)):
        cv.border(rect, inset=.10, dash=5, edges='LRBT', width=1.1, dark=.72, aspect=1.6)
    for side in ('L', 'R'):
        cv.leather(L.C_BOOT[side], 1.0, scale=1.0, bump=.25)
        foot = sub(L.C_BOOT[side], .01, .01, .99, .52)
        cv.line(foot, (0, .62), (1, .62), 1.2, 5, .74)
        cv.line(foot, (.0, .09), (1, .09), 1.4, 5, .72)
        shaft = sub(L.C_BOOT[side], .01, .55, .99, .80)
        cv.line(shaft, (.5, 0), (.5, 1), 2.0, None, .76, 1.0)
        cv.line(shaft, (.488, 0), (.488, 1), 1.1, 5, .78)
        cv.line(shaft, (.512, 0), (.512, 1), 1.1, 5, .78)
        cuff = sub(L.C_BOOT[side], .01, .82, .99, .99)
        cv.line(cuff, (0, .5), (1, .5), 1.1, 5, .76)
    # Hair: strands along each lock and up the cap
    cv.hair_fill(L.T_HAIR, along_v=True)
    cv.fill(L.U_CAP, mod=np.linspace(.86, 1.0, box(L.U_CAP)[1] - box(L.U_CAP)[0])[:, None])
    # Metals
    cv.metal_fill(L.T_BRASS, .32)
    cv.metal_fill(L.T_COPPER, .44)
    cv.fill(L.U_PIPING, metal=.85, rough=.38)
    base = np.clip(cv.base, 0, 1)
    surface = np.stack((cv.metal, cv.rough, cv.skin), axis=2)
    normal = cv.normal()
    save('SkinBase', base, out_dir / 'SkinBase.png')
    save('SkinSurface', surface, out_dir / 'SkinSurface.png', linear=True)
    save('SkinNormal', normal, out_dir / 'SkinNormal.png', linear=True)
    return base, surface, normal
