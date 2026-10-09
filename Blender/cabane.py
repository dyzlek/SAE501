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
#   Paysage.glb  : le même paysage (prairie, végétation, palmiers, montagnes) pour la scène Labyrinthe,
#                  avec la zone de jeu laissée libre au centre.
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
    elif vcol:
        # pas de texture : la couleur vient seulement des sommets (montagnes : herbe, roche, neige selon la hauteur)
        vc = nt.nodes.new("ShaderNodeVertexColor")
        vc.layer_name = "Col"
        nt.links.new(vc.outputs["Color"], bsdf.inputs["Base Color"])
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
    M["roche"] = material("Roche", color=(0.55, 0.53, 0.5), rough=0.9)
    M["joint"] = material("Joint", color=(0.40, 0.33, 0.25), rough=0.95)
    M["buisson"] = material("Buisson", color=(0.22, 0.45, 0.16), rough=0.85)
    M["feuille"] = material("Feuille", color=(0.24, 0.55, 0.18), rough=0.8)
    M["douelles"] = material("Douelles", *tex_staves(), rough=0.75)
    M["caisse"] = material("Caisse", *tex_wood("Caisse", (0.78, 0.6, 0.36), 24, 81), rough=0.75)
    M["banane"] = material("Banane", tex_banana(), rough=0.6)
    M["montagne"] = material("Montagne", rough=0.95, vcol=True)
    M["palme"] = material("Palme", color=(0.25, 0.55, 0.16), rough=0.7)
    M["palme"].use_backface_culling = False            # les feuilles de palmier sont des plans fins
    # Herbes hautes et fleurs : la couleur vient des sommets (vert foncé au pied, clair à la pointe ; pétales colorés)
    M["vegetation"] = material("Vegetation", vcol=True, rough=0.85)
    M["vegetation"].use_backface_culling = False       # brins et pétales sont des plans fins, vus des deux côtés
    M["plume_rouge"] = material("Plume", color=(0.85, 0.12, 0.1), rough=0.6)
    M["plume_rouge"].use_backface_culling = False     # les ailettes des fléchettes sont de simples plans
    # Champignons (rouge à points blancs, comme dans les contes) et décor des murs (ballons de Bloons, pots)
    M["champi_chapeau"] = material("Champi_Chapeau", color=(0.85, 0.15, 0.1), rough=0.5)
    M["champi_pied"] = material("Champi_Pied", color=(0.95, 0.9, 0.78), rough=0.8)
    M["champi_point"] = material("Champi_Point", color=(0.98, 0.97, 0.94), rough=0.6)
    M["ballon_deco"] = material("Ballon_Deco", vcol=True, rough=0.3)     # chaque ballon de la guirlande a sa couleur (sommets)
    M["ficelle"] = material("Ficelle", color=(0.85, 0.8, 0.7), rough=0.9)
    M["pot"] = material("Pot", color=(0.72, 0.38, 0.22), rough=0.85)    # terre cuite
    M["livre"] = material("Livre", vcol=True, rough=0.8)                # la couleur de chaque livre vient des sommets
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

def build_floor(M, coll, x0=-4.0, x1=4.0, z0=-5.6, z1=4.0, suffix=""):
    """Plateforme en planches (dedans + terrasse derrière la porte), posée sur une poutre de rive et des pieux.
    x0..x1 et z0..z1 : ses bornes dans le repère Unity (par défaut, la cabane et sa terrasse, côté porte)."""
    b = Builder([M["plancher"]])
    width, gap, thick = 0.19, 0.008, 0.05
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
    floor = b.finish("Plancher" + suffix, coll)

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
    return floor, b.finish("Charpente_Plancher" + suffix, coll)


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
            # 4 cm de plus de chaque côté : le bout des rondins se cache dans les montants de la fenêtre
            # (au ras du montant, les deux faces se superposaient et « clignotaient » selon l'angle de vue)
            cuts.append((-WIN_W / 2 - 0.04, WIN_W / 2 + 0.04, WIN_Y0, WIN_Y1))
        y = LOG_R + (i % 2) * step / 2
        while y < H - LOG_R * 0.3:
            pieces = [(-side_len / 2 - over, side_len / 2 + over)]
            for (cx0, cx1, cy0, cy1) in cuts:
                # Tout rondin qui touche l'ouverture est coupé (avant : seulement s'il y entrait de plus d'un demi-rayon,
                # et les autres débordaient de quelques centimètres sur la fenêtre)
                if y + LOG_R > cy0 and y - LOG_R < cy1:
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
            b.box(center + B @ Vector((0, 0, WIN_Y0 - 0.17)), (WIN_W + 0.08, 0.26, 0.3), B, M["poutre"], uv_scale=1 / 1.5, bevel=0.008)    # allège : bouche le jour sous l'appui
            b.box(center + B @ Vector((0, 0, wy)), (0.035, 0.05, WIN_Y1 - WIN_Y0), B, M["poutre"], uv_scale=1 / 1.5)                     # croisillon
            b.box(center + B @ Vector((0, 0, wy)), (WIN_W, 0.05, 0.035), B, M["poutre"], uv_scale=1 / 1.5)
    frames = b.finish("Encadrements", coll)

    # Volets peints (ouverts contre le mur, dehors) ; le battant de la porte est un objet à part (door_leaf)
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
    shutters = b.finish("Volets", coll)
    B = basis(DOOR)
    door = door_leaf(M, coll, polar(DOOR, R) + B @ Vector((DOOR_W / 2 + 0.06, LOG_R + 0.05, 0)), B)
    return [walls, chinking, frames, shutters, door]


