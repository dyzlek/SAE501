# [Nom du jeu] — SAÉ 5D.01 · Jeu VR

> **Pitch :** _une phrase._

BUT MMI 3, IUT de Béziers. Jeu en réalité virtuelle réalisé sous Unity (C#) en 3 semaines. Rendu le 13 novembre 2026.

## Équipe
| Membre | Rôle |
|---|---|
| Dylan | |
| Maxens | |
| Nicolas | |

## Structure
```
Unity/   projet Unity (ouvrir ce dossier dans Unity Hub)
docs/    GDD.md (Game Design Document), JOURNAL.md (journal de bord + usage de l'IA)
```

## Lancer le projet
1. Installer Git LFS : `git lfs install` (une seule fois), puis cloner.
2. Unity Hub → *Add* → dossier `Unity/` (version Unity : _à préciser_).
3. Build casque : _à préciser (plateforme, étapes)_.

## Règles d'équipe
- Jamais deux personnes sur la même scène : on travaille en **prefabs**, chacun a sa scène sandbox.
- Commits petits et fréquents, messages clairs ; on relit et on teste avant de pousser.
- Un test sous casque par jour, un build casque chaque vendredi.
- Tout usage de l'IA est noté dans [docs/JOURNAL.md](docs/JOURNAL.md).

## Workflow Git
```bash
git switch main && git pull
git switch -c feat/mon-sujet      # une branche par tâche
# ... travail, petits commits ...
git push -u origin feat/mon-sujet # puis Pull Request vers main sur GitHub
```
`main` reste toujours jouable. La PR est relue par un autre membre avant le merge.
