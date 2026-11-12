# Fabrique GDD_final.pdf à partir de GDD_final.md.
#   python build_final.py            -> GDD_final.html + GDD_final.pdf
#   python build_final.py --propre   -> sans les cadres « capture à faire » (les images manquantes sont retirées)
# Markdown maison (titres numérotés, listes, tableaux, citations, images, « !! » = images côte à côte,
# « === » = page de partie, HTML brut gardé tel quel, {{NOM}} = schéma dessiné par ce script), puis Edge imprime en PDF.
# Deux passes : la 2e écrit les numéros de page dans le sommaire.
import html, math, os, re, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = r'C:\Users\maxen\Desktop\sae 505\SAE501'
CAPTURES = os.path.join(REPO, 'docs', 'captures')
FONTS = os.path.join(REPO, 'Unity', 'Assets', '_Project', 'Art', 'Resources', 'Fonts')
EDGE = r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
NAME = 'GDD_final'
DRAFT = '--propre' not in sys.argv
MISSING = []

# Palette du jeu : bois, or, ardoise, prairie
WOOD, WOOD_D, GOLD, SLATE, CREAM, LEAF, RED, INK, MUTED = '#8a5a1c', '#5b3a14', '#e3a72f', '#2f3b33', '#fbf3df', '#5d9440', '#d8432f', '#2b2118', '#75644f'


def uri(path):
    return 'file:///' + path.replace('\\', '/').replace(' ', '%20')


def image_path(name, width=1500):
    from PIL import Image
    for folder in (os.path.join(HERE, 'images'), CAPTURES, os.path.join(CAPTURES, 'gdd')):
        p = os.path.join(folder, name)
        if os.path.exists(p):
            small = os.path.join(HERE, 'cache', f'f{width}_' + name.rsplit('.', 1)[0] + '.jpg')
            if not os.path.exists(small) or os.path.getmtime(small) < os.path.getmtime(p):
                os.makedirs(os.path.dirname(small), exist_ok=True)
                im = Image.open(p).convert('RGB')
                im.thumbnail((width, width))
                im.save(small, quality=86, optimize=True)
            return uri(small)
    return None


def inline(text):
    t = html.escape(text, quote=False)
    t = re.sub(r'`([^`]+)`', r'<code>\1</code>', t)
    t = re.sub(r'\*\*([^*]+)\*\*', r'<b>\1</b>', t)
    t = re.sub(r'(?<![\w/])_([^_]+)_(?![\w])', r'<i>\1</i>', t)
    t = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', r'\1', t)
    t = t.replace('✅', '<span class="ok">✔</span>').replace('✂', '<span class="cut">✂</span>').replace('◐', '<span class="half">◐</span>')
    return t


IMG = re.compile(r'!\[([^\]]*)\]\(([^)]+)\)')


def figure(caption, name, cls='fig'):
    src = image_path(name)
    if not src:
        MISSING.append(name)
        if not DRAFT:
            return ''
        return f'<figure class="{cls} todo"><div class="todo-box"><b>CAPTURE À FAIRE</b><span>{html.escape(name)}</span></div><figcaption>{inline(caption)}</figcaption></figure>'
    return f'<figure class="{cls}"><img src="{src}"><figcaption>{inline(caption)}</figcaption></figure>'


# ---------------------------------------------------------------- schémas (SVG)

def svg(w, h, body, cls='diagram'):
    return (f'<div class="{cls}"><svg viewBox="0 0 {w} {h}" xmlns="http://www.w3.org/2000/svg" font-family="Segoe UI, Arial">'
            f'<defs><marker id="ah" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
            f'<path d="M0,0 L10,5 L0,10 z" fill="{WOOD_D}"/></marker>'
            f'<marker id="ahg" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
            f'<path d="M0,0 L10,5 L0,10 z" fill="{GOLD}"/></marker></defs>{body}</svg></div>')


def box(x, y, w, h, title, lines=(), fill=CREAM, stroke=WOOD, tcol=WOOD_D, tsize=15):
    s = f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="9" fill="{fill}" stroke="{stroke}" stroke-width="1.6"/>'
    s += f'<text x="{x + w / 2}" y="{y + 22}" text-anchor="middle" font-family="Oswald, Segoe UI" font-size="{tsize}" fill="{tcol}" letter-spacing="0.4">{html.escape(title)}</text>'
    for i, l in enumerate(lines):
        col = '#f3ead6' if fill in (SLATE, WOOD_D) else INK
        s += f'<text x="{x + w / 2}" y="{y + 41 + i * 14.5}" text-anchor="middle" font-size="10.6" fill="{col}">{html.escape(l)}</text>'
    return s


def diagram_loop():
    b = ''
    steps = [('1 · PRÉPARER', ['poser ses singes', 'fusionner, ouvrir le coffre', 'améliorer']),
             ('2 · LANCER', ['le gros bouton', 'la vague part', '(annonce si boss)']),
             ('3 · DÉFENDRE', ['tirer à l\'arc sur la carte', 'ou surveiller la maquette', '20 vies par vague']),
             ('4 · RÉCOLTER', ['bananes au panier', 'récompense de vague', '20 + 10 × n°'])]
    for i, (t, l) in enumerate(steps):
        x = 14 + i * 184
        b += box(x, 34, 150, 92, t, l, fill=SLATE if i == 2 else CREAM, tcol=GOLD if i == 2 else WOOD_D)
        if i < 3:
            b += f'<line x1="{x + 153}" y1="80" x2="{x + 181}" y2="80" stroke="{WOOD_D}" stroke-width="2" marker-end="url(#ah)"/>'
    b += f'<path d="M 641 130 C 641 178, 560 182, 380 182 S 89 178, 89 132" fill="none" stroke="{GOLD}" stroke-width="2.4" stroke-dasharray="6 4" marker-end="url(#ahg)"/>'
    b += f'<rect x="232" y="168" width="296" height="27" rx="13.5" fill="{GOLD}"/>'
    b += f'<text x="380" y="186" text-anchor="middle" font-family="Oswald, Segoe UI" font-size="12.5" fill="{INK}">LES BANANES PAIENT SINGES, COFFRES ET AMÉLIORATIONS</text>'
    b += f'<text x="380" y="20" text-anchor="middle" font-size="11" fill="{MUTED}" font-style="italic">Une vague = un tour de boucle (30 s à 2 min). Pendant la défense, les bananes continuent de tomber… et de pourrir.</text>'
    return svg(760, 204, b)


