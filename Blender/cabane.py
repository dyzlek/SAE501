# Cabane du hub (SAÉ 5D.01) — modèle 3D entièrement généré par ce script Blender (aucun fichier téléchargé).
#
# Lancer depuis la racine du dépôt :
#   blender -b --python Blender/cabane.py                       -> exporte les .glb dans Unity/Assets/_Project/Art/Cabane
#   blender -b --python Blender/cabane.py -- --render <dossier>  -> exporte, puis rend des images d'aperçu dans <dossier>
#
# Ce qu'il fabrique :
#   Cabane.glb   : plancher-terrasse, murs en rondins croisés aux angles, grande porte (derrière) avec son battant ouvert,
#                  3 fenêtres à volets, toit de chaume conique et sa charpente, lustre en roue de charrette,
#                  2 lanternes murales, tapis rond à franges, cible de fléchettes, herbe, rochers et buissons dehors ;
#                  plus des repères vides (Repere_*, Lumiere_*) que Unity lit pour s'aligner et poser ses lumières.
#   Tonneau.glb, Caisse.glb, Regime.glb : les accessoires que Unity pose lui-même (PrototypeGenerator.BuildDecor).
#
# Repère : on raisonne comme dans Unity (Y en haut, +Z = devant le joueur, angle 0 = devant, positif = à droite).
# P() convertit un point Unity en point Blender. Les mesures sont celles de HubLayout.cs (à garder identiques).

import bpy, bmesh, math, random, os, sys, tempfile
import numpy as np
from mathutils import Vector, Matrix

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Unity", "Assets", "_Project", "Art", "Cabane")
TEXDIR = os.path.join(tempfile.gettempdir(), "sae_cabane_textures")

# --- mesures (HubLayout.cs) ---
R = 3.4            # distance du centre au milieu d'un mur (CabinRadius)
H = 3.0            # hauteur des murs (CabinHeight)
RUG = 1.5          # rayon du tapis (CarpetRadius)
SIDES = 12
LOG_R = 0.12       # rayon d'un rondin
DOOR_H, DOOR_W = 2.4, 1.3
WIN_Y0, WIN_Y1, WIN_W = 1.72, 2.42, 0.9
WINDOWS = (60, 150, 210)          # murs avec une fenêtre (angle du mur)
DOOR = 180
ROOF_EAVE_R, ROOF_EAVE_Y, ROOF_TOP_Y = R + 0.55, 2.98, 5.0
GROUND_Y = -0.35

rng = random.Random(7)


def P(x, y, z):
    """Point Unity (x, y, z) -> Blender. glTF/glTFast : Blender (x, y, z) arrive en Unity (-x, z, -y)."""
    return Vector((-x, -z, y))


def polar(angle_deg, r, y=0.0):
    a = math.radians(angle_deg)
    return P(math.sin(a) * r, y, math.cos(a) * r)


# ============================================================ textures (numpy)

def tnoise(n, fx, fy, seed, m=None):
    """Bruit lisse et RACCORDABLE (périodique) : bruit blanc filtré en fréquence. fx, fy = finesse en x / en y."""
    m = m or n
    g = np.random.default_rng(seed)
    w = g.standard_normal((m, n))
    F = np.fft.fft2(w)
    ky = np.fft.fftfreq(m)[:, None] * m
    kx = np.fft.fftfreq(n)[None, :] * n
    r = np.real(np.fft.ifft2(F * np.exp(-((kx / fx) ** 2 + (ky / fy) ** 2))))
    return (r - r.mean()) / (r.std() + 1e-9)


def grain(n, base, lines, seed, m=None, warp=2.5, contrast=0.2):
    """Bois : veines le long de l'axe v (lignes ondulées), taches et pores. Renvoie (couleur, hauteur)."""
    m = m or n
    x = np.arange(n)[None, :] / n
    w = tnoise(n, 3, 1.5, seed, m)
    phase = 2 * np.pi * (lines * x + 0.04 * w * warp)
    l = (0.5 + 0.5 * np.sin(phase)) ** 4                       # lignes fines et sombres
    blotch = tnoise(n, 5, 2, seed + 1, m)
    pores = tnoise(n, 160, 12, seed + 2, m)
    shade = (0.9 + 0.08 * blotch) * (1 - contrast * l) * (1 + 0.035 * pores)
    col = np.clip(np.array(base)[None, None, :] * shade[..., None], 0, 1)
    height = 0.55 * (1 - l) + 0.08 * pores + 0.1 * blotch
    return col, height


def normal_from_height(h, strength):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5 * strength
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5 * strength
    nrm = np.dstack([-dx, -dy, np.ones_like(h)])
    nrm /= np.linalg.norm(nrm, axis=2, keepdims=True)
    return nrm * 0.5 + 0.5


def image(name, rgb, alpha=None, data=False, fmt="JPEG"):
    """Tableau numpy (lignes = du bas vers le haut) -> image Blender enregistrée sur le disque."""
    hgt, wid = rgb.shape[:2]
    a = np.ones((hgt, wid)) if alpha is None else alpha
    px = np.dstack([rgb, a]).astype(np.float32)
    img = bpy.data.images.new(name, wid, hgt, alpha=alpha is not None)
    if data:
        img.colorspace_settings.name = "Non-Color"
    img.pixels.foreach_set(px.ravel())
    os.makedirs(TEXDIR, exist_ok=True)
    ext = "png" if fmt == "PNG" else "jpg"
    img.filepath_raw = os.path.join(TEXDIR, f"{name}.{ext}")
    img.file_format = fmt
    img.save()
    return img


def tex_wood(name, base, lines, seed, n=1024, strength=6.0):
    col, h = grain(n, base, lines, seed)
    return image(name, col), image(name + "_N", normal_from_height(h, strength), data=True)


def tex_log_end(n=512):
    y, x = np.mgrid[0:n, 0:n] / (n - 1) * 2 - 1
    r = np.sqrt(x * x + y * y)
    w = tnoise(n, 6, 6, 31)
    rings = (0.5 + 0.5 * np.sin(2 * np.pi * (r * 11 + 0.15 * w))) ** 2
    col = np.array([0.86, 0.66, 0.42]) * (0.92 - 0.18 * rings)[..., None]
    bark = np.clip((r - 0.86) / 0.06, 0, 1)[..., None]
    col = col * (1 - bark) + np.array([0.33, 0.2, 0.11]) * bark
    crack = (np.abs(np.arctan2(y, x) - 0.7) < 0.012) & (r < 0.75)
    col[crack] *= 0.4
    h = 0.6 * (1 - rings) - 0.5 * bark[..., 0]
    return image("Rondin_Bout", np.clip(col, 0, 1)), image("Rondin_Bout_N", normal_from_height(h, 4), data=True)


def tex_thatch(n=1024):
    """Chaume : des brins de paille verticaux, en 6 rangées qui se recouvrent, pointes irrégulières."""
    y = np.arange(n)[:, None] / n
    strands = tnoise(n, 220, 5, 41)
    clumps = tnoise(n, 24, 3, 42)
    rows = 6
    t = (y * rows) % 1.0                                  # 0 = bas de la rangée (pointes), 1 = haut (sous la rangée du dessus)
    tips = 0.06 * tnoise(n, 60, 1, 43, 1)[0][None, :]      # bas irrégulier
    t2 = np.clip(t - tips, 0, 1)
    under = np.clip((t2 - 0.7) / 0.3, 0, 1)                # ombre sous la rangée du dessus
    shade = (0.82 + 0.1 * strands + 0.06 * clumps) * (1 - 0.45 * under) * (1 - 0.35 * (t2 < 0.04))
    base = np.array([0.80, 0.62, 0.32])
    col = np.clip(base * shade[..., None], 0, 1)
    h = 0.5 + 0.25 * strands - 0.6 * under + 0.3 * (1 - t2)
    return image("Chaume", col), image("Chaume_N", normal_from_height(h, 7), data=True)


