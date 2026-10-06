# Journal de bord — Dylan

_Entrée la plus récente en haut. Une entrée par jour travaillé. Toute aide de l'IA est notée ici (outil, pour quoi, gardé/jeté)._

## Mar. 6 oct. 2026
- **Fait :**
  - remarque de départ : le palmier de Maxens est trop petit dans le hub, il faut l'agrandir (fait : ×1,6) ;
  - chantier A, **une seule économie avec de vrais coûts** (branche `feat/economie`), sans aucun affichage collé à l'écran, parce qu'en VR ça donne le vertige. Tout se lit dans le décor du hub :
    - une **caisse** en bois avec l'argent total, dont le chiffre défile et qui clignote vert ou rouge ;
    - des « +5 » / « −25 » qui flottent là où l'argent bouge ;
    - un **panneau d'amélioration** du bananier avec 3 gros boutons ;
    - le **prix du coffre**, affiché au-dessus de lui.
- **Bloque :** —
- **Demain :** _

**Captures de l'économie dans le hub**

![Hub avec la caisse, le panneau du bananier et le coffre](../captures/economie-hub-vue.webp)

![Hub vu du dessus](../captures/economie-hub-dessus.webp)

**Mon analyse critique** _(corrigée dans la foulée, à tester)_
1. **Le hub est trop petit.** On est trop serré, les éléments sont les uns devant les autres : c'est compliqué de circuler et d'utiliser les objets.
2. **Le prix et la qualité du coffre doivent dépendre des vagues vaincues.** Plus on bat de vagues, plus le coffre est cher, mais plus il est intéressant : il donne plus de singes et de meilleur niveau.
3. **Afficher les probabilités de drop** du coffre (chances par rareté), et les informations qui vont avec.
4. **Les bananes ne doivent plus traverser le bananier.** Aujourd'hui, elles peuvent passer à travers et rester cachées autour, à des endroits inaccessibles.