def door_leaf(M, coll, hinge, B, width=DOOR_W + 0.06, height=DOOR_H):
    """Battant de porte FERMÉ, en planches peintes, avec deux traverses et une poignée de chaque côté.
    Objet à part, dont l'origine est sur la charnière (montant droit vu de l'intérieur) : Unity l'ouvre en le tournant
    autour de l'axe vertical (PortalDoor). B : repère du mur (x le long du mur, y vers l'extérieur, z en haut)."""
    b = Builder([M["peint"], M["fer"]])
    for k in range(6):
        x = -width + (k + 0.5) * width / 6
        b.box(B @ Vector((x, 0.03, height / 2 - 0.02)), (width / 6 - 0.01, 0.045, height - 0.06), B, M["peint"], uv_scale=1 / 1.2, bevel=0.005)
    for zz in (0.35, height - 0.4):
        b.box(B @ Vector((-width / 2, -0.01, zz)), (width - 0.06, 0.03, 0.12), B, M["peint"], uv_scale=1 / 1.2, bevel=0.006)
    for side in (-1, 1):   # poignées en fer, dedans et dehors, du côté opposé à la charnière
        b.box(B @ Vector((-width + 0.14, 0.03 + side * 0.06, 1.0)), (0.04, 0.03, 0.2), B, M["fer"], bevel=0.008)
    for zz in (0.3, height - 0.3):   # pentures (les charnières en fer)
        b.box(B @ Vector((-0.2, 0.058, zz)), (0.4, 0.008, 0.05), B, M["fer"])
    ob = b.finish("Porte_Battant", coll)
    ob.location = hinge
    return ob


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


FLAMES = []   # les flammes du lustre et des lanternes : un objet chacune, exportées avec la cabane


def flame(M, coll, base, name, size=1.0):
    """Une flamme de bougie, à part du reste et avec son origine à sa base : dans Unity, FlameFlicker la fait vaciller
    en l'étirant et en la penchant autour de ce point (8 oct. : « que les flammes soient un peu animées »)."""
    b = Builder([M["flamme"]])
    b.lathe([(0.001 * size, 0.0), (0.012 * size, 0.015 * size), (0.009 * size, 0.035 * size), (0.0, 0.065 * size)], 6, M["flamme"], Vector())
    ob = b.finish(name, coll)
    ob.location = base
    FLAMES.append(ob)
    return ob


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
        flame(M, coll, c + Vector((0, 0, 0.045 + h)), f"Flamme_Lustre_{k + 1}")
    hook = P(0, y + 0.75, 0)
    for k in range(4):
        b.log(polar(45 + 90 * k, rad, y + 0.02), hook, 0.008, M["fer"], M["fer"], 840 + k, segs=4)
    b.log(hook, P(0, ROOF_TOP_Y - 0.7, 0), 0.01, M["fer"], M["fer"], 850, segs=4)
    ob = b.finish("Lustre", coll)
    empty("Lumiere_Lustre", P(0, y + 0.1, 0), coll)
    return ob


def build_lanterns(M, coll):
    """Deux lanternes en fer accrochées aux murs, une bougie allumée entre les montants.
    À -28° (au-dessus du pupitre LANCER / JOUER) : à -60°, elle tombait dans la bibliothèque."""
    b = Builder([M["fer"], M["cire"]])
    for i, ang in enumerate(LANTERNS):
        B = basis(ang)
        wall = polar(ang, R - LOG_R - 0.02)
        c = wall + B @ Vector((0, -0.24, 2.35))
        b.box(wall + B @ Vector((0, -0.12, 2.6)), (0.03, 0.26, 0.03), B, M["fer"])           # potence
        b.box(c + Vector((0, 0, 0.27)), (0.02, 0.02, 0.12), B, M["fer"])
        b.lathe([(0.1, 0.0), (0.11, 0.02), (0.1, 0.03)], 8, M["fer"], c - Vector((0, 0, 0.12)), cap_bottom=M["fer"])
        # Une vraie bougie allumée entre les montants (8 oct.) : avant, une vitre lumineuse pleine cachait tout,
        # et rien ne bougeait. La flamme est un objet à part, animée dans Unity.
        b.lathe([(0.022, 0.03), (0.022, 0.1), (0.018, 0.108)], 8, M["cire"], c - Vector((0, 0, 0.12)), cap_top=M["cire"])
        flame(M, coll, c - Vector((0, 0, 0.007)), f"Flamme_Lanterne_{i + 1}", size=1.3)
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
    """Une cible de fléchettes au mur (les singes de Bloons lancent des fléchettes !). La cible est vide (on y lance les vraies,
    voir Dart.cs) ; trois fléchettes ratées sont plantées dans le mur autour (9 oct.)."""
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
    for (dx, dz, tilt) in ((0.33, 0.12, 8), (-0.31, -0.17, -6), (0.08, -0.36, 12)):   # hors de la cible (rayon 0,24)
        tip = c + B @ Vector((dx, 0.0, dz))   # dans les rondins, juste derrière le devant de la cible
        d = (B @ Vector((math.sin(math.radians(tilt)) * 0.3, -1, 0.15))).normalized()
        b.log(tip, tip + d * 0.1, 0.006, M["fer"], M["fer"], 900, segs=6)
        b.log(tip + d * 0.1, tip + d * 0.16, 0.009, M["plume_rouge"], M["plume_rouge"], 901, segs=6)
        side = d.cross(Vector((0, 0, 1))).normalized()
        for s in (side, Vector((0, 0, 1))):
            q0, q1 = tip + d * 0.14, tip + d * 0.19
            b.face([q0 - s * 0.025, q1 - s * 0.03, q1 + s * 0.03, q0 + s * 0.025], [(0, 0), (1, 0), (1, 1), (0, 1)], M["plume_rouge"])
    return b.finish("Cible", coll)


