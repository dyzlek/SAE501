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

- **Fait : test d'un hub « sans bouger »** (d'après mon schéma) :
  - au hub, le joueur reste **au centre** et fait tout en tournant la tête et en prenant les objets de loin : plus de déplacement au hub (ZQSD coupé), on marche toujours sur la carte. C'est le point d'arrivée qui le dit (`SpawnPoint.canWalk`) ;
  - tout est rapproché en rond autour du joueur (cercle de 3,5 m au lieu de 5 m) : le plateau devant, JOUER à droite, LANCER et Vider à gauche, les deux bibliothèques sur les côtés, le coffre et le panneau d'amélioration derrière à gauche, le bananier, son panier et la caisse derrière à droite ;
  - la zone de chute des bananes est un long tapis jaune qui part du pied du bananier et descend vers le joueur : les bananes arrivent à portée ;
  - portée plus longue : 6 m pour prendre les bananes et pour ouvrir le coffre depuis le centre ;
  - **plateau plus grand** : 2,4 m au lieu de 1,6 m, pour bien reconnaître chaque singe posé ;
  - **plus de texte au-dessus des singes** (carte, plateau, main, coffre) : le modèle dit le type, l'aura dit la rareté.
  Scène `Jeu.unity` régénérée avec le menu **SAE → Générer le prototype** (sur ma branche). Testé en Play : on ne peut pas marcher au hub, le coffre s'ouvre de loin, les bananes tombent sur le tapis, aucune erreur.
- **Confort VR :** rester au centre sans se déplacer, c'est le plus confortable (aucune locomotion, donc aucun risque de nausée). En revanche, prendre « de loin » demandera un rayon (Ray Interactor, support 5) au casque, pas seulement les mains.

Mon schéma du hub (le joueur au centre, tout à portée du regard) :

![Schéma du hub : joueur au centre](../captures/maxens-schema-hub-centre.png)

**Captures** (bibliothèque avec les singes 3D et leur aura, et singe tenu en main) :

![Bibliothèque : singes 3D avec l'aura de leur rareté](../captures/maxens-bibliotheque-singes-3d.png)

![Singe tenu en main avec son aura, plateau du hub](../captures/maxens-singe-en-main-aura.webp)

- **À vérifier au casque :** les fps avec beaucoup d'auras allumées (≤ 80 particules par singe, seulement sur les cases possédées).
- **Attention (équipe) :** ça touche des scripts de Dylan (`Visuals`, `LibrarySlot`, `ChestReward`, `PlacementSurface`, `Board`, `TowerManager`, `PlayerController`, `PrototypeGenerator`) et sa scène `Jeu.unity`.
- **Rangé :** changements parasites d'Unity annulés (`Jeu.unity`, réglages URP, réécrits à l'ouverture du projet) ; le package MCP for Unity reste installé sur mon PC seulement (non commité).
- **Bloque : le hub « sans bouger » n'est pas adapté à la VR.** Depuis le centre, tout est à 2,5-4 m : les boutons (JOUER, LANCER, Vider, améliorations), les 56 cases de la bibliothèque et le plateau sont **hors de portée des mains**. Au casque, il faudrait viser de loin avec un rayon pour tout, ce qui ressemble à un jeu à la souris (« test de l'écran » raté) et va contre la règle « tout à portée de bras ». Les 56 cases (7 types × 8 raretés) prennent aussi trop de place pour tenir près du joueur.

  ![Hub vu depuis le centre : boutons et bibliothèques hors de portée](../captures/maxens-hub-pas-adapte-vr.png)

  **Pistes** (à décider en équipe) :
  1. **Réagencer, sans changer le jeu** : un établi en arc de cercle à portée de bras (~0,6 m) avec le plateau incliné au milieu et de vrais boutons à enfoncer sur le bord ; le coffre et le bananier derrière, en se retournant (snap turn). Limite : les 56 cases ne tiennent toujours pas à portée.
  2. **Changer la bibliothèque** : elle n'affiche plus 56 cases fixes. Une étagère tournante (carrousel) à portée de main, 7 compartiments (un par type) qu'on fait tourner à la main ; chaque compartiment montre seulement les raretés qu'on possède. Geste VR en plus (tourner), et beaucoup moins de place.
  3. **Inventaire sur la main** (comme le sac de Half-Life Alyx ou la ceinture de Job Simulator) : on ouvre un menu sur le poignet et on y attrape directement le singe. Le plus compact, mais c'est un vrai changement de fonctionnement et plus de travail.
  4. **Garder la disposition et viser au rayon** (Ray Interactor, support 5) : le moins de travail, mais le moins « VR ».

  **Mon avis (proposé par Claude) :** 1 + 2 ensemble. L'établi pour le plateau et les boutons, le carrousel pour les singes. Le rayon seulement pour ce qui reste loin (prendre une banane au sol, par exemple).
