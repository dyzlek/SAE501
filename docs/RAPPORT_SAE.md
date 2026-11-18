# Rapport de SAÉ — Bloons VR

_SAÉ 5D.01 · Développer pour le web ou concevoir un dispositif interactif · BUT MMI 3, IUT de Béziers_
_Dylan, Maxens et Nicolas · rendu le 16 novembre 2026 pour l'évaluation du 18 novembre_

**Le jeu en une phrase :** un tower defense en réalité virtuelle dans l'univers de Bloons TD : le joueur est Quincy, il tire à l'arc sur les ballons, pose ses singes à la main sur une maquette de la carte et fait tourner sa bananeraie pour payer le tout.

- **Moteur :** Unity 6000.6 (URP), XR Interaction Toolkit 3.6, OpenXR ; casque Meta Quest, plus un mode PC clavier-souris.
- **Dépôt :** github.com/dyzlek/SAE501 (code, GDD, journaux, suivi des tâches). Build PC : Release v1.0-pc.
- **Chiffres :** 161 scripts C# (environ 20 000 lignes), 3 scripts Blender, 144 issues, 91 Pull Requests fusionnées, environ 410 commits en 3 semaines.

## 1. Division des responsabilités

Le 5 octobre, chacun a fait son propre prototype au clavier et à la souris, puis on les a réunis dans un seul projet Unity. Chacun a gardé ensuite « sa » mécanique. Le travail est suivi sur GitHub : une issue par tâche (avec la personne responsable), un tableau À faire / En cours / En relecture / Fait, un jalon par semaine, une branche et une Pull Request par tâche. Chacun tient aussi un journal (`docs/journal/`).

| | Dylan | Maxens | Nicolas |
|---|---|---|---|
| **Mécaniques** | Hub et maquette (poser et fusionner les singes), carte et chemin, vagues (`WaveBook`), économie et inventaire, tutoriel (première version), menu principal, sauvegarde | Bananeraie (bananiers, pourriture, récolteurs, comptoir), coffre et sa roulette, arc de Quincy et mains riggées, boss qui se divisent, bananes dorées, tutoriel complet (23 étapes), refonte de la roulette du casino | Prototype des coffres (raretés, probabilités), refonte de l'arc (perforation, améliorations : perforante, transperçante, tir triple, explosive), roulette de casino (1re version), armoire à trophées |
| **VR** | Rig XR, téléportation et rotation par crans, boutons qu'on enfonce avec la main, premier build casque | Arc et corde au casque, mains animées par les boutons, vibrations par action | — |
| **Graphismes** | Cabane, bananeraie et paysage générés par script Blender (`cabane.py`), lumière, météo des boss | Modèles 3D des singes, ballons, dirigeables (MOAB, BFB), bananier ; coffre (`coffre.py`) et roue (`roulette.py`) en script Blender | — |
| **Son** | — | Tous les sons et musiques, lecteur `Sounds.cs` | — |
| **Réseau** | Coop par Internet (Unity Relay + Netcode for GameObjects), classement en ligne (PHP + MySQL) | Coop : bananeraie, coffre, fléchettes et singe tenu partagés | — |
| **Performances** | Optimisations pour le Quest (19 points notés dans `docs/opti.md`), réglages du build | Sons (pas plus de 3 éclatements par image) | — |
| **Gestion de projet** | Tableau GitHub, issues, intégration et fusion des branches, builds, GDD | GDD (sections de ses mécaniques), tests au casque | Analyse critique du jeu (S1), tests |

**Ce qui a moins bien marché :** Dylan a fait une grande partie de l'intégration et des fusions, ce qui a créé un goulot d'étranglement. Les PR n'ont pas toujours été relues par un autre membre avant d'être fusionnées, surtout en fin de projet. La part de Nicolas est plus réduite : il a surtout travaillé en semaine 1 et 2, et ses branches ont été fusionnées par Dylan.

## 2. Ressources prises en ligne

**Outils et bibliothèques (gratuits) :**
- **Unity** et ses paquets : XR Interaction Toolkit 3.6.1 (dont les « Starter Assets » : rig XR, interacteurs de téléportation), OpenXR, Meta XR SDK, Input System, URP, TextMesh Pro, glTFast (import des `.glb`), Netcode for GameObjects et Unity Multiplayer Services (Relay) pour le coop.
- **Blender** (scripts Python `bpy`) pour tous nos modèles faits maison.

**Polices :** Bangers et Oswald Bold (Google Fonts, licence libre SIL OFL 1.1, fichier de licence dans le projet).

**Sons et musiques :**
- La plupart des bruitages et les trois musiques (Party Time, Primal One, Onslaught) viennent des jeux **Bloons TD** de Ninja Kiwi (les musiques étaient au format `.brstm` et ont été converties en `.wav`). On l'assume parce que le jeu reprend l'univers de Bloons, mais ces sons ne sont pas libres de droits.
- Le son de téléportation vient de **Pixabay** (« teleport whoosh », auteur humordome, licence Pixabay).
- Certains sons sont **fabriqués par le code** (tic des roulettes, cloches des vagues de boss, tonnerre, ding du tutoriel) parce qu'on n'en trouvait pas de bons en ligne.