LANTERNS = (120, -28)                 # angles des lanternes murales (build_lanterns)
SHELVES = (120, 330)                  # murs des étagères hautes (build_wall_decor)


def build_wall_decor(M, coll):
    """Les murs de la cabane habillés (critique du 7 oct. : « surtout les murs ») :
    - une guirlande de petits ballons (ceux de Bloons !) qui court en haut des murs, en festons ;
    - deux étagères hautes avec des pots en terre cuite, des bocaux et des livres.
    Corrigé le 8 oct. (« beaucoup d'éléments se chevauchent ») : les étagères étaient pile derrière les lanternes,
    et les ballons de la guirlande pendaient dans les pots et rentraient dans les rondins. Maintenant :
    l'étagère est à côté de la lanterne, la guirlande passe tout en haut (au-dessus de 2,65 m) et pas au-dessus
    des étagères, et les ballons sont devant les rondins.
    Pas de guirlande sur le mur de la porte (180°) ni sur celui du tableau des chances du coffre (90°), qui monte haut."""
    g = random.Random(31)
    b = Builder([M["ficelle"], M["ballon_deco"]])   # la guirlande à part : les chanfreins des étagères effaceraient ses couleurs
    # Des couleurs de vrais ballons de baudruche, un peu moins criardes qu'avant
    colors = [lin(c) for c in ((0.82, 0.13, 0.12), (0.16, 0.4, 0.82), (0.2, 0.62, 0.26), (0.95, 0.78, 0.15), (0.88, 0.42, 0.68))]
    half = math.tan(math.pi / SIDES)                # demi-largeur d'un mur, en part de son rayon
    r_in = R - LOG_R - 0.13                          # devant les rondins (un ballon fait 7 cm de rayon)
    top, sag, drop, radius = 2.96, 0.08, 0.1, 0.07
    for i in range(SIDES):
        ang = i * 360 / SIDES
        if ang in (90, 180) or ang in SHELVES:
            continue
        B = basis(ang)
        center = polar(ang, r_in)
        a0, a1 = center + B @ Vector((-half * r_in, 0, top)), center + B @ Vector((half * r_in, 0, top))
        pts = []
        for k in range(13):                          # la ficelle pend en feston (un arc de cercle vers le bas)
            t = k / 12
            pts.append(a0.lerp(a1, t) + Vector((0, 0, -sag * math.sin(math.pi * t))))
        for p0, p1 in zip(pts, pts[1:]):
            mid, d = (p0 + p1) / 2, (p1 - p0)
            rot = d.to_track_quat("X", "Z").to_matrix()
            b.box(mid, (d.length, 0.008, 0.008), rot, M["ficelle"])
        for k in range(1, 12, 2):                     # un ballon sous un point sur deux de la ficelle
            col = colors[g.randrange(len(colors))]
            c = pts[k] + Vector((0, 0, -drop))
            res = bmesh.ops.create_icosphere(b.bm, subdivisions=1, radius=radius)   # 80 triangles, lissés : assez pour un petit ballon
            for v in res["verts"]:
                v.co.z *= 1.2                         # un ballon est un peu plus haut que large...
                if v.co.z > 0:
                    v.co.x *= 1 - v.co.z * 3          # ... et se resserre vers son nœud, en haut, attaché à la ficelle
                    v.co.y *= 1 - v.co.z * 3
                v.co += c
            for f in {f for v in res["verts"] for f in v.link_faces}:
                f.material_index = b.mi(M["ballon_deco"])
                f.smooth = True
                f.tag = True
                for loop in f.loops:
                    loop[b.col] = (*col, 1.0)
            b.box(c + Vector((0, 0, (radius * 1.2 + drop) / 2)), (0.006, 0.006, drop - radius * 1.2 + 0.01), Matrix.Identity(3), M["ficelle"])   # la ficelle du ballon

    garland = b.finish("Guirlande", coll)

    # Deux étagères hautes (à 2,45 m) sur un côté du mur, l'autre côté étant pour la lanterne
    b = Builder([M["poutre"], M["pot"], M["livre"], M["cire"]])
    width = 0.62
    books = [lin(c) for c in ((0.55, 0.12, 0.1), (0.15, 0.32, 0.25), (0.62, 0.48, 0.2), (0.18, 0.2, 0.4), (0.45, 0.3, 0.2))]   # cuir rouge, vert, ocre, bleu, brun
    for ang in SHELVES:
        B = basis(ang)
        center = polar(ang, R - LOG_R - 0.18)
        lantern = min(LANTERNS, key=lambda a: abs((a - ang + 180) % 360 - 180))
        x_lantern = (polar(lantern, R) - polar(ang, R)).dot(B @ Vector((1, 0, 0)))
        side = -1 if x_lantern >= 0 else 1             # du côté opposé à la lanterne
        center = center + B @ Vector((side * 0.5, 0, 0))
        b.box(center + B @ Vector((0, 0, 2.45)), (width, 0.24, 0.04), B, M["poutre"], uv_scale=1 / 1.5, bevel=0.005)
        for s in (-0.22, 0.22):                       # les équerres
            b.box(center + B @ Vector((s, 0.06, 2.36)), (0.04, 0.12, 0.16), B, M["poutre"])
        x = -width / 2 + 0.03
        for item in ("pot", "livres", "bocal", "pot"):  # un peu de tout, dans le même ordre sur les deux étagères
            if item == "pot":
                h, r = g.uniform(0.14, 0.2), g.uniform(0.05, 0.065)
                base = center + B @ Vector((x + r, 0, 2.47))
                b.lathe([(r * 0.6, 0), (r * 0.95, h * 0.35), (r, h * 0.6), (r * 0.6, h * 0.92), (r * 0.7, h)], 12, M["pot"], base)
                x += 2 * r + 0.03
            elif item == "bocal":                     # un bocal de cire (des bougies de rechange), couvercle en bois
                r = 0.045
                base = center + B @ Vector((x + r, 0, 2.47))
                b.lathe([(r, 0), (r, 0.11), (r * 0.8, 0.13)], 12, M["cire"], base, cap_top=M["cire"])
                b.lathe([(r * 0.85, 0.13), (r * 0.85, 0.15)], 12, M["poutre"], base, cap_top=M["poutre"])
                x += 2 * r + 0.03
            else:                                     # des livres debout, serrés, de hauteurs et de couleurs différentes
                for n in range(5):
                    w, h = g.uniform(0.025, 0.04), g.uniform(0.15, 0.2)
                    faces = b.box(center + B @ Vector((x + w / 2, 0.02, 2.47 + h / 2)), (w, 0.13, h), B @ Matrix.Rotation(g.uniform(-0.05, 0.05), 3, "Y"),
                                  M["livre"], tint=1.0)
                    col = books[g.randrange(len(books))]          # la couverture : une couleur par livre
                    for f in faces:
                        for loop in f.loops:
                            loop[b.col] = (*col, 1.0)
                    x += w + 0.004
                x += 0.03
    return [garland, b.finish("Etageres", coll)]


