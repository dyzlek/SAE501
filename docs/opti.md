# Journal des optimisations

Le jeu doit tenir **72 images par seconde sur le Meta Quest** (règle de confort de la SAÉ, issue [#35](https://github.com/dyzlek/SAE501/issues/35)). Ce fichier garde une trace de **chaque modification faite pour les performances**. C'est utile pour l'oral (volet technique, Davide Di Pierro) et pour la note « assets 3D et optimisation » (Ilyasse Lojdi).

## Comment noter une optimisation
Une entrée par modification, **la plus récente en haut**, avec toujours les mêmes parties :

1. **Le problème** : ce qui coûtait cher, et comment on l'a vu (fps, Profiler, Frame Debugger, intuition).
2. **Ce qui a été fait** : le changement, les fichiers, la branche ou la PR.
3. **Avant / après** : des chiffres mesurés de la même façon (même scène, même endroit, même vague). Sans mesure, écrire « à mesurer » et ne pas conclure : pas de ligne de tableau vide avec des « ? », on ajoute la ligne quand le chiffre existe.
4. **Est-ce que ça a vraiment marché ?** Oui, non ou partiellement, avec le gain réel, et ce qu'on a perdu en visuel ou en jeu.
5. **Suggestions** : ce qu'on pourrait faire ensuite sur le même sujet.

### Comment mesurer (toujours pareil)
- **Au casque :** le compteur de fps au poignet gauche (`Player/FpsCounter.cs`, vert à partir de 72), dans un build de développement. On note les fps au même endroit, en regardant dans la même direction.
- **Dans l'éditeur :** *Window → Analysis → Profiler* (CPU et Rendering : *Batches*, *SetPass calls*, *Triangles*) et *Frame Debugger* (le détail des appels de dessin). Les chiffres de l'éditeur ne sont pas ceux du casque, mais ils montrent si on va dans le bon sens.
- **Endroits de test :** (1) au hub, face à la bibliothèque ; (2) sur la carte, au point d'arrivée, pendant une vague.

---

## Sol de la cabane et de la bananeraie : une dalle texturée au lieu de centaines de planches
**Mar. 10 nov. 2026** · Dylan (avec Claude) · branche `feat/opti-tapis-lustre` · issue #195

**Le problème.** Le plancher était fait de **planches en 3D, une par une** (chacune un pavé biseauté) : **9 064 triangles** dans la cabane et **14 036** dans la bananeraie, pour un sol plat. On voyait même l'herbe entre deux planches (capture de Dylan).

**Ce qui a été fait** (`Blender/cabane.py`, `build_floor` et nouvelle texture `tex_floor`) : le sol est **une seule dalle plate (12 triangles)**. Les planches sont **dessinées dans la texture** `Plancher_Sol` (JPEG 1024 + carte de normales) : 8 planches de 19 cm, chacune avec sa teinte et ses veines, des joints en bout de planche, des fentes sombres. La texture se répète tous les 1,52 m. Le script tire toujours les mêmes nombres au hasard qu'avant, pour que le reste du décor (herbe, bougies…) ne bouge pas : vérifié, seuls les sols changent dans `Cabane.glb` et `Bananeraie.glb`.

**Avant / après.** Triangles : cabane 106 789 → 97 737 (dont le lustre), bananeraie 24 720 → 10 696. Fps à mesurer au casque.

**Est-ce que ça a vraiment marché ?** À mesurer. Rendu vérifié en image dans Blender, **pas testé dans Unity ni au casque**. Perte visuelle : plus de relief réel entre les planches (seulement la carte de normales), le motif se répète tous les 1,52 m.

**Suggestions.** Même méthode possible pour d'autres surfaces faites de pièces (allée de la bananeraie, murs en rondins vus de loin).

## Cabane : tapis opaque et lustre en « donut » de bois
**Mar. 10 nov. 2026** · Dylan (avec Claude) · branche `feat/opti-tapis-lustre` · issue #195

**Le problème.** Le hub lague près du tapis et du lustre (ressenti de Dylan, pas mesuré). Les deux viennent du modèle généré par `Blender/cabane.py` (`Cabane.glb`) :
- **Tapis** : un disque de 3 m de diamètre (~7 m² de sol) en **transparence découpée** (glTF « MASK », pour les franges). Sur le GPU du Quest, un pixel découpé empêche d'éliminer tôt ce qui est caché derrière, et le MSAA 4x le rend plus cher. Sa texture était un **PNG 1024 avec alpha (1,7 Mo)**, importée sans compression par glTFast (≈ 4 Mo en mémoire avec les mipmaps).
- **Lustre** : 2 436 triangles, **4 matériaux** (bois, bout de rondin, fer, cire) ; la jante était faite de 16 rondins avec chacun ses deux bouts.

**Ce qui a été fait** (`Blender/cabane.py`, puis `Cabane.glb` régénéré avec Blender 5.2 en mode batch ; les autres .glb régénérés ont été remis comme avant) :
- Tapis **opaque** : plus de franges ni d'alpha, le tissu va jusqu'au bord du disque ; texture **JPEG 512 sans alpha (114 Ko)**.
- Lustre : la jante devient **un seul tore de bois** (6 × 24 faces) et tout le bois prend le matériau Poutre (plus de bouts de rondin) : **3 matériaux au lieu de 4**, **1 636 triangles au lieu de 2 436**. Bougies, flammes et chaînes inchangées.
- Rien d'autre ne change dans la cabane (vérifié : mêmes objets, mêmes maillages et matériaux, sauf ces deux-là). Fichier : 11,8 → 10,1 Mo.

**Avant / après.** À mesurer au casque (compteur au poignet), au centre de la cabane, en regardant le tapis puis le lustre. Profiler : *Batches* en baisse d'au moins 1 sous le lustre.

**Est-ce que ça a vraiment marché ?** À mesurer. Rendu vérifié en image (tapis, lustre), **pas testé dans Unity ni au casque**. Perte visuelle : plus de franges au tapis (bord doré net) ; le lustre garde sa forme de roue.

**Suggestions.** La lumière du lustre (`PrototypeGenerator.cs`, `AddLight(t, 8f, 3.2f)`) est une lumière ponctuelle en temps réel de **8 m de portée** : elle éclaire toute la cabane et une partie de dehors, donc presque chaque pixel du hub fait un calcul de lumière en plus. C'est probablement le plus gros coût « du lustre ». Pistes : portée 5 m (la cabane fait 3,4 m de rayon), ou lumière cuite (*Baked*) puisqu'elle ne bouge pas (on perd le vacillement). Pas fait ici : il faut regénérer ou modifier la scène Hub.

## Corrections après relecture de la PR #188 : couleurs animées, miniatures des boss
**Mar. 10 nov. 2026** · Dylan (avec Claude) · branche `feat/opti-fusion` · PR #188

**Le problème.** Relecture de la PR avant le merge :
- `ColorTint` crée en jeu un matériau partagé **par couleur** (pour le SRP Batcher). Mais une dizaine de scripts animent une couleur à chaque image (cadre du tutoriel qui pulse, panneau d'argent, boutons refusés, roulette, nuages de l'orage) : chaque image créait un nouveau matériau, gardé à vie dans le cache. Environ 72 matériaux par seconde pendant tout le tutoriel : la mémoire monte et des à-coups arrivent, l'inverse du but. `Set` recréait aussi un tableau de matériaux à chaque appel, même sans changement de couleur.
- Les miniatures du plateau (instancing) dessinaient les boss et les dirigeables en **boules étirées** : on ne les reconnaissait plus. Perte visuelle non notée jusqu'ici.

**Ce qui a été fait.**
- `ColorTint` : deux méthodes. `Set(couleur)` pour une couleur fixe (ballons, décor) → matériau partagé comme avant. `Animate(couleur)` pour une couleur qui change à chaque image → `MaterialPropertyBlock`, aucun matériau créé. Les 11 appels qui animent passent par `Animate`. Si la couleur n'a pas changé, `Set` et `Animate` ne font plus rien (plus de tableau recréé).
- Plateau (`Hub/Board.cs`) : boss et dirigeables gardent une vraie copie de leur modèle (peu nombreux, sans ombre ni collider ; cachée quand le boss retourne au pool, réutilisée ensuite). Les ballons normaux restent en instancing.
- Hors perf, même relecture : projectiles qui suivaient un ballon recyclé (numéro de vie `Balloon.Life`), 4 sources de son réservées aux sons 2D (annonces, voix, jingles) pour que les pops ne les coupent plus.

**Avant / après.** À mesurer (Profiler, *Memory* → nombre de matériaux, pendant le tutoriel) : avant, +72 matériaux par seconde ; après, attendu stable. Le plateau coûte quelques appels de dessin de plus pendant un boss.

**Est-ce que ça a vraiment marché ?** À mesurer. Compilé, pas testé au casque.

**Suggestions.** Une couleur animée perd le regroupement du SRP Batcher : ça ne concerne que quelques boutons et panneaux, sans effet visible attendu.

## Modèles : Mesh LOD automatique, textures des singes, pluie au casque
**Lun. 9 nov. 2026** · Dylan (avec Claude) · branche `feat/opti-fusion` · issues #176, #180, #185

**Le problème.** Triangles comptés dans les FBX (script, 9 nov.) : les singes sont légers (700 à 1 100 triangles), mais **un ballon en a 1 032** (le cœur 1 680) et il y en a des centaines par vague ; le bananier en a 5 474. Les textures des singes (2048 px) sont **intégrées dans les FBX** : c'est pour ça qu'ils pèsent 1,5 à 1,9 Mo, et les PNG rangés à côté ne servent à rien (aucun matériau ne les utilise). La pluie de l'orage : jusqu'à 1 000 gouttes transparentes.

**Ce qui a été fait.**
- **Mesh LOD** (Unity 6) coché à l'import (`generateMeshLods: 1`) des modèles sans squelette : ballons normal, blindé et cœur, MOAB, BFB, bananier, Canon, Tireur. Unity calcule tout seul des versions simplifiées dans le même maillage, et la carte graphique prend la plus légère qui suffit selon la taille à l'écran. Pas de réexport Blender, réversible en décochant la case.
- Nouveau menu **SAE → Optimiser les textures (Quest)** (`Editor/TextureMenu.cs`) : sort les textures intégrées des FBX (comme le bouton *Extract Textures*), puis limite sur Android les textures du dossier Art à 1024 px (512 pour les projectiles), avec mipmaps. **Lancé le 10 nov.** (mode batch) : 30 FBX vidés de leurs textures intégrées (18 PNG extraits, 7,9 Mo, dans `<Sujet>/Textures/Extraites`), 39 textures limitées sur Android. À vérifier dans Unity : les singes et ballons gardent leurs couleurs (pas de modèle blanc ou rose). Poids du build : à mesurer.
- Pluie au casque : 40 % des gouttes (400 au plus), traits 1,5 fois plus longs (`Map/Weather.cs`).
- **Pas fait, volontairement :**
  - fusionner les 2-3 maillages de chaque singe (Blender) : réexporter des modèles riggés sans pouvoir tester dans Unity, à 4 jours du rendu, c'est trop risqué (noms d'os, échelle, animations) ;
  - #185 (synchro réseau) : un ballon normal ne tourne jamais, donc NGO n'envoie déjà pas sa rotation (seuil). Couper la rotation ne gagnerait rien. Baisser le *Tick Rate* toucherait aussi les joueurs. À revoir seulement si une partie coop rame (mesure d'abord) ;
  - #180 MSAA : gardé en 4x (le crénelage se voit beaucoup en VR) ; textes 3D et occlusion : à mesurer d'abord.

**Avant / après.**

| Mesure | Avant | Après |
|---|---|---|
| Triangles d'un ballon normal (FBX) | 1 032 | 1 032 au plus près, moins de loin (Mesh LOD) |

Le reste *à mesurer* : triangles et fps du mode admin (200 singes, grosse vague) ; mémoire des textures (*Memory Profiler*) avant / après le menu.

**Est-ce que ça a vraiment marché ?** *Pas encore mesuré.* À regarder au casque : un ballon de loin ne doit pas paraître cabossé. Si c'est le cas, monter *Mesh LOD Threshold* (Quality) ou décocher la case sur ce modèle.

**Suggestions.** Si les *skinned meshes* restent le problème après mesure : fusionner les maillages des singes dans Blender (`Ctrl+J`, même armature), un singe à la fois, en vérifiant chacun dans Unity.

---

## Pools (sons, projectiles, ondes, traits), préchargement, Update coupés
**Lun. 9 nov. 2026** · Dylan (avec Claude) · branche `feat/opti-fusion` · issues #178, #181, #182

**Le problème.** Pendant une vague, le jeu créait et détruisait des objets sans arrêt : un `GameObject` + une `AudioSource` par son, un objet + son modèle 3D par projectile, un objet par onde de choc, un système de particules neuf (40 étincelles) par explosion, un `LineRenderer` par tir du Sniper. Ça charge le ramasse-miettes et donne des à-coups. Les sons étaient chargés au premier usage (`Resources.LoadAll` + lecture de tout le fichier pour sauter le silence) : le premier « pop » de la partie faisait un à-coup. Et `ColorTint.Update` tournait à chaque image sur chaque objet teinté (des centaines), alors qu'il ne sert qu'à l'arc-en-ciel.

**Ce qui a été fait.**
- `Core/Sounds.cs` : 24 sources créées une fois (objet « Sons », gardé entre les scènes), la première libre est prise, sinon la plus ancienne. `Preload()` charge tous les sons au lancement du jeu (sauf la musique).
- `Map/Projectile.cs` : un pool par type de singe, remise à zéro (position, rotation, âge, cible) à chaque lancer ; projectiles sans ombre.
- `Weapon/Shockwave.cs` : pool d'ondes ; **un seul** système d'étincelles par scène, déplacé puis `Emit(25)` (au lieu de 40 étincelles dans un système neuf).
- `Map/Tower.cs` (dans la fusion) : pool des traits du Sniper.
- `Map/WaveSpawner.cs` : au chargement de la carte, 30 ballons (dont 10 avec leurs taches de camo) et 12 projectiles par type de singe posé sont créés d'avance et rangés.
- `Core/ColorTint.cs` : le composant se coupe tout seul s'il n'est pas en arc-en-ciel (`Set` marche toujours). `Core/Aura.cs` : l'aura ne bat et ne tourne que si une caméra la voit.

**Avant / après.** *À mesurer* dans le Profiler (build de dev au casque, vague chargée avec le panneau admin) : *GC Alloc* par image, pics, temps de `Update.ScriptRunBehaviourUpdate`, et le premier pop de la partie.

**Est-ce que ça a vraiment marché ?** *Pas encore mesuré.* Rien ne change à l'écran, sauf des gerbes d'étincelles un peu moins fournies (25 au lieu de 40). Risque connu des pools : un objet recyclé qui garde un état de sa vie d'avant. Tout est remis à zéro à la sortie du pool ; à vérifier en jeu (ballons camo recyclés, boss qui se divise, projectiles qui partent du bon singe).

**Suggestions.**
- Si un son se coupe en pleine grosse vague (24 sources occupées), monter `Voices` à 32.
- `Billboard`, `FlameFlicker`, `Spinner` gardent leur `Update` : ils sont posés sur peu d'objets.
- Préchauffage des shaders (`GraphicsStateCollection`) pas fait : les ballons et projectiles créés d'avance ne sont pas dessinés. À faire seulement si le Profiler montre encore un pic au premier boss.

---

## Fusion de `feat/optim-plateau` avec le coop et les ballons camo
**Lun. 9 nov. 2026** · Dylan (avec Claude) · branche `feat/opti-fusion` · issues #174, #153, #177, #179

**Le problème.** Les optimisations de #153 (mesure : 41 fps, CPU limitant, 231 skinned meshes, 626 k triangles, 141 SetPass) étaient dans `feat/optim-plateau`, jamais fusionnée, partie d'un `main` plus ancien que le coop et les ballons camouflés. Rien n'arrivait au casque. Sur `main`, chaque ballon avait en plus un `BoxCollider` **sans Rigidbody** : pour la physique, un collider « statique » qu'on déplace à chaque image.

**Ce qui a été fait.** Fusion dans `feat/opti-fusion`, conflits résolus : ciblage sans LINQ **avec** le filtre camo ; explosion du Canon sans LINQ ; pool de ballons compatible avec le coop (le double réseau est retiré quand le ballon est rangé, et un ballon recyclé est re-partagé à sa 1re image) et avec le camo (un ballon recyclé n'est plus camouflé) ; les copies d'un invité ne vont jamais dans le pool. Ce que la branche apporte : Rigidbody kinematic sur les ballons, matériaux partagés (`ColorTint.Shared`), ombres à 15 m, décor static + occlusion, singes figés sur le plateau (`MonkeyView.Freeze`), singes immobiles loin du joueur (`TowerAnimator`), 2 os par sommet, mode admin (#151).
- **#179** : les 6 sphères de Unity des taches du camo (~4 500 triangles, 6 objets) sont remplacées par **un seul maillage** de 6 petites boules (480 triangles), partagé par tous les ballons camouflés, avec un matériau partagé.
- **#177** : les modèles des ballons, les taches du camo, les projectiles et les traits du Sniper ne projettent plus d'ombre (l'aura n'en avait déjà plus).
- Corrigé en passant : dans la branche, le corps du `if` de `Tutorial.TryPress` était dans le commentaire (le bouton de test était refusé).

**Avant / après.** Mesure de #153 avant la fusion : 41 fps (mode admin, 200 singes). *Après : à mesurer* au même endroit, avec les mêmes réglages du panneau admin.

**Est-ce que ça a vraiment marché ?** *Pas encore mesuré.* À vérifier en jeu : une vague normale, une vague camo, un boss qui se divise, une partie en coop (hôte + invité).

**Suggestions.** Les lumières du hub (#177, étape 3 : *Per Object Limit* 4 → 2 ou lumières par sommet) n'ont pas été touchées : c'est un changement d'image, à décider au casque.

---

## Réglages de rendu du casque : bon asset URP, sans post-traitement, fovéation, distance de vue
**Lun. 9 nov. 2026** · Dylan (avec Claude) · branche `feat/opti-fusion` · issues #186, #187, #175, #184, #183

**Le problème.**
- **#186.** Le profil de build *Meta Quest* prend la qualité « Meta Quest (Build Profile) », qui pointait sur **l'asset URP par défaut du paquet**, pas sur `Mobile_RPAsset`. Au casque : rendu à 100 % au lieu de 80 %, ombres jusqu'à 50 m, carte d'ombres 2048. Tous nos réglages mobiles étaient sans doute ignorés dans le build. Vu en lisant `QualitySettings.asset` (le GUID ne correspond à aucun fichier de `Assets/`).
- **#187.** La caméra du joueur avait le post-traitement (WhiteBalance, ColorAdjustments, Tonemapping ; seul le Bloom était coupé). Sur Quest, ça ajoute une texture intermédiaire et une passe plein écran par œil, et ça empêche la fovéation.
- **#175.** La fovéation de Meta était cochée dans OpenXR (Android), mais aucun code ne réglait son niveau : elle restait à 0.
- **#184.** La caméra voyait jusqu'à 400 m (1 000 m dans le prefab XRI) pour des niveaux de quelques dizaines de mètres.
- **#183.** Traces de pile calculées pour chaque `Log` et `Warning` ; un `Debug.Log` dans une boucle du coffre.

**Ce qui a été fait.**
- `ProjectSettings/QualitySettings.asset` : la qualité « Meta Quest (Build Profile) » utilise maintenant `Mobile_RPAsset`. `FpsCounter` écrit dans la console (build de dev, logcat) la qualité et l'asset vraiment utilisés.
- Nouveau `Player/HeadsetRendering.cs` (ajouté par `PlayerRig`) : distance de vue **250 m** partout (couvre les nuages de l'orage, qui arrivent de 150 m, et la fin du brouillard à 230 m) ; au casque seulement : **post-traitement coupé** sur la caméra, **fovéation High dynamique** (`OVRManager.foveatedRenderingLevel`, le casque ne l'active que si l'image risque d'être en retard). Le générateur utilise la même distance.
- On a gardé la fovéation de Meta (déjà cochée) plutôt que celle de Unity : pas de changement dans les réglages OpenXR, et pas de permission de suivi des yeux ajoutée au build.
- Traces de pile de `Log` et `Warning` à *None* (profil Meta Quest et Player) ; le `Debug.Log` du coffre seulement dans l'éditeur. *Development Build* était déjà décoché dans le profil.

**Avant / après.** *À mesurer* au casque, aux deux endroits de test, build normal (pas de dev pour les chiffres finaux, mais le compteur ne s'affiche qu'en dev : mesurer d'abord en dev, puis vérifier à l'œil).

**Est-ce que ça a vraiment marché ?** *Pas encore mesuré.* Ce qu'on perd en image : un peu de netteté (80 % de résolution, bords flous de la fovéation), et l'étalonnage des couleurs du post-traitement au casque. À regarder au casque : si l'image est trop froide ou trop terne, réchauffer la lumière du soleil et l'ambiance dans le générateur. Si le texte des panneaux bave sur les bords, passer la fovéation à `Medium`.

**Suggestions.**
- Les Graphics Jobs « Legacy » activés dans les réglages locaux non commités de Dylan : à tester au casque (garder seulement si les fps montent).
- *Managed Stripping Level* Medium : pas fait (risque de casser XR et Netcode, qui utilisent la réflexion, sans pouvoir tester à 4 jours du rendu).

---

## Limite de 60 singes posés
**Mer. 21 oct. 2026** · Dylan (avec Claude) · branche `feat/opti-aura`

**Le problème.** On pouvait poser autant de singes qu'on voulait, et chaque singe coûte à chaque image : son modèle animé, son aura, la recherche d'une cible et ses projectiles. Il existe en plus **en double**, parce qu'il est visible sur la carte et en miniature sur le plateau du hub. En fin de partie, le nombre de singes pouvait grimper sans limite et faire tomber les fps.

**Ce qui a été fait.** `Placement.MaxPlaced = 60` dans `Core/Placement.cs`. Plateau plein, le fantôme devient rouge et un texte « PLATEAU PLEIN (60) · Fusionne tes singes ! » s'affiche au-dessus (`Core/PlacementSurface.cs`). On peut toujours **fusionner**, puisque ça ne fait pas un singe de plus, et reprendre un singe posé. La limite devient un choix de jeu : il vaut mieux fusionner que d'empiler.

**Avant / après.** *À mesurer.* Il faudra noter les fps au casque, sur la carte pendant une vague, avec 60 singes posés (en bac à sable). C'est le pire cas maintenant garanti.

| Mesure | Avant | Après |
|---|---|---|
| Singes posés au maximum | illimité | 60 |

**Est-ce que ça a vraiment marché ?** *Pas encore mesuré.* Ça ne rend pas le jeu plus rapide avec peu de singes. Ça fixe un **plafond** : le pire cas est connu et peut être testé.

**Suggestions.**
- Mesurer avec 60 singes. Si on est sous 72 fps, baisser la limite (40 ?) ou alléger chaque singe : animation coupée hors du champ de vision, cible cherchée moins souvent (toutes les 0,1 s au lieu de chaque image).
- ~~Afficher le compteur « 42 / 60 »~~ : fait. Une plaque à part était trop petite (retour de Dylan), alors c'est maintenant une ligne « Singes : 42 / 60 » sur le tableau de la vague, au hub et sur la carte, en orange dès 50 et en rouge « PLEIN ».

---

## Aura des singes : un maillage au lieu des particules
**Mer. 21 oct. 2026** · Dylan (avec Claude) · branche `feat/opti-aura`

**Le problème.** Chaque singe, dans la bibliothèque, sur le plateau ou sur la carte, a une aura de la couleur de sa rareté. C'était le prefab `Prefabs/Aura.prefab`, avec **deux systèmes de particules** :

| Système | Particules max | Nouvelles / s | Prewarm |
|---|---|---|---|
| Flammes | 80 | 70 | oui |
| Lueur | 6 | 4 | oui |

La bibliothèque montre jusqu'à 56 singes (7 types × 8 raretés), plus ceux du plateau et de la carte, plus les bananes dorées. Cela fait **plus de 100 systèmes de particules et plusieurs milliers de particules**, recalculés à chaque image et dessinés en transparence, les unes sur les autres (*overdraw*). C'est exactement ce que le GPU mobile du Quest supporte mal.

**Ce qui a été fait.** `Core/Aura.cs` est réécrit, avec la même interface (`Aura.Add`) : le reste du code n'a pas changé. Une aura est maintenant **un seul maillage fixe**, façon transformation « super saiyan » :
- un **tube de flamme** autour du singe, plus étroit en haut, au bord dentelé, coloré en bas et transparent en haut (dégradé dans les couleurs des sommets) ;
- un **cœur** plus serré et presque blanc, l'éclat « super saiyan » (v2, après le premier test) ;
- un **disque lumineux au sol**, sous les pieds ;
- un petit **battement** de hauteur et une **rotation lente**, qui font bouger les langues de flamme (deux lignes de calcul par image, pas de simulation) ;
- le maillage est **partagé** entre toutes les auras de même couleur, et il utilise le matériau des lignes du jeu (aucun nouveau shader).

Une aura = 120 triangles, 1 objet, 1 appel de dessin, 0 particule (avant : jusqu'à 86 particules et 2 systèmes).

**Avant / après.** *À mesurer.* Au casque : fps face à la bibliothèque, avant (`main` du 21 oct.) et après (cette branche). Dans le Profiler : *Batches* et temps CPU de `ParticleSystem.Update`.

| Mesure | Avant | Après |
|---|---|---|
| Particules vivantes (hub) | ~ 56 × 80 | 0 |

**Premier test dans Unity (Dylan, 21 oct.).** Seule la colonne arc-en-ciel avait son aura. C'était un bug de cache : les maillages sont gardés dans un dictionnaire statique, et sans rechargement du domaine (*Enter Play Mode* rapide), ceux de la partie d'avant étaient détruits mais toujours renvoyés. L'arc-en-ciel était reconstruit, parce que son test vérifiait le maillage détruit. Corrigé : le cache est vidé au lancement, et un maillage détruit est refait. Dylan trouvait aussi l'aura trop simple (un cône plat) : v2 avec des langues de flamme irrégulières, un cœur blanc et une rotation lente, toujours en un seul maillage.

**Est-ce que ça a vraiment marché ?** *Pas encore mesuré.* On s'attend à un vrai gain au hub. À vérifier aussi : que l'aura reste lisible (la rareté se reconnaît toujours à sa couleur), et que l'arc-en-ciel, en dégradé de teintes autour du tube, se distingue bien.

**Suggestions.**
- Si les *Batches* restent hauts : activer le **GPU Instancing** sur le matériau, ou fusionner les auras de la bibliothèque (elles ne bougent pas).
- Dans la bibliothèque, les auras pourraient ne battre que pour le singe que la main survole.
- L'ancien prefab `Prefabs/Aura.prefab` ne sert plus. On peut le supprimer, avec le champ `auraPrefab` de `MonkeyVisuals`, une fois l'aura validée au casque.
- Prochaines pistes à mesurer : la pluie de l'orage (`Map/Weather.cs`, jusqu'à 1 000 gouttes), les ombres de la lumière du soleil, et les textes 3D (un TextMesh = un appel de dessin).