def tex_rug(n=1024):
    """Tapis rond : bordure dorée, frise de losanges, champ rouge à fleur, médaillon, franges (transparence)."""
    y, x = np.mgrid[0:n, 0:n] / (n - 1) * 2 - 1
    r = np.sqrt(x * x + y * y)
    a = np.arctan2(y, x)
    gold, red, navy, cream, rust = [np.array(c) for c in ((0.86, 0.64, 0.24), (0.58, 0.09, 0.07), (0.1, 0.13, 0.28), (0.93, 0.86, 0.68), (0.75, 0.32, 0.12))]
    col = np.zeros((n, n, 3)) + red
    rr = r / 0.92                                        # le tissu va jusqu'à 0,92 ; au-delà, les franges
    petal = np.abs(np.cos(a * 4)) * 0.22 + 0.36
    field = (rr < 0.68) & (rr > 0.3)
    col[field & (np.abs(rr - petal - 0.1) < 0.012)] = gold
    col[field & (np.abs(rr - petal * 0.7 - 0.12) < 0.008)] = cream
    dots = field & (np.abs(rr - 0.5) < 0.02) & (np.cos(a * 24) > 0.85)
    col[dots] = cream
    med = rr < 0.3
    star = np.abs(np.cos(a * 4)) * 0.2 + 0.06 > rr
    col[med] = navy
    col[med & star] = cream
    col[med & (rr < 0.07)] = rust
    col[(rr >= 0.28) & (rr < 0.3)] = gold
    band = (rr >= 0.7) & (rr < 0.9)
    t = np.mod(a / (2 * np.pi) * 28, 1) - 0.5
    k = (rr - 0.8) / 0.1
    diamond = np.abs(t) * 2 + np.abs(k) < 0.85
    col[band] = navy
    col[band & diamond] = cream
    col[band & (np.abs(t) * 2 + np.abs(k) < 0.35)] = rust
    col[(rr >= 0.68) & (rr < 0.7)] = gold
    col[(rr >= 0.9) & (rr <= 1.0)] = gold
    wool = tnoise(n, 300, 300, 51)
    col = col * (0.92 + 0.05 * wool)[..., None]
    alpha = (r <= 0.92).astype(float)
    # Franges : des brins crème tout autour, de longueur un peu irrégulière
    strand = (np.cos(a * 360) > -0.1)
    length = 0.92 + 0.07 * (0.8 + 0.2 * np.cos(a * 37))
    fr = (r > 0.92) & (r < length) & strand
    col[fr] = cream * 0.95
    alpha[fr] = 1
    edge_ao = np.clip(1 - np.abs(r - 0.92) / 0.02, 0, 1) * 0.25
    col = col * (1 - edge_ao)[..., None]
    return image("Tapis", np.clip(col, 0, 1), alpha=alpha, fmt="PNG")


def tex_dartboard(n=512):
    y, x = np.mgrid[0:n, 0:n] / (n - 1) * 2 - 1
    r = np.sqrt(x * x + y * y)
    a = np.mod(np.arctan2(y, x) + np.pi / 20, 2 * np.pi)
    sector = (a / (2 * np.pi) * 20).astype(int) % 2
    black, cream, red, green = [np.array(c) for c in ((0.08, 0.08, 0.08), (0.92, 0.86, 0.68), (0.8, 0.1, 0.08), (0.1, 0.5, 0.2))]
    col = np.where(sector[..., None] == 0, black, cream)
    ring = ((r > 0.56) & (r < 0.62)) | ((r > 0.9) & (r < 0.96))
    col[ring] = np.where(sector[ring][..., None] == 0, red, green)
    col[r < 0.12] = green
    col[r < 0.05] = red
    col[r > 0.96] = black
    col[(r > 0.995)] = np.array([0.25, 0.16, 0.09])
    wire = (np.abs(np.mod(a / (2 * np.pi) * 20, 1)) < 0.02) & (r < 0.96) & (r > 0.12)
    col[wire] = np.array([0.7, 0.7, 0.7])
    return image("Cible", col)


def tex_grass(n=1024):
    patches = tnoise(n, 5, 5, 61)
    blades = tnoise(n, 260, 260, 62)
    clumps = tnoise(n, 40, 40, 63)
    g1, g2 = np.array([0.30, 0.52, 0.17]), np.array([0.52, 0.58, 0.24])
    t = np.clip(0.5 + 0.3 * patches, 0, 1)[..., None]
    col = g1 * (1 - t) + g2 * t
    col = col * (0.88 + 0.08 * blades + 0.06 * clumps)[..., None]
    h = 0.3 * blades + 0.3 * clumps
    return image("Herbe", np.clip(col, 0, 1)), image("Herbe_N", normal_from_height(h, 3), data=True)


def tex_staves(n=1024):
    """Tonneau : 16 douelles verticales (veines le long de v), joints sombres."""
    col, h = grain(n, (0.6, 0.38, 0.2), 64, 71, warp=4)
    x = (np.arange(n)[None, :] / n * 16) % 1
    seam = (x < 0.025) | (x > 0.975)
    col[np.broadcast_to(seam, col.shape[:2])] *= 0.35
    h = h - 0.6 * seam
    return image("Douelles", col), image("Douelles_N", normal_from_height(h, 6), data=True)


def tex_banana():
    n, m = 256, 16
    u = np.arange(n)[None, :] / (n - 1)
    yellow, green, brown = np.array([1.0, 0.82, 0.18]), np.array([0.6, 0.7, 0.2]), np.array([0.3, 0.2, 0.08])
    t_green = np.clip((u - 0.75) / 0.2, 0, 1)[..., None]
    col = yellow * (1 - t_green) + green * t_green
    tip = ((u < 0.05) | (u > 0.96))[..., None]
    col = np.where(tip, brown, col)
    col = np.repeat(col, m, axis=0)
    return image("Banane", col)


# ============================================================ matériaux