# Zones à laisser libres (repère Unity : x à droite, z devant) : la cabane, sa terrasse et la bananeraie derrière elle,
# ou la zone de jeu de la carte
GROVE_Z = -10.1     # le centre de la bananeraie, derrière la porte (GroveCenter dans PrototypeGenerator)


def hub_clear(x, z):
    return (math.hypot(x, z) < 5.2 or (abs(x) < 4.6 and -6.4 < z < 4.6)
            or (abs(x) < 11.4 and GROVE_Z - 6.9 < z < GROVE_Z + 4.4))   # 11,4 : la terrasse agrandie (BAN_X1) + 2 m


def map_clear(x, z):
    # le labyrinthe (24 x 24 m), l'estrade et le pupitre devant, et 4 m de plus que la zone de téléportation
    # (16 m autour du centre, PrototypeGenerator.BuildMap) : on ne se pose plus dans un arbre ou un buisson (8 oct.)
    return abs(x) < 20 and -24 < z < 20


def scatter(g, count, rmin, rmax, clear):
    """Des points au hasard entre rmin et rmax du centre, hors de la zone libre : (x, z, angle)."""
    pts = []
    while len(pts) < count:
        ang = g.uniform(0, 360)
        dist = rmin + (rmax - rmin) * math.sqrt(g.random())       # répartis sur la surface, pas serrés au centre
        x, z = math.sin(math.radians(ang)) * dist, math.cos(math.radians(ang)) * dist
        if not clear(x, z):
            pts.append((x, z, ang))
    return pts


def build_outside(M, coll, clear=hub_clear, seed=5, suffix="", palm_ring=(8, 16), palm_count=14):
    """Dehors : une prairie, des rochers, des buissons, des herbes hautes, des fleurs, des palmiers et les montagnes.
    clear(x, z) : la zone à laisser libre (la cabane, ou la zone de jeu de la carte)."""
    b = Builder([M["herbe"]])
    n = 64
    rad = 95.0                                   # jusqu'au pied des montagnes
    c = P(0, GROUND_Y, 0)
    ring = [P(math.sin(2 * math.pi * k / n) * rad, GROUND_Y, math.cos(2 * math.pi * k / n) * rad) for k in range(n)]
    for k in range(n):
        p0, p1 = ring[k], ring[(k + 1) % n]
        f = b.face([c, p0, p1], [(c.x / 3, c.y / 3), (p0.x / 3, p0.y / 3), (p1.x / 3, p1.y / 3)], M["herbe"])
        f.normal_update()
        if f.normal.z < 0:
            f.normal_flip()
    ground = b.finish("Prairie" + suffix, coll, recalc=False)

    deco = []
    g = random.Random(seed)
    for kind in ("roche", "buisson"):
        bm = bmesh.new()
        count = 22 if kind == "roche" else 60
        for (x, z, ang) in scatter(g, count, 6.0, 30.0, clear):
            pos = P(x, GROUND_Y, z)
            for j in range(1 if kind == "roche" else 3):
                size = g.uniform(0.3, 0.9) if kind == "roche" else g.uniform(0.45, 0.85)
                off = Vector((g.uniform(-0.5, 0.5), g.uniform(-0.5, 0.5), 0)) if j else Vector()
                res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=size)
                for v in res["verts"]:
                    v.co.x *= g.uniform(0.9, 1.3)
                    v.co.z *= 0.6 if kind == "roche" else 0.85
                    v.co += v.co.normalized() * g.uniform(-0.08, 0.08) * size
                    v.co += pos + off + Vector((0, 0, size * (0.15 if kind == "roche" else 0.5)))
        me = bpy.data.meshes.new(kind + suffix)
        bm.to_mesh(me)
        bm.free()
        me.materials.append(M[kind])
        for p in me.polygons:
            p.use_smooth = kind == "buisson"
        ob = bpy.data.objects.new(("Rochers" if kind == "roche" else "Buissons") + suffix, me)
        coll.objects.link(ob)
        deco.append(ob)
    deco.append(build_grass_and_flowers(M, coll, g, clear, suffix))
    deco.append(build_mushrooms(M, coll, g, clear, suffix))
    mountains = build_mountains(M, coll, suffix)
    return [ground] + deco + [mountains, build_palms(M, coll, clear, suffix, palm_ring, palm_count)]


