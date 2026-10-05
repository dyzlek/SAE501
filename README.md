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

## Journal commun du jour : lun. 5 oct. 2026 (semaine 1 : PROUVER)
- **Lancement de la SAÉ** : jeu VR sous Unity, à rendre le 13 novembre. Idée retenue : un **tower defense VR dans l'univers de Bloons** (voir le [GDD](docs/GDD.md)).
- **Organisation** : dépôt GitHub, `main` stable et une branche par tâche, un journal général et un journal par personne, les prompts de chacun dans `prompts/`.
- **Trois prototypes au clavier et à la souris**, sans casque disponible aujourd'hui :
  - Dylan : le hub, le plateau et la carte ;
  - Nicolas : l'ouverture des coffres ;
  - Maxens : le bananier.
- **Tests croisés** : chacun a testé et commenté le travail des autres (voir les journaux perso).
- **Intégration** : un seul projet Unity en **6000.6**. Les trois prototypes sont réunis sur la branche `feat/integration`, et le hub les dispose **en cercle autour du joueur** :
  - le plateau devant ;
  - les deux meubles de la bibliothèque à gauche et à droite ;
  - le bananier et son panier derrière ;
  - le coffre derrière à droite.

  Les bananes rapportent l'argent qui permet d'ouvrir le coffre.
- **Prochaines étapes** :
  - au coffre, faire sortir un singe de la rareté gagnée, qui rejoint la bibliothèque ;
  - rendre le plateau plus grand et retirer les textes au-dessus des cubes (retours de Maxens) ;
  - tester le geste au casque ;
  - GDD v1 pour vendredi 9 octobre.

Détails : [journal général](docs/journal/GENERAL.md) · [Dylan](docs/journal/dylan.md) · [Maxens](docs/journal/maxens.md) · [Nicolas](docs/journal/nicolas.md)

## Structure
```
Unity/     le projet Unity unique (Unity 6000.6, URP)
  Assets/_Project/Scripts/   Core, Hub, Map, Player (Dylan) · Runtime (coffres de Nicolas, bananier de Maxens) · Editor
  Assets/_Project/Scenes/    Jeu.unity (hub + carte, générée par le menu SAE → Générer le prototype)
docs/      GDD.md · journal/ (GENERAL.md + un journal par personne, avec l'usage de l'IA) · captures/
prompts/   les prompts de chacun, reformulés et corrigés
```

## Lancer le projet
1. Installer Git LFS : `git lfs install` (une seule fois), puis cloner.
2. Unity Hub → *Add* → dossier `Unity/` (version **Unity 6000.6**).
3. Ouvrir `Assets/_Project/Scenes/Jeu.unity` (au besoin : menu **SAE → Générer le prototype**), puis Play.
4. Build casque : _à préciser (plateforme, étapes)_.

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
