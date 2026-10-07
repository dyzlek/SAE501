# Coffre du hub (SAÉ 5D.01) — modèle 3D généré par ce script Blender, dans le style de la cabane (cabane.py) :
# mêmes textures de bois, mêmes ferrures, même outil de modélisation (Builder).
#
# Lancer depuis la racine du dépôt :
#   blender -b --python Blender/coffre.py                        -> exporte Unity/Assets/_Project/Art/Coffre/Coffre.glb
#   blender -b --python Blender/coffre.py -- --render <dossier>   -> exporte, puis rend des images d'aperçu
#
# Le coffre est en deux objets, pour pouvoir l'animer dans Unity :
#   Coffre_Caisse    : la caisse (planches, coins et bandes de fer, serrure) ;
#   Coffre_Couvercle : le couvercle bombé, son origine sur la charnière (arrière, en haut) : il s'ouvre en tournant autour de X.
# Repère Unity : Y en haut, +Z = devant (côté serrure, vers le joueur). Taille : 0,8 × 0,5 × 0,55 m (avec le couvercle).

import os, sys, math

# On reprend tout cabane.py (textures, matériaux, Builder) sans lancer sa fonction main().
HERE = os.path.dirname(os.path.abspath(__file__))
with open(os.path.join(HERE, "cabane.py"), encoding="utf-8") as f:
    source = f.read()
exec(compile(source.rsplit("\nmain()", 1)[0], "cabane.py", "exec"))

OUT_CHEST = os.path.join(ROOT, "Unity", "Assets", "_Project", "Art", "Coffre")

W, D, H = 0.8, 0.5, 0.36          # caisse : largeur (X), profondeur (Z), hauteur (Y)
LID_H = 0.16                      # hauteur du bombé du couvercle
PLANK = 0.12                      # hauteur d'une planche
BAND = 0.045                      # largeur d'une bande de fer
I3 = Matrix.Identity(3)


def U(x, y, z):
    """Point Unity -> Blender (le coffre est autour de l'origine)."""
    return P(x, y, z)


def box(b, center, size, mat, **kw):
    """Pavé en mesures Unity : size = (largeur X, hauteur Y, profondeur Z). (Builder.box attend l'ordre Blender x, y, z.)"""
    return b.box(U(*center), (size[0], size[2], size[1]), I3, mat, **kw)


def arc_point(t, inflate=0.0):
    """Point de l'arc du couvercle (t = 0 à l'arrière, 1 à l'avant), autour de la charnière : (y, z) Unity."""
    a = math.pi * t
    return math.sin(a) * (LID_H + inflate), -math.cos(a) * (D / 2 + inflate) + D / 2


def arc_strip(b, x0, x1, inflate, mat, steps, tints=None, y0=0.0):
    """Surface bombée entre x0 et x1, en lattes (une par pas). Renvoie ses faces."""
    faces = []
    for i in range(steps):
        ya, za = arc_point(i / steps, inflate)
        yb, zb = arc_point((i + 1) / steps, inflate)
        verts = [U(x0, ya + y0, za), U(x1, ya + y0, za), U(x1, yb + y0, zb), U(x0, yb + y0, zb)]
        seg = math.hypot(yb - ya, zb - za)
        f = b.face(verts, [(0, i * 0.3), (abs(x1 - x0) * 1.6, i * 0.3), (abs(x1 - x0) * 1.6, i * 0.3 + seg * 1.6), (0, i * 0.3 + seg * 1.6)], mat)
        faces.append(f)
        if tints:
            for loop in f.loops:
                loop[b.col] = (tints[i], tints[i], tints[i], 1.0)
            f.tag = True
    return faces