def build_mushrooms(M, coll, g, clear, suffix):
    """Des champignons rouges à points blancs : de petites touffes dans l'herbe, et quelques géants plus loin
    (2 à 3 m, on les voit depuis la cabane et la carte). Pied crème légèrement courbé, chapeau bombé, points blancs."""
    b = Builder([M["champi_pied"], M["champi_chapeau"], M["champi_point"]])

    def mushroom(x, z, h):
        base = P(x, GROUND_Y, z)
        r_stem, r_cap = h * 0.14, h * 0.45
        b.lathe([(r_stem * 1.2, 0), (r_stem, h * 0.4), (r_stem * 0.9, h * 0.85)], 8, M["champi_pied"], base)
        cap = [(r_cap * math.cos(t), h * 0.8 + r_cap * 0.75 * math.sin(t)) for t in np.linspace(0, math.pi / 2, 4)]
        cap[-1] = (0.0001, cap[-1][1])
        b.lathe([(r_stem * 0.9, h * 0.8)] + cap, 10, M["champi_chapeau"], base)
        for k in range(int(2 + h * 2.5)):               # les points blancs, posés sur le chapeau (peu : léger pour le casque)
            t = g.uniform(0.25, 1.2)
            a = g.uniform(0, 2 * math.pi)
            p = base + Vector((math.cos(a) * r_cap * math.cos(t), math.sin(a) * r_cap * math.cos(t), h * 0.8 + r_cap * 0.75 * math.sin(t)))
            s = r_cap * g.uniform(0.08, 0.14)
            b.box(p, (s, s, s * 0.4), Matrix.Identity(3), M["champi_point"])

    for (cx, cz, _) in scatter(g, 20, 5.5, 28.0, clear):        # petites touffes de 2 à 4
        for _ in range(g.randint(2, 4)):
            x, z = cx + g.gauss(0, 0.35), cz + g.gauss(0, 0.35)
            if not clear(x, z):
                mushroom(x, z, g.uniform(0.15, 0.4))
    for (x, z, _) in scatter(g, 7, 14.0, 32.0, clear):          # les géants
        mushroom(x, z, g.uniform(1.8, 3.0))
    return b.finish("Champignons" + suffix, coll)


def build_grass_and_flowers(M, coll, g, clear, suffix):
    """Des touffes d'herbe haute (brins fins, vert foncé au pied, clair à la pointe) et des fleurs (tige, 5 pétales colorés).
    Peu de triangles par touffe : on en met beaucoup sans ralentir le casque."""
    b = Builder([M["vegetation"]])
    dark, light = lin((0.16, 0.36, 0.1)), lin((0.5, 0.75, 0.28))
    petals = [lin(c) for c in ((0.95, 0.85, 0.2), (0.95, 0.4, 0.45), (0.98, 0.98, 0.95), (0.6, 0.45, 0.9), (1.0, 0.6, 0.2))]

    def tri(a, b_, c_, cols):
        f = b.face([a, b_, c_], [(0, 0), (1, 0), (0.5, 1)], M["vegetation"])
        for loop, col in zip(f.loops, cols):
            loop[b.col] = (*col, 1)
        f.tag = True

    def patch(cx, cz, spread, n):
        """n points autour de (cx, cz), en massif, hors de la zone libre"""
        out = []
        for _ in range(n):
            x, z = cx + g.gauss(0, spread), cz + g.gauss(0, spread)
            if not clear(x, z):
                out.append((x, z))
        return out

    # L'herbe haute en massifs (plus beau que des brins isolés), plus serrée près du centre
    for (cx, cz, _) in scatter(g, 110, 5.0, 30.0, clear):
        for (x, z) in patch(cx, cz, 1.2, 9):
            base = P(x, GROUND_Y, z)
            for k in range(7):                               # 7 brins en éventail
                yaw = g.uniform(0, 2 * math.pi)
                d = Vector((math.cos(yaw), math.sin(yaw), 0))
                side = Vector((-d.y, d.x, 0)) * 0.035
                h = g.uniform(0.35, 0.8)
                tip = base + d * g.uniform(0.08, 0.25) + Vector((0, 0, h))
                tri(base - side, base + side, tip, (dark, dark, light))

    # Les fleurs en massifs d'une seule couleur, comme dans un pré
    for (cx, cz, _) in scatter(g, 45, 5.0, 26.0, clear):
        col = petals[g.randrange(len(petals))]
        for (x, z) in patch(cx, cz, 0.7, 8):
            base = P(x, GROUND_Y, z)
            h = g.uniform(0.3, 0.55)
            head = base + Vector((g.uniform(-0.05, 0.05), g.uniform(-0.05, 0.05), h))
            tri(base - Vector((0.015, 0, 0)), base + Vector((0.015, 0, 0)), head, (dark, dark, dark))   # tige
            rot = g.uniform(0, 2 * math.pi)
            for k in range(5):                               # 5 pétales autour d'un cœur jaune
                a0 = rot + 2 * math.pi * k / 5
                p1 = head + Vector((math.cos(a0 - 0.4), math.sin(a0 - 0.4), 0.015)) * 0.11
                p2 = head + Vector((math.cos(a0 + 0.4), math.sin(a0 + 0.4), 0.015)) * 0.11
                tri(head, p1, p2, (lin((1.0, 0.85, 0.2)), col, col))
    return b.finish("Herbes et fleurs" + suffix, coll, recalc=False)


def smooth(e0, e1, x):
    t = min(max((x - e0) / (e1 - e0), 0.0), 1.0)
    return t * t * (3 - 2 * t)