- **Fait : pistes 1 + 2 retenues et prototypées** (sur ma branche, scène régénérée) : **tout est à portée de main** autour du joueur, qui reste au centre :
  - **devant, l'établi** : le plateau incliné à 30°, le bord le plus proche à 0,45 m (1,4 m de côté) ; JOUER, LANCER et Vider sont des boutons sur des socles à hauteur de main (0,7 m du joueur) ; le tableau de la vague est plus loin, seulement à lire ;
  - **sur les côtés, deux étagères tournantes** (nouveau script `Hub/Carousel.cs`) : un meuble carré par côté, une face par type de singe, 8 cases par face ; la face avant est à 0,7 m. On clique sur la manivelle jaune du dessus pour faire un quart de tour (au casque : la tourner à la main) ;
  - **derrière** : le coffre et le panneau d'amélioration à gauche ; à droite le bananier, dont les bananes tombent maintenant **sur une table à hauteur de main** qui vient jusqu'au joueur (plus de ramassage au sol), avec le panier juste à côté ;
  - les textes du coffre (consigne, roulette, chances, prix) et des boutons sont réduits, sinon ils prenaient tout l'écran de près.
  Testé en Play : l'étagère tourne, les bananes se posent sur la table près du panier, le coffre s'ouvre depuis le centre, aucune erreur.

  ![Hub à portée de main : établi, étagères tournantes, table des bananes](../captures/maxens-hub-a-portee-de-main.png)

  **Reste à faire pour la VR :** de vrais gestes au casque (enfoncer les boutons, tourner la manivelle, prendre un singe ou une banane à la main avec XR Grab), et vérifier les tailles au casque.
- **Annulé :** après l'avoir essayé, **ça faisait surchargé** (tout entassé autour du joueur, on ne voyait plus rien clairement), donc je reviens à la version d'avant (le hub « sans bouger » sur un grand cercle, avec la bibliothèque courbe). L'établi, les étagères tournantes et la table des bananes sont retirés (`Carousel.cs` supprimé, générateur et scène `Jeu.unity` remis comme avant). Le problème VR noté plus haut reste donc ouvert.

### IA
| Outil | Pour quoi | Gardé / jeté |
|---|---|---|
| Claude (Code) | Retour à la version d'avant (hub « sans bouger » sur un grand cercle) : générateur et scène remis, `Carousel.cs` supprimé | Gardé |
| Claude (Code) | Prototype des pistes 1 + 2 : établi à portée de main (plateau, boutons sur socles), deux étagères tournantes (`Carousel`), table des bananes à hauteur de main, textes du coffre réduits ; scène régénérée et testée en Play | Jeté (trop surchargé, annulé après essai) |
| Claude (Code) | Analyse du problème VR du hub « sans bouger » (portée des boutons et de la bibliothèque) et 4 pistes de solution, capture ajoutée | À décider en équipe |
| Claude (Cowork) | Tronc du bananier rallongé ×3 avec un script Blender, export FBX | Gardé (intégré sur `feat/prototype-maxens`) |
| Claude (Code) | Test du hub « sans bouger » d'après mon schéma : joueur fixé au centre, tout rapproché en rond, plateau agrandi, zone de chute vers le joueur, portée 6 m, plus de textes au-dessus des singes ; scène régénérée et testée en Play | À tester au casque |
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