**Autres points vus sur les captures** _(notés par l'IA)_
- Les textes au-dessus des cubes de la bibliothèque se chevauchent encore (CA5, CA6…), et ceux des boutons du panneau BANANIER sont trop petits pour être lus.
- Le cube « Vider » et le pied du plateau gênent le passage devant le joueur.
- Les vies et la vague sont encore affichées à l'écran (prévu avec le chantier D).

**Mon schéma du hub en cercle**
Le hub était plus espacé, mais pas encore assez. J'ai dessiné une disposition où tout suit vraiment un cercle (rouge), avec le centre libre pour circuler :
- le **plateau** (carré marron) est reculé sur le cercle, devant le joueur, avec le bouton **JOUER** (orange) juste à côté ;
- les deux **bibliothèques** (vert) sont courbes et suivent le cercle sur les côtés ;
- le **bananier** (BANANE) est au fond, avec le panier (rond marron) et la **zone de chute des bananes** (jaune) devant lui ;
- le **coffre** (chest) est en haut à gauche, sur le cercle.

![Mon schéma du hub en cercle](../captures/schema-hub-cercle.png)

**Captures du hub en cercle** (base de ma deuxième analyse)

![Hub en cercle, vue joueur : vies et vague encore affichées en haut à gauche](../captures/hub-cercle-vue.webp)

![Hub en cercle vu du dessus](../captures/hub-cercle-dessus.webp)

**Ma deuxième analyse critique** _(corrigée dans la foulée, à tester après avoir régénéré la scène)_
1. **Un bouton pour lancer la vague à la main.** Le joueur décide quand il est prêt, au lieu que la vague parte toute seule.
2. **Perdre une vague ne fait pas tout recommencer.** On reprend au début de la vague perdue, pas à la vague 1.
3. **Un ou plusieurs boss à chaque vague** : des ballons plus gros que les autres.
4. **Les raretés élevées du coffre se débloquent plus tard**, par paliers de vagues (par exemple après la vague 20, etc.).
5. **Les singes blancs et arc-en-ciel ne sortent jamais du coffre.**
6. **Plus de vies ni de numéro de vague affichés à l'écran** : une interface collée au visage en VR donne la nausée. Ces informations doivent se lire dans le décor.

### IA
| Outil | Pour quoi | Gardé / jeté |
|---|---|---|
| Claude (Claude Code) | Économie unique (`Economy`) : bananes et vagues rapportent, coffre et améliorations coûtent ; `Wallet` et pont supprimés ; caisse dans le décor, « +/− » flottants, panneau d'amélioration du bananier (3 boutons qui s'enfoncent, vert si payable), prix du coffre qui augmente (25, +30 % par coffre) et affiché au-dessus ; argent retiré de l'affichage écran ; palmier ×1,6 ; coin « économie » réorganisé derrière le joueur | À tester |
| Claude (Claude Code) | Mise en forme de mon analyse critique du hub et ajout de mes captures (rien de corrigé pour l'instant, à ma demande) | Gardé |
| Claude (Claude Code) | Corrections de mon analyse critique : hub agrandi (cercle de 4 m, sol 12 × 12 m, éléments espacés, boutons hors du passage) ; coffre qui dépend des vagues vaincues (prix 25 + 20 par vague, raretés débloquées par vague, 1 singe de plus toutes les 3 vagues) ; panneau des chances à côté du coffre ; bananes remises hors du bac si elles s'y coincent ; plus de textes sur les cubes de la bibliothèque ; textes du panneau du bananier agrandis | À tester |
| Claude (Claude Code) | Hub refait d'après mon schéma : tout posé sur un cercle de 5 m (centre libre), plateau reculé sur le cercle devant avec JOUER/Vider à côté, bibliothèques courbes sur les côtés, bananier au fond avec une zone de chute des bananes devant lui, panier et caisse d'un côté, panneau d'amélioration et coffre de l'autre | À tester |
| Claude (Claude Code) | Mise en forme de ma deuxième analyse critique (vague lancée à la main, reprise à la vague perdue, boss à chaque vague, raretés par paliers, blanc et arc-en-ciel absents du coffre, vies et vague retirées de l'écran) et ajout de mes 2 captures du hub en cercle ; rien de corrigé pour l'instant | Gardé |
| Claude (Claude Code) | Corrections de ma deuxième analyse : bouton LANCER (hub et carte), la vague ne part plus toute seule ; vague perdue = on la recommence avec les vies du départ (singes et argent gardés) ; 1 boss par vague (+1 toutes les 5 vagues), gros ballon violet lent et solide ; coffre sans arc-en-ciel ni blanc (fusion seulement), raretés par paliers (Bleu 3, Violet 7, Jaune 12, Rouge 20 vagues) ; vies et vague retirées de l'écran, affichées sur un tableau au-dessus du plateau et sur la carte | Gardé (testé et validé par moi) |
| Claude (Claude Code) | Scène régénérée commitée, branche `feat/economie` poussée et fusionnée dans `main` par Pull Request (pas de remplacement forcé de main : `main` était déjà contenu dans la branche, rien n'est perdu) | Gardé |
| Claude (Claude Code) | Chantier B, vrai inventaire (branche `feat/inventaire`) : bibliothèque vide au départ, chaque case affiche le nombre de singes (grisée et plus petite si vide) ; prendre un singe le sort de l'inventaire, clic droit ou clic sur une case le range, « Vider » range tous les singes posés, la fusion consomme 2 singes pour en créer 1 ; le coffre donne des singes (type au hasard, rareté tirée) qui sortent avec une **aura de la couleur de leur rareté** puis volent jusqu'à leur case | Gardé (testé et validé par moi) |
| Claude (Claude Code) | Un singe Classique gris offert au départ ; chances de drop par **type** de singe au coffre : au début seulement des Classiques, puis les autres arrivent petit à petit (Boomerang vague 1, Punaise 2, Glace 3, Canon 5, Colle 7, Sniper 9), affichées sur le panneau des chances (agrandi) | Gardé (testé et validé par moi) |
| Claude (Claude Code) | Chantier D, vrai système de vagues : 10 vagues écrites à l'avance (groupes de ballons normaux, **rapides**, **blindés** qui prennent moitié moins de dégâts et ne se ralentissent pas, boss), **dirigeable rouge** à la vague 10, victoire puis mode infini ; tableau de la vague avec la composition de la prochaine vague. **Fiche des singes** dans le décor quand on vise un singe (bibliothèque, plateau, carte) : effet, dégâts, portée, cadence, cibles et gain de la fusion, plus le cercle de portée sur le plateau et la carte | Gardé (testé et validé par moi) |
| Claude (Claude Code) | La fiche du singe ne s'affiche plus toute seule : il faut viser le singe **en maintenant A** (clavier AZERTY), ou le bouton A de la manette droite en VR ; aide en bas de l'écran mise à jour | Gardé (testé et validé par moi) |
| Claude (Claude Code) | Scène régénérée commitée, branche `feat/inventaire` poussée et fusionnée dans `main` par Pull Request | Gardé |
| Claude (Claude Code) | Nouvelle branche `feat/vr-maxens` : le travail de Maxens (commit 1cc56b1 : singes 3D, aura en prefab) + ma VR. Seul conflit de code : la case de la bibliothèque, où le singe 3D de Maxens est maintenant celui qu'on saisit. Ajout d'un menu **SAE → Mode de jeu : VR / PC (clavier-souris)** : les deux joueurs sont dans la scène et le bon est activé au lancement ; en mode PC, ZQSD + souris, clic pour appuyer ou prendre/lâcher (singe posé là où on vise, banane dans le panier), A pour la fiche ; le casque (Android) est toujours en VR | À tester |
| Claude (Claude Code) | **Prendre de loin, jusqu'à 2 m** les singes et les bananes, en VR comme en mode PC : en VR, un filtre (`GrabReach`) sur le XR Grab empêche de les attraper au-delà de 2 m de la main (avant : jusqu'à 10 m), le rayon ne devient bleu qu'à portée ; en mode PC, le clic prend jusqu'à 2 m ; les boutons et le coffre restent utilisables de plus loin | À tester |
| Claude (Claude Code) | Correction d'après mes captures : le rayon devenait bleu et prenait le singe même en passant à côté (le Near-Far détecte avec un cône de 6°). `HandRay` passe maintenant la détection en rayon exact au démarrage : il faut pointer sur le singe pour interagir | À tester |
| Claude (Claude Code) | Le rayon exact ne survolait plus rien, même en pointant le singe (capture) : le tir de détection partait d'une origine lissée (stabilisation du caster des Starter Assets) différente du rayon dessiné. `HandRay` coupe la stabilisation et dessine le rayon depuis l'origine réelle du tir | À tester |
| Claude (Claude Code) | « De loin ça ne marche pas, je dois le coller » : le hub est un cercle de 5 m et les bibliothèques sont dessus, donc ma limite de 2 m les rendait inatteignables depuis le centre. Portée de prise passée à **6 m** (`GrabReach.Reach`, VR et PC) | À tester |
| Claude (Claude Code) | Aide pour le premier build casque (mode développeur, débogage USB autorisé, Build and Run sur « Default device ») ; scène régénérée et réglages du build commités, branche `feat/ajout-vr` poussée | Gardé |
| Claude (Claude Code) | Rayon toujours pas visible sur les manettes (d'après ma capture) : le rayon des Starter Assets est éteint et remplacé par le nôtre (`HandRay`), droit, qui part de chaque manette, blanc, bleu quand il vise quelque chose d'utilisable, caché quand la main tient un objet ; mots LANCER / JOUER remontés au-dessus des cubes | À tester |
| Claude (Claude Code) | Plus adapté à la VR : **les vies repartent à 20 à chaque vague** ; **rayon blanc toujours visible** au bout des manettes (3 m, il ne disparaît plus) pour viser les boutons, les bananes et les singes ; boutons et coffre activés en les visant + **gâchette** (ou grip) ; **singe posé de loin** là où pointe le rayon de la main qui le tient (rayon blanc jusqu'au plateau ou à la carte) ; lignes (rayon, cercle de portée, tirs) passées sur le shader compatible casque | À tester |
| Claude (Claude Code) | Correction d'après ma capture : les textes étaient décalés (sous les cubes, à côté du tableau). Le shader de texte était écrit pour l'ancien rendu de Unity : réécrit pour URP, qui donne la bonne caméra à chaque œil | À tester |
| Claude (Claude Code) | Textes réparés au casque : nouveau shader « SAE/Texte 3D » (celui de Unity ne gère pas l'affichage stéréo : texte dans un seul œil, visible à travers les murs) et textes qui restent droits face au joueur ; boutons, améliorations et coffre utilisables **de loin** (viser avec le rayon + grip), qui grossissent quand on les vise, avec vibration | À tester |
| Claude (Claude Code) | Correction : avec Quest Link, la tête et les manettes ne bougeaient pas, car le simulateur XR se lançait quand même et remplaçait le vrai casque. Le simulateur ne démarre plus que si aucun casque n'est branché (`SimulatorWhenNoHeadset`) | À tester |
| Claude (Claude Code) | Chantier E, la VR (branche `feat/ajout-vr`) : rig XR des Starter Assets (téléportation sur les deux sticks, rotation par crans, pas de déplacement continu) et simulateur XR lancé tout seul dans l'éditeur ; menu **SAE → Configurer la VR** (OpenXR + Meta Quest, Android IL2CPP/ARM64/API 32, couche Teleport) ; singes à saisir sur l'étagère (XR Grab), lâchés au-dessus du plateau = posés, sur un singe identique = fusionnés, ailleurs = rangés, avec l'aperçu vert/rouge et le halo ; boutons, coffre et améliorations enfoncés avec le bout du doigt, avec vibration ; bananes sur une **table à hauteur de main** et panier sur un socle ; fiche du singe en visant avec la manette droite + A ; contrôles clavier/souris retirés | À tester (compile, scène à régénérer, pas encore testé au casque) |
| Claude (Claude Code) | Ajout de mon schéma du hub en cercle (image + légende) dans ce journal, sur la nouvelle branche `feat/ajout-vr` | Gardé |
| Claude (Claude Code) | Lecture des 7 supports de cours de D. Di Pierro et synthèse dans `docs/GUIDE_CODE.md` (organisation, style C#, cycle de vie, événements, collisions, mise en place VR, saisie, UI en World Space, tests, perf, check-list avant commit) ; CLAUDE.md demande de le lire avant de coder | À relire par l'équipe |
| Claude (Claude Code) | Prise de loin des singes qui ne marchait pas : la boîte de collision de chaque case de la bibliothèque englobait le singe, le rayon exact s'arrêtait dessus. La boîte est coupée tant qu'un singe est posé (`LibrarySlot.UpdateCollider`) | À tester |
| Claude (Claude Code) | Le singe pris de loin vient jusqu'à la main au lieu de rester au bout du rayon (`farAttachMode = Near` dans `MonkeyToken.Create`) | À tester |
| Claude (Claude Code) | Le bouton « Vider » coûte 10 bananes (`ActionCube.ClearBoardPrice`, rien n'est payé si le plateau est vide ou s'il manque des bananes) ; plus de texte au-dessus des singes posés sur le plateau (`Board`) | À tester |
| Claude (Claude Code) | Prix écrit sur le bouton « Vider » (`ActionCube.Start`) ; en mode PC le singe tenu est décalé en bas à droite (`DesktopPlayer.holdOffset`) ; en VR il se cale directement dans la main (`useDynamicAttach = false`) | À tester |
| Claude (Claude Code) | Correction d'une erreur de compilation que j'avais introduite (retour à la ligne dans le texte du bouton Vider), qui empêchait de lancer le jeu | Gardé |
| Claude (Claude Code) | Singe récolteur (branche `feat/singe-recolteur`) : un singe classique qu'on achète au panneau « RÉCOLTEUR » (gratuit pour l'instant), qui marche jusqu'à une banane de la table, saute pour la prendre, la porte au-dessus de sa tête, marche au panier et la jette dedans ; 3 améliorations gratuites (vitesse, cadence, rendement) ; animations faites en code sur le squelette du modèle (marche, porter, lancer), car le FBX n'a aucune animation. Installé au lancement par `HarvesterSetup`, sans toucher la scène | À tester |
| Claude (Claude Code) | Retouches du récolteur : tête plus grosse (×1,4), fléchette retirée ; panier avancé vers le centre (−156°, 3,7 m), loin de la table et de la caisse (générateur + déplacement au lancement pour la scène actuelle) | À tester |
| Claude (Claude Code) | Récolteur plus drôle : beaucoup plus lent de base (0,25 m/s, jusqu'à 1,05) ; panier éloigné vers le plateau (−160°, 2 m du centre) ; 20 % de dunks (grand saut, banane écrasée dans le panier, tour sur lui-même bras levés), 25 % de lancers ratés (la banane tombe à côté, il secoue la tête et tape du pied, la ramasse et retente) | À tester |
| Claude (Claude Code) | Panier déplacé à côté du plateau des singes (+32°, après « Vider ») ; le récolteur va aussi chercher les bananes que le joueur a lâchées ailleurs (par terre, sur un meuble), une fois immobiles | À tester |

## Lun. 5 oct. 2026
- **Fait :** création du dépôt GitHub, lecture des consignes, première analyse de l'idée et brouillon du GDD, test du prototype v2 et captures, **analyse critique du prototype (personnelle, pas encore discutée avec l'équipe)**, test et analyse des prototypes de Nicolas (coffres) et de Maxens (bananier), intégration des trois prototypes dans un seul projet (branche `feat/integration`).
- **Bloque :** —
- **Demain :** corriger les points de mon analyse critique (disposition VR, bibliothèque, halo de fusion, déplacement sur la carte).

**Captures du prototype v2** (clavier/souris, greybox) :

Hub : la bibliothèque à gauche et le plateau devant.

![Hub : bibliothèque et plateau](../captures/proto-v2-hub.png)

Le plateau, c'est la carte en direct : on y voit les ballons et les singes.

![Plateau en direct](../captures/proto-v2-plateau-direct.png)

On pose un singe sur le plateau, avec un aperçu vert quand c'est possible.

![Placement d'un singe](../captures/proto-v2-placement.png)

La carte, avec les singes posés depuis le hub.

![Carte](../captures/proto-v2-carte.png)

**Mon analyse critique du prototype v2** _(mon avis seul pour l'instant)_

- **Ce qui marche :** l'idée globale est la bonne. Le hub avec sa bibliothèque, le plateau qui montre la carte en direct, et la carte avec les singes posés depuis le hub, tout ça fonctionne ensemble.
- **Disposition pour la VR :** en VR, le joueur sera au centre, à son point d'apparition. Tous les éléments du hub doivent être **autour de lui**, inclinés vers lui et à portée de main. Aujourd'hui, ils sont alignés devant et sur les côtés, comme pour un écran.
- **La bibliothèque n'est pas utilisable :** 56 cubes (7 types × 8 raretés) avec leurs étiquettes, c'est trop d'informations d'un coup. On n'arrive pas à la lire. Il faut une autre solution.
- **Rendre la fusion visible :** quand on vise avec un singe en main un singe identique déjà posé, l'aperçu doit montrer qu'**on ne peut pas poser** à cet endroit. Un **halo blanc** autour du singe déjà posé doit indiquer qu'on peut **fusionner**. Aujourd'hui, l'aperçu devient simplement vert, et on ne distingue pas « poser » de « fusionner ».
- **Déplacer les singes sur la carte :** on doit pouvoir **prendre et déplacer** un singe directement quand on est sur la carte, pas seulement depuis le plateau du hub.


**Proposition alternative (testée, non retenue)** : hub en cercle autour du joueur, avec une bibliothèque réduite à 7 socles (un par type) et un sélecteur de rareté à 8 pastilles.
On garde de cet essai le plateau incliné, mais pas la bibliothèque : on revient à la bibliothèque en étagères (7 types × 8 raretés).

![Alternative : hub en cercle, vue de face](../captures/alternative-hub-cercle-1.png)

![Alternative : bibliothèque à 7 socles et pastilles de rareté](../captures/alternative-hub-cercle-2.png)

![Alternative : vue d'ensemble](../captures/alternative-hub-cercle-3.png)

**Version actuelle (prototype v3)** : bibliothèque en deux meubles d'étagères (×1,1), plateau incliné posé sur une planche et un pied.

![v3 : les deux meubles de la bibliothèque et le plateau](../captures/proto-v3-hub-bibliotheques.webp)

![v3 : plateau incliné devant la bibliothèque](../captures/proto-v3-plateau-incline.webp)

**Analyse des prototypes de mes camarades** _(mon avis)_

- **Nicolas (ouverture de coffres)** :
  - La roulette qui défile pour afficher la rareté gagnée est une bonne idée.
  - L'animation du coffre fait trop maladroite (« goofy ») : elle est à retravailler.
  - Je proposerais qu'une fois la rareté choisie par la roulette, **le singe sorte du coffre** avec la couleur de sa rareté. Ce serait plus satisfaisant et plus lisible.
- **Maxens (bananier)** : rien à redire. Le prototype est simple et efficace.

### IA
| Outil | Pour quoi | Gardé / jeté |
|---|---|---|
| Claude (Claude Code) | Résumé du support de lancement dans CLAUDE.md, squelette du dépôt (.gitignore Unity, LFS, README, modèles) | Gardé |
| Claude (Claude Code) | Règle « journal obligatoire » pour l'IA, création du dépôt GitHub | Gardé |
| Claude (Claude Code) | Analyse de l'idée TD Bloons par rapport aux consignes, GDD v0 (boucle, pourquoi la VR, périmètre proposé) | À valider en équipe |
| Claude (Claude Code) | Workflow Git (branches + PR), journaux général et par personne | Gardé |
| Claude (Claude Code) | Prototype clavier/souris (sans VR) : générateur de scènes Hub + Map, bibliothèque des 7 singes × 8 raretés, plateau 8×8 (poser / fusion 2→1 / échanger), piste en labyrinthe, vagues de ballons, singes qui tirent | À tester |
| Claude (Claude Code) | Prototype v2 : une seule scène (hub + carte), plateau = carte en miniature en direct (ballons, singes, joueur), placement libre hors piste, aperçu vert/rouge, téléportation | À tester |
| Claude (Claude Code) | Tags Unity (Ballon, Singe, Joueur, Plateau, Bibliotheque, Piste, Terrain, Bouton) créés automatiquement | Gardé |
| Claude (Claude Code) | Mode démo + build Windows pour captures d'écran | Jeté (retiré à ma demande) |
| Claude (Claude Code) | Ajout de mes captures du prototype dans mon compte rendu du jour | Gardé |
| Claude (Claude Code) | Reformulation de mon analyse critique (le fond est le mien) | Gardé |
| Claude (Claude Code) | Corrections suite à mon analyse : aperçu rouge + halo blanc pour la fusion, prendre/poser/fusionner directement sur la carte | Gardé |
| Claude (Claude Code) | Proposition alternative : hub en cercle, bibliothèque à 7 socles + sélecteur de rareté | Jeté (non retenu, voir captures) |
| Claude (Claude Code) | Retour à la bibliothèque en étagères ×1,1, en 2 meubles espacés (rangée la plus haute à 1,65 m) ; plateau incliné posé sur une planche + pied qui ne traverse plus | À tester |
| Claude (Claude Code) | Ajout de mes captures de la version actuelle (v3) | Gardé |
| Claude (Claude Code) | Préparation des projets de Nicolas et Maxens pour les tester (dossiers à part), mise en forme de mon analyse de leurs prototypes | Gardé |
| Claude (Claude Code) | Intégration des 3 prototypes sur `feat/integration` : merge des branches de Nicolas et Maxens, socle Unity 6000.6 gardé, packages glTFast / XR Toolkit / OpenXR ajoutés (le module VR ajouté aussi a été retiré : il n'existe plus en 6000.6), `.glb` en LFS ; package perso `unity-mcp` de Maxens non repris | À tester |
| Claude (Claude Code) | Ajout des retours de Maxens (envoyés sur Discord) dans son journal, à sa place | Gardé |
| Claude (Claude Code) | Hub en cercle autour du joueur : plateau plus grand (1,6 m) devant, bibliothèque à gauche et à droite, bananier + panier de Maxens derrière, coffre de Nicolas derrière à droite ; branchements : prendre les bananes, ouvrir le coffre (clic ou E), argent commun (bananes → coffre) | À tester |
| Claude (Claude Code) | Dossier `prompts/` (mes prompts reformulés et corrigés), règle « prompts » dans CLAUDE.md pour nous trois, README (rôles + journal commun du jour) | Gardé |
| Claude (Claude Code) | Un seul menu, « SAE » : menu « SAE501 » de Nicolas supprimé (packages déjà dans le projet, coffre monté par le hub), menus « Bananes » de Maxens retirés (son installeur reste utilisé par le hub) | Gardé |
| Claude (Claude Code) | Suppression des scènes de test de Nicolas et Maxens (`Sandbox/`) et des anciennes scènes `Hub.unity` / `Map.unity` (prototype v1) : il ne reste que `Jeu.unity` | Gardé |
| Claude (Claude Code) | Ajout de mes captures de l'intégration dans le journal général | Gardé |
| Claude (Claude Code) | Corrections du hub intégré : bibliothèques courbes qui suivent le cercle (lisibles depuis le centre, textes qui ne se chevauchent plus), coffre qui se tourne automatiquement vers le joueur, panier écarté du bananier | À tester |
| Claude (Claude Code) | Fusion de `feat/integration` dans `main` (PR #3), sans relecture de Maxens et Nicolas, à ma demande ; PR #2 fermée | Gardé |
| Claude (Claude Code) | Plan du mardi 6 oct. dans le journal général : points à harmoniser, chantiers A (économie), B (inventaire), D (vagues), E (VR) | Gardé |