def diagram_places():
    b = box(10, 62, 130, 76, 'MENU', ['CONTINUER', 'NOUVELLE PARTIE', 'OPTIONS · REJOINDRE'])
    b += box(220, 40, 180, 120, 'LA CABANE (HUB)', ['géant sur la maquette', 'bibliothèque · coffre', 'trophées · fléchettes', 'LANCER / SE TP', '', 'préparation'], fill=SLATE, stroke=SLATE, tcol=GOLD)
    b += box(500, 8, 250, 82, 'LA CARTE', ['à taille réelle, l\'arc en main', 'estrade · pupitre ARC', 'défense'])
    b += box(500, 110, 250, 82, 'LA BANANERAIE', ['bananiers · panier · récolteurs', 'casino (vague 5) · classement', 'économie'])
    b += f'<line x1="142" y1="100" x2="216" y2="100" stroke="{WOOD_D}" stroke-width="2" marker-end="url(#ah)"/>'
    b += f'<line x1="402" y1="78" x2="496" y2="52" stroke="{WOOD_D}" stroke-width="2" marker-end="url(#ah)" marker-start="url(#ah)"/>'
    b += f'<line x1="402" y1="124" x2="496" y2="150" stroke="{WOOD_D}" stroke-width="2" marker-end="url(#ah)" marker-start="url(#ah)"/>'
    b += f'<text x="446" y="50" text-anchor="middle" font-size="10" fill="{MUTED}">SE TP / HUB</text><text x="446" y="38" text-anchor="middle" font-size="9" fill="{MUTED}">voile noir</text>'
    b += f'<text x="447" y="156" text-anchor="middle" font-size="10" fill="{MUTED}">la porte</text>'
    b += f'<text x="179" y="92" text-anchor="middle" font-size="10" fill="{MUTED}">jouer</text>'
    return svg(760, 200, b)


def diagram_economy():
    b = ''
    src = [('PANIER', 'bananes vendues : 5 → 32 pièce', 'dorée (4 %) : × 10'), ('VAGUE FINIE', '20 + 10 × n° de vague', '30 (v. 1) → 1 020 (v. 100)'), ('CASINO', 'roulette × 2 ou × 36', 'blackjack × 2 ou × 2,5')]
    out = [('COFFRE', '40 + 15 × vague vaincue'), ('NOUVEAUX TYPES', '150 à 600'), ('ARC', '100 à 1 300 par palier'), ('BANANERAIE', 'améliorations × 1,6 / niveau · arbres 500, 1 500'), ('RÉCOLTEURS', '150, 300, 450, 600 · améliorations'), ('DIVERS', 'VIDER le plateau : 10 · mises du casino')]
    for i, (t, l1, l2) in enumerate(src):
        y = 22 + i * 74
        b += box(8, y, 216, 62, t, [l1, l2], fill='#eef4e4', stroke=LEAF, tcol='#3d6a26')
        b += f'<line x1="226" y1="{y + 31}" x2="318" y2="126" stroke="{LEAF}" stroke-width="2" marker-end="url(#ah)"/>'
    b += f'<circle cx="380" cy="126" r="58" fill="{GOLD}" stroke="{WOOD_D}" stroke-width="2"/>'
    b += f'<text x="380" y="120" text-anchor="middle" font-family="Bangers, Segoe UI" font-size="25" fill="{INK}" letter-spacing="1">LA BANANE</text>'
    b += f'<text x="380" y="140" text-anchor="middle" font-size="10.5" fill="{INK}">une seule monnaie</text>'
    for i, (t, l) in enumerate(out):
        y = 6 + i * 41
        b += f'<rect x="530" y="{y}" width="222" height="35" rx="7" fill="{CREAM}" stroke="{WOOD}" stroke-width="1.4"/>'
        b += f'<text x="540" y="{y + 15}" font-family="Oswald, Segoe UI" font-size="11.5" fill="{WOOD_D}">{t}</text><text x="540" y="{y + 28.5}" font-size="9.4" fill="{INK}">{html.escape(l)}</text>'
        b += f'<line x1="441" y1="126" x2="526" y2="{y + 17}" stroke="{WOOD}" stroke-width="1.6" marker-end="url(#ah)"/>'
    b += f'<text x="116" y="14" text-anchor="middle" font-family="Oswald, Segoe UI" font-size="11" fill="#3d6a26">CE QUI RAPPORTE</text>'
    b += f'<text x="380" y="210" text-anchor="middle" font-size="10" fill="{MUTED}" font-style="italic">Éclater un ballon ne rapporte rien.</text>'
    return svg(760, 254, b)