def build_body(M, coll):
    b = Builder([M["caisse"], M["poutre"], M["fer"], M["or"]])
    # Planches horizontales, chacune un peu teintée différemment (comme les rondins)
    rows = int(round(H / PLANK))
    for i in range(rows):
        y = (i + 0.5) * H / rows
        tint = 0.86 + 0.14 * ((i * 37) % 5) / 4
        box(b, (0, y, 0), (W, H / rows - 0.006, D), M["caisse"], uv_scale=1 / 0.6, uv_offset=(0.13 * i, 0.31 * i), bevel=0.006, tint=tint)
    # Montants de coin en bois sombre
    for sx in (-1, 1):
        for sz in (-1, 1):
            box(b, (sx * (W / 2 - 0.02), H / 2, sz * (D / 2 - 0.02)), (0.07, H + 0.01, 0.07), M["poutre"], uv_scale=1 / 0.6, bevel=0.008)
    # Bandes de fer : 2 qui font le tour (avant, dessous, arrière) et une en bas tout autour
    for x in (-W * 0.28, W * 0.28):
        box(b, (x, H / 2, 0), (BAND, H + 0.012, D + 0.014), M["fer"], bevel=0.004)
    box(b, (0, 0.035, 0), (W + 0.014, BAND, D + 0.014), M["fer"], bevel=0.004)
    # Rivets dorés sur les bandes, devant et derrière
    for x in (-W * 0.28, W * 0.28):
        for y in (0.1, H - 0.08):
            for sz in (-1, 1):
                box(b, (x, y, sz * (D / 2 + 0.01)), (0.02, 0.02, 0.012), M["or"], bevel=0.003)
    # Serrure dorée sur la face avant (+Z), et son trou
    box(b, (0, H - 0.075, D / 2 + 0.012), (0.12, 0.13, 0.024), M["or"], bevel=0.008)
    box(b, (0, H - 0.09, D / 2 + 0.025), (0.02, 0.045, 0.004), M["fer"])
    # Poignées sur les côtés : une barre de fer horizontale tenue par deux pattes
    for sx in (-1, 1):
        box(b, (sx * (W / 2 + 0.045), H * 0.62, 0), (0.025, 0.025, 0.16), M["fer"], bevel=0.005)
        for sz in (-1, 1):
            box(b, (sx * (W / 2 + 0.025), H * 0.62, sz * 0.07), (0.04, 0.02, 0.02), M["fer"])
    return b.finish("Coffre_Caisse", coll)


def build_lid(M, coll):
    """Couvercle bombé, construit autour de la charnière (origine) puis posé en haut de la caisse, à l'arrière."""
    b = Builder([M["caisse"], M["poutre"], M["fer"], M["or"]])
    steps = 6
    tints = [0.86 + 0.14 * ((i * 53) % 5) / 4 for i in range(steps)]
    arc_strip(b, W / 2, -W / 2, 0.0, M["caisse"], steps, tints)                 # le dessus, en 6 lattes
    inner = arc_strip(b, -W / 2, W / 2, -0.03, M["poutre"], steps, [0.45] * steps)   # le dessous (sombre), vu coffre ouvert
    # Flancs en demi-disque, en bois sombre
    for sx in (-1, 1):
        x = sx * W / 2
        pts = [arc_point(k / 12) for k in range(13)]
        verts = [U(x, y, z) for (y, z) in pts]
        uvs = [(z * 1.6, y * 1.6) for (y, z) in pts]
        if sx > 0:
            verts, uvs = verts[::-1], uvs[::-1]
        b.face(verts, uvs, M["poutre"])
    # Bandes de fer qui suivent le bombé
    for x in (-W * 0.28, W * 0.28):
        arc_strip(b, x + BAND / 2, x - BAND / 2, 0.008, M["fer"], 12)
    # Moraillon doré devant (il vient sur la serrure)
    box(b, (0, -0.03, D + 0.014), (0.07, 0.09, 0.016), M["or"], bevel=0.006)
    # Orientation des faces (une seule face dessinée) : le dessus et les flancs regardent vers l'extérieur du
    # couvercle, le dessous regarde vers l'intérieur (vers la caisse)
    center = U(0, 0, D / 2)
    for f in b.bm.faces:
        outward = (f.calc_center_median() - center)
        if f in inner:
            outward = -outward
        f.normal_update()
        if f.normal.dot(outward) < 0:
            f.normal_flip()
    ob = b.finish("Coffre_Couvercle", coll, recalc=False)
    ob.location = U(0, H, -D / 2)          # l'origine (la charnière) en haut de la caisse, à l'arrière
    return ob


def chest_preview(objs, folder):
    cam = setup_preview(objs)
    cam.data.lens = 50
    os.makedirs(folder, exist_ok=True)
    lid = next(o for o in objs if o.name == "Coffre_Couvercle")
    for name, loc, open_deg in (("coffre_ferme", U(1.3, 0.9, 1.8), 0), ("coffre_ouvert", U(1.2, 1.2, 1.7), 70)):
        lid.rotation_euler = (math.radians(-open_deg), 0, 0)   # autour de la charnière ; le signe se vérifie sur l'image
        cam.location = loc
        target = U(0, 0.25, 0)
        cam.rotation_euler = (target - loc).to_track_quat("-Z", "Y").to_euler()
        bpy.context.scene.render.filepath = os.path.join(folder, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("rendu :", name)
    lid.rotation_euler = (0, 0, 0)


def main_chest():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(OUT_CHEST, exist_ok=True)
    M = build_materials()
    M["or"] = material("Or", color=(0.95, 0.72, 0.25), rough=0.35, metal=0.9)
    coll = bpy.data.collections.new("Coffre")
    bpy.context.scene.collection.children.link(coll)
    body = build_body(M, coll)
    lid = build_lid(M, coll)
    export([body, lid], os.path.join(OUT_CHEST, "Coffre.glb"))
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in (body, lid))
    print("triangles du coffre :", tris)
    if "--render" in argv:
        chest_preview([body, lid], argv[argv.index("--render") + 1])


main_chest()
