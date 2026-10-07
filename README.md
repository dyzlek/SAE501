# [Nom du jeu] — SAÉ 5D.01 · Jeu VR

> **Pitch :** _une phrase._

BUT MMI 3, IUT de Béziers. Jeu en réalité virtuelle réalisé sous Unity (C#) en 3 semaines. Rendu le 13 novembre 2026.

## Équipe et rôles actuels
_Répartition au 5 oct. 2026, elle évoluera avec le projet._

| Membre | Rôle actuel | Ce qu'il a produit |
|---|---|---|
| **Dylan** | Hub, plateau et carte · intégration du projet · dépôt Git et journaux | Hub (bibliothèque, plateau = carte en direct, placement libre, fusion 2 → 1, halo de fusion), carte en labyrinthe et vagues de ballons ; fusion des 3 prototypes |
| **Nicolas** | Coffres et rareté (gacha) | Ouverture de coffre avec roulette façon CS, 7 raretés et probabilités qui se débloquent au fil des coffres, coffre 3D animé |
| **Maxens** | Économie : bananier et bananes | Bananier qui produit des bananes, bananes qui pourrissent, panier qui rapporte de l'argent, récolteur automatique limité |

Encadrement : Antoine Chollet (game design), Davide Di Pierro (technique), Nicolas Maurin (gestion de projet), Ilyasse Lojdi (3D et optimisation).

## Où on en est : mer. 7 oct. 2026 (semaine 1 : PROUVER)
- **Idée** : un tower defense VR dans l'univers de Bloons (voir le [GDD](docs/GDD.md)). Le joueur récolte des bananes, ouvre des coffres pour gagner des singes, les pose et les fusionne sur un plateau, puis défend la carte à l'arc de Quincy.
- **Ce qui marche (dans `main`)** :
  - deux niveaux, **Hub** (cabane modélisée dans Blender) et **Labyrinthe** (carte, vagues, arc), chargés tous les deux : la vague continue quand on revient au hub ;
  - **un seul joueur VR** déplacé d'un niveau à l'autre (téléportation + rotation par crans), et un mode PC (clavier-souris) pour tester vite ;
  - économie : bananier, bananes, panier, argent, améliorations, singes récolteurs (jusqu'à 4) ;
  - coffre avec roulette et raretés, inventaire dans la bibliothèque, plateau = carte en direct, fusion de singes ;
  - 10 vagues écrites à l'avance, victoire après le dirigeable rouge ; arc de Quincy sur la carte ;
  - textes posés sur le décor (ardoises, plaques), pas d'interface collée au visage.
- **Pas encore fait** : jamais testé en build sur le casque (le Quest n'est pas reconnu en débogage USB), GDD v1, nom du jeu, tutoriel, sons et vibrations.
- **Suivi** : toutes les tâches sont des issues sur le [tableau GitHub](https://github.com/users/dyzlek/projects/2) (mode d'emploi : [ORGANISATION.md](docs/ORGANISATION.md)).

## Urgent pour jeu. 8 oct.
Vendredi 9 oct. : **GDD v1 + prototype jouable au casque**. Dans l'ordre :
1. **Build sur le casque** ([#13](https://github.com/dyzlek/SAE501/issues/13)) : activer le mode développeur et autoriser le débogage USB sur le Quest, sinon rien n'est testable.
2. **Tester `main` au casque** ([#14](https://github.com/dyzlek/SAE501/issues/14)) : un seul joueur, téléportation partout, LANCER qui ne fait rien ([#15](https://github.com/dyzlek/SAE501/issues/15)).
3. **GDD v1** ([#12](https://github.com/dyzlek/SAE501/issues/12)) : 2 pages, avec le nom du jeu et le périmètre DOIT / DEVRAIT / POURRAIT / NE FERA PAS.
4. **Bugs qui cassent le jeu au casque** : point d'apparition ([#65](https://github.com/dyzlek/SAE501/issues/65)), téléportation sur le plateau ([#67](https://github.com/dyzlek/SAE501/issues/67)) et hors de la carte ([#81](https://github.com/dyzlek/SAE501/issues/81)), saut à enlever ([#55](https://github.com/dyzlek/SAE501/issues/55)), arc qu'on peut spammer ([#60](https://github.com/dyzlek/SAE501/issues/60)).
5. **Réserver les casques** sur Moodle ([#25](https://github.com/dyzlek/SAE501/issues/25)).

Ensuite, si le temps le permet : argent plus lisible ([#63](https://github.com/dyzlek/SAE501/issues/63)), bananes visibles sur la carte ([#64](https://github.com/dyzlek/SAE501/issues/64)), visuel « fusion possible » ([#57](https://github.com/dyzlek/SAE501/issues/57)), explication des touches ([#54](https://github.com/dyzlek/SAE501/issues/54)), texte du coffre ([#72](https://github.com/dyzlek/SAE501/issues/72)). Le détail est dans le [journal général](docs/journal/GENERAL.md).

Journaux : [général](docs/journal/GENERAL.md) · [Dylan](docs/journal/dylan.md) · [Maxens](docs/journal/maxens.md) · [Nicolas](docs/journal/nicolas.md)

## Structure
```
Unity/     le projet Unity unique (Unity 6000.6, URP)
  Assets/_Project/Scripts/   Core, Hub, Map, Player (Dylan) · Weapon (arc) · Runtime (coffres, bananier) · Editor
  Assets/_Project/Scenes/    Hub.unity (scène de départ) · Labyrinthe.unity (carte)
Blender/   cabane.py (le hub, source unique) et autres modèles
docs/      GDD.md · journal/ (GENERAL.md + un journal par personne, avec l'usage de l'IA) · captures/
prompts/   les prompts de chacun, reformulés et corrigés
```

## Lancer le projet
1. Installer Git LFS : `git lfs install` (une seule fois), puis cloner.
2. Unity Hub → *Add* → dossier `Unity/` (version **Unity 6000.6**).
3. Ouvrir `Assets/_Project/Scenes/Hub.unity` (on lance **toujours depuis le Hub**), choisir le mode dans **SAE → Mode de jeu** (VR ou PC), puis Play.
4. Build casque : Android (Quest 3), casque en mode développeur avec le débogage USB autorisé. _Étapes à compléter après le premier build._

## Règles d'équipe
- Jamais deux personnes sur la même scène : on travaille en **prefabs**, chacun a sa scène sandbox.
- Commits petits et fréquents, messages clairs ; on relit et on teste avant de pousser.
- Un test sous casque par jour, un build casque chaque vendredi.
- Tout usage de l'IA est noté dans son journal perso ([docs/journal/](docs/journal/GENERAL.md)), et les prompts dans [prompts/](prompts/).

## Workflow Git
```bash
git switch main && git pull
git switch -c feat/mon-sujet      # une branche par tâche
# ... travail, petits commits ...
git push -u origin feat/mon-sujet # puis Pull Request vers main sur GitHub
```
`main` reste toujours jouable. La PR est relue par un autre membre avant le merge.
