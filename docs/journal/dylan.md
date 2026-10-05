# Journal de bord — Dylan

_Entrée la plus récente en haut. Une entrée par jour travaillé. Toute aide de l'IA est notée ici (outil, pour quoi, gardé/jeté)._

## Lun. 5 oct. 2026
- **Fait :** création du dépôt GitHub, lecture des consignes, première analyse de l'idée et brouillon du GDD, test du prototype v2 et captures, **analyse critique du prototype (personnelle, pas encore discutée avec l'équipe)**, test et analyse des prototypes de Nicolas (coffres) et de Maxens (bananier), intégration des trois prototypes dans un seul projet (branche `feat/integration`).
- **Bloque :** —
- **Demain :** corriger les points de mon analyse critique (disposition VR, bibliothèque, halo de fusion, déplacement sur la carte).

**Captures du prototype v2** (clavier/souris, greybox) :

Hub : la bibliothèque à gauche et le plateau devant.

![Hub : bibliothèque et plateau](../captures/proto-v2-hub.png)

Le plateau, c'est la carte en direct : on y voit les ballons et les singes.

![Plateau en direct](../captures/proto-v2-plateau-direct.png)

On pose un singe sur le plateau, avec un aperçu vert quand c'est possible.

![Placement d'un singe](../captures/proto-v2-placement.png)

La carte, avec les singes posés depuis le hub.

![Carte](../captures/proto-v2-carte.png)

**Mon analyse critique du prototype v2** _(mon avis seul pour l'instant)_

- **Ce qui marche :** l'idée globale est la bonne. Le hub avec sa bibliothèque, le plateau qui montre la carte en direct, et la carte avec les singes posés depuis le hub, tout ça fonctionne ensemble.
- **Disposition pour la VR :** en VR, le joueur sera au centre, à son point d'apparition. Tous les éléments du hub doivent être **autour de lui**, inclinés vers lui et à portée de main. Aujourd'hui, ils sont alignés devant et sur les côtés, comme pour un écran.
- **La bibliothèque n'est pas utilisable :** 56 cubes (7 types × 8 raretés) avec leurs étiquettes, c'est trop d'informations d'un coup. On n'arrive pas à la lire. Il faut une autre solution.
- **Rendre la fusion visible :** quand on vise avec un singe en main un singe identique déjà posé, l'aperçu doit montrer qu'**on ne peut pas poser** à cet endroit. Un **halo blanc** autour du singe déjà posé doit indiquer qu'on peut **fusionner**. Aujourd'hui, l'aperçu devient simplement vert, et on ne distingue pas « poser » de « fusionner ».
- **Déplacer les singes sur la carte :** on doit pouvoir **prendre et déplacer** un singe directement quand on est sur la carte, pas seulement depuis le plateau du hub.


**Proposition alternative (testée, non retenue)** : hub en cercle autour du joueur, avec une bibliothèque réduite à 7 socles (un par type) et un sélecteur de rareté à 8 pastilles.
On garde de cet essai le plateau incliné, mais pas la bibliothèque : on revient à la bibliothèque en étagères (7 types × 8 raretés).

![Alternative : hub en cercle, vue de face](../captures/alternative-hub-cercle-1.png)

![Alternative : bibliothèque à 7 socles et pastilles de rareté](../captures/alternative-hub-cercle-2.png)

![Alternative : vue d'ensemble](../captures/alternative-hub-cercle-3.png)

**Version actuelle (prototype v3)** : bibliothèque en deux meubles d'étagères (×1,1), plateau incliné posé sur une planche et un pied.

![v3 : les deux meubles de la bibliothèque et le plateau](../captures/proto-v3-hub-bibliotheques.webp)

![v3 : plateau incliné devant la bibliothèque](../captures/proto-v3-plateau-incline.webp)

**Analyse des prototypes de mes camarades** _(mon avis)_

- **Nicolas (ouverture de coffres)** :
  - La roulette qui défile pour afficher la rareté gagnée est une bonne idée.
  - L'animation du coffre fait trop maladroite (« goofy ») : elle est à retravailler.
  - Je proposerais qu'une fois la rareté choisie par la roulette, **le singe sorte du coffre** avec la couleur de sa rareté. Ce serait plus satisfaisant et plus lisible.
- **Maxens (bananier)** : rien à redire. Le prototype est simple et efficace.

### IA
| Outil | Pour quoi | Gardé / jeté |
|---|---|---|
| Claude (Claude Code) | Résumé du support de lancement dans CLAUDE.md, squelette du dépôt (.gitignore Unity, LFS, README, modèles) | Gardé |
| Claude (Claude Code) | Règle « journal obligatoire » pour l'IA, création du dépôt GitHub | Gardé |
| Claude (Claude Code) | Analyse de l'idée TD Bloons par rapport aux consignes, GDD v0 (boucle, pourquoi la VR, périmètre proposé) | À valider en équipe |
| Claude (Claude Code) | Workflow Git (branches + PR), journaux général et par personne | Gardé |
| Claude (Claude Code) | Prototype clavier/souris (sans VR) : générateur de scènes Hub + Map, bibliothèque des 7 singes × 8 raretés, plateau 8×8 (poser / fusion 2→1 / échanger), piste en labyrinthe, vagues de ballons, singes qui tirent | À tester |
| Claude (Claude Code) | Prototype v2 : une seule scène (hub + carte), plateau = carte en miniature en direct (ballons, singes, joueur), placement libre hors piste, aperçu vert/rouge, téléportation | À tester |
| Claude (Claude Code) | Tags Unity (Ballon, Singe, Joueur, Plateau, Bibliotheque, Piste, Terrain, Bouton) créés automatiquement | Gardé |
| Claude (Claude Code) | Mode démo + build Windows pour captures d'écran | Jeté (retiré à ma demande) |
| Claude (Claude Code) | Ajout de mes captures du prototype dans mon compte rendu du jour | Gardé |
| Claude (Claude Code) | Reformulation de mon analyse critique (le fond est le mien) | Gardé |
| Claude (Claude Code) | Corrections suite à mon analyse : aperçu rouge + halo blanc pour la fusion, prendre/poser/fusionner directement sur la carte | Gardé |
| Claude (Claude Code) | Proposition alternative : hub en cercle, bibliothèque à 7 socles + sélecteur de rareté | Jeté (non retenu, voir captures) |
| Claude (Claude Code) | Retour à la bibliothèque en étagères ×1,1, en 2 meubles espacés (rangée la plus haute à 1,65 m) ; plateau incliné posé sur une planche + pied qui ne traverse plus | À tester |
| Claude (Claude Code) | Ajout de mes captures de la version actuelle (v3) | Gardé |
| Claude (Claude Code) | Préparation des projets de Nicolas et Maxens pour les tester (dossiers à part), mise en forme de mon analyse de leurs prototypes | Gardé |
| Claude (Claude Code) | Intégration des 3 prototypes sur `feat/integration` : merge des branches de Nicolas et Maxens, socle Unity 6000.6 gardé, packages glTFast / XR Toolkit / OpenXR ajoutés (le module VR ajouté aussi a été retiré : il n'existe plus en 6000.6), `.glb` en LFS ; package perso `unity-mcp` de Maxens non repris | À tester |
| Claude (Claude Code) | Ajout des retours de Maxens (envoyés sur Discord) dans son journal, à sa place | Gardé |
| Claude (Claude Code) | Hub en cercle autour du joueur : plateau plus grand (1,6 m) devant, bibliothèque à gauche et à droite, bananier + panier de Maxens derrière, coffre de Nicolas derrière à droite ; branchements : prendre les bananes, ouvrir le coffre (clic ou E), argent commun (bananes → coffre) | À tester |
| Claude (Claude Code) | Dossier `prompts/` (mes prompts reformulés et corrigés), règle « prompts » dans CLAUDE.md pour nous trois, README (rôles + journal commun du jour) | Gardé |
| Claude (Claude Code) | Un seul menu, « SAE » : menu « SAE501 » de Nicolas supprimé (packages déjà dans le projet, coffre monté par le hub), menus « Bananes » de Maxens retirés (son installeur reste utilisé par le hub) | Gardé |
| Claude (Claude Code) | Suppression des scènes de test de Nicolas et Maxens (`Sandbox/`) et des anciennes scènes `Hub.unity` / `Map.unity` (prototype v1) : il ne reste que `Jeu.unity` | Gardé |
| Claude (Claude Code) | Ajout de mes captures de l'intégration dans le journal général | Gardé |
| Claude (Claude Code) | Corrections du hub intégré : bibliothèques courbes qui suivent le cercle (lisibles depuis le centre, textes qui ne se chevauchent plus), coffre qui se tourne automatiquement vers le joueur, panier écarté du bananier | À tester |