def lin(c):
    """Couleur choisie à l'œil (sRGB, comme dans un logiciel de dessin) -> valeur linéaire attendue par le matériau (et par glTF)."""
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def material(name, base=None, normal=None, color=(0.8, 0.8, 0.8), rough=0.75, metal=0.0, emission=None, strength=0.0, clip=False, vcol=False):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.use_backface_culling = True       # une seule face dessinée (glTF « doubleSided » = false) : moins de travail pour le casque
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if base:
        t = nt.nodes.new("ShaderNodeTexImage")
        t.image = base
        if vcol:
            # couleur de la texture × couleur des sommets (ombre dans les creux, teinte de chaque rondin ou planche)
            vc = nt.nodes.new("ShaderNodeVertexColor")
            vc.layer_name = "Col"
            mix = nt.nodes.new("ShaderNodeMix")
            mix.data_type = "RGBA"
            mix.blend_type = "MULTIPLY"
            mix.inputs[0].default_value = 1.0
            nt.links.new(t.outputs["Color"], mix.inputs[6])
            nt.links.new(vc.outputs["Color"], mix.inputs[7])
            nt.links.new(mix.outputs[2], bsdf.inputs["Base Color"])
        else:
            nt.links.new(t.outputs["Color"], bsdf.inputs["Base Color"])
        if clip:
            # Transparence découpée (glTF « MASK ») : l'exporteur la reconnaît à ce nœud « Arrondi » entre l'alpha et le BSDF
            rnd = nt.nodes.new("ShaderNodeMath")
            rnd.operation = "ROUND"
            nt.links.new(t.outputs["Alpha"], rnd.inputs[0])
            nt.links.new(rnd.outputs[0], bsdf.inputs["Alpha"])
    else:
        bsdf.inputs["Base Color"].default_value = (*lin(color), 1)
    if normal:
        t = nt.nodes.new("ShaderNodeTexImage")
        t.image = normal
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(t.outputs["Color"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*lin(emission), 1)
        bsdf.inputs["Emission Strength"].default_value = strength
    return mat


def build_materials():
    M = {}
    M["plancher"] = material("Plancher", *tex_wood("Plancher", (0.68, 0.47, 0.27), 26, 11), rough=0.6, vcol=True)
    M["rondin"] = material("Rondin", *tex_wood("Rondin", (0.74, 0.49, 0.27), 18, 21, strength=8), rough=0.8, vcol=True)
    M["bout"] = material("Rondin_Bout", *tex_log_end(), rough=0.85)
    M["poutre"] = material("Poutre", *tex_wood("Poutre", (0.42, 0.27, 0.15), 34, 23), rough=0.8, vcol=True)
    M["chaume"] = material("Chaume", *tex_thatch(), rough=0.95)
    M["peint"] = material("Bois_Peint", *tex_wood("Bois_Peint", (0.20, 0.50, 0.42), 20, 27, strength=4), rough=0.7)
    M["tapis"] = material("Tapis", tex_rug(), rough=0.95, clip=True)
    M["cible"] = material("Cible", tex_dartboard(), rough=0.8)
    M["herbe"] = material("Herbe", *tex_grass(), rough=0.9)
    M["fer"] = material("Fer", color=(0.2, 0.2, 0.21), rough=0.45, metal=0.8)
    M["cire"] = material("Cire", color=(0.95, 0.9, 0.78), rough=0.5)
    M["flamme"] = material("Flamme", color=(1.0, 0.6, 0.2), emission=(1.0, 0.62, 0.25), strength=12)
    M["flamme"].use_backface_culling = False
    M["vitre_lanterne"] = material("Lanterne_Lumiere", color=(1.0, 0.8, 0.45), emission=(1.0, 0.72, 0.35), strength=5)
    M["vitre_lanterne"].use_backface_culling = False
    M["roche"] = material("Roche", color=(0.55, 0.53, 0.5), rough=0.9)
    M["joint"] = material("Joint", color=(0.40, 0.33, 0.25), rough=0.95)
    M["buisson"] = material("Buisson", color=(0.22, 0.45, 0.16), rough=0.85)
    M["feuille"] = material("Feuille", color=(0.24, 0.55, 0.18), rough=0.8)
    M["douelles"] = material("Douelles", *tex_staves(), rough=0.75)
    M["caisse"] = material("Caisse", *tex_wood("Caisse", (0.78, 0.6, 0.36), 24, 81), rough=0.75)
    M["banane"] = material("Banane", tex_banana(), rough=0.6)
    M["plume_rouge"] = material("Plume", color=(0.85, 0.12, 0.1), rough=0.6)
    M["plume_rouge"].use_backface_culling = False     # les ailettes des fléchettes sont de simples plans
    return M


# ============================================================ outils de modélisation (bmesh)

class Builder:
    """Accumule la géométrie d'un objet (plusieurs matériaux), avec des coordonnées de texture en mètres."""

    def __init__(self, mats):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.verify()
        self.col = self.bm.loops.layers.float_color.new("Col")   # ombrage par sommet (blanc = rien)
        self.mats = list(mats)

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def face(self, verts, uvs, mat, smooth=False):
        vs = [self.bm.verts.new(v) for v in verts]
        f = self.bm.faces.new(vs)
        for loop, uv in zip(f.loops, uvs):
            loop[self.uv].uv = uv
        f.material_index = self.mi(mat)
        f.smooth = smooth
        return f

    def box(self, center, size, rot, mat, uv_scale=1.0, uv_offset=(0, 0), bevel=0.0, tint=None):
        """Pavé (taille locale x, y, z), tourné par la matrice 3x3 rot. Texture projetée face par face (en mètres)."""
        hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
        local = [Vector((sx * hx, sy * hy, sz * hz)) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
        vs = [self.bm.verts.new(center + rot @ p) for p in local]
        idx = lambda sx, sy, sz: ((sx > 0) * 4 + (sy > 0) * 2 + (sz > 0))
        quads = [
            ([(-1, -1, -1), (-1, 1, -1), (-1, 1, 1), (-1, -1, 1)], (1, 2)),
            ([(1, -1, -1), (1, -1, 1), (1, 1, 1), (1, 1, -1)], (1, 2)),
            ([(-1, -1, -1), (-1, -1, 1), (1, -1, 1), (1, -1, -1)], (0, 2)),
            ([(-1, 1, -1), (1, 1, -1), (1, 1, 1), (-1, 1, 1)], (0, 2)),
            ([(-1, -1, -1), (1, -1, -1), (1, 1, -1), (-1, 1, -1)], (0, 1)),
            ([(-1, -1, 1), (-1, 1, 1), (1, 1, 1), (1, -1, 1)], (0, 1)),
        ]
        faces = []
        for corners, (ua, va) in quads:
            f = self.bm.faces.new([vs[idx(*c)] for c in corners])
            for loop, c in zip(f.loops, corners):
                p = Vector((c[0] * hx, c[1] * hy, c[2] * hz))
                loop[self.uv].uv = (p[ua] * uv_scale + uv_offset[0], p[va] * uv_scale + uv_offset[1])
            f.material_index = self.mi(mat)
            faces.append(f)
        if tint is not None:
            for f in faces:
                for loop in f.loops:
                    loop[self.col] = (tint, tint, tint, 1.0)
                f.tag = True
        if bevel > 0:
            edges = list({e for f in faces for e in f.edges})
            bmesh.ops.bevel(self.bm, geom=edges, offset=bevel, segments=1, affect="EDGES", clamp_overlap=True)
        return faces

    def log(self, p0, p1, r, side_mat, end_mat, seed, segs=14):
        """Rondin de p0 à p1 : un cylindre un peu irrégulier, bouts chanfreinés, bois de bout (cernes) aux extrémités."""
        g = random.Random(seed)
        axis = (p1 - p0)
        length = axis.length
        axis.normalize()
        up = Vector((0, 0, 1)) if abs(axis.z) < 0.9 else Vector((1, 0, 0))
        u_ax = axis.cross(up).normalized()
        w_ax = axis.cross(u_ax).normalized()
        stations = [0.0, 0.025, 0.33, 0.66, 0.975, 1.0]
        radii = [0.86, 1.0, 1.0 + g.uniform(-0.04, 0.04), 1.0 + g.uniform(-0.04, 0.04), 1.0, 0.86]
        bend = Vector((0, 0, g.uniform(-0.012, 0.012)))
        uoff, voff = g.random(), g.random()
        rings = []
        tint = g.uniform(0.82, 1.05)              # chaque rondin a sa teinte
        shade = {}
        for s, k in zip(stations, radii):
            c = p0 + axis * (length * s) + bend * math.sin(math.pi * s)
            ring = []
            for j in range(segs):
                a = 2 * math.pi * j / segs
                d = u_ax * math.cos(a) + w_ax * math.sin(a)
                v = self.bm.verts.new(c + d * (r * k))
                # Ombre dans les creux : le dessus et surtout le dessous du rondin, là où il touche ses voisins
                crevice = abs(d.z) ** 3
                shade[v] = tint * (1 - 0.5 * crevice) * (1 - 0.12 * max(0.0, -d.z))
                ring.append(v)
            rings.append((ring, s))
        circ = 2 * math.pi * r
        si = self.mi(side_mat)
        for (ra, sa), (rb, sb) in zip(rings, rings[1:]):
            for j in range(segs):
                j2 = (j + 1) % segs
                f = self.bm.faces.new([ra[j], ra[j2], rb[j2], rb[j]])
                ua, ub = j / segs * circ / 0.9 + uoff, (j + 1) / segs * circ / 0.9 + uoff
                va, vb = sa * length / 1.8 + voff, sb * length / 1.8 + voff
                for loop, uv in zip(f.loops, [(ua, va), (ub, va), (ub, vb), (ua, vb)]):
                    loop[self.uv].uv = uv
                f.material_index = si
                f.smooth = True
                for loop in f.loops:
                    v = shade[loop.vert]
                    loop[self.col] = (v, v, v, 1.0)
                f.tag = True
        ei = self.mi(end_mat)
        rot = g.random() * 6.28
        for ring, s in (rings[0], rings[-1]):
            f = self.bm.faces.new(ring if s > 0.5 else list(reversed(ring)))
            for loop, j in zip(f.loops, range(segs) if s > 0.5 else reversed(range(segs))):
                a = 2 * math.pi * j / segs + rot
                loop[self.uv].uv = (0.5 + 0.47 * math.cos(a), 0.5 + 0.47 * math.sin(a))
                loop[self.col] = (tint, tint, tint, 1.0)
            f.material_index = ei
            f.tag = True

    def lathe(self, profile, segs, mat, center, u_scale=1.0, v_scale=1.0, cap_top=None, cap_bottom=None):
        """Objet de révolution : profile = [(rayon, hauteur)] du bas vers le haut, autour de l'axe vertical."""
        rings = []
        for (rad, h) in profile:
            rings.append([self.bm.verts.new(center + Vector((rad * math.cos(2 * math.pi * j / segs), rad * math.sin(2 * math.pi * j / segs), h)))
                          for j in range(segs)])
        mi = self.mi(mat)
        vacc = 0.0
        for k in range(len(profile) - 1):
            dv = math.hypot(profile[k + 1][0] - profile[k][0], profile[k + 1][1] - profile[k][1])
            for j in range(segs):
                j2 = (j + 1) % segs
                f = self.bm.faces.new([rings[k][j], rings[k][j2], rings[k + 1][j2], rings[k + 1][j]])
                for loop, uv in zip(f.loops, [(j / segs, vacc), ((j + 1) / segs, vacc), ((j + 1) / segs, vacc + dv), (j / segs, vacc + dv)]):
                    loop[self.uv].uv = (uv[0] * u_scale, uv[1] * v_scale)
                f.material_index = mi
                f.smooth = True
            vacc += dv
        for ring, mat_cap, top in ((rings[-1], cap_top, True), (rings[0], cap_bottom, False)):
            if mat_cap is None:
                continue
            f = self.bm.faces.new(ring if top else list(reversed(ring)))
            rad = profile[-1][0] if top else profile[0][0]
            for loop in f.loops:
                d = loop.vert.co - center
                loop[self.uv].uv = (d.x * 1.0 + 0.5, d.y * 1.0 + 0.5)
            f.material_index = self.mi(mat_cap)
        return rings

    def finish(self, name, collection, recalc=True):
        for f in self.bm.faces:                   # tout ce qui n'a pas d'ombrage à soi : blanc (aucun effet)
            if not f.tag:
                for loop in f.loops:
                    loop[self.col] = (1.0, 1.0, 1.0, 1.0)
        if recalc:
            bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces[:])
        me = bpy.data.meshes.new(name)
        self.bm.to_mesh(me)
        self.bm.free()
        for m in self.mats:
            me.materials.append(m)
        ob = bpy.data.objects.new(name, me)
        collection.objects.link(ob)
        return ob


def basis(angle_deg):
    """Matrice d'un mur à cet angle : x = le long du mur (vers la droite vu du centre), y = vers l'extérieur, z = haut."""
    out = polar(angle_deg, 1.0)
    along = Vector((-out.y, out.x, 0))
    return Matrix((along, out, Vector((0, 0, 1)))).transposed()


def empty(name, loc, coll):
    e = bpy.data.objects.new(name, None)
    e.location = loc
    coll.objects.link(e)
    return e


# ============================================================ la cabane

def build_floor(M, coll):
    """Plateforme en planches (dedans + terrasse derrière la porte), posée sur une poutre de rive et des pieux."""
    b = Builder([M["plancher"]])
    width, gap, thick = 0.19, 0.008, 0.05
    x0, x1 = -4.0, 4.0           # Unity x
    z0, z1 = -5.6, 4.0           # Unity z (la terrasse s'étend derrière, côté porte)
    x = x0
    row = 0
    while x < x1 - 0.01:
        z = z0 - rng.uniform(0, 1.5)
        while z < z1:
            length = rng.uniform(1.6, 3.2)
            za, zb = max(z, z0), min(z + length, z1)
            if zb - za > 0.15:
                c = P(x + width / 2, -thick / 2 + rng.uniform(-0.002, 0.002), (za + zb) / 2)
                rot = Matrix(((1, 0, 0), (0, 1, 0), (0, 0, 1)))    # x Blender = x Unity (inversé, sans importance), y = le long
                b.box(c, (width - gap, zb - za - gap, thick), rot, M["plancher"], uv_scale=1 / 1.2, uv_offset=(rng.random(), rng.random()), bevel=0.006,
                      tint=rng.uniform(0.78, 1.05))   # chaque planche a sa teinte
            z += length
        x += width
        row += 1
    floor = b.finish("Plancher", coll)

    # Poutres de rive et pieux (on les voit depuis la terrasse et par les fenêtres)
    b = Builder([M["poutre"]])
    for (xa, za, xb, zb) in ((x0, z0, x1, z0), (x0, z1, x1, z1), (x0, z0, x0, z1), (x1, z0, x1, z1)):
        c = P((xa + xb) / 2, -0.12, (za + zb) / 2)
        size = (abs(xb - xa) + 0.12, abs(zb - za) + 0.12, 0.14)
        b.box(c, size, Matrix.Identity(3), M["poutre"], uv_scale=1 / 1.5, bevel=0.01)
    for xa in np.linspace(x0, x1, 5):
        for za in np.linspace(z0, z1, 6):
            if abs(xa) in (abs(x0),) or za in (z0, z1):
                b.log(P(xa, -0.19, za), P(xa, GROUND_Y - 0.2, za), 0.08, M["poutre"], M["bout"], int(xa * 100 + za * 7))
    return floor, b.finish("Charpente_Plancher", coll)


def build_walls(M, coll):
    """Murs en rondins empilés, croisés aux angles (un mur sur deux décalé d'un demi-rondin, comme une vraie cabane)."""
    b = Builder([M["rondin"], M["bout"]])
    side_len = 2 * R * math.tan(math.pi / SIDES)
    over = 0.2                                     # les rondins dépassent de 20 cm aux angles, dehors
    step = LOG_R * 2 * 1.05                       # un petit jour entre deux rondins : on y voit le joint (torchis)
    seed = 0
    for i in range(SIDES):
        ang = i * 360 / SIDES
        B = basis(ang)
        center = polar(ang, R)
        cuts = []                                  # morceaux du mur à NE PAS couvrir : (x0, x1, y0, y1) dans le repère du mur
        if ang == DOOR:
            cuts.append((-DOOR_W / 2, DOOR_W / 2, -1, DOOR_H))
        elif ang in WINDOWS:
            cuts.append((-WIN_W / 2, WIN_W / 2, WIN_Y0, WIN_Y1))
        y = LOG_R + (i % 2) * step / 2
        while y < H - LOG_R * 0.3:
            pieces = [(-side_len / 2 - over, side_len / 2 + over)]
            for (cx0, cx1, cy0, cy1) in cuts:
                if y + LOG_R * 0.6 > cy0 and y - LOG_R * 0.6 < cy1:
                    new = []
                    for (a, c) in pieces:
                        if a < cx0:
                            new.append((a, min(c, cx0)))
                        if c > cx1:
                            new.append((max(a, cx1), c))
                    pieces = new
            for (a, c) in pieces:
                if c - a < 0.12:
                    continue
                p0 = center + B @ Vector((a, 0, y))
                p1 = center + B @ Vector((c, 0, y))
                seed += 1
                b.log(p0, p1, LOG_R * rng.uniform(0.94, 1.04), M["rondin"], M["bout"], seed)
            y += step
    walls = b.finish("Murs", coll)

    # Le joint (torchis clair) entre les rondins : un panneau dans l'axe de chaque mur, les rondins dépassent devant et derrière
    b = Builder([M["joint"]])
    for i in range(SIDES):
        ang = i * 360 / SIDES
        B = basis(ang)
        center = polar(ang, R)
        if ang == DOOR:
            for s in (-1, 1):
                w = (side_len - DOOR_W) / 2
                b.box(center + B @ Vector((s * (DOOR_W / 2 + w / 2), 0, DOOR_H / 2)), (w, 0.06, DOOR_H), B, M["joint"])
            b.box(center + B @ Vector((0, 0, (DOOR_H + H) / 2)), (side_len, 0.06, H - DOOR_H), B, M["joint"])
        elif ang in WINDOWS:
            w = (side_len - WIN_W) / 2
            for s in (-1, 1):
                b.box(center + B @ Vector((s * (WIN_W / 2 + w / 2), 0, H / 2)), (w, 0.06, H), B, M["joint"])
            b.box(center + B @ Vector((0, 0, WIN_Y0 / 2)), (WIN_W, 0.06, WIN_Y0), B, M["joint"])
            b.box(center + B @ Vector((0, 0, (WIN_Y1 + H) / 2)), (WIN_W, 0.06, H - WIN_Y1), B, M["joint"])
        else:
            b.box(center + B @ Vector((0, 0, H / 2)), (side_len, 0.06, H), B, M["joint"])
    chinking = b.finish("Joints", coll)

    # Poteaux d'angle, sablière (poutre en haut des murs), encadrements de la porte et des fenêtres
    b = Builder([M["poutre"]])
    for i in range(SIDES):
        ang = i * 360 / SIDES
        B = basis(ang)
        center = polar(ang, R)
        b.box(center + B @ Vector((0, 0, H + 0.05)), (side_len + 0.3, 0.3, 0.14), B, M["poutre"], uv_scale=1 / 1.5, bevel=0.012)
        if ang == DOOR:
            for s in (-1, 1):
                b.box(center + B @ Vector((s * (DOOR_W / 2 + 0.06), 0, DOOR_H / 2)), (0.14, 0.32, DOOR_H), B, M["poutre"], uv_scale=1 / 1.5, bevel=0.012)
            b.box(center + B @ Vector((0, 0, DOOR_H + 0.07)), (DOOR_W + 0.4, 0.34, 0.16), B, M["poutre"], uv_scale=1 / 1.5, bevel=0.015)
            b.box(center + B @ Vector((0, 0, 0.012)), (DOOR_W, 0.36, 0.025), B, M["poutre"], uv_scale=1 / 1.5, bevel=0.006)
        elif ang in WINDOWS:
            wy = (WIN_Y0 + WIN_Y1) / 2
            for s in (-1, 1):
                b.box(center + B @ Vector((s * (WIN_W / 2 + 0.04), 0, wy)), (0.08, 0.3, WIN_Y1 - WIN_Y0 + 0.16), B, M["poutre"], uv_scale=1 / 1.5, bevel=0.008)
            b.box(center + B @ Vector((0, 0, WIN_Y1 + 0.05)), (WIN_W + 0.24, 0.3, 0.1), B, M["poutre"], uv_scale=1 / 1.5, bevel=0.01)
            b.box(center + B @ Vector((0, -0.03, WIN_Y0 - 0.04)), (WIN_W + 0.3, 0.42, 0.07), B, M["poutre"], uv_scale=1 / 1.5, bevel=0.01)   # appui
            b.box(center + B @ Vector((0, 0, wy)), (0.035, 0.05, WIN_Y1 - WIN_Y0), B, M["poutre"], uv_scale=1 / 1.5)                     # croisillon
            b.box(center + B @ Vector((0, 0, wy)), (WIN_W, 0.05, 0.035), B, M["poutre"], uv_scale=1 / 1.5)
    frames = b.finish("Encadrements", coll)

    # Volets peints (ouverts contre le mur, dehors) et battant de la porte (ouvert vers l'extérieur)
    b = Builder([M["peint"]])
    for ang in WINDOWS:
        B = basis(ang)
        center = polar(ang, R)
        wy = (WIN_Y0 + WIN_Y1) / 2
        for s in (-1, 1):
            c = center + B @ Vector((s * (WIN_W / 2 + 0.08 + WIN_W / 4), LOG_R + 0.06, wy))
            for k in range(3):   # trois lattes par volet
                b.box(c + B @ Vector((s * (k - 1) * WIN_W / 6, 0, 0)), (WIN_W / 6 - 0.01, 0.03, WIN_Y1 - WIN_Y0), B, M["peint"], uv_scale=1 / 1.2, bevel=0.004)
            b.box(c + B @ Vector((0, 0.02, 0)), (WIN_W / 2, 0.025, 0.06), B, M["peint"], uv_scale=1 / 1.2, bevel=0.004)
    # Battant de porte : pivote sur le montant droit et s'ouvre dehors, contre le mur
    B = basis(DOOR)
    hinge = polar(DOOR, R) + B @ Vector((DOOR_W / 2 + 0.06, LOG_R + 0.05, 0))
    Bd = B @ Matrix.Rotation(math.radians(-100), 3, "Z")
    for k in range(6):
        x = -DOOR_W + (k + 0.5) * DOOR_W / 6
        b.box(hinge + Bd @ Vector((x, 0.03, DOOR_H / 2 - 0.02)), (DOOR_W / 6 - 0.01, 0.045, DOOR_H - 0.06), Bd, M["peint"], uv_scale=1 / 1.2, bevel=0.005)
    for zz in (0.35, DOOR_H - 0.4):
        b.box(hinge + Bd @ Vector((-DOOR_W / 2, -0.01, zz)), (DOOR_W - 0.06, 0.03, 0.12), Bd, M["peint"], uv_scale=1 / 1.2, bevel=0.006)
    door = b.finish("Volets_Porte", coll)
    return [walls, chinking, frames, door]


def build_roof(M, coll):
    """Toit conique en chaume (dessus) et en planches (dessous), avec une épaisseur au bord, sa charpente et un épi au sommet."""
    corner = lambda k, rad, y: polar(15 + k * 30, rad / math.cos(math.pi / SIDES), y)
    apex_out, apex_in = P(0, ROOF_TOP_Y, 0), P(0, ROOF_TOP_Y - 0.18, 0)
    b = Builder([M["chaume"], M["poutre"]])
    thick = 0.2
    for k in range(SIDES):
        e0, e1 = corner(k, ROOF_EAVE_R, ROOF_EAVE_Y), corner(k + 1, ROOF_EAVE_R, ROOF_EAVE_Y)
        i0, i1 = corner(k, ROOF_EAVE_R - 0.05, ROOF_EAVE_Y - thick), corner(k + 1, ROOF_EAVE_R - 0.05, ROOF_EAVE_Y - thick)
        edge = (e1 - e0).length
        slant = ((e0 + e1) / 2 - apex_out).length
        # dessus (chaume) : v remonte la pente
        f = b.face([e0, e1, apex_out], [(0, 0), (edge / 1.6, 0), (edge / 3.2, slant / 1.6)], M["chaume"])
        f.normal_update()
        if f.normal.z < 0:
            f.normal_flip()
        # dessous (planches)
        f = b.face([i0, apex_in, i1], [(0, 0), (edge / 2.4, slant / 1.2), (edge / 1.2, 0)], M["poutre"])
        f.normal_update()
        if f.normal.z > 0:
            f.normal_flip()
        # tranche du chaume, au bord
        f = b.face([e0, i0, i1, e1], [(0, 0.8), (0, 0.7), (edge / 1.6, 0.7), (edge / 1.6, 0.8)], M["chaume"])
        f.normal_update()
        mid = (e0 + e1) / 2
        if f.normal.dot(Vector((mid.x, mid.y, 0))) < 0:
            f.normal_flip()
    roof = b.finish("Toit", coll, recalc=False)

    # Charpente : un chevron par arête, un cerclage à mi-hauteur, un poinçon au centre ; épi en haut, dehors
    b = Builder([M["poutre"], M["bout"]])
    for k in range(SIDES):
        lo = corner(k, R + 0.1, H + 0.12)
        hi = P(0, ROOF_TOP_Y - 0.3, 0)
        b.log(lo, hi, 0.075, M["poutre"], M["bout"], 500 + k, segs=8)
    y_ring = (H + ROOF_TOP_Y) / 2 - 0.1
    r_ring = (R + 0.1) * (ROOF_TOP_Y - 0.3 - y_ring) / (ROOF_TOP_Y - 0.3 - H - 0.12)
    for k in range(SIDES):
        b.log(corner(k, r_ring, y_ring), corner(k + 1, r_ring, y_ring), 0.06, M["poutre"], M["bout"], 600 + k, segs=8)
    b.log(P(0, ROOF_TOP_Y - 0.1, 0), P(0, ROOF_TOP_Y - 0.75, 0), 0.11, M["poutre"], M["bout"], 700, segs=12)
    b.log(P(0, ROOF_TOP_Y - 0.15, 0), P(0, ROOF_TOP_Y + 0.35, 0), 0.07, M["poutre"], M["bout"], 701, segs=10)
    frame = b.finish("Charpente", coll)
    return [roof, frame]


def build_chandelier(M, coll):
    """Lustre en roue de charrette : jante, 8 rayons, moyeu, 8 bougies avec flamme, 4 chaînes jusqu'au crochet."""
    b = Builder([M["poutre"], M["fer"], M["cire"], M["flamme"], M["bout"]])
    y = 2.75
    rad = 0.55
    n = 16
    for k in range(n):
        a0, a1 = 360 * k / n, 360 * (k + 1) / n
        b.log(polar(a0, rad, y), polar(a1, rad, y), 0.035, M["poutre"], M["bout"], 800 + k, segs=6)
    for k in range(8):
        b.log(polar(45 * k, 0.07, y), polar(45 * k, rad - 0.02, y), 0.018, M["poutre"], M["bout"], 820 + k, segs=6)
    b.lathe([(0.08, -0.06), (0.09, 0.0), (0.08, 0.06)], 10, M["poutre"], P(0, y, 0), cap_top=M["bout"], cap_bottom=M["bout"])
    for k in range(8):
        c = polar(22.5 + 45 * k, rad, y)
        b.lathe([(0.045, 0.0), (0.05, 0.02)], 8, M["fer"], c + Vector((0, 0, 0.03)), cap_bottom=M["fer"])
        h = rng.uniform(0.08, 0.14)
        b.lathe([(0.022, 0.03), (0.022, 0.03 + h), (0.018, 0.04 + h)], 8, M["cire"], c, cap_top=M["cire"], cap_bottom=M["cire"])
        b.lathe([(0.001, 0.045 + h), (0.012, 0.06 + h), (0.009, 0.08 + h), (0.0, 0.11 + h)], 6, M["flamme"], c)
    hook = P(0, y + 0.75, 0)
    for k in range(4):
        b.log(polar(45 + 90 * k, rad, y + 0.02), hook, 0.008, M["fer"], M["fer"], 840 + k, segs=4)
    b.log(hook, P(0, ROOF_TOP_Y - 0.7, 0), 0.01, M["fer"], M["fer"], 850, segs=4)
    ob = b.finish("Lustre", coll)
    empty("Lumiere_Lustre", P(0, y + 0.1, 0), coll)
    return ob


def build_lanterns(M, coll):
    """Deux lanternes en fer accrochées aux murs, une bougie allumée derrière les vitres."""
    b = Builder([M["fer"], M["vitre_lanterne"]])
    for i, ang in enumerate((120, -60)):
        B = basis(ang)
        wall = polar(ang, R - LOG_R - 0.02)
        c = wall + B @ Vector((0, -0.24, 2.35))
        b.box(wall + B @ Vector((0, -0.12, 2.6)), (0.03, 0.26, 0.03), B, M["fer"])           # potence
        b.box(c + Vector((0, 0, 0.27)), (0.02, 0.02, 0.12), B, M["fer"])
        b.lathe([(0.1, 0.0), (0.11, 0.02), (0.1, 0.03)], 8, M["fer"], c - Vector((0, 0, 0.12)), cap_bottom=M["fer"])
        b.lathe([(0.075, 0.03), (0.075, 0.2)], 8, M["vitre_lanterne"], c - Vector((0, 0, 0.12)))
        for k in range(4):
            a = 45 + 90 * k
            p = c + Vector((math.cos(math.radians(a)) * 0.08, math.sin(math.radians(a)) * 0.08, 0))
            b.box(p - Vector((0, 0, 0.01)), (0.015, 0.015, 0.22), B, M["fer"])
        b.lathe([(0.11, 0.21), (0.02, 0.32)], 8, M["fer"], c - Vector((0, 0, 0.12)), cap_bottom=M["fer"])
        empty(f"Lumiere_Lanterne_{i + 1}", c, coll)
    return b.finish("Lanternes", coll)


def build_rug(M, coll):
    """Tapis rond (un disque un peu épais), texture à franges découpée par la transparence."""
    b = Builder([M["tapis"]])
    n = 64
    top = [P(math.sin(2 * math.pi * k / n) * RUG, 0.012, math.cos(2 * math.pi * k / n) * RUG) for k in range(n)]
    c = P(0, 0.012, 0)
    for k in range(n):
        a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 1) / n
        f = b.face([c, top[k], top[(k + 1) % n]], [(0.5, 0.5), (0.5 + 0.5 * math.sin(a0), 0.5 + 0.5 * math.cos(a0)), (0.5 + 0.5 * math.sin(a1), 0.5 + 0.5 * math.cos(a1))], M["tapis"])
        f.normal_update()
        if f.normal.z < 0:
            f.normal_flip()
    return b.finish("Tapis", coll, recalc=False)


