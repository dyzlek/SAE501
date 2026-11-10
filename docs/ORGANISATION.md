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

S1 et S2 sont **fermés** (10 nov.) : tout ce qui restait a été fait, coupé ou reporté en S3.
Une issue sans jalon = une idée pas encore validée dans le GDD (souvent `prio: POURRAIT` ou `hors périmètre`), ou une idée abandonnée.

## Trois niveaux : objectif, chantier, tâche
On ne met pas tout au même niveau : un objectif de la semaine n'est pas une petite correction.
- **Objectif de la semaine = le jalon** (S1, S2, S3) : on le suit dans [Milestones](https://github.com/dyzlek/SAE501/milestones), avec son %.
- **Chantier = une issue « parente »** (label `chantier`) qui regroupe les tâches liées en **sous-issues** et affiche sa progression :
  [#85 Jouable du début à la fin](https://github.com/dyzlek/SAE501/issues/85) · [#86 Tutoriel et 2 premières minutes](https://github.com/dyzlek/SAE501/issues/86) · [#87 Confort et VR](https://github.com/dyzlek/SAE501/issues/87) · [#88 Visuel et DA](https://github.com/dyzlek/SAE501/issues/88) · [#89 Économie et coffre](https://github.com/dyzlek/SAE501/issues/89) · [#90 Gestion et rendu](https://github.com/dyzlek/SAE501/issues/90).
- **Tâche = une issue concrète**, rangée sous son chantier (sur l'issue : *Relationships → Add parent*). C'est elle qu'on déplace sur le tableau. Toutes les tâches depuis le 19 oct. sont rangées (le coop a son propre sous-chantier, #170, dans #85 ; toutes les optimisations sont dans #87).

Sur le tableau, le champ **Niveau** (Chantier / Tâche) sert à filtrer : vue des tâches avec le filtre `niveau:Tâche`, vue des chantiers avec `niveau:Chantier`.

## Les labels
- **Priorité = périmètre du GDD :** `prio: DOIT`, `prio: DEVRAIT`, `prio: POURRAIT`, `hors périmètre` (NE FERA PAS).
- **Type :** `type: feature`, `bug`, `type: art`, `type: opti` (performances, détaillées dans [opti.md](opti.md)), `documentation`, `type: test`, `type: gestion`, `vr-confort`.
- Les labels par défaut de GitHub, jamais utilisés, ont été supprimés (10 nov.).
- **Zone du jeu :** `zone: hub`, `zone: carte`, `zone: arc`, `zone: singes`, `zone: bananes`, `zone: coffre`, `zone: ballons`, `zone: tuto`.

## Le cycle d'une tâche
1. **Créer l'issue** (modèle « Tâche » ou « Bug ») : **Quoi / Qui / Pourquoi / C'est fini quand** (cases à cocher), un label `prio:`, un label `type:`/`zone:`, le jalon, son chantier parent ; sur le tableau, la **Taille** (S < 2 h, M demi-journée, L 1 jour +) et les dates **Début / Fin**. Nouvelle mécanique → l'écrire d'abord dans le GDD.
2. **Commencer** : `git pull` sur `main`, branche `feat/…`, `fix/…`, `docs/…` ou `art/…` ; passer la carte en **En cours**.
3. **Finir** : PR vers `main` avec `Closes #N` dans la description (en anglais : GitHub ne reconnaît pas `Ferme #N`, l'issue resterait ouverte) (le modèle de PR a la check-list) ; carte en **En relecture**.
4. **Relire** : un autre membre relit et teste ; merge, suppression de la branche ; l'issue se ferme et passe en **Fait**.
5. **Couper** : une tâche abandonnée n'est pas laissée ouverte. On la ferme en **« not planned »**, avec le label `hors périmètre` et une phrase qui dit pourquoi (ex. #132, #134, #135, coupées au gel des features de la S3).

## Les rituels
- **Lundi** (visio 1 h, Antoine) : on trie les issues de la semaine, chacun prend les siennes.
- **Chaque jour** : tableau à jour, test au casque, bilan dans `GENERAL.md`.
- **Vendredi** : build casque, bilan visio 30 min ; on ferme le jalon et on reporte ce qui reste (ou on le coupe).