**Modèles 3D :** pas d'asset téléchargé dans le jeu final. Un coffre téléchargé (`chest_cartoon_animations.glb`) a servi au début, puis a été remplacé par notre propre coffre parce qu'il ne collait pas au style de la cabane (le fichier est encore dans le dépôt et peut être supprimé).

**Univers et références :** l'univers, les personnages (Quincy, les singes, les ballons, les dirigeables) et la carte Monkey Lane appartiennent à Ninja Kiwi (Bloons TD 6). Le jeu est un fan-game d'école, non commercial et sans logo officiel. Autres références de game design : Rush Royale (fusion, coffre), Job Simulator, In Death et The Lab (tir à l'arc), Moss (maquette), How to Fish (roulette).

**Intelligence artificielle :** nous avons utilisé **Claude Code** (Anthropic) pendant tout le projet pour écrire et corriger du code, les scripts Blender, les documents et le suivi GitHub. Chaque usage est noté dans le journal de la personne (outil, pour quoi, gardé ou jeté), et chaque demande est gardée dans `prompts/`. Nous avons testé et relu ce qui a été gardé, et nous savons expliquer le code rendu.

## 3. Contribution technique

**Architecture**
- **Scènes générées par le code.** Le menu `SAE > Générer le prototype` (`PrototypeGenerator`) reconstruit les scènes Hub et Labyrinthe à partir des scripts et des modèles. On n'édite pas la scène à la main, donc il n'y a jamais de conflit de scène entre nous dans Git.
- **Deux scènes chargées en même temps** (cabane et carte), avec **un seul joueur** qu'on déplace d'une scène à l'autre. La vague continue quand on retourne à la cabane. Au début, il y avait un joueur par scène, mais l'XR Interaction Toolkit gère mal deux rigs VR (rotation, prise et téléportation buguaient).
- **Données au même endroit :** `MonkeyData` (stats et prix des singes), `WaveBook` (les 100 vagues), `BowUpgrades` (l'arc), `Economy` et `GameState` (bananes, inventaire, singes posés). L'équilibrage se règle en quelques minutes.
- **Une seule interface pour tous les boutons** (`IPressable`) : on les enfonce avec la main (`HandPress`), on les vise (`RayPress`) ou on clique (`DesktopPlayer`). Le tutoriel filtre ces appuis (`Tutorial.TryPress`) pour n'autoriser que l'action demandée.
- **Tutoriel** : une machine à états (`Tutorial`), des tableaux qui s'abonnent à ses événements, une flèche dorée qui montre la cible.

**Interactions VR**
- Arc physique : on attrape la corde, on tire, on vise (courbe de visée), on lâche. Perforation par couche plutôt que des points de dégâts.
- Singes saisis dans une bibliothèque et posés sur la maquette (aperçu vert ou rouge sous la main), fusion de deux singes identiques.
- Confort : téléportation (zones limitées), rotation par crans de 45°, voile noir à chaque téléportation, textes posés dans le décor (ardoises, pancartes), vibration différente selon l'action.

**Réseau**
- **Coop par Internet** jusqu'à 4 joueurs avec Netcode for GameObjects et Unity Relay (WebSocket sécurisé sur le port 443, pour passer le Wi-Fi de l'IUT). L'hôte fait tourner la partie, vérifie le mot de passe à l'entrée, et les invités lui envoient leurs actions (RPC).
- **Classement en ligne** : une petite API PHP + MySQL (`server/classement/`) sur l'espace web de l'IUT. Le classement local, sur le casque, marche aussi hors ligne.

**Optimisation pour le Quest** (objectif 72 images par seconde, détail dans `docs/opti.md`)
- Décor fusionné par couleur (`MeshBatch`) pour réduire les appels de dessin, matériaux partagés et instancing, pool de ballons au lieu d'en créer et détruire, occlusion calculée, objets statiques marqués.
- Pas de post-traitement au casque, fovéation dynamique, réglages graphiques mobiles appliqués au build.
- Modèles allégés : par exemple les palmiers de la carte passent de 14 520 à 7 392 triangles. Compteur d'images par seconde au poignet dans les builds de test.

## 4. Améliorations possibles

**Pour finir ce qui existe**
- **Tester le coop à deux casques** : il a surtout été testé sur un seul PC. La synchronisation de la bananeraie et du coffre n'est pas assez vérifiée.
- **Mesurer les fps au casque** sur les vagues hautes (des centaines de ballons) et compléter `docs/opti.md` avec les chiffres réels.
- **Équilibrer les 100 vagues** avec de vrais joueurs (elles ont été durcies le dernier jour, sans test complet).
- **Faire plus de « tests du silence »** : donner le casque sans rien expliquer et corriger le tutoriel là où les joueurs bloquent.
- **Nettoyer le projet** : fichiers inutiles (`_Recovery/`, ancien coffre téléchargé, dossiers en double `XR 1` à `XR 6`).

**Pour aller plus loin**
- **Changer l'habillage** (nom, singes, ballons, sons) pour avoir un jeu 100 % original et pouvoir le présenter aux Pégases. La mécanique ne dépend pas de la marque Bloons.
- **Remplacer les sons de Bloons** par des sons libres de droits ou faits par nous.
- **Ajouter une 2e carte**, d'autres types de singes, et un écran de fin plus marquant.
- **Améliorer le travail d'équipe** : relire chaque PR par une autre personne, mieux répartir l'intégration et les fusions, et faire un build casque chaque vendredi comme prévu.