def build_mountains(M, coll, suffix=""):
    """Une vraie chaîne de montagnes tout autour (refaite le 8 oct. : « je veux que les montagnes soient vraiment plus belles »).
    Avant : des cônes posés côte à côte. Maintenant : un seul relief continu, une grille en anneaux de 40 à 180 m,
    sculptée par un bruit « à crêtes » (ridged noise, le bruit des générateurs de terrain) : vallées, arêtes et pics.
    Couleurs par sommet, selon la hauteur ET la pente : prairie au pied, forêt sombre, roche, et de la neige sur
    les sommets mais pas dans les pentes raides (comme en vrai). Au loin, tout se fond dans le bleu du ciel
    (perspective aérienne : c'est ce qui donne la profondeur). Facettes nettes : on garde le style low poly du jeu.
    Devant le joueur (angle 0), la chaîne commence plus loin : on voit plus de prairie devant le plateau."""
    from mathutils import noise
    g = random.Random(11 if not suffix else 23)
    seed = Vector((g.uniform(0, 500), g.uniform(0, 500), g.uniform(0, 500)))
    b = Builder([M["montagne"]])
    grass, forest = lin((0.38, 0.56, 0.24)), lin((0.16, 0.3, 0.14))
    rock, dark_rock, snow = lin((0.56, 0.52, 0.47)), lin((0.36, 0.33, 0.31)), lin((0.95, 0.96, 0.98))
    haze = lin((0.64, 0.74, 0.88))

    def ridged(x, y, scale, octaves=5):
        """Bruit à crêtes : 1 - |bruit|, au carré, sur plusieurs octaves (des arêtes vives là où le bruit passe par 0)"""
        h, amp, total, freq = 0.0, 1.0, 0.0, 1.0 / scale
        for o in range(octaves):
            n = 1.0 - abs(noise.noise(Vector((x * freq, y * freq, 0.0)) + seed * (o + 1)))
            h += n * n * amp
            total += amp
            amp *= 0.5
            freq *= 2.05
        return h / total

    segs, rings, r0, r1 = 192, 34, 40.0, 180.0
    radii = [r0 * (r1 / r0) ** (k / rings) for k in range(rings + 1)]     # serré devant, plus lâche au loin
    grid = []
    for k, r in enumerate(radii):
        row = []
        for j in range(segs):
            ang = 360 * j / segs
            front = max(0.0, 1 - abs((ang + 180) % 360 - 180) / 50)      # 0° = devant le joueur
            start = r0 + 22 * front
            p = polar(ang, r)
            main = smooth(start, start + 38, r)                              # la chaîne principale monte...
            far = smooth(115, 160, r)                                        # ... et une plus haute derrière
            big = 0.65 + 0.7 * (noise.noise(Vector((p.x / 90, p.y / 90, 0)) + seed) * 0.5 + 0.5)   # des massifs plus hauts que d'autres
            h = main * (4 + 34 * ridged(p.x, p.y, 44, 4) ** 1.3 * big) + far * 30 * ridged(p.x, p.y, 62, 4)
            row.append(p + Vector((0, 0, GROUND_Y - 0.4 + h)))
        grid.append(row)

    def colour(centre, normal, dist):
        """La couleur d'une facette : sa hauteur, sa pente (lue sur sa normale) et sa distance"""
        h = centre.z - GROUND_Y
        slope = math.sqrt(max(1 - normal.z ** 2, 0.0)) / max(normal.z, 0.05)   # 0 = plat, 1 = 45°
        wobble = noise.noise(Vector((centre.x / 11, centre.y / 11, 1.0)) + seed)   # des limites irrégulières
        snowline = 25 + 5 * wobble
        if h > snowline and slope < 1.5:
            c = snow
        elif h > snowline - 7 or slope > 1.3:
            c = dark_rock if slope > 1.7 or wobble > 0.3 else rock
        elif h > 2.5 + 2 * wobble:
            t = smooth(2.5, 9, h)
            c = tuple(a * (1 - t) + f * t for a, f in zip(grass, forest))      # la forêt sur les flancs
        else:
            c = grass
        mist = 0.72 * smooth(55, 175, dist)
        return tuple(x * (1 - mist) + hz * mist for x, hz in zip(c, haze))

    for k in range(rings):
        for j in range(segs):
            j2 = (j + 1) % segs
            for tri in ((grid[k][j], grid[k + 1][j], grid[k + 1][j2]), (grid[k][j], grid[k + 1][j2], grid[k][j2])):
                f = b.face(tri, [(0, 0)] * 3, M["montagne"])
                f.normal_update()
                if f.normal.z < 0:
                    f.normal_flip()
                centre = (tri[0] + tri[1] + tri[2]) / 3
                c = colour(centre, f.normal, Vector((centre.x, centre.y)).length)   # une couleur par facette : le style low poly net
                for loop in f.loops:
                    loop[b.col] = (*c, 1)
                f.tag = True
    ob = b.finish("Montagnes" + suffix, coll, recalc=False)
    for p in ob.data.polygons:
        p.use_smooth = False                                         # facettes nettes : style « low poly »
    return ob


def build_palms(M, coll, clear=hub_clear, suffix="", ring=(8, 16), count=14):
    """Des palmiers tout autour : tronc courbe en anneaux, couronne de palmes arquées."""
    g = random.Random(17 if not suffix else 29)
    b = Builder([M["poutre"], M["bout"], M["palme"]])
    for idx, (x, z, ang) in enumerate(scatter(g, count, ring[0], ring[1], clear)):
        base = P(x, GROUND_Y, z)
        height = g.uniform(5, 8)
        lean = Vector((g.uniform(-1, 1), g.uniform(-1, 1), 0)).normalized() * g.uniform(0.6, 1.4)
        pts = [base + lean * (t * t) + Vector((0, 0, height * t)) for t in np.linspace(0, 1, 7)]
        for k in range(6):
            b.log(pts[k], pts[k + 1] + (pts[k + 1] - pts[k]) * 0.05, 0.16 - 0.015 * k, M["poutre"], M["bout"], 1000 + idx * 10 + k, segs=8)
        crown = pts[-1]
        for f in range(9):
            yaw = 2 * math.pi * f / 9 + g.uniform(-0.2, 0.2)
            d = Vector((math.cos(yaw), math.sin(yaw), 0))
            side = Vector((-d.y, d.x, 0))
            length = g.uniform(2.4, 3.4)
            prev_l = prev_r = None
            for s_ in range(7):
                t = s_ / 6
                p = crown + d * (length * t) + Vector((0, 0, 0.6 * t - 1.6 * t * t))
                w = 0.45 * math.sin(math.pi * min(t + 0.08, 1.0)) + 0.02
                l, r = p - side * w, p + side * w
                if prev_l is not None:
                    b.face([prev_l, prev_r, r, l], [(0, t), (1, t), (1, t), (0, t)], M["palme"])
                prev_l, prev_r = l, r
    return b.finish("Palmiers" + suffix, coll, recalc=False)