def diagram_map():
    wp = [(-12, 5), (2.5, 5), (2.5, 10.5), (-3.5, 10.5), (-3.5, -6), (-8.5, -6), (-8.5, 0.5), (6, 0.5), (6, 6.5), (10.8, 6.5), (10.8, -4), (2, -4), (2, -9), (12, -9)]
    k, ox, oy = 12.5, 180, 170          # 1 m = 12,5 px ; centre de la carte
    P = lambda x, z: (ox + x * k, oy - z * k)
    pts = ' '.join(f'{P(x, z)[0]:.1f},{P(x, z)[1]:.1f}' for x, z in wp)
    b = f'<rect x="{ox - 150}" y="{oy - 150}" width="300" height="300" rx="6" fill="#8cc152" stroke="#5d9440" stroke-width="2"/>'
    for i in range(1, 8):   # la grille de 8 cases de 3 m
        g = ox - 150 + i * 37.5
        b += f'<line x1="{g}" y1="{oy - 150}" x2="{g}" y2="{oy + 150}" stroke="#7fb548" stroke-width="0.7"/><line x1="{ox - 150}" y1="{g - ox + oy}" x2="{ox + 150}" y2="{g - ox + oy}" stroke="#7fb548" stroke-width="0.7"/>'
    b += f'<polyline points="{pts}" fill="none" stroke="#8d8573" stroke-width="{2.2 * k + 3}" stroke-linejoin="round"/>'
    b += f'<polyline points="{pts}" fill="none" stroke="#d9d2bf" stroke-width="{2.2 * k}" stroke-linejoin="round"/>'
    b += f'<polyline points="{pts}" fill="none" stroke="{GOLD}" stroke-width="1.6" stroke-dasharray="5 6" stroke-linejoin="round" marker-end="url(#ahg)"/>'
    for x, z in ((-3.5, 5), (-3.5, 0.5)):   # les deux croisements
        cx, cy = P(x, z)
        b += f'<circle cx="{cx}" cy="{cy}" r="21" fill="none" stroke="{RED}" stroke-width="2.4" stroke-dasharray="4 3"/>'
    ex, ey = P(-12, 5)
    sx, sy = P(12, -9)
    b += f'<rect x="{ex - 7}" y="{ey - 19}" width="9" height="38" rx="3" fill="{SLATE}"/><rect x="{sx - 2}" y="{sy - 19}" width="9" height="38" rx="3" fill="{SLATE}"/>'
    b += f'<rect x="{ox - 46}" y="{oy + 152}" width="92" height="26" rx="4" fill="{WOOD}" stroke="{WOOD_D}" stroke-width="1.5"/>'
    b += f'<text x="{ox}" y="{oy + 169}" text-anchor="middle" font-family="Oswald, Segoe UI" font-size="11" fill="#fff">ESTRADE (joueur)</text>'
    # légende à droite
    lx = 362
    items = [('rect', SLATE, 'ENTRÉE (ouest) et SORTIE (sud-est)', 'deux portails en pierre, voile lumineux, chevrons dorés'),
             ('path', '#d9d2bf', 'LE CHEMIN DE DALLES', '2,2 m de large, 13 lignes droites aux coins arrondis'),
             ('ring', RED, 'LES 2 CROISEMENTS', 'les ballons y passent deux fois : les meilleures places'),
             ('rect', WOOD, 'L\'ESTRADE', 'LANCER / HUB, tableau de la vague, caisse, pupitre ARC'),
             ('rect', '#8cc152', 'LA PRAIRIE', '24 × 24 m (8 cases de 3 m) ; téléportation : la carte + 4 m')]
    for i, (kind, col, t, d) in enumerate(items):
        y = 44 + i * 56
        if kind == 'ring':
            b += f'<circle cx="{lx + 11}" cy="{y + 2}" r="10" fill="none" stroke="{col}" stroke-width="2.4" stroke-dasharray="4 3"/>'
        else:
            b += f'<rect x="{lx}" y="{y - 8}" width="22" height="20" rx="4" fill="{col}" stroke="{WOOD_D}" stroke-width="0.8"/>'
        b += f'<text x="{lx + 34}" y="{y}" font-family="Oswald, Segoe UI" font-size="12.5" fill="{WOOD_D}">{t}</text><text x="{lx + 34}" y="{y + 16}" font-size="10.3" fill="{INK}">{html.escape(d)}</text>'
    b += f'<text x="{ox}" y="14" text-anchor="middle" font-size="10" fill="{MUTED}">NORD</text>'
    return svg(760, 356, b)


def diagram_cabin():
    cx, cy, R = 190, 178, 118
    b = f'<circle cx="{cx}" cy="{cy}" r="{R + 44}" fill="#e9d6b0" stroke="{WOOD_D}" stroke-width="7"/>'
    b += f'<circle cx="{cx}" cy="{cy}" r="{R}" fill="none" stroke="{WOOD}" stroke-width="1.2" stroke-dasharray="5 5"/>'
    b += f'<circle cx="{cx}" cy="{cy}" r="48" fill="#c9503a" stroke="#27306b" stroke-width="5"/><circle cx="{cx}" cy="{cy}" r="78" fill="none" stroke="{LEAF}" stroke-width="1.4" stroke-dasharray="2 4"/>'
    b += f'<circle cx="{cx}" cy="{cy}" r="9" fill="{INK}"/><path d="M {cx - 7} {cy - 12} L {cx} {cy - 24} L {cx + 7} {cy - 12} z" fill="{INK}"/>'
    items = [(0, 'Maquette', SLATE), (-41, 'LANCER / SE TP', LEAF), (37, 'VIDER', RED), (-82, 'Bibliothèque', WOOD), (97, 'Coffre', GOLD), (-136, 'Caisse', WOOD), (180, 'Porte → bananeraie', WOOD_D)]
    for a, label, col in items:
        r = math.radians(a)
        x, y = cx + math.sin(r) * R, cy - math.cos(r) * R
        b += f'<line x1="{cx}" y1="{cy}" x2="{x:.1f}" y2="{y:.1f}" stroke="{WOOD}" stroke-width="0.6" opacity="0.5"/>'
        b += f'<circle cx="{x:.1f}" cy="{y:.1f}" r="13" fill="{col}" stroke="#fff" stroke-width="2"/>'
        b += f'<text x="{x:.1f}" y="{y + 3.5:.1f}" text-anchor="middle" font-size="8.5" font-weight="700" fill="#fff">{a:+d}°</text>'.replace('+0°', '0°')
    b += f'<text x="{cx}" y="{cy + 66}" text-anchor="middle" font-size="9" fill="#3d6a26">zone de téléportation (1,9 m)</text>'
    lx = 420
    b += f'<text x="{lx}" y="34" font-family="Oswald, Segoe UI" font-size="13" fill="{WOOD_D}">TOUT EN CERCLE, À 2,8 M DU JOUEUR</text>'
    b += f'<text x="{lx}" y="51" font-size="10.3" fill="{INK}">Debout sur le tapis, un ou deux pas suffisent pour tout atteindre.</text>'
    rows = [('0°', 'devant', 'La maquette (1,6 m, inclinée de 25°) et le tableau de la vague', SLATE), ('−41°', '', 'Pupitre LANCER / SE TP', LEAF), ('+37°', '', 'Pupitre VIDER', RED),
            ('−82°', 'gauche', 'La bibliothèque : 7 types × 8 raretés', WOOD), ('+97°', 'droite', 'Le coffre et le tableau de ses chances', GOLD),
            ('−136°', '', 'La caisse (les bananes)', WOOD), ('180°', 'derrière', 'La grande porte, l\'armoire à trophées', WOOD_D), ('mur', '+30°', 'La cible de fléchettes', MUTED)]
    for i, (a, side, d, col) in enumerate(rows):
        y = 78 + i * 32
        b += f'<rect x="{lx}" y="{y - 13}" width="50" height="20" rx="10" fill="{col}"/><text x="{lx + 25}" y="{y + 1.5}" text-anchor="middle" font-size="10" font-weight="700" fill="#fff">{a}</text>'
        b += f'<text x="{lx + 60}" y="{y + 1.5}" font-size="10.5" fill="{INK}">{html.escape(d)}</text>'
    return svg(760, 350, b)


