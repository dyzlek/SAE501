# Journal de bord — Maxens

_Entrée la plus récente en haut. Une entrée par jour travaillé. Toute aide de l'IA est notée ici (outil, pour quoi, gardé/jeté)._

## Mar. 6 oct. 2026
- **Fait :** nouvelle branche `feat/prototype-maxens`, créée depuis le `main` à jour (PR #7 incluse), pour faire mes tests : placer le bananier dans la scène, essayer des réglages, etc.
- **Fait :** tous mes modèles 3D ajoutés au projet Unity sur cette branche, dans `Unity/Assets/_Project/Art/<Nom>/` (`FBX/` + `Textures/`) : Ballons (normal, blindé, cœur), BFB, MOAB, Boomerang, Canon, Colle, Glace, Pat Fusty (FBX + `Pat_Fusty.glb`), Quincy (niv. 3, 7, 10, 20 + flèche), Singe de base (+ fléchette), Sniper, Tireur (+ punaise).
- **Fait :** le bananier passe à la version **grand tronc** (`Bananier.fbx` remplacé, mêmes noms d'objets, donc les scripts et la scène marchent toujours). La branche locale `art/bananier-grand-tronc` n'est plus utile.
- **Pas mis dans Unity :** les `.blend` (Unity essaierait de les ouvrir avec Blender), les dossiers `Source/` et `Ancien/`, les images d'aperçu et les scripts du Tireur (à reprendre à part si besoin).
- **Fait :** `.meta` des modèles créés par Unity et commités.
- **Fait :** les **vrais singes dans la bibliothèque** (et partout où un singe s'affiche : sortie du coffre, singe en main, aperçu de pose, carte) à la place des cubes de couleur :
  - un type = un modèle : Classique → Singe de base, Boomerang, Canon, Sniper, Punaise → Tireur, Glace, Colle. Les modèles sont rangés dans `Resources/MonkeyVisuals.asset`, rempli par le menu **SAE → Brancher les modèles des singes** ;
  - la rareté n'est plus la couleur du cube mais une **aura « à la Dragon Ball »** : des flammes de la couleur de la rareté qui montent autour du singe et se resserrent en pointe, plus une lueur douce (arc-en-ciel = flammes de toutes les couleurs). C'est un **prefab** (`Prefabs/Aura.prefab`, deux systèmes de particules « Flammes » et « Lueur ») qu'on règle dans l'Inspector ; le script `Core/Aura.cs` ne fait que le colorer et l'adapter à la taille du singe ;
  - case vide de la bibliothèque : le singe est sombre, plus petit et sans aura ; le nombre possédé est écrit devant lui ;
- **Testé** dans l'éditeur (Play) : bibliothèque remplie, singe en main, singe posé sur la carte, aucune erreur dans la console. Les singes riggés étaient exportés de dos : ils sont retournés de 180° (réglage par modèle dans `MonkeyVisuals`).
- **Corrigé après mon test :**
  - le singe posé sur le **plateau du hub** apparaissait en cube : la miniature est maintenant le vrai singe en petit, avec son aura ;
  - l'aura du **Canon** et du **Tireur** (objets larges et bas) était cachée par le modèle : les flammes partent maintenant du bord de l'objet, quelle que soit sa forme ;
  - le **Canon** était tourné vers le mur : il est retourné vers le joueur ;
  - hors Play, la bibliothèque repassait en **cubes** : la scène gardait les cubes enregistrés, et les singes 3D n'étaient construits qu'au lancement. Nouveau menu **SAE → Mettre à jour les singes de la bibliothèque**, lancé puis scène `Jeu.unity` enregistrée **sur ma branche** (accord : la scène est à Dylan, à voir avec lui au moment de fusionner). Hors Play on voit les modèles sans l'aura : les particules enregistrées dans la scène la faisaient passer de 1,3 à 7,6 Mo, donc l'aura n'est créée qu'en jeu. Les modèles sont enregistrés comme liens vers les FBX (sinon 9 Mo) ; la scène fait 1,5 Mo.

- **Relu avec les cours de D. Di Pierro** (les PDF des supports 2, 3, 4 et le TP 1, plus `docs/GUIDE_CODE.md`), et code repris pour suivre leurs exemples :
  - l'aura était construite entièrement en code, avec beaucoup de nombres en dur : elle devient un **prefab** réglé dans l'Inspector, comme le pistolet et sa balle du support 4 ;
  - plus de recherche de composants à chaque frame : l'aperçu de pose teintait le modèle en cherchant ses renderers à chaque image, ils sont maintenant gardés en mémoire (guide, section 4) ;
  - les derniers nombres en dur ont un nom (`WidthWeight`, `MinRadius`) ;
  - corrigé au passage : sur la carte et le plateau, l'aura apparaissait d'abord blanche (le prefab démarrait avant de recevoir sa couleur).
  Retesté en Play (bibliothèque, singe en main, carte, plateau du hub) : aucune erreur dans la console.

**Captures** (bibliothèque avec les singes 3D et leur aura, et singe tenu en main) :

![Bibliothèque : singes 3D avec l'aura de leur rareté](../captures/maxens-bibliotheque-singes-3d.png)

![Singe tenu en main avec son aura, plateau du hub](../captures/maxens-singe-en-main-aura.webp)

- **À vérifier au casque :** les fps avec beaucoup d'auras allumées (≤ 80 particules par singe, seulement sur les cases possédées).
- **Attention (équipe) :** ça touche des scripts de Dylan (`Visuals`, `LibrarySlot`, `ChestReward`, `PlacementSurface`, `Board`, `PrototypeGenerator`).
- **Rangé :** changements parasites d'Unity annulés (`Jeu.unity`, réglages URP, réécrits à l'ouverture du projet) ; le package MCP for Unity reste installé sur mon PC seulement (non commité).
- **Bloque :** —

### IA
| Outil | Pour quoi | Gardé / jeté |
|---|---|---|
| Claude (Cowork) | Tronc du bananier rallongé ×3 avec un script Blender, export FBX | Gardé (intégré sur `feat/prototype-maxens`) |
| Claude (Code) | Lecture des PDF du cours (installation de `pypdf`), relecture du code avec le guide : aura en prefab réglable dans l'Inspector, renderers gardés en mémoire, constantes nommées, aura blanche corrigée | À tester au casque |
| Claude (Code) | Corrections après mon test : singe 3D aussi en miniature sur le plateau, aura élargie pour le Canon et le Tireur, Canon retourné, menu pour mettre les singes 3D dans la scène hors Play (scène `Jeu.unity` enregistrée, 1,5 Mo) ; testé en Play et hors Play, aucune erreur | À tester au casque |
| Claude (Code) | Modèles 3D des singes dans la bibliothèque (et coffre, main, carte) + aura « Dragon Ball » en particules à la couleur de la rareté, outil d'éditeur pour brancher les modèles, testé en Play via Unity MCP | À tester au casque |
| Claude (Code) | Copie de tous mes modèles (FBX, GLB, textures) dans `_Project/Art/`, bananier grand tronc à la place de l'ancien | Gardé |
| Claude (Code) | Mise à jour de `main`, création et push de la branche de test `feat/prototype-maxens`, journal et prompt | Gardé |

## Lun. 5 oct. 2026
- **Fait :** prototype du **bananier** (branche `feat/proto-bananier`), testé à la souris dans l'éditeur :
  - le bananier fait tomber des bananes à intervalle régulier, avec un petit « boing » de l'arbre ;
  - 3 stats améliorables : fréquence, pourriture, valeur (niveaux et prix déjà calculés, pas encore payés) ;
  - les bananes brunissent puis pourrissent : une banane pourrie ne vaut plus rien et disparaît ;
  - le panier compte l'argent des bananes déposées dedans ;
  - le récolteur automatique (le futur singe récolteur) est volontairement limité : 1 banane toutes les 6 s, et il garde 40 % ;
  - menu `Bananes > Installer / Créer une scène de test` qui monte tout seul le système.
- **Corrigé dans la journée :**
  - on ne voyait pas les bananes tomber, elles atterrissaient **dans le socle** du bananier. Elles tombent maintenant en dehors du bac, du côté du panier ;
  - au début, les bananes **n'étaient pas portées** : un clic et elles disparaissaient direct dans le panier. Maintenant, clic maintenu = banane en main, on la glisse au-dessus du panier et on relâche (même logique `Prise()` / `Lachee()` qu'en VR).
- **Bloque :** pas encore de packages XR dans ce projet (XR Interaction Toolkit + OpenXR), donc pas encore testé au casque. Point de confort à garder en tête : le support classe « ramasser des objets au sol en boucle » dans les choses à éviter. Pistes : attraper en l'air, panier et bananes sur une table, ou ramassage à distance.
- **Demain :** installer XRI + OpenXR, tester la prise de banane au casque.

**Captures du prototype bananier** (souris, éditeur) :

Le bananier et le panier au lancement.

![Bananier et panier au départ](../captures/proto-bananier-depart.png)

Les bananes tombent à côté du bac, du côté du panier, et on les prend pour les mettre dedans.

![Bananes tombées à côté du panier](../captures/proto-bananier-bananes-au-sol.png)

**Rangement** (comme le reste du projet) : `Unity/Assets/_Project/` → `Art/Bananier` (FBX, textures, matériaux), `Prefabs/Banane.prefab`, `Scripts/Runtime` et `Scripts/Editor`, scène de test dans `Scenes/Sandbox/Maxens/BananierSandbox.unity`. Projet en Unity 6000.3.8f1, URP.

**Mes retours sur les prototypes des autres** _(mon avis, 18h17)_

- **Dylan (hub, plateau, bibliothèque)** :
  - Je n'aime pas trop les textes au-dessus de chaque cube. Ce n'est pas essentiel et ça rend l'ensemble moins lisible (peut-être temporaire ?).
  - Le plateau devrait être vraiment plus grand : c'est l'élément principal.
  - Il faut trouver une autre idée pour les bibliothèques : elles ne sont pas très lisibles et prennent trop de place. Je n'ai pas encore d'idée, on en parle demain.
  - Sinon, tout est bon, j'aime bien la mécanique.
- **Nicolas (coffres)** :
  - J'aime bien, mais je ne sais pas si je ne préférerais pas l'idée de départ : un bruit, le singe qui sort du coffre et qui tourne, avec l'aura qui apparaît.
  - Si on garde la version actuelle, c'est pas mal. Il faudrait juste que le coffre se tourne automatiquement vers le joueur.
- **Moi (bananier)** : rien à redire.

### IA
| Outil | Pour quoi | Gardé / jeté |
|---|---|---|
| Claude (à confirmer) | Scripts du bananier : Bananier (production + stats améliorables), Banane (chute, pourriture), Panier (compte l'argent), Récolteur, installeur automatique dans l'éditeur | Gardé |
| Claude (Cowork) | Vérification du projet avant push (compilation, références des prefabs et de la scène, .meta) et liste de ce qui manque pour la VR | Gardé |
| Claude (Cowork) | Test souris : banane tenue en main (clic maintenu) au lieu de disparaître au clic | Gardé |
| Claude (Cowork) | Rangement dans `_Project/` comme le reste du dépôt, branche, journal et captures pour le push | Gardé |
