# Journal de bord — Maxens

_Entrée la plus récente en haut. Une entrée par jour travaillé. Toute aide de l'IA est notée ici (outil, pour quoi, gardé/jeté)._

## Mar. 6 oct. 2026
- **Fait :** nouvelle branche `feat/prototype-maxens`, créée depuis le `main` à jour (PR #7 incluse), pour faire mes tests : placer le bananier dans la scène, essayer des réglages, etc.
- **Fait :** tous mes modèles 3D ajoutés au projet Unity sur cette branche, dans `Unity/Assets/_Project/Art/<Nom>/` (`FBX/` + `Textures/`) : Ballons (normal, blindé, cœur), BFB, MOAB, Boomerang, Canon, Colle, Glace, Pat Fusty (FBX + `Pat_Fusty.glb`), Quincy (niv. 3, 7, 10, 20 + flèche), Singe de base (+ fléchette), Sniper, Tireur (+ punaise).
- **Fait :** le bananier passe à la version **grand tronc** (`Bananier.fbx` remplacé, mêmes noms d'objets, donc les scripts et la scène marchent toujours). La branche locale `art/bananier-grand-tronc` n'est plus utile.
- **Pas mis dans Unity :** les `.blend` (Unity essaierait de les ouvrir avec Blender), les dossiers `Source/` et `Ancien/`, les images d'aperçu et les scripts du Tireur (à reprendre à part si besoin).
- **À faire :** ouvrir le projet dans Unity pour qu'il crée les `.meta` (réglages d'import), vérifier les modèles riggés, puis committer les `.meta`.
- **Bloque :** —

### IA
| Outil | Pour quoi | Gardé / jeté |
|---|---|---|
| Claude (Cowork) | Tronc du bananier rallongé ×3 avec un script Blender, export FBX | Gardé (intégré sur `feat/prototype-maxens`) |
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
