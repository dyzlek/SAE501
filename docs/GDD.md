# Game Design Document — Bloons VR _(v2)_

_SAÉ 5D.01 · BUT MMI 3 · IUT de Béziers. Équipe : Dylan, Maxens, Nicolas._
_v1 rendue le ven. 9 oct. 2026 ; v2 le 9 oct. (état du jeu après la fusion des trois branches) ; version finale le ven. 13 nov._

## 1. Pitch
Un tower defense en VR dans l'univers de Bloons TD : tu es Quincy, tu tires à l'arc sur les ballons, tu poses tes singes à la main sur la maquette, et tu fais tourner ta bananeraie pour payer le tout.

## 2. Le geste et la boucle
- **Gestes :** bander l'arc, viser et lâcher · saisir un singe et le poser sur la maquette · attraper une banane et la lancer dans le panier · enfoncer de gros boutons et pousser une porte.
- **Boucle (30 s) :** le joueur **tire à l'arc et place ses singes** pour **arrêter la vague de ballons**, mais **les ballons arrivent plus nombreux et plus solides, et les bananes pourrissent si on ne les ramasse pas**, et il gagne **des bananes, qui achètent des singes, des coffres et des améliorations**.

## 3. Univers et ambiance
Univers de Bloons TD assumé : singes, ballons colorés, ton cartoon et chaleureux.
- **Le hub :** une cabane en rondins éclairée aux bougies et au lustre, au milieu d'une prairie et de montagnes low-poly.
- **La bananeraie :** derrière la grande porte de la cabane, une terrasse en planches fermée par une barrière : bananiers, panier, comptoir d'améliorations sous un abri, et la roulette.
- **La carte :** une prairie façon Bloons TD (chemin de dalles qui se croise, fleurs, buissons), à taille réelle, avec deux portails en pierre qui montrent l'entrée et la sortie des ballons.

**Droits :** l'univers appartient à Ninja Kiwi. On le garde comme exercice d'école et fan-game non commercial. Nos modèles sont faits maison (scripts Blender), sans logo ni asset officiel. Une candidature aux Pégases demanderait de renommer le jeu et de changer l'habillage.

