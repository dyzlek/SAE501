# Game Design Document — [Nom du jeu] _(brouillon v0)_

_v1 à rendre le ven. 9 oct. 2026 (2 pages max) · version finale le ven. 13 nov. Le GDD vit : on le met à jour jusqu'au rendu._

## 1. Pitch
_Proposition :_ Un tower defense en VR où tu es Quincy : tu tires à l'arc sur les ballons, puis tu poses tes singes à la main sur la table de jeu, et tu récoltes tes bananes avant qu'elles ne pourrissent pour en avoir d'autres.

## 2. Le geste et la boucle
- **Gestes :** tirer à l'arc (Quincy) · saisir et poser un singe sur la maquette · ramasser une banane et la jeter dans la caisse.
- **Boucle :** Le joueur **tire à l'arc et place ses singes** pour **arrêter la vague de ballons**, mais **les ballons arrivent plus vite et plus solides et les bananes pourrissent**, et il gagne **de l'argent à chaque vague finie, qui ouvre des coffres et de nouveaux singes**.

## 3. Univers et ambiance
Univers de Bloons : singes, ballons colorés, ton cartoon. Le guide du tutoriel est Pat Fusty.
**À trancher :** les assets officiels de Bloons appartiennent à Ninja Kiwi. Ça peut passer en exercice d'école, mais c'est bloquant pour une candidature aux Pégases. Faut-il des assets maison ou low-poly « inspirés de » ?

## 4. Type de jeu et références
- **C'est Bloons TD, mais avec ton arc en main et tes singes posés à la main sur une maquette.**
- Références : Bloons TD 6 (codes TD, types de ballons), Rush Royale (fusion 2 → 1, coffres), Bloons TD VR / Tower Tag (TD en VR), Job Simulator (saisir et lancer des objets).

## 5. Mécaniques et patterns
- **Tours (singes) :** classique, boomerang, canon, sniper, tireur de punaises, singe de glace, encolleur.
- **Fusion :** 2 singes de même type et même niveau donnent 1 singe du niveau suivant (en VR : on prend un singe et on le pose sur l'autre).
- **Coffres (gacha) :** raretés gris, vert, bleu, violet, légendaire, arc-en-ciel. Le prix augmente à chaque coffre ouvert. Le légendaire et l'arc-en-ciel sont peut-être hors des coffres.
- **Économie :** tuer un ballon ne rapporte rien ; finir une vague ou tuer un boss rapporte de l'argent.
- **Bananier** (améliorable) : les bananes tombent, il faut les ramasser et les mettre dans la caisse avant qu'elles pourrissent. Plus tard, on peut acheter un singe récolteur, payant et limité.
- **Roulette :** on mise ses gains, qui sont soit perdus, soit multipliés.
- **Quincy :** il tire à l'arc pendant les vagues. Son arc s'améliore (cadence, tir x3). Il ne tire pas dans le hub où l'on place les singes.
- **Ennemis :** ballons normaux (couleurs = résistance), ballons cœur (ils se régénèrent), ballons blindés. Deux boss : le dirigeable rouge et le dirigeable bleu.
- **Affordances :** la banane mûre brille (on la ramasse), la caisse est ouverte (on y dépose), un singe saisi montre sa portée au sol, un ballon rouge est dangereux.

## 6. Pourquoi la VR
- **Les mains :** bander l'arc, viser et lâcher ; poser un singe sur la carte ; ramasser et jeter les bananes.
- **Le regard :** surveiller plusieurs chemins et repérer la banane qui pourrit pendant la vague.
- **La présence et l'échelle :** on est géant penché sur la maquette (le hub), puis à taille réelle au milieu des ballons (Quincy).
- *Test de l'écran :* à la souris, poser des tours et cliquer des bananes marche aussi bien. **Ce sont l'arc et le double jeu « gérer la base tout en défendant » qui justifient la VR.**

## 7. Périmètre _(proposition, à valider en équipe)_
| DOIT | DEVRAIT | POURRAIT | NE FERA PAS |
|---|---|---|---|
| 1 carte, 1 chemin, ~10 vagues + un boss final (le dirigeable rouge) | Fusion 2 → 1 (niveaux 1 à 3) | 5-7 singes | Plusieurs cartes |
| Quincy à l'arc (tir physique) | Bananier améliorable et bananes qui pourrissent | Roulette | Méta-progression entre les parties |
| 3 singes posables à la main (classique, canon, glace) | Coffre simple avec 3 raretés | Ballon cœur, dirigeable bleu | Raretés légendaire et arc-en-ciel, gacha complet |
| Bananes → caisse → argent | Ballon blindé | Améliorations de l'arc | Singe récolteur automatique |
| Tutoriel avec Pat Fusty (niveau 1) | | | Multijoueur |
| Ballons normaux (3 couleurs) | | | |

## 8. Choix de confort
- **Déplacement :** téléportation entre quelques points fixes (hub et postes de tir). Pas de déplacement libre au stick.
- **Rotation :** snap turn par crans de 45°.
- **Interface :** argent et vague affichés sur un panneau dans le décor, et sur la montre au poignet.
- **Technique :** 72 fps minimum ; peu de ballons à l'écran (pooling, low-poly) ; 1 unité = 1 m ; vagues courtes.