def build_dartboard(M, coll):
    """Une cible de fléchettes au mur (les singes de Bloons lancent des fléchettes !), avec trois fléchettes plantées."""
    ang, offset, y = 30, -0.35, 1.85
    B = basis(ang)
    c = polar(ang, R - LOG_R - 0.02) + B @ Vector((offset, -0.03, y))
    b = Builder([M["cible"], M["poutre"], M["fer"], M["plume_rouge"]])
    rad = 0.24
    n = 32
    front, back = [], []
    for k in range(n):
        a = 2 * math.pi * k / n
        front.append(c + B @ Vector((math.cos(a) * rad, -0.03, math.sin(a) * rad)))
        back.append(c + B @ Vector((math.cos(a) * rad, 0.02, math.sin(a) * rad)))
    f = b.face(front, [(0.5 + 0.5 * math.cos(2 * math.pi * k / n), 0.5 + 0.5 * math.sin(2 * math.pi * k / n)) for k in range(n)], M["cible"])
    f.normal_update()
    if f.normal.dot(B @ Vector((0, -1, 0))) < 0:
        f.normal_flip()
    for k in range(n):
        k2 = (k + 1) % n
        f = b.face([front[k], back[k], back[k2], front[k2]], [(0, 0), (0, 0.05), (0.1, 0.05), (0.1, 0)], M["poutre"])
    b.face(list(reversed(back)), [(0, 0)] * n, M["poutre"])           # le dos (contre le mur) : la cible est un volume fermé
    for (dx, dz, tilt) in ((0.02, 0.03, 8), (-0.09, 0.11, -6), (0.12, -0.08, 12)):
        tip = c + B @ Vector((dx, -0.03, dz))
        d = (B @ Vector((math.sin(math.radians(tilt)) * 0.3, -1, 0.15))).normalized()
        b.log(tip, tip + d * 0.1, 0.006, M["fer"], M["fer"], 900, segs=6)
        b.log(tip + d * 0.1, tip + d * 0.16, 0.009, M["plume_rouge"], M["plume_rouge"], 901, segs=6)
        side = d.cross(Vector((0, 0, 1))).normalized()
        for s in (side, Vector((0, 0, 1))):
            q0, q1 = tip + d * 0.14, tip + d * 0.19
            b.face([q0 - s * 0.025, q1 - s * 0.03, q1 + s * 0.03, q0 + s * 0.025], [(0, 0), (1, 0), (1, 1), (0, 1)], M["plume_rouge"])
    return b.finish("Cible", coll)