## 4. Type de jeu et références
- **C'est Bloons TD, mais avec ton arc en main et tes singes posés à la main sur une maquette.**
- **Références :** Bloons TD 6 (codes du TD, types de ballons, carte Monkey Lane), Rush Royale (fusion, coffre et raretés), Job Simulator (saisir et lancer des objets), In Death et The Lab (tir à l'arc en VR), How to Fish (la mise visible sur la table).

## 5. Mécaniques et patterns
- **Deux temps :**
  - au **hub**, on est géant devant la maquette : on pose, fusionne, achète ;
  - pendant la **vague**, on est à taille réelle sur la carte, l'arc en main.

  On passe de l'un à l'autre par un bouton SE TP. Un voile noir couvre chaque téléportation.
- **Singes :** 7 types (Classique, Boomerang, Canon, Sniper, Punaise, Glace, Colle), à débloquer avec des bananes.
  - **Pose :** on prend un singe dans la bibliothèque et on le pose sur la maquette. Un singe saisi montre sa portée au sol.
  - **En jeu :** les singes posés se tournent vers leur cible, font un geste propre à leur type et lancent de vrais projectiles. Le Glace envoie une onde de froid.
  - **Fiche :** le bouton A affiche la fiche du singe (perce, portée, cadence, cibles).
- **Fusion et raretés :** 2 singes identiques donnent 1 singe de la rareté suivante. Il y a 8 raretés : gris, vert, bleu, violet, jaune, rouge, arc-en-ciel et blanc.
- **Coffre :** on le paie en bananes et il s'ouvre en roulette pour donner un singe. Ses chances sont affichées à côté.
- **Économie (une seule monnaie : la banane) :**
  - **Sources :** les bananes ramassées et lancées dans le panier, plus un bonus à chaque vague finie.
  - **Dépenses :** les singes, le coffre, les arbres et les améliorations.
- **Bananeraie :**
  - **Arbres :** les bananes tombent, mûrissent puis pourrissent : il faut les récolter à temps. On peut planter jusqu'à 3 bananiers (500 puis 1 500 bananes).
  - **Comptoir :** il améliore la production, la fraîcheur et la valeur des bananes pour tous les arbres.
  - **Récolteurs :** des singes récolteurs ramassent et lancent les bananes à notre place.
- **Quincy à l'arc :** bander, viser, lâcher, avec 0,3 s entre deux tirs. Le pupitre ARC propose des améliorations qui se cumulent : flèche perforante, transperçante, tir triple, explosion.
- **Ballons :** normal (la couleur indique la résistance), rapide, blindé, cœur (il se regonfle), puis les boss : un gros ballon lent (dès la vague 15) et le dirigeable rouge (vagues 25, 50, 75). **100 vagues** de difficulté progressive : chaque sorte de ballon arrive à son palier, puis son nombre et sa solidité montent. La vague 100 (4 dirigeables rouges) donne la victoire, et le mode infini continue ensuite.
- **Bonus du hub :**
  - une cible de fléchettes au mur (3 fléchettes par manche, meilleur score) ;
  - une roulette de casino dans la bananeraie, où l'on mise des bananes sur une couleur (version bêta-test).
- **Affordances :**
  - un gros bouton rond s'enfonce ;
  - la banane mûre se voit et la pourrie brunit et sent mauvais ;
  - un objet s'éclaire quand la main ou le rayon le vise ;
  - des panneaux sur les portes indiquent où elles mènent (BANANERAIE depuis la cabane, CABANE depuis dehors).

## 5 bis. Tutoriel (niveau 1)
Une consigne à la fois, écrite sur des tableaux du décor (suspendu dans la cabane, sur poteaux dans la bananeraie et sur la carte), avec une flèche dorée qui rebondit au-dessus de ce qu'il faut toucher. Chaque étape arrive avec son titre qui surgit, sa consigne écrite lettre par lettre et une gerbe de petits ballons.
1. **L'univers** en trois tableaux (Monkey Lane, l'alerte aux Bloons, « toi, c'est Quincy »), avec un bouton SUIVANT.
2. **La vague 1 à l'arc seul** : SE TP, LANCER, puis tirer. On n'a encore aucun singe.
3. **Le premier singe**, offert : on rentre au hub, on le prend dans la bibliothèque et on le pose sur le plateau.
4. **Les bananes** : passer la porte, lancer une banane dans le panier.
5. **Le coffre et la fusion**, puis on lance la vague 2 et le tutoriel est fini.

Un bouton PASSER saute tout (le premier singe est quand même offert).

**Prix (équilibrés le 9 oct.) :**
- **Gains :** une banane vaut 5 au départ, une vague finie rapporte 20 + 10 × son numéro.
- **Coffre :** 40, plus 15 par vague vaincue.
- **Déblocage des types :** Boomerang 150, Punaise 200, Glace 300, Canon 400, Colle 500, Sniper 600.
- **Améliorations de l'arc :** de 100 à 1 300.
- **Singes récolteurs :** 150, puis 300, etc.
- **Arbres :** 500, puis 1 500.

## 6. Pourquoi la VR
- **Les mains :** on bande l'arc, on vise et on lâche ; on pose un singe sur la maquette ; on lance les bananes dans le panier ; on pousse la porte.
- **Le regard :** on surveille le chemin des ballons, on repère la banane qui pourrit, on suit la bille de la roulette.
- **La présence et l'échelle :** dans le hub, on est penché en géant sur la maquette ; pendant la vague, on est à taille réelle au milieu des ballons.
- **Le corps dans l'espace :** on passe la porte de la cabane pour aller récolter, puis on revient poser ses singes.
- **Test de l'écran :** à la souris, poser des tours marcherait aussi bien. **Ce qui justifie la VR, c'est l'arc, le changement d'échelle et le double jeu « gérer la base tout en défendant ».**

## 7. Périmètre
| DOIT | DEVRAIT | POURRAIT | NE FERA PAS |
|---|---|---|---|
| 1 carte, 1 chemin, 100 vagues progressives + finale (dirigeables rouges) ✅ | Fusion et raretés ✅ | Plus de types de singes | Plusieurs cartes |
| Quincy à l'arc (tir physique) ✅ | Bananes qui pourrissent, singes récolteurs ✅ | Améliorations de l'arc ✅ | Méta-progression entre les parties |
| Singes posés à la main ✅ | Coffre et roulette simples ✅ | Ballons cœur et blindé ✅ | Gacha complet, achats réels |
| Bananes → panier → déblocage des singes ✅ | Décor soigné (cabane, montagnes, lumière) ✅ | Roulette de casino, cible de fléchettes ✅ (bonus) | Multijoueur |
| Tutoriel intégré (niveau 1) ✅ | Animations des singes ✅ | 2e et 3e bananiers ✅ | Scénario, monde ouvert |
| Ballons de couleur, argent lisible ✅ | | | |

_✅ = déjà dans le jeu (à valider au casque). Reste pour S2-S3 : l'équilibrage, les tests du silence et la stabilité._

## 8. Choix de confort
- **Déplacement :** téléportation seulement, limitée aux zones de jeu (pas de saut, pas de déplacement au stick). Un voile noir fondu couvre chaque téléportation et chaque passage de porte.
- **Rotation :** snap turn par crans de 45°.
- **Interface :** l'argent, la vague et les prix sont affichés sur des ardoises et des panneaux dans le décor, à 1-2 m, ou au poignet. Pas de texte collé au visage.
- **Portée :** tout tient sur un cercle de 2,8 m autour du joueur dans la cabane, à portée de bras. On peut attraper une banane ou un singe de loin, jusqu'à 6 m, sans se baisser au sol.
- **Technique :**
  - 72 fps minimum, avec un compteur au poignet en build de test ;
  - modèles low-poly et décor fusionné par couleur ;
  - 1 unité Unity = 1 m ;
  - vagues courtes.
