# Roue de la roulette du casino (SAÉ 5D.01) — modèle 3D entièrement généré par ce script Blender (aucun fichier téléchargé).
# Inspirée de la roulette de How to Fish : cuvette en bois épaisse, cases en parts de gâteau, tourelle en laiton à 4 branches.
#
# Lancer depuis la racine du dépôt :
#   blender -b --python Blender/roulette.py                       -> exporte Unity/Assets/_Project/Art/Roulette/Roue.glb
#   blender -b --python Blender/roulette.py -- --render <dossier>  -> exporte, puis rend une image d'aperçu dans <dossier>
#
# Ce qu'il fabrique (Roue.glb) :
#   Roue (vide)                     la racine, posée au centre de la roue
#     Cuvette                       la partie fixe : fond, bord en bois épais, cerclage en laiton, piste en pente où roule
#                                   la bille, et le repère doré côté joueur (-Z) sous lequel la bille s'arrête
#     Plateau (vide)                la partie qui tourne autour de son axe Y (RouletteWheel.rotor)
#       Case_00 ... Case_36         les 37 cases, rangées dans l'ordre de RouletteRules.WheelOrder (Case_00 = le zéro vert) :
#                                   la case i est à l'angle i × 360/37 (0 = +Z, positif vers +X, comme le yaw d'Unity)
#       Separateurs                 les filets en laiton entre les cases
#       Cone                        le cône en bois au centre
#       Tourelle                    la tourelle en laiton à 4 branches et ses boules : la poignée qu'on attrape pour lancer
#
# Repère : on raisonne comme dans Unity (Y en haut, la case i à l'angle i × 360/37 autour de Y, 0 = +Z, positif vers +X).
# P() convertit un point Unity en point Blender. Les mesures doivent rester celles de PrototypeGenerator.BuildWheel.

import bpy, bmesh, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Unity", "Assets", "_Project", "Art", "Roulette")

# --- mesures (en mètres, repère Unity de la roue) ---
SEGS = 74                         # 2 facettes par case : un rond « low poly » assumé, comme How to Fish
POCKET_IN, POCKET_OUT = 0.30, 0.425
POCKET_BOTTOM, POCKET_TOP = 0.02, 0.04
DIVIDER_W, DIVIDER_TOP = 0.007, 0.058
BOWL_IN, BOWL_OUT = 0.44, 0.585
RIM_TOP = 0.11

ORDER = [0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10,
         5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26]   # RouletteRules.WheelOrder
REDS = {1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36}
N = len(ORDER)


def P(x, y, z):
    """Point Unity (x, y, z) -> Blender. glTF/glTFast : Blender (x, y, z) arrive en Unity (-x, z, -y)."""
    return Vector((-x, -z, y))


def polar(angle_deg, r, y):
    """Point Unity à l'angle (0 = +Z, positif vers +X), au rayon r et à la hauteur y."""
    a = math.radians(angle_deg)
    return P(math.sin(a) * r, y, math.cos(a) * r)


# ============================================================ matériaux