# ============================================================ la bananeraie (derrière la porte de la cabane)
# Une terrasse en planches au milieu de la prairie, fermée par une barrière en rondins, ouverte côté cabane sur une allée
# bordée de barrières qui mène à la porte de la cabane (la seule porte : on entre et on sort par elle).
# Modélisée comme si le joueur arrivait par l'allée (côté -Z) en regardant vers +Z : les bananiers à gauche, le panier
# à droite, et au fond un abri au toit de chaume, au mur de rondins, où Unity pose l'armoire des améliorations.
# Unity la pose ensuite derrière la cabane, retournée : l'allée arrive à la porte (GroveCenter, GroveYaw).
# Sans paysage : c'est celui de la cabane, qui laisse sa place libre (hub_clear).
# Mesures à garder identiques dans PrototypeGenerator (Grove*, Shed*).
BAN_X0, BAN_X1 = -4.6, 9.4          # la terrasse (repère Unity), agrandie à droite pour la roulette (9 oct.)
BAN_Z0, BAN_Z1 = -3.6, 4.8
PATH_HALF = 1.1                     # demi-largeur de l'allée : le battant de la porte, ouvert, y tient sans toucher la barrière
WALK_Z = -4.7                       # le passage en planches, de la terrasse (BAN_Z0) jusqu'à la terrasse de la cabane
PATH_Z = -6.45                      # le bout de l'allée, contre le mur de la cabane (sa porte est à -6,7 dans ce repère)
SHED_X, SHED_Z0, SHED_Z1 = 3.2, 2.9, 4.6   # l'abri : de -SHED_X à SHED_X, de SHED_Z0 à SHED_Z1
SHED_H = 2.75


