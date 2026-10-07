# Organisation du projet sur GitHub

_Comment on suit le projet (note de gestion de projet, N. Maurin). Tout se passe sur GitHub, dans le dépôt [dyzlek/SAE501](https://github.com/dyzlek/SAE501)._

## Les outils
| Outil | Lien | À quoi il sert |
|---|---|---|
| **Tableau Project** | [SAÉ 501 · Jeu VR](https://github.com/users/dyzlek/projects/2) | Kanban : **À faire → En cours → En relecture → Fait**, avec dates de début et de fin et taille |
| **Issues** | [Issues](https://github.com/dyzlek/SAE501/issues) | Une issue = une tâche ou un bug, avec un responsable |
| **Jalons** | [Milestones](https://github.com/dyzlek/SAE501/milestones) | Une par semaine, avec la date du rendu et le % d'avancement |
| **Pull Requests** | [PR](https://github.com/dyzlek/SAE501/pulls) | Chaque branche finit en PR relue par un autre membre |
| **Journaux** | [`docs/journal/`](journal/GENERAL.md) | Décisions, bilans du jour, usage de l'IA |

## Les jalons (calendrier de la SAÉ)
| Jalon | Dates | Objectif |
|---|---|---|
| **S1 · Prouver** | 5-9 oct. | Le geste marche au casque, le GDD tient. Ven. 9 oct. : GDD v1 + prototype gris jouable |
| **S2 · Construire** | 19-23 oct. | Jouable du début à la fin, même moche. Ven. 23 oct. : un autre groupe teste |
| **S3 · Finir** | 9-13 nov. | Gel des features le lundi ; tuto, confort, fluidité, build stable. Ven. 13 nov. : rendu + oral |

Une issue sans jalon = une idée pas encore validée dans le GDD (souvent `prio: POURRAIT` ou `hors périmètre`).

## Les labels
- **Priorité = périmètre du GDD :** `prio: DOIT`, `prio: DEVRAIT`, `prio: POURRAIT`, `hors périmètre` (NE FERA PAS).
- **Type :** `type: feature`, `bug`, `type: art`, `documentation`, `type: test`, `type: gestion`, `vr-confort`.
- **Zone du jeu :** `zone: hub`, `zone: carte`, `zone: arc`, `zone: singes`, `zone: bananes`, `zone: coffre`, `zone: ballons`, `zone: tuto`.

## Le cycle d'une tâche
1. **Créer l'issue** (modèle « Tâche » ou « Bug ») : un label `prio:`, un label `type:`/`zone:`, le jalon, un responsable. Nouvelle mécanique → l'écrire d'abord dans le GDD.
2. **Commencer** : `git pull` sur `main`, branche `feat/…`, `fix/…`, `docs/…` ou `art/…` ; passer la carte en **En cours**.
3. **Finir** : PR vers `main` avec `Ferme #N` dans la description (le modèle de PR a la check-list) ; carte en **En relecture**.
4. **Relire** : un autre membre relit et teste ; merge, suppression de la branche ; l'issue se ferme et passe en **Fait**.

## Les rituels
- **Lundi** (visio 1 h, Antoine) : on trie les issues de la semaine, chacun prend les siennes.
- **Chaque jour** : tableau à jour, test au casque, bilan dans `GENERAL.md`.
- **Vendredi** : build casque, bilan visio 30 min ; on ferme le jalon et on reporte ce qui reste (ou on le coupe).