def material(name, color, rough=0.7, metal=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.use_backface_culling = True       # une seule face dessinée : moins de travail pour le casque
    bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    return mat


# ============================================================ construction (bmesh)

class Builder:
    """Un objet maillé : on y ajoute des solides, chacun avec son matériau."""

    def __init__(self, name, parent=None):
        self.name, self.parent = name, parent
        self.bm = bmesh.new()
        self.mats = []

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def face(self, verts, mat):
        f = self.bm.faces.new(verts)
        f.material_index = self.mi(mat)
        return f

    def lathe(self, profile, mat, segs=SEGS):
        """Tourne un profil FERMÉ [(rayon, hauteur), ...] autour de l'axe Y : un solide de révolution."""
        rings = [[self.bm.verts.new(polar(360 * s / segs, r, y)) for (r, y) in profile] for s in range(segs)]
        n = len(profile)
        for s in range(segs):
            a, b = rings[s], rings[(s + 1) % segs]
            for k in range(n):
                k2 = (k + 1) % n
                quad = [a[k], a[k2], b[k2], b[k]]
                # un point au centre (rayon 0) : le quadrilatère devient un triangle
                uniq = []
                for v in quad:
                    if all((v.co - u.co).length > 1e-6 for u in uniq):
                        uniq.append(v)
                if len(uniq) >= 3:
                    self.face(uniq, mat)

    def sector(self, r0, r1, a0, a1, y0, y1, mat, steps=3):
        """Une part de couronne pleine (entre les rayons r0 et r1, les angles a0 et a1, les hauteurs y0 et y1)."""
        angles = [a0 + (a1 - a0) * i / steps for i in range(steps + 1)]
        ib = [self.bm.verts.new(polar(a, r0, y0)) for a in angles]
        it = [self.bm.verts.new(polar(a, r0, y1)) for a in angles]
        ob = [self.bm.verts.new(polar(a, r1, y0)) for a in angles]
        ot = [self.bm.verts.new(polar(a, r1, y1)) for a in angles]
        for i in range(steps):
            self.face([it[i], ot[i], ot[i + 1], it[i + 1]], mat)   # dessus
            self.face([ib[i], ib[i + 1], ob[i + 1], ob[i]], mat)   # dessous
            self.face([ob[i], ob[i + 1], ot[i + 1], ot[i]], mat)   # bord extérieur
            self.face([ib[i], it[i], it[i + 1], ib[i + 1]], mat)   # bord intérieur
        self.face([ib[0], ob[0], ot[0], it[0]], mat)               # côtés
        self.face([ib[-1], it[-1], ot[-1], ob[-1]], mat)

    def bar(self, a, r0, r1, w, y0, y1, mat):
        """Une barre droite le long d'un rayon (angle a), de largeur w (filets, branches de la tourelle)."""
        rad = math.radians(a)
        side = Vector((math.cos(rad), 0, -math.sin(rad))) * (w / 2)   # perpendiculaire au rayon, dans le plan
        dirv = Vector((math.sin(rad), 0, math.cos(rad)))
        pts = []
        for r in (r0, r1):
            for sgn in (-1, 1):
                for y in (y0, y1):
                    c = dirv * r + side * sgn
                    pts.append(self.bm.verts.new(P(c.x, y, c.z)))
        # pts : [r0-,y0] [r0-,y1] [r0+,y0] [r0+,y1] [r1-,y0] [r1-,y1] [r1+,y0] [r1+,y1]
        q = pts
        for f in ([0, 2, 3, 1], [4, 5, 7, 6], [0, 1, 5, 4], [2, 6, 7, 3], [1, 3, 7, 5], [0, 4, 6, 2]):
            self.face([q[i] for i in f], mat)

    def ball(self, center_unity, radius, mat):
        res = bmesh.ops.create_icosphere(self.bm, subdivisions=2, radius=radius)
        c = P(*center_unity)
        for v in res["verts"]:
            v.co += c
        for f in {f for v in res["verts"] for f in v.link_faces}:
            f.material_index = self.mi(mat)

    def finish(self, smooth=False):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=1e-6)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)   # tous les solides sont fermés : normales vers l'extérieur
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(m)
        for p in mesh.polygons:
            p.use_smooth = smooth
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        obj.parent = self.parent
        return obj


def empty(name, parent=None):
    o = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(o)
    o.parent = parent
    return o


# ============================================================ la roue