def build_outside(M, coll):
    """Dehors : une prairie, des rochers et des buissons (vus par la porte et les fenêtres)."""
    b = Builder([M["herbe"]])
    n = 48
    rad = 17.0
    c = P(0, GROUND_Y, 0)
    ring = [P(math.sin(2 * math.pi * k / n) * rad, GROUND_Y, math.cos(2 * math.pi * k / n) * rad) for k in range(n)]
    for k in range(n):
        p0, p1 = ring[k], ring[(k + 1) % n]
        f = b.face([c, p0, p1], [(c.x / 3, c.y / 3), (p0.x / 3, p0.y / 3), (p1.x / 3, p1.y / 3)], M["herbe"])
        f.normal_update()
        if f.normal.z < 0:
            f.normal_flip()
    ground = b.finish("Prairie", coll, recalc=False)

    deco = []
    g = random.Random(5)
    for kind in ("roche", "buisson"):
        bm = bmesh.new()
        count = 14 if kind == "roche" else 26
        for i in range(count):
            ang = g.uniform(0, 360)
            if kind == "buisson" and -30 < ((ang + 180) % 360 - 180) < 30:
                continue                                        # rien devant : on garde la vue vers la carte
            dist = g.uniform(6.0, 15.0)
            pos = polar(ang, dist, GROUND_Y)
            for j in range(1 if kind == "roche" else 3):
                size = g.uniform(0.3, 0.9) if kind == "roche" else g.uniform(0.45, 0.85)
                off = Vector((g.uniform(-0.5, 0.5), g.uniform(-0.5, 0.5), 0)) if j else Vector()
                res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=size)
                for v in res["verts"]:
                    v.co.x *= g.uniform(0.9, 1.3)
                    v.co.z *= 0.6 if kind == "roche" else 0.85
                    v.co += v.co.normalized() * g.uniform(-0.08, 0.08) * size
                    v.co += pos + off + Vector((0, 0, size * (0.15 if kind == "roche" else 0.5)))
        me = bpy.data.meshes.new(kind)
        bm.to_mesh(me)
        bm.free()
        me.materials.append(M[kind])
        for p in me.polygons:
            p.use_smooth = kind == "buisson"
        ob = bpy.data.objects.new("Rochers" if kind == "roche" else "Buissons", me)
        coll.objects.link(ob)
        deco.append(ob)
    return [ground] + deco