def wave_layers(w):
    g = [(8 if w == 1 else 10 + w * 9 // 10, 1 + w // 6)]
    if w >= 2: g.append((5 + w // 3, 2 + w // 6))
    if w >= 3: g.append((3 + (w - 3) * 4 // 5, 1 + (w - 3) // 7))
    if w >= 6: g.append((2 + (w - 6) * 2 // 3, 2 + (w - 6) // 7))
    if w >= 10 and w % 2 == 0: g.append((2 + (w - 10) // 4, 2 + (w - 10) // 6))
    if w >= 10: g.append((2 + (w - 10) // 3, 1 + (w - 10) // 8))
    if w >= 20: g.append((1 + (w - 20) // 4, 1 + (w - 20) // 10))
    if w >= 12 and (w % 4 == 0 or w >= 40): g.append((1 + max(0, w - 12) // 15, 14 + w * 7 // 10))
    if w % 25 == 0: g.append((w // 25, 80 + w * 3 // 2))
    return sum(c * l for c, l in g)


def diagram_waves():
    W, H, L, T, Rm, B = 760, 268, 52, 22, 20, 36
    data = [wave_layers(w) for w in range(1, 101)]
    top = 7000
    X = lambda w: L + (w - 1) / 99 * (W - L - Rm)
    Y = lambda v: T + (1 - v / top) * (H - T - B)
    b = ''
    for v in range(0, 7001, 1000):
        b += f'<line x1="{L}" y1="{Y(v):.1f}" x2="{W - Rm}" y2="{Y(v):.1f}" stroke="#e8dfcd" stroke-width="1"/>'
        b += f'<text x="{L - 7}" y="{Y(v) + 3.5:.1f}" text-anchor="end" font-size="9.5" fill="{MUTED}">{v:,}</text>'.replace(',', ' ')
    for w in (1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100):
        b += f'<text x="{X(w):.1f}" y="{H - B + 15}" text-anchor="middle" font-size="9.5" fill="{MUTED}">{w}</text>'
    b += f'<text x="{(L + W - Rm) / 2}" y="{H - 4}" text-anchor="middle" font-size="10" fill="{MUTED}">numéro de la vague</text>'
    area = f'M {X(1):.1f} {Y(0):.1f} ' + ' '.join(f'L {X(w):.1f} {Y(data[w - 1]):.1f}' for w in range(1, 101)) + f' L {X(100):.1f} {Y(0):.1f} z'
    line = 'M ' + ' L '.join(f'{X(w):.1f} {Y(data[w - 1]):.1f}' for w in range(1, 101))
    b += f'<path d="{area}" fill="{GOLD}" opacity="0.16"/><path d="{line}" fill="none" stroke="{WOOD}" stroke-width="2" stroke-linejoin="round"/>'
    notes = [(12, '1er boss', -30, -44), (25, '1 dirigeable', -8, -46), (50, '2 dirigeables', -36, -26), (75, '3 dirigeables', -38, -24), (100, 'FINALE : 4 dirigeables', -122, 4)]
    for w, t, dx, dy in notes:
        x, y = X(w), Y(data[w - 1])
        b += f'<circle cx="{x:.1f}" cy="{y:.1f}" r="4.5" fill="{RED}" stroke="#fff" stroke-width="2"/>'
        b += f'<text x="{x + dx:.1f}" y="{y + dy:.1f}" font-family="Oswald, Segoe UI" font-size="11" fill="{WOOD_D}">{t}</text>'
        b += f'<text x="{x + dx:.1f}" y="{y + dy + 12:.1f}" font-size="9.5" fill="{MUTED}">{data[w - 1]:,} couches</text>'.replace(',', ' ')
        if w in (12, 25):
            b += f'<line x1="{x:.1f}" y1="{y - 6:.1f}" x2="{x + dx + 14:.1f}" y2="{y + dy + 16:.1f}" stroke="{MUTED}" stroke-width="0.7"/>'
    b += f'<text x="{L}" y="12" font-size="10" fill="{MUTED}">couches de ballon à percer dans la vague</text>'
    return svg(W, H, b, 'diagram chart')


def diagram_timeline():
    b = ''
    weeks = [('S1 · 5-9 OCT.', 'PROUVER', ['3 prototypes au clavier, puis un seul projet', 'passage à la VR, cabane Blender', 'arc de Quincy, coffre, bananier', 'Jalon : GDD v1 + prototype au casque'], LEAF),
             ('S2 · 19-23 OCT.', 'CONSTRUIRE', ['boss qui se divisent, bananes dorées, météo', 'sons, musiques, vibrations, menu, sauvegarde', 'trophées, classement, casino, coop', 'Jalon : test par un autre groupe'], GOLD),
             ('S3 · 9-13 NOV.', 'FINIR', ['gel des nouveautés, 3 idées coupées', '19 optimisations pour le Quest', 'tutoriel complet (23 étapes), coop partagé', 'Jalon : build, rendu et oral'], RED)]
    for i, (d, t, lines, col) in enumerate(weeks):
        x = 8 + i * 252
        b += f'<rect x="{x}" y="8" width="240" height="132" rx="9" fill="{CREAM}" stroke="{col}" stroke-width="1.8"/><rect x="{x}" y="8" width="240" height="34" rx="9" fill="{col}"/><rect x="{x}" y="30" width="240" height="12" fill="{col}"/>'
        b += f'<text x="{x + 12}" y="31" font-family="Bangers, Segoe UI" font-size="19" fill="#fff" letter-spacing="1">{t}</text><text x="{x + 228}" y="30" text-anchor="end" font-family="Oswald, Segoe UI" font-size="11" fill="#fff">{d}</text>'
        for j, l in enumerate(lines):
            bold = ' font-weight="700"' if j == 3 else ''
            b += f'<text x="{x + 12}" y="{62 + j * 20}" font-size="10.4" fill="{INK}"{bold}>{html.escape(l)}</text>'
    return svg(760, 148, b)


def rarities():
    cols = [('Gris', '#8c8c8c'), ('Vert', '#33cc4d'), ('Bleu', '#3373ff'), ('Violet', '#9940e6'), ('Jaune', '#e6b800'), ('Rouge', '#e62626'), ('Arc-en-ciel', 'linear-gradient(90deg,#e62626,#e6b800,#33cc4d,#3373ff,#9940e6)'), ('Blanc', '#ffffff')]
    out = '<div class="rarities">'
    for i, (n, c) in enumerate(cols):
        style = f'background:{c};' + ('color:#2b2118;border:1.5px solid #b9ab92;' if n == 'Blanc' else '')
        out += f'<span class="chip" style="{style}">{i + 1} · {n}</span>' + ('<span class="plus">›</span>' if i < 7 else '')
    return out + '</div><p class="legend">Coffre : du gris au rouge. Arc-en-ciel et blanc : par fusion seulement. Dès le violet, un singe voit les ballons camouflés.</p>'


DIAGRAMS = {'LOOP': diagram_loop, 'PLACES': diagram_places, 'ECONOMY': diagram_economy, 'MAP': diagram_map, 'CABIN': diagram_cabin,
            'WAVES': diagram_waves, 'TIMELINE': diagram_timeline, 'RARITIES': rarities}


# ---------------------------------------------------------------- Markdown -> HTML

def convert(md, pages):
    out, toc, lines, i = [], [], md.split('\n'), 0
    h2n = h3n = 0
    part = ''
    while i < len(lines):
        s = lines[i].strip()
        if not s:
            i += 1
            continue
        m = re.fullmatch(r'\{\{(\w+)\}\}', s)
        if m:
            out.append('<!--TOC-->' if m.group(1) == 'TOC' else DIAGRAMS[m.group(1)]())
            i += 1
            continue
        if s.startswith('==='):   # page de partie : === A | Titre | phrase | image
            letter, title, sub, img = [c.strip() for c in s[3:].split('|')]
            part = title
            src = image_path(img, 1800)
            out.append(f'<section class="part" style="background-image:linear-gradient(180deg,rgba(30,22,12,0.25),rgba(30,22,12,0.88)),url(\'{src}\')">'
                       f'<div class="part-letter">PARTIE {letter}</div><div class="part-title">{inline(title)}</div><div class="part-sub">{inline(sub)}</div></section>')
            toc.append(('part', letter, title, None))
            i += 1
            continue
        if s.startswith('<'):     # HTML brut, jusqu'à la ligne vide
            raw = []
            while i < len(lines) and lines[i].strip():
                raw.append(lines[i])
                i += 1
            out.append('\n'.join(raw))
            continue
        if s.startswith('!!'):
            figs = [figure(c, n, 'half') for c, n in IMG.findall(s)]
            figs = [f for f in figs if f]
            if figs:
                out.append(f'<div class="pair n{len(figs)}">' + ''.join(figs) + '</div>')
            i += 1
            continue
        m = IMG.fullmatch(s)
        if m:
            out.append(figure(m.group(1), m.group(2)))
            i += 1
            continue
        if s.startswith('## '):
            h2n, h3n = h2n + 1, 0
            title = s[3:].strip()
            toc.append(('h2', h2n, title, pages.get(h2n)))
            out.append(f'<h2 id="s{h2n}"><span class="num">{h2n:02d}</span><span class="h2t">{inline(title)}</span></h2>')
            i += 1
            continue
        if s.startswith('### '):
            h3n += 1
            out.append(f'<h3><span class="n3">{h2n}.{h3n}</span> {inline(s[4:].strip())}</h3>')
            i += 1
            continue
        if s.startswith('#### '):
            out.append(f'<h4>{inline(s[5:].strip())}</h4>')
            i += 1
            continue
        if s.startswith('|'):
            rows = []
            while i < len(lines) and lines[i].strip().startswith('|'):
                cells = [c.strip() for c in lines[i].strip().strip('|').split('|')]
                if not all(re.fullmatch(r':?-{3,}:?', c) for c in cells):
                    rows.append(cells)
                i += 1
            cls = ' class="moscow"' if rows[0][0] == 'DOIT' else ''
            h = f'<table{cls}><thead><tr>' + ''.join(f'<th>{inline(c)}</th>' for c in rows[0]) + '</tr></thead><tbody>'
            for r in rows[1:]:
                h += '<tr>' + ''.join(f'<td>{inline(c)}</td>' for c in r) + '</tr>'
            out.append(h + '</tbody></table>')
            continue
        if s.startswith('>'):
            cls = 'key' if s.startswith('>!') else 'quote'
            quote = []
            while i < len(lines) and lines[i].strip().startswith('>'):
                quote.append(lines[i].strip().lstrip('>!').strip())
                i += 1
            out.append(f'<blockquote class="{cls}">' + inline(' '.join(quote)) + '</blockquote>')
            continue
        if re.match(r'^(- |\d+\. )', s):
            ordered = bool(re.match(r'^\d+\. ', s))
            tag = 'ol' if ordered else 'ul'
            h, open_sub = f'<{tag}>', False
            while i < len(lines) and lines[i].strip() and re.match(r'^\s*(- |\d+\. )', lines[i]):
                l = lines[i]
                sub = l.startswith('  ')
                text = re.sub(r'^\s*(- |\d+\. )', '', l)
                if sub and not open_sub:
                    h, open_sub = h[:-5] + '<ul>', True
                if not sub and open_sub:
                    h, open_sub = h + '</ul></li>', False
                h += f'<li>{inline(text)}</li>'
                i += 1
            if open_sub:
                h += '</ul></li>'
            out.append(h + f'</{tag}>')
            continue
        para = []
        while i < len(lines) and lines[i].strip() and not re.match(r'^\s*(#|\||>|- |\d+\. |!|<|\{\{|===)', lines[i]):
            para.append(lines[i].strip())
            i += 1
        out.append('<p>' + inline(' '.join(para)) + '</p>')
    # le sommaire, en deux colonnes
    t = '<div class="toc">'
    for kind, n, title, page in toc:
        if kind == 'part':
            t += f'<div class="toc-part"><span>PARTIE {n}</span> {inline(title)}</div>'
        else:
            t += f'<div class="toc-row"><span class="toc-n">{n:02d}</span><span class="toc-t">{inline(title)}</span><span class="toc-dots"></span><span class="toc-p">{page or ""}</span></div>'
    t += '</div>'
    return '\n'.join(out).replace('<!--TOC-->', t), toc


CSS = """
@font-face { font-family: Bangers; src: url('%(bangers)s'); }
@font-face { font-family: Oswald; src: url('%(oswald)s'); }
@page { size: A4; margin: 16mm 15mm 17mm 15mm;
  @bottom-left { content: "BLOONS VR · Game Design Document"; font-family: Oswald, 'Segoe UI'; font-size: 7.5pt; color: #a08c70; letter-spacing: 0.5px; }
  @bottom-right { content: counter(page); font-family: Bangers, 'Segoe UI'; font-size: 12pt; color: #8a5a1c; } }
@page full { margin: 0; @bottom-left { content: none; } @bottom-right { content: none; } }
* { -webkit-print-color-adjust: exact; print-color-adjust: exact; box-sizing: border-box; }
html, body { margin: 0; padding: 0; }
body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 9.8pt; line-height: 1.45; color: #2b2118; }
p { margin: 0 0 2.4mm; }
b { color: #3a2a18; }
code { font-family: Consolas, monospace; font-size: 8.5pt; background: #f2e8d6; padding: 0 1mm; border-radius: 1mm; }

/* Couverture */
.cover { page: full; width: 210mm; height: 297mm; position: relative; overflow: hidden; break-after: page; background: #1e160c; color: #fff; }
.cover .bg { position: absolute; inset: 0; background-size: cover; background-position: center 40%%; }
.cover .shade { position: absolute; inset: 0; background: linear-gradient(180deg, rgba(20,14,6,0.55) 0%%, rgba(20,14,6,0.05) 30%%, rgba(20,14,6,0.25) 55%%, rgba(20,14,6,0.95) 82%%); }
.cover .top { position: absolute; top: 16mm; left: 18mm; right: 18mm; display: flex; justify-content: space-between; font-family: Oswald, 'Segoe UI'; font-size: 10pt; letter-spacing: 1.5px; text-transform: uppercase; color: #f6e7c4; }
.cover .block { position: absolute; left: 18mm; right: 18mm; bottom: 22mm; }
.cover .kicker { display: inline-block; font-family: Oswald, 'Segoe UI'; font-size: 11pt; letter-spacing: 3px; color: #2b2118; background: #e3a72f; padding: 1.2mm 4mm; border-radius: 1.5mm; }
.cover h1 { font-family: Bangers, 'Segoe UI'; font-weight: normal; font-size: 92pt; line-height: 0.92; letter-spacing: 3px; margin: 4mm 0 2mm; color: #ffd76a; text-shadow: 0 1.2mm 0 #5b3a14, 0 2.4mm 6mm rgba(0,0,0,0.6); }
.cover .tag { font-size: 14.5pt; line-height: 1.35; max-width: 150mm; color: #fdf6e3; margin-bottom: 9mm; }
.cover .meta { display: flex; gap: 10mm; border-top: 0.5mm solid #e3a72f; padding-top: 4mm; font-size: 9.5pt; color: #f1e3c2; }
.cover .meta b { display: block; font-family: Oswald, 'Segoe UI'; font-weight: normal; font-size: 8pt; letter-spacing: 1.5px; color: #e3a72f; text-transform: uppercase; margin-bottom: 0.6mm; }

/* Sommaire, chiffres */
.lead-title { font-family: Bangers, 'Segoe UI'; font-size: 34pt; color: #8a5a1c; letter-spacing: 1.5px; margin: 0 0 1mm; line-height: 1; }
.lead-sub { color: #75644f; margin-bottom: 6mm; }
.stats { display: grid; grid-template-columns: repeat(6, 1fr); gap: 2.5mm; margin: 0 0 7mm; }
.stat { background: #2f3b33; color: #f3ead6; border-radius: 2.5mm; padding: 3mm 2mm 2.6mm; text-align: center; border-bottom: 1.2mm solid #e3a72f; }
.stat b { display: block; font-family: Bangers, 'Segoe UI'; font-weight: normal; font-size: 23pt; line-height: 1; color: #ffd76a; letter-spacing: 1px; }
.stat span { font-size: 7.6pt; line-height: 1.2; display: block; margin-top: 1mm; }
.toc { columns: 2; column-gap: 9mm; }
.toc-part { font-family: Oswald, 'Segoe UI'; font-size: 11pt; color: #fff; background: linear-gradient(90deg, #5b3a14, #9a6a2c); padding: 1.2mm 3mm; border-radius: 1.5mm; margin: 3.2mm 0 1.6mm; break-after: avoid; letter-spacing: 0.4px; }
.toc-part span { color: #ffd76a; margin-right: 2mm; font-size: 8.5pt; letter-spacing: 1.5px; }
.toc-part:first-child { margin-top: 0; }
.toc-row { display: flex; align-items: baseline; gap: 2mm; padding: 1.35mm 0; break-inside: avoid; font-size: 10.6pt; }
.toc-n { font-family: Bangers, 'Segoe UI'; color: #e3a72f; font-size: 11.5pt; width: 6.5mm; }
.toc-dots { flex: 1; border-bottom: 1px dotted #c9b894; transform: translateY(-1mm); }
.toc-p { font-family: Oswald, 'Segoe UI'; color: #8a5a1c; font-size: 9.5pt; min-width: 5mm; text-align: right; }

/* Pages de partie */
.part { page: full; width: 210mm; height: 297mm; break-before: page; break-after: page; background-size: cover; background-position: center; color: #fff; display: flex; flex-direction: column; justify-content: flex-end; padding: 0 20mm 34mm; }
.part-letter { font-family: Oswald, 'Segoe UI'; letter-spacing: 4px; font-size: 12pt; color: #2b2118; background: #e3a72f; align-self: flex-start; padding: 1.2mm 4mm; border-radius: 1.5mm; }
.part-title { font-family: Bangers, 'Segoe UI'; font-size: 64pt; line-height: 1; letter-spacing: 2px; color: #ffd76a; margin: 4mm 0 3mm; text-shadow: 0 1mm 0 #5b3a14, 0 2mm 5mm rgba(0,0,0,0.6); }
.part-sub { font-size: 13.5pt; max-width: 140mm; color: #fdf6e3; line-height: 1.35; }

/* Titres */
h2 { display: flex; align-items: stretch; margin: 7mm 0 3.5mm; break-after: avoid; border-radius: 2mm; overflow: hidden; background: linear-gradient(90deg, #6e4520, #a37331); }
h2 .num { font-family: Bangers, 'Segoe UI'; font-weight: normal; font-size: 19pt; background: #e3a72f; color: #2b2118; padding: 1mm 3.2mm 0.6mm; letter-spacing: 1px; display: flex; align-items: center; }
h2 .h2t { font-family: Bangers, 'Segoe UI'; font-weight: normal; font-size: 18pt; color: #fff; letter-spacing: 1px; padding: 1.2mm 3.5mm 0.8mm; }
.part + h2, .newpage { break-before: page; }
h2.first, .part + h2 { margin-top: 0; }
h3 { font-family: Oswald, 'Segoe UI'; font-size: 12.3pt; color: #5b3a14; border-bottom: 1.5px solid #e3c57f; padding-bottom: 0.7mm; margin: 5mm 0 2.2mm; break-after: avoid; }
h3 .n3 { color: #e3a72f; margin-right: 1.2mm; }
h4 { font-family: Oswald, 'Segoe UI'; font-size: 10.3pt; color: #2f3b33; margin: 3.2mm 0 1.2mm; break-after: avoid; letter-spacing: 0.2px; text-transform: uppercase; }

/* Tableaux, listes, encadrés */
table { width: 100%%; border-collapse: separate; border-spacing: 0; margin: 2mm 0 4mm; font-size: 8.7pt; border: 1px solid #e0d3bb; border-radius: 2mm; overflow: hidden; }
th { background: #2f3b33; color: #f6f1e4; text-align: left; padding: 1.5mm 2.2mm; font-family: Oswald, 'Segoe UI'; font-weight: normal; letter-spacing: 0.4px; font-size: 8.9pt; }
td { padding: 1.3mm 2.2mm; border-top: 1px solid #ece2cf; vertical-align: top; }
tbody tr:nth-child(even) td { background: #fbf6ec; }
tr { break-inside: avoid; }
table.moscow th:nth-child(1) { background: #3d6a26; } table.moscow th:nth-child(2) { background: #a37331; }
table.moscow th:nth-child(3) { background: #5a6b8c; } table.moscow th:nth-child(4) { background: #8c3a30; }
table.moscow td { width: 25%%; }
.ok { color: #3d8a26; font-weight: 700; } .cut { color: #b1392b; } .half { color: #c98a12; }
ul, ol { margin: 1mm 0 2.6mm; padding-left: 5.5mm; }
li { margin: 0.55mm 0; }
li::marker { color: #c98a12; }
blockquote { margin: 3mm 0 4mm; padding: 3mm 4.5mm; border-radius: 2mm; break-inside: avoid; }
blockquote.quote { background: #2f3b33; color: #f6f1e4; font-size: 11pt; line-height: 1.5; border-left: 1.6mm solid #e3a72f; }
blockquote.quote b { color: #ffd76a; }
blockquote.key { background: #fbf3df; border: 1px solid #e3c57f; border-left: 1.6mm solid #e3a72f; }
.cards { display: grid; grid-template-columns: repeat(2, 1fr); gap: 3mm; margin: 2mm 0 4mm; }
.cards.c3 { grid-template-columns: repeat(3, 1fr); } .cards.c4 { grid-template-columns: repeat(4, 1fr); }
.card { background: #fbf6ec; border: 1px solid #e6d9bf; border-top: 1.2mm solid #e3a72f; border-radius: 2mm; padding: 2.6mm 3mm; break-inside: avoid; font-size: 8.9pt; }
.card h5 { font-family: Oswald, 'Segoe UI'; font-size: 10.8pt; margin: 0 0 1mm; color: #5b3a14; letter-spacing: 0.3px; }
.card p { margin: 0; }
.card.dark { background: #2f3b33; color: #f3ead6; border-color: #2f3b33; border-top-color: #e3a72f; } .card.dark h5 { color: #ffd76a; } .card.dark b { color: #fff; }
.rarities { display: flex; align-items: center; flex-wrap: wrap; gap: 1mm; margin: 2.5mm 0 1mm; }
.chip { color: #fff; font-family: Oswald, 'Segoe UI'; font-size: 8.6pt; padding: 0.9mm 2.6mm; border-radius: 4mm; letter-spacing: 0.3px; text-shadow: 0 0.2mm 0.4mm rgba(0,0,0,0.35); }
.chip[style*="ffffff"] { text-shadow: none; }
.plus { color: #c98a12; font-weight: 700; }
.legend { font-size: 8.4pt; color: #75644f; font-style: italic; margin-bottom: 4mm; }

/* Images et schémas */
figure { margin: 3mm 0 4.5mm; text-align: center; break-inside: avoid; }
figure img { max-width: 100%%; max-height: 80mm; border-radius: 2.5mm; border: 0.6mm solid #fff; box-shadow: 0 0 0 0.3mm #d8c8a8, 0 1mm 3mm rgba(60,40,10,0.28); }
figcaption { font-size: 8.2pt; color: #75644f; font-style: italic; margin-top: 1.4mm; line-height: 1.3; }
.pair { display: flex; gap: 4mm; break-inside: avoid; margin: 3mm 0 4.5mm; }
.pair.n3 { gap: 3mm; }
.pair figure { margin: 0; flex: 1 1 0; min-width: 0; }
.pair img { height: 56mm; width: 100%%; object-fit: cover; }
.pair.n3 img { height: 40mm; }
.todo-box { height: 56mm; border: 0.6mm dashed #d8432f; border-radius: 2.5mm; background: repeating-linear-gradient(45deg, #fdf1ee, #fdf1ee 3mm, #fbe4df 3mm, #fbe4df 6mm); display: flex; flex-direction: column; align-items: center; justify-content: center; color: #b1392b; gap: 1mm; }
.todo-box b { font-family: Oswald, 'Segoe UI'; color: #b1392b; letter-spacing: 1px; } .todo-box span { font-family: Consolas, monospace; font-size: 8pt; }
.pair.n3 .todo-box { height: 40mm; }
.diagram { margin: 3mm 0 4.5mm; padding: 3mm; background: #fffdf7; border: 1px solid #e6d9bf; border-radius: 2.5mm; break-inside: avoid; }
.diagram svg { width: 100%%; height: auto; display: block; }
.cap { font-size: 8.2pt; color: #75644f; font-style: italic; text-align: center; margin: -3mm 0 4.5mm; }
.end { margin-top: 8mm; text-align: center; font-family: Bangers, 'Segoe UI'; font-size: 20pt; color: #8a5a1c; letter-spacing: 1.5px; }
"""


def render(pages):
    md = open(os.path.join(HERE, NAME + '.md'), encoding='utf-8').read()
    css = CSS % {'bangers': uri(os.path.join(FONTS, 'Bangers.ttf')), 'oswald': uri(os.path.join(FONTS, 'Oswald-Bold.ttf'))}
    md = md.replace('{{COVER_IMG}}', image_path('n-cabane-interieur.png', 2200) or '')
    body, toc = convert(md, pages)
    page = f'<!doctype html><html lang="fr"><head><meta charset="utf-8"><title>Bloons VR - Game Design Document</title><style>{css}</style></head><body>{body}</body></html>'
    html_path = os.path.join(HERE, NAME + '.html')
    open(html_path, 'w', encoding='utf-8').write(page)
    pdf_path = os.path.join(HERE, NAME + '.pdf')
    subprocess.run([EDGE, '--headless=new', '--disable-gpu', '--no-pdf-header-footer', '--allow-file-access-from-files',
                    f'--print-to-pdf={pdf_path}', uri(html_path)], check=True, timeout=240, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return pdf_path, toc


def find_pages(pdf_path, toc):
    # La page de chaque section : on cherche son titre dans le texte du PDF, après la page du sommaire
    import pymupdf
    doc = pymupdf.open(pdf_path)
    norm = lambda s: re.sub(r'[^a-z0-9]', '', s.lower().replace('é', 'e').replace('è', 'e').replace('ê', 'e').replace('à', 'a').replace('ô', 'o').replace('î', 'i').replace('ç', 'c').replace('û', 'u'))
    texts = [norm(p.get_text()) for p in doc]
    pages, start = {}, 2
    for kind, n, title, _ in toc:
        if kind != 'h2':
            continue
        key = norm(f'{n:02d}' + re.sub(r'[*_`]', '', title))
        for p in range(start, len(texts)):
            if key in texts[p]:
                pages[n], start = p + 1, p
                break
    return pages, len(doc)


MISSING.clear()
pdf, toc = render({})
pages, count = find_pages(pdf, toc)
MISSING.clear()
pdf, toc = render(pages)
print(pdf, os.path.getsize(pdf) // 1024, 'Ko,', count, 'pages,', len(pages), '/', sum(1 for t in toc if t[0] == 'h2'), 'sections trouvées')
if MISSING:
    print('Captures à faire :', ', '.join(sorted(set(MISSING))))