def build():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)

    wood = material("Roulette_Bois", (0.30, 0.15, 0.06), rough=0.65)
    dark = material("Roulette_BoisFonce", (0.12, 0.06, 0.025), rough=0.7)
    track = material("Roulette_Piste", (0.20, 0.10, 0.04), rough=0.5)
    brass = material("Roulette_Laiton", (0.85, 0.58, 0.18), rough=0.35, metal=0.5)   # à moitié métal : il reste doré même sans reflets
    red = material("Roulette_Rouge", (0.62, 0.03, 0.02), rough=0.45)
    black = material("Roulette_Noir", (0.02, 0.02, 0.02), rough=0.45)
    green = material("Roulette_Vert", (0.03, 0.40, 0.12), rough=0.45)

    root = empty("Roue")

    # --- la cuvette (fixe) ---
    bowl = Builder("Cuvette", root)
    bowl.lathe([(0.0, 0.0), (BOWL_IN, 0.0), (BOWL_IN, POCKET_BOTTOM), (0.0, POCKET_BOTTOM)], dark)   # le fond, sous le plateau
    # le bord épais : paroi extérieure, dessus arrondi en deux pans, puis la piste en pente vers les cases
    bowl.lathe([(BOWL_IN, 0.0), (BOWL_OUT, 0.0), (BOWL_OUT, RIM_TOP - 0.025), (BOWL_OUT - 0.012, RIM_TOP),
                (0.535, RIM_TOP), (0.47, 0.065), (BOWL_IN, 0.05)], wood)
    bowl.lathe([(0.47, 0.0645), (0.535, 0.1095), (0.534, 0.1105), (0.469, 0.0655)], track)       # la piste de la bille
    bowl.lathe([(BOWL_OUT - 0.001, 0.055), (BOWL_OUT + 0.008, 0.055), (BOWL_OUT + 0.008, 0.072), (BOWL_OUT - 0.001, 0.072)], brass)   # le cerclage
    bowl.bar(180, BOWL_OUT - 0.06, BOWL_OUT - 0.005, 0.03, RIM_TOP - 0.004, RIM_TOP + 0.016, brass)  # le repère, côté joueur (-Z)
    bowl.finish()

    # --- le plateau (tourne) ---
    rotor = empty("Plateau", root)
    half = 180.0 / N
    gap = math.degrees(DIVIDER_W / ((POCKET_IN + POCKET_OUT) / 2)) / 2
    for i, n in enumerate(ORDER):
        mat = green if n == 0 else red if n in REDS else black
        case = Builder(f"Case_{i:02d}", rotor)
        a = i * 360.0 / N
        case.sector(POCKET_IN, POCKET_OUT, a - half + gap, a + half - gap, POCKET_BOTTOM, POCKET_TOP, mat)
        case.finish()

    sep = Builder("Separateurs", rotor)
    for i in range(N):
        sep.bar(i * 360.0 / N + half, POCKET_IN, POCKET_OUT, DIVIDER_W, POCKET_BOTTOM, DIVIDER_TOP, brass)
    sep.lathe([(POCKET_OUT, POCKET_BOTTOM), (POCKET_OUT + 0.012, POCKET_BOTTOM), (POCKET_OUT + 0.012, DIVIDER_TOP), (POCKET_OUT, DIVIDER_TOP)], brass)   # le liseré autour des cases
    sep.finish()

    cone = Builder("Cone", rotor)
    cone.lathe([(0.0, POCKET_BOTTOM), (POCKET_IN, POCKET_BOTTOM), (POCKET_IN, 0.045), (0.11, 0.10), (0.05, 0.112), (0.0, 0.112)], dark)
    cone.lathe([(POCKET_IN - 0.012, 0.045), (POCKET_IN, 0.045), (POCKET_IN, 0.06), (POCKET_IN - 0.012, 0.06)], brass)   # le liseré intérieur
    cone.finish()

    turret = Builder("Tourelle", rotor)
    turret.lathe([(0.0, 0.11), (0.032, 0.11), (0.032, 0.15), (0.048, 0.158), (0.048, 0.172), (0.02, 0.18), (0.0, 0.18)], brass, segs=24)
    for k in range(4):
        a = 45 + 90 * k
        turret.bar(a, 0.025, 0.165, 0.022, 0.154, 0.176, brass)       # une branche
        rad = math.radians(a)
        turret.ball((math.sin(rad) * 0.175, 0.165, math.cos(rad) * 0.175), 0.022, brass)   # sa boule au bout
    turret.ball((0, 0.195, 0), 0.02, brass)                            # la boule du sommet
    turret.finish(smooth=False)
    return root


# ============================================================ export et aperçu

def descendants(o):
    out = [o]
    for c in o.children:
        out += descendants(c)
    return out


def export(root, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in descendants(root):
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True, export_yup=True, export_apply=True,
                              export_lights=False, export_cameras=False)
    print("exporté :", path)


def render(folder):
    scene = bpy.context.scene
    for engine in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            continue
    scene.render.resolution_x, scene.render.resolution_y = 1000, 700
    scene.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("Ciel")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.35, 0.45, 0.35, 1)
    scene.world = world
    sun = bpy.data.lights.new("Soleil", "SUN")
    sun.energy = 4
    so = bpy.data.objects.new("Soleil", sun)
    so.rotation_euler = (math.radians(40), 0, math.radians(160))
    scene.collection.objects.link(so)
    cam = bpy.data.cameras.new("Oeil")
    cam.lens = 35
    co = bpy.data.objects.new("Oeil", cam)
    scene.collection.objects.link(co)
    scene.camera = co
    # Vue du joueur : debout devant la table, côté repère (-Z), qui regarde la roue d'en haut
    co.location = P(0.15, 0.85, -1.05)
    co.rotation_euler = (P(0, 0.05, 0) - co.location).to_track_quat("-Z", "Y").to_euler()
    os.makedirs(folder, exist_ok=True)
    scene.render.filepath = os.path.join(folder, "roulette.png")
    bpy.ops.render.render(write_still=True)
    print("rendu :", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    root = build()
    export(root, os.path.join(OUT, "Roue.glb"))
    if "--render" in argv:
        render(argv[argv.index("--render") + 1])


main()