# ============================================================ accessoires (posés par Unity)

def build_barrel(M, coll):
    b = Builder([M["douelles"], M["fer"], M["caisse"]])
    h = 0.9
    prof = [(0.25 + 0.05 * math.sin(math.pi * t), h * t) for t in np.linspace(0, 1, 9)]
    b.lathe(prof, 20, M["douelles"], Vector((0, 0, 0)), u_scale=1.0, v_scale=1 / h, cap_bottom=M["caisse"])
    b.lathe([(0.25, h), (0.24, h - 0.05)], 20, M["caisse"], Vector((0, 0, 0)), cap_top=M["caisse"])   # couvercle un peu enfoncé
    for t in (0.12, 0.3, 0.7, 0.88):
        r = 0.25 + 0.05 * math.sin(math.pi * t) + 0.006
        b.lathe([(r, h * t - 0.025), (r + 0.004, h * t), (r, h * t + 0.025)], 20, M["fer"], Vector((0, 0, 0)))
    return b.finish("Tonneau", coll, recalc=False)


def build_crate(M, coll):
    b = Builder([M["caisse"], M["poutre"]])
    s = 0.5
    I = Matrix.Identity(3)
    b.box(Vector((0, 0, s / 2)), (s - 0.04, s - 0.04, s - 0.04), I, M["caisse"], uv_scale=1 / 0.6, bevel=0.004)
    # 12 arêtes renforcées
    t = 0.05
    for ax in range(3):
        for sa in (-1, 1):
            for sb in (-1, 1):
                p = [0, 0, 0]
                size = [t, t, t]
                o = [i for i in range(3) if i != ax]
                p[o[0]] = sa * (s / 2 - t / 2)
                p[o[1]] = sb * (s / 2 - t / 2)
                size[ax] = s
                b.box(Vector(p) + Vector((0, 0, s / 2)), tuple(size), I, M["poutre"], uv_scale=1 / 0.6, bevel=0.006)
    # une diagonale sur deux faces
    for ang, axis in ((45, "Y"), (-45, "X")):
        rot = Matrix.Rotation(math.radians(ang), 3, axis)
        n = Vector((1, 0, 0)) if axis == "Y" else Vector((0, 1, 0))
        for sgn in (-1, 1):
            size = (0.025, s * 1.25, 0.06) if axis == "Y" else (s * 1.25, 0.025, 0.06)
            b.box(n * sgn * (s / 2 - 0.005) + Vector((0, 0, s / 2)), (0.02, 0.06, s * 1.25) if axis == "Y" else (0.06, 0.02, s * 1.25), rot, M["poutre"], uv_scale=1 / 0.6, bevel=0.004)
    return b.finish("Caisse", coll)


