# SAÉ 5D.01 · Dispositifs interactifs — Jeu VR (BUT MMI 3, IUT Béziers)

> Résumé du support de lancement du 5 oct. 2026 (A. Chollet). À relire en début de session.

## La commande
Livrer un **jeu vidéo VR court, fini et jouable sous casque** pour le **vendredi 13 novembre 2026** (pas de délai).
- **Unity + C#**, casques VR de l'IUT, thème libre, groupes de 3-4, 3 semaines (14 jours de prod).
- Ambition : un jeu présentable aux **Pégases** (catégorie Meilleur jeu étudiant, cérémonie en mars à Paris).

## Les deux questions de l'oral final
1. **Pourquoi en VR ?** Le regard, la présence/l'échelle, le corps dans l'espace, les mains.
   *Test de l'écran* : si le jeu marche aussi bien à la manette devant un écran, ce n'est pas un jeu VR.
2. **Pourquoi c'est un bon jeu ?** Il se prend en main sans doc, s'appuie sur des codes connus, hérite de la culture JV.

## Mot d'ordre : simple, c'est bien
- OUI : une mécanique centrale, 1-2 niveaux + une fin, assets simples au style assumé, tutoriel intégré.
- NON : monde ouvert, 10 armes / arbre de compétences, scénario à embranchements, multijoueur en ligne.
- Fini et court > ambitieux et à moitié fait. Soigner les **2 premières minutes**.

## Méthode VR-first (dans l'ordre)
1. **Geste** : l'action physique qui fait le jeu (saisir, lancer, viser, esquiver, se pencher, empiler…).
2. **Boucle** (toutes les 30 s) : « Le joueur [geste] pour [objectif], mais [obstacle], et il gagne [récompense]. »
   Si ce n'est pas fun en cubes gris, les textures ne sauveront rien.
3. **Codes** : « c'est X, mais avec Y ». Références : Beat Saber, I Expect You To Die, Superhot VR, Job Simulator, Moss, Keep Talking. Affordances (poignée = saisir, bouton = enfoncer, brillant = ramasser, rouge = danger).
4. **Confort** (non négociable) : téléportation, snap turn, **≥ 72 fps**, UI dans le décor à 1-2 m, tout à portée de bras, **1 unité Unity = 1 m**. À éviter : caméra bougée à la place du joueur, accélérations/secousses/horizon penché, texte collé au visage, ramasser au sol en boucle, sessions longues.
5. **Apprentissage** : le niveau 1 est le tutoriel ; l'objet s'éclaire quand la main approche ; feedback son/image/vibration à chaque action ; une consigne à la fois dans le décor. *Test du silence* : donner le casque, ne rien dire, noter les blocages, corriger le jeu.
6. **Périmètre** : DOIT / DEVRAIT / POURRAIT / NE FERA PAS (écrit dans le GDD). Greybox d'abord. On coupe en semaine 2.
- **En continu : tester sous casque chaque jour**, un build casque chaque vendredi, faire tester par un autre groupe, réserver les casques sur Moodle.

## Calendrier
| Semaine | Objectif | Jalon |
|---|---|---|
| S1 · 5-9 oct. | **Prouver** : le geste marche au casque, le GDD tient | Ven. 9 oct. : GDD v1 + prototype gris jouable (bilan 15h30-16h30) |
| S2 · 19-23 oct. | **Construire** : jouable du début à la fin, même moche | Ven. 23 oct. : un autre groupe teste le jeu |
| S3 · 9-13 nov. | **Finir** : plus de nouvelle feature dès lundi ; tuto, confort, fluidité, build stable (mer. 11 férié) | Ven. 13 nov. : rendu + oral |

Semaine type : **lundi** cadrage visio 1 h (Antoine) · **mar-jeu** production (salle A124, créneaux Prose) · **vendredi** bilan visio 30 min (« qu'avez-vous produit ? »).

## Livrables
- **GDD** ([docs/GDD.md](docs/GDD.md)) — v1 le 9 oct. (2 pages), final le 13 nov. Contenu : pitch 1 phrase, geste + boucle, univers/ambiance, type + références, mécaniques/patterns, pourquoi la VR, périmètre dedans/dehors, choix de confort.
- **Projet Unity sur Git + build casque + captures/vidéo à plat** — 13 nov.
- **Oral** — 13 nov. après-midi, Zoom, 45 min/groupe : présentation, tuto de prise en main, démo à plat, volet technique, pourquoi la VR.
- **Suivi de projet** — en continu : outil de suivi, répartition, journal ([docs/JOURNAL.md](docs/JOURNAL.md)). Format précisé par Nicolas Maurin.

## Évaluation (une note, cinq regards)
- Production & gameplay + Démo/expérience VR — **Antoine Chollet** (game design, visio)
- Gestion de projet — **Nicolas Maurin**
- Qualité technique (code, archi, stabilité, interactions VR, savoir expliquer) — **Davide Di Pierro**
- Assets 3D & optimisation (pertinence, complexité, fluidité) — **Ilyasse Lojdi**

## IA : autorisée, transparente, comprise
- Tout usage d'IA (dont Claude) est **noté dans [docs/JOURNAL.md](docs/JOURNAL.md)** : outil, pour quoi, gardé/jeté.
- Le code rendu doit pouvoir être **expliqué à l'oral** : quand Claude écrit du code, il l'explique et reste simple/lisible.
- Réflexes : relire et tester avant de committer, commits petits, garder la main sur l'architecture.

## Règles de travail pour Claude dans ce dépôt
- Projet Unity dans `Unity/` ; docs dans `docs/`. `.gitignore` Unity à la racine.
- **Jamais deux personnes sur la même scène** : privilégier les prefabs, une scène de test par personne (`Assets/_Project/Scenes/Sandbox/<Prénom>`).
- Ne pas proposer de feature hors périmètre sans le signaler ; en S3, aucune nouvelle feature.
- Vérifier toute proposition contre les règles de confort et le test de l'écran.
- Après chaque tâche, ajouter une entrée IA dans `docs/JOURNAL.md`.
