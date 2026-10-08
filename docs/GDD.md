# Game Design Document — Bloons VR _(v1)_

_v1 rendue le ven. 9 oct. 2026 · version finale le ven. 13 nov. Le GDD vit : on le met à jour jusqu'au rendu._

## 1. Pitch
Un tower defense en VR dans l'univers de Bloons TD : tu es Quincy, tu tires à l'arc sur les ballons, tu poses tes singes à la main sur la maquette, et tu ramasses tes bananes avant qu'elles ne pourrissent.

## 2. Le geste et la boucle
- **Gestes :** tirer à l'arc (Quincy) · saisir et poser un singe sur la maquette · ramasser une banane et la lancer dans la caisse.
- **Boucle :** Le joueur **tire à l'arc et place ses singes** pour **arrêter la vague de ballons**, mais **les ballons arrivent plus vite et plus solides et les bananes pourrissent**, et il gagne **de l'argent et des bananes, qui ouvrent le coffre et débloquent de nouveaux singes**.

## 3. Univers et ambiance
Univers de Bloons TD assumé : singes, ballons colorés, ton cartoon et chaleureux. Le hub est une cabane en rondins éclairée aux bougies, au milieu des montagnes ; la carte est un labyrinthe à taille réelle. Le guide du tutoriel est Pat Fusty.
**Droits :** l'univers appartient à Ninja Kiwi. On le garde comme exercice d'école et fan-game non commercial ; nos modèles sont faits maison (Blender), sans logo ni asset officiel. Une candidature aux Pégases demanderait de renommer et de changer l'habillage.

## 4. Type de jeu et références
- **C'est Bloons TD, mais avec ton arc en main et tes singes posés à la main sur une maquette.**
- Références : Bloons TD 6 (codes TD, types de ballons), Rush Royale (fusion 3 → 1, coffre), Job Simulator (saisir et lancer des objets), In Death / The Lab (tir à l'arc VR).

## 5. Mécaniques et patterns
- **Deux temps :** le **hub** (on est géant devant la maquette : on pose, fusionne, achète) puis la **vague** (on est à taille réelle sur la carte, l'arc en main).
- **Singes :** posés à la main ; un singe saisi montre sa portée au sol. **Fusion :** 3 singes de même type et même niveau donnent 1 singe du niveau suivant.
- **Économie :** finir une vague rapporte de l'argent ; les **bananes** (ramassées au bananier et lancées dans la caisse) débloquent les nouveaux singes. Le **coffre** donne des récompenses tirées à la roulette.
- **Bananier :** les bananes tombent, mûrissent puis pourrissent ; il faut les récolter à temps. Un singe récolteur peut aider.
- **Quincy :** tire à l'arc pendant les vagues (bander, viser, lâcher ; 0,3 s entre deux tirs).
- **Ennemis :** ballons de couleur (la couleur = la résistance), puis un boss final : le dirigeable rouge.
- **Affordances :** la banane mûre brille, la caisse est ouverte, la poignée de l'arc appelle la main, un objet s'éclaire quand la main approche.

## 6. Pourquoi la VR
- **Les mains :** bander l'arc, viser et lâcher ; poser un singe sur la carte ; lancer les bananes.
- **Le regard :** surveiller le chemin des ballons et repérer la banane qui pourrit.
- **La présence et l'échelle :** géant penché sur la maquette dans le hub, puis à taille réelle au milieu des ballons.
- *Test de l'écran :* à la souris, poser des tours marche aussi bien. **C'est l'arc, le changement d'échelle et le double jeu « gérer la base tout en défendant » qui justifient la VR.**

## 7. Périmètre
| DOIT | DEVRAIT | POURRAIT | NE FERA PAS |
|---|---|---|---|
| 1 carte, 1 chemin, ~10 vagues + boss final (dirigeable rouge) | Fusion 3 → 1 (niveaux 1 à 3) | Plus de singes (5 à 7) | Plusieurs cartes |
| Quincy à l'arc (tir physique) | Bananes qui pourrissent, singe récolteur | Améliorations de l'arc | Méta-progression entre les parties |
| 3 singes posables à la main | Coffre et roulette simples | Ballon cœur, ballon blindé | Raretés légendaire / arc-en-ciel, gacha complet |
| Bananes → caisse → déblocage des singes | Décor soigné (cabane, montagnes, lumière) | Casino, cible de fléchettes | Multijoueur |
| Tutoriel avec Pat Fusty (niveau 1) | | | Scénario, monde ouvert |
| Ballons normaux (3 couleurs), argent lisible | | | |

## 8. Choix de confort
- **Déplacement :** téléportation seulement, limitée à la zone de jeu (pas de saut, pas de déplacement au stick).
- **Rotation :** snap turn par crans de 45°.
- **Interface :** argent, bananes et vague sur des panneaux dans le décor et au poignet, à 1-2 m ; pas de texte collé au visage.
- **Technique :** 72 fps minimum (compteur au poignet en build de test) ; low-poly ; 1 unité = 1 m ; vagues courtes ; tout à portée de bras.