def build_bunch(M, coll):
    """Régime de 6 bananes courbes autour d'une tige."""
    b = Builder([M["banane"], M["poutre"], M["bout"]])
    b.log(Vector((0, 0, 0.02)), Vector((0, 0, 0.16)), 0.016, M["poutre"], M["bout"], 950, segs=6)
    segs, steps = 6, 8
    for k in range(6):
        yaw = 2 * math.pi * k / 6 + 0.2
        d = Vector((math.cos(yaw), math.sin(yaw), 0))
        pts = []
        for i in range(steps + 1):
            t = i / steps
            r = 0.02 + 0.13 * t
            z = 0.12 - 0.09 * t + 0.08 * t * t
            pts.append(d * r + Vector((0, 0, z)))
        rings = []
        for i, p in enumerate(pts):
            t = i / steps
            tangent = (pts[min(i + 1, steps)] - pts[max(i - 1, 0)]).normalized()
            side = tangent.cross(Vector((0, 0, 1))).normalized()
            up = side.cross(tangent).normalized()
            rad = 0.016 * math.sin(math.pi * min(max(t, 0.06), 0.94)) + 0.003
            rings.append([b.bm.verts.new(p + (side * math.cos(2 * math.pi * j / segs) + up * math.sin(2 * math.pi * j / segs)) * rad) for j in range(segs)])
        mi = b.mi(M["banane"])
        for i in range(steps):
            for j in range(segs):
                j2 = (j + 1) % segs
                f = b.bm.faces.new([rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]])
                for loop, uv in zip(f.loops, [(1 - i / steps, j / segs), (1 - i / steps, (j + 1) / segs), (1 - (i + 1) / steps, (j + 1) / segs), (1 - (i + 1) / steps, j / segs)]):
                    loop[b.uv].uv = uv
                f.material_index = mi
                f.smooth = True
    return b.finish("Regime", coll)


