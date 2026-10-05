# Journal de bord — Dylan

_Entrée la plus récente en haut. Une entrée par jour travaillé. Toute aide de l'IA est notée ici (outil, pour quoi, gardé/jeté)._

## Lun. 5 oct. 2026
- **Fait :** création du dépôt GitHub, lecture des consignes, première analyse de l'idée et brouillon du GDD.
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

**Mon analyse critique du prototype v2**

- **Ce qui marche :** l'idée globale est la bonne. Le hub avec sa bibliothèque, le plateau qui montre la carte en direct, et la carte avec les singes posés depuis le hub, tout ça fonctionne ensemble.
- **Disposition pour la VR :** en VR, le joueur sera au centre, à son point d'apparition. Tous les éléments du hub doivent être **autour de lui**, inclinés vers lui et à portée de main. Aujourd'hui, ils sont alignés devant et sur les côtés, comme pour un écran.
- **La bibliothèque n'est pas utilisable :** 56 cubes (7 types × 8 raretés) avec leurs étiquettes, c'est trop d'informations d'un coup. On n'arrive pas à la lire. Il faut une autre solution.
- **Rendre la fusion visible :** quand on vise avec un singe en main un singe identique déjà posé, l'aperçu doit montrer qu'**on ne peut pas poser** à cet endroit. Un **halo blanc** autour du singe déjà posé doit indiquer qu'on peut **fusionner**. Aujourd'hui, l'aperçu devient simplement vert, et on ne distingue pas « poser » de « fusionner ».
- **Déplacer les singes sur la carte :** on doit pouvoir **prendre et déplacer** un singe directement quand on est sur la carte, pas seulement depuis le plateau du hub.


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