def build_bananeraie(M, coll):
    objs = list(build_floor(M, coll, BAN_X0, BAN_X1, BAN_Z0, BAN_Z1, suffix=" bananeraie"))

    # La barrière : des poteaux en rondins tous les 1,6 m au plus, et deux lisses. Ouverte devant sur l'allée,
    # elle-même bordée de deux barrières jusqu'au mur de la cabane.
    b = Builder([M["poutre"], M["bout"]])
    segments = [((BAN_X0, BAN_Z0), (-PATH_HALF, BAN_Z0)), ((PATH_HALF, BAN_Z0), (BAN_X1, BAN_Z0)),
                ((-PATH_HALF, BAN_Z0), (-PATH_HALF, PATH_Z)), ((PATH_HALF, BAN_Z0), (PATH_HALF, PATH_Z)),
                ((BAN_X1, BAN_Z0), (BAN_X1, BAN_Z1)), ((BAN_X1, BAN_Z1), (BAN_X0, BAN_Z1)), ((BAN_X0, BAN_Z1), (BAN_X0, BAN_Z0))]
    seed = 2000
    for (xa, za), (xb, zb) in segments:
        n = max(1, math.ceil(math.hypot(xb - xa, zb - za) / 1.6))
        pts = [(xa + (xb - xa) * k / n, za + (zb - za) * k / n) for k in range(n + 1)]
        for (x, z) in pts:
            b.log(P(x, GROUND_Y - 0.1, z), P(x, 1.05, z), 0.07, M["poutre"], M["bout"], seed, segs=8)
            seed += 1
        for y in (0.5, 0.92):
            b.log(P(xa, y, za), P(xb, y, zb), 0.045, M["poutre"], M["bout"], seed, segs=8)
            seed += 1
    objs.append(b.finish("Barriere", coll))

    # Le passage en planches de l'allée, au-dessus de l'herbe, entre la terrasse et celle de la cabane
    b = Builder([M["plancher"], M["poutre"]])
    z = BAN_Z0
    while z > WALK_Z + 0.01:
        b.box(P(0, -0.025, z - 0.095), (2 * PATH_HALF, 0.18, 0.05), Matrix.Identity(3), M["plancher"],
              uv_scale=1 / 1.2, uv_offset=(rng.random(), rng.random()), bevel=0.006, tint=rng.uniform(0.78, 1.05))
        z -= 0.19
    for side in (-1, 1):   # deux poutres dessous, posées dans l'herbe
        b.box(P(side * (PATH_HALF - 0.1), -0.12, (BAN_Z0 + WALK_Z) / 2), (0.12, abs(WALK_Z - BAN_Z0), 0.14), Matrix.Identity(3), M["poutre"], uv_scale=1 / 1.5, bevel=0.01)
    objs.append(b.finish("Allee", coll))

    # L'abri : un mur de rondins au fond, quatre poteaux, des poutres et un toit de chaume à quatre pans
    b = Builder([M["rondin"], M["bout"], M["poutre"]])
    y, k = LOG_R, 0
    while y < SHED_H - 0.05:
        b.log(P(-SHED_X - 0.15, y, SHED_Z1), P(SHED_X + 0.15, y, SHED_Z1), LOG_R, M["rondin"], M["bout"], 2600 + k)
        y += 2 * LOG_R * 0.92
        k += 1
    for sx in (-1, 1):
        b.log(P(sx * SHED_X, GROUND_Y - 0.1, SHED_Z0), P(sx * SHED_X, SHED_H + 0.05, SHED_Z0), 0.11, M["rondin"], M["bout"], 2700 + sx)
        b.log(P(sx * SHED_X, SHED_H, SHED_Z0 - 0.2), P(sx * SHED_X, SHED_H, SHED_Z1 + 0.2), 0.09, M["poutre"], M["bout"], 2710 + sx)
    b.log(P(-SHED_X - 0.3, SHED_H, SHED_Z0), P(SHED_X + 0.3, SHED_H, SHED_Z0), 0.1, M["poutre"], M["bout"], 2720)
    objs.append(b.finish("Abri", coll))

    b = Builder([M["chaume"], M["poutre"]])
    over, eave, top = 0.45, SHED_H + 0.05, SHED_H + 1.1
    zc = (SHED_Z0 + SHED_Z1) / 2
    ex0, ex1, ez0, ez1 = -SHED_X - over, SHED_X + over, SHED_Z0 - over, SHED_Z1 + over
    rl, rr = P(-SHED_X + 1.0, top, zc), P(SHED_X - 1.0, top, zc)
    c00, c10, c11, c01 = P(ex0, eave, ez0), P(ex1, eave, ez0), P(ex1, eave, ez1), P(ex0, eave, ez1)
    for quad in ((c00, c10, rr, rl), (c11, c01, rl, rr), (c01, c00, rl), (c10, c11, rr)):
        uvs = [((v.x + v.y) / 1.6, v.z / 1.6) for v in quad]
        for mat, dz, up in ((M["chaume"], 0.0, True), (M["poutre"], -0.12, False)):
            f = b.face([v + Vector((0, 0, dz)) for v in quad], uvs, mat)
            f.normal_update()
            if (f.normal.z < 0) == up:
                f.normal_flip()
    objs.append(b.finish("Toit_Abri", coll, recalc=False))

    objs.append(empty("Repere_Porte", polar(DOOR, R), coll))      # mêmes repères que la cabane : Unity l'aligne pareil
    objs.append(empty("Repere_Droite", polar(90, R), coll))
    return objs


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
    # une planche en diagonale sur les 4 côtés, À PLAT dans la face (on tourne autour de la normale de la face)
    for axis, n in (("X", Vector((1, 0, 0))), ("Y", Vector((0, 1, 0)))):
        rot = Matrix.Rotation(math.radians(45), 3, axis)
        size = (0.02, 0.06, (s - 2 * t) * 1.38) if axis == "X" else (0.06, 0.02, (s - 2 * t) * 1.38)
        for sgn in (-1, 1):
            b.box(n * sgn * (s / 2 - 0.012) + Vector((0, 0, s / 2)), size, rot, M["poutre"], uv_scale=1 / 0.6, bevel=0.004)
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
        "horizon": (P(0, 1.7, -5.0), 160, 6),
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
    objs += FLAMES                                    # les flammes du lustre et des lanternes (objets à part, animés dans Unity)
    objs.append(build_rug(M, cabin))
    objs.append(build_dartboard(M, cabin))
    objs += build_wall_decor(M, cabin)
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

    # Le paysage de la carte (scène Labyrinthe) : même style, la zone de jeu laissée libre, plus de palmiers
    land = bpy.data.collections.new("Paysage")
    bpy.context.scene.collection.children.link(land)
    scenery = build_outside(M, land, clear=map_clear, seed=41, suffix=" carte", palm_ring=(18, 34), palm_count=22)
    scenery.append(empty("Repere_Porte", polar(DOOR, R), land))      # mêmes repères que la cabane : Unity l'aligne pareil
    scenery.append(empty("Repere_Droite", polar(90, R), land))
    export(scenery, os.path.join(OUT, "Paysage.glb"))
    for o in scenery:
        o.hide_render = True                                       # pas dans les aperçus de la cabane
    print("triangles du paysage :", sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in scenery if o.type == "MESH"))

    # La bananeraie, derrière la cabane : terrasse, barrière, portail, abri (le paysage est celui de la cabane)
    grove = bpy.data.collections.new("Bananeraie")
    bpy.context.scene.collection.children.link(grove)
    grove_objs = build_bananeraie(M, grove)
    export(grove_objs, os.path.join(OUT, "Bananeraie.glb"))
    print("triangles de la bananeraie :", sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in grove_objs if o.type == "MESH"))
    for o in grove_objs:
        o.hide_render = True

    if "--render-bananeraie" in argv:
        for o in objs:
            o.hide_render = True
        for o in grove_objs:
            o.hide_render = False
        cam = setup_preview(grove_objs)
        folder = argv[argv.index("--render-bananeraie") + 1]
        os.makedirs(folder, exist_ok=True)
        for name, loc, target in (("bananeraie_arrivee", P(0, 1.6, -2.4), P(0, 1.2, 3.5)),
                                  ("bananeraie_portail", P(0.5, 1.6, 2.0), P(0, 1.2, -3.6)),
                                  ("bananeraie_dehors", P(9, 5, -11), P(0, 0.5, 0.5))):
            cam.location = loc
            cam.rotation_euler = (target - loc).to_track_quat("-Z", "Y").to_euler()
            bpy.context.scene.render.filepath = os.path.join(folder, name + ".png")
            bpy.ops.render.render(write_still=True)

    if "--render" in argv:
        cam = setup_preview(objs)
        render_views(cam, argv[argv.index("--render") + 1])


main()