# ============================================================ export et aperçu

def export(objs, path):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    kwargs = dict(filepath=path, export_format="GLB", use_selection=True, export_yup=True, export_apply=True,
                  export_lights=False, export_cameras=False)
    try:
        bpy.ops.export_scene.gltf(export_image_format="AUTO", **kwargs)
    except TypeError:
        bpy.ops.export_scene.gltf(**kwargs)
    print("exporté :", path)


def setup_preview(cabin_objs):
    scene = bpy.context.scene
    for engine in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            continue
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    scene.view_settings.view_transform = "Standard"      # comme Unity (pas de AgX/Filmic) : couleurs fidèles
    try:
        scene.eevee.taa_render_samples = 32
    except AttributeError:
        pass
    world = bpy.data.worlds.new("Ciel")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.55, 0.72, 0.95, 1)
    bg.inputs[1].default_value = 0.9
    scene.world = world
    sun = bpy.data.lights.new("Soleil", "SUN")
    sun.energy = 3.5
    sun.color = (1.0, 0.95, 0.85)
    so = bpy.data.objects.new("Soleil", sun)
    so.rotation_euler = (math.radians(50), 0, math.radians(150))
    scene.collection.objects.link(so)
    for o in list(scene.objects):
        if o.type == "EMPTY" and o.name.startswith("Lumiere_"):
            l = bpy.data.lights.new(o.name + "_L", "POINT")
            l.energy = 220 if "Lustre" in o.name else 70
            l.color = (1.0, 0.75, 0.45)
            l.shadow_soft_size = 0.2
            lo = bpy.data.objects.new(o.name + "_L", l)
            lo.location = o.location
            scene.collection.objects.link(lo)
    cam = bpy.data.cameras.new("Oeil")
    cam.lens = 14
    co = bpy.data.objects.new("Oeil", cam)
    scene.collection.objects.link(co)
    scene.camera = co
    return co


def render_views(cam, folder):
    os.makedirs(folder, exist_ok=True)
    views = {
        "dedans_porte": (P(0, 1.6, 0.4), 180, -2),
        "dedans_devant": (P(0, 1.6, -0.6), 0, -4),
        "dedans_droite": (P(-0.3, 1.6, 0), 60, 2),
        "dedans_gauche": (P(0.3, 1.6, 0), -110, 0),
        "dehors": (P(5.5, 3.2, -9.0), None, None),
    }
    only = os.environ.get("SAE_VIEWS")
    for name, (loc, ang, pitch) in views.items():
        if only and name not in only.split(","):
            continue
        cam.location = loc
        if ang is None:
            target = P(0, 1.5, 0)
        else:
            target = loc + polar(ang, 1, 0) + Vector((0, 0, math.sin(math.radians(pitch))))
        cam.rotation_euler = (target - loc).to_track_quat("-Z", "Y").to_euler()
        bpy.context.scene.render.filepath = os.path.join(folder, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("rendu :", name)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(OUT, exist_ok=True)
    M = build_materials()

    cabin = bpy.data.collections.new("Cabane")
    bpy.context.scene.collection.children.link(cabin)
    objs = []
    objs += build_floor(M, cabin)
    objs += build_walls(M, cabin)
    objs += build_roof(M, cabin)
    objs.append(build_chandelier(M, cabin))
    objs.append(build_lanterns(M, cabin))
    objs.append(build_rug(M, cabin))
    objs.append(build_dartboard(M, cabin))
    objs += build_outside(M, cabin)
    objs.append(empty("Repere_Porte", polar(DOOR, R), cabin))
    objs.append(empty("Repere_Droite", polar(90, R), cabin))
    objs += [o for o in cabin.objects if o.name.startswith("Lumiere_")]
    export(objs, os.path.join(OUT, "Cabane.glb"))

    props = bpy.data.collections.new("Accessoires")
    bpy.context.scene.collection.children.link(props)
    for builder, name in ((build_barrel, "Tonneau"), (build_crate, "Caisse"), (build_bunch, "Regime")):
        ob = builder(M, props)
        export([ob], os.path.join(OUT, name + ".glb"))
        ob.hide_render = True

    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs if o.type == "MESH")
    print("triangles de la cabane :", tris)

    if "--render" in argv:
        cam = setup_preview(objs)
        render_views(cam, argv[argv.index("--render") + 1])


main()
