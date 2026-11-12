<section class="cover">
<div class="bg" style="background-image:url('{{COVER_IMG}}')"></div><div class="shade"></div>
<div class="top"><span>SAÉ 5D.01 · Dispositifs interactifs</span><span>BUT MMI 3 · IUT de Béziers</span></div>
<div class="block">
<span class="kicker">GAME DESIGN DOCUMENT</span>
<h1>BLOONS VR</h1>
<div class="tag">Un tower defense en réalité virtuelle : l'arc dans une main, les singes dans l'autre, et une bananeraie à faire tourner.</div>
<div class="meta"><div><b>Équipe</b>Dylan · Maxens · Nicolas</div><div><b>Support</b>Meta Quest 3 · PC</div><div><b>Moteur</b>Unity 6000.6 · OpenXR</div><div><b>Version</b>Finale · 13 nov. 2026</div></div>
</div>
</section>

<div class="lead-title">Sommaire</div>
<div class="lead-sub">Le jeu en six chiffres, puis les cinq parties du document.</div>
<div class="stats">
<div class="stat"><b>100</b><span>vagues, puis le mode infini</span></div>
<div class="stat"><b>7 × 8</b><span>types de singes × raretés</span></div>
<div class="stat"><b>7</b><span>sortes de ballons, dont 2 boss</span></div>
<div class="stat"><b>6</b><span>améliorations de l'arc</span></div>
<div class="stat"><b>23</b><span>étapes de tutoriel intégré</span></div>
<div class="stat"><b>1 à 4</b><span>joueurs (coop en bonus)</span></div>
</div>

{{TOC}}

=== A | Le jeu | Ce qu'on fait, pourquoi c'est en VR, et à quoi ça ressemble en une phrase. | n-carte-vue.png

## Fiche d'identité

| Élément | Détail |
|---|---|
| **Titre de travail** | Bloons VR (fan-game d'école, à renommer pour une diffusion) |
| **Genre** | Tower defense + tir à l'arc + gestion légère |
| **Support** | Casque VR autonome (Meta Quest 3), debout, espace de jeu de 2 × 2 m minimum. Un mode PC clavier-souris sert au développement et à la démo à plat (build Windows publié : v1.0-pc) |
| **Joueurs** | 1 joueur. En bonus : coop jusqu'à 4 par Internet (casques et PC mélangés) |
| **Public** | Joueurs occasionnels dès 10 ans, découvreurs de la VR, fans de Bloons TD |
| **Durée d'une session** | 3 à 5 min de tutoriel, puis des parties de 10 à 30 min (une vague dure 30 s à 2 min). La partie est sauvegardée automatiquement |
| **Ton** | Cartoon, coloré, chaleureux, jamais violent : on éclate des ballons |
| **Moteur** | Unity 6000.6, URP, XR Interaction Toolkit 3.6, OpenXR, Netcode for GameObjects (coop) |

## Pitch

> **En une phrase :** un tower defense en VR dans l'univers de Bloons TD. Tu es Quincy : tu tires à l'arc sur les ballons, tu poses tes singes à la main sur une maquette, et tu fais tourner ta bananeraie pour payer le tout.

Des vagues de ballons suivent un chemin de dalles vers la cabane des singes. Le joueur passe sans cesse entre **deux échelles** :

<div class="cards">
<div class="card dark"><h5>GÉANT DANS LA CABANE</h5><p>Penché sur une <b>maquette vivante</b> de la carte, il prend ses singes dans une bibliothèque, les pose à la main et les fusionne. Les ballons y circulent en miniature, en direct.</p></div>
<div class="card dark"><h5>À TAILLE RÉELLE SUR LA CARTE</h5><p><b>L'arc en main</b>, debout sur une estrade, il défend lui-même le chemin au milieu des mêmes ballons, sous un dirigeable de plusieurs mètres.</p></div>
</div>

Entre deux tirs, il passe la porte de la cabane pour **récolter ses bananes avant qu'elles ne pourrissent** : elles paient les coffres, les nouveaux singes et les améliorations. L'objectif : tenir **100 vagues** jusqu'aux dirigeables rouges, puis aller le plus loin possible en mode infini et grimper au classement.

!![La cabane : la bibliothèque à gauche, la maquette au centre, le coffre à droite.](n-cabane-interieur.png) | ![La carte : le chemin de dalles, les portails, les singes posés, l'estrade du joueur.](n-carte-vue.png)

## Le geste et la boucle

### Le geste central : tirer à l'arc
L'arc est dans la main gauche. La main droite vient chercher la corde, serre le grip, recule jusqu'à la joue pour tendre, vise et lâche. **La précision vient du geste, pas d'un clic** : une flèche trop peu tendue tombe avant sa cible. C'est le geste VR du jeu, impossible à faire aussi bien à la souris.

![L'arc tendu : la corde suit la main droite, la tension règle la force du tir.](maxens-arc-tendu.png)

### Les gestes secondaires

<div class="cards c3">
<div class="card"><h5>SAISIR ET POSER</h5><p>Un singe pris dans la bibliothèque, posé sur la maquette. Un rayon montre où il va tomber.</p></div>
<div class="card"><h5>ATTRAPER ET LANCER</h5><p>Une banane, prise de près ou de loin, lancée dans le panier pour la vendre.</p></div>
<div class="card"><h5>FAIRE TOURNER</h5><p>La roue de la roulette, attrapée et lancée à la main, comme un croupier.</p></div>
<div class="card"><h5>ENFONCER</h5><p>De gros boutons ronds et la porte de la cabane, du bout de la manette. Un bouton grossit quand on le vise.</p></div>
<div class="card"><h5>LANCER UN SINGE</h5><p>Un singe récolteur, attrapé et lancé pour rire : il atterrit, sonné, puis retourne au travail.</p></div>
<div class="card"><h5>VISER ET LANCER</h5><p>Des fléchettes sur la cible du mur : la pause sans enjeu.</p></div>
</div>

### La boucle de 30 secondes
> Le joueur **tire à l'arc et place ses singes** pour **arrêter la vague de ballons**, mais **les ballons arrivent plus nombreux et plus solides, et les bananes pourrissent si on ne les ramasse pas**, et il gagne **des bananes, qui achètent des singes, des coffres et des améliorations**.

{{LOOP}}

### Les trois boucles imbriquées

| Échelle | Durée | Le joueur… | Il obtient… |
|---|---|---|---|
| **Micro** | 2 à 5 s | bande l'arc, vise, éclate un ballon ; lance une banane | un ballon de moins, une banane vendue |
| **Vague** | 30 s à 2 min | prépare (pose, fusionne, achète), lance la vague, défend | 20 + 10 × n° de vague bananes |
| **Partie** | 10 à 30 min et plus | débloque des types, fusionne vers les raretés hautes, améliore l'arc et la bananeraie, gagne des trophées | la vague 100, puis le mode infini et le classement |

## Pourquoi la VR

<div class="cards">
<div class="card"><h5>LES MAINS</h5><p>Bander l'arc, viser, lâcher. Poser un singe à la main, lancer une banane dans le panier, faire tourner la roulette, lancer des fléchettes.</p></div>
<div class="card"><h5>LE REGARD</h5><p>Surveiller le chemin des ballons, repérer la banane qui brunit, suivre la roue qui ralentit, chercher la flèche dorée du tutoriel.</p></div>
<div class="card"><h5>LA PRÉSENCE ET L'ÉCHELLE</h5><p><b>Géant</b> penché sur une maquette vivante, puis <b>à taille réelle</b> au milieu des mêmes ballons, sous un dirigeable de plusieurs mètres, dans l'orage.</p></div>
<div class="card"><h5>LE CORPS DANS L'ESPACE</h5><p>On passe la porte pour récolter, on se retourne vers la bibliothèque, on se penche sur le plateau. La cabane est un cercle autour de soi.</p></div>
</div>

>! **Le test de l'écran.** À la souris, poser des tours marcherait aussi bien : c'est Bloons TD. Ce qui justifie la VR, c'est **l'arc** (un geste, pas un clic), **le changement d'échelle** (géant, puis minuscule devant un boss) et **le double jeu** « gérer sa base tout en défendant », qui demande de se déplacer et de se retourner.

## Type de jeu et références

> **« C'est Bloons TD, mais avec ton arc en main et tes singes posés à la main sur une maquette. »**

| Référence | Ce qu'on lui emprunte |
|---|---|
| **Bloons TD 6** | Les codes du tower defense, les ballons (couches, rapides, blindés, camouflés, dirigeables qui se divisent), la carte Monkey Lane, les sons et musiques |
| **Rush Royale** | La fusion de deux unités identiques, le coffre et ses raretés |
| **Job Simulator** | Saisir, lancer, enfoncer des boutons : tout est physique |
| **In Death, The Lab (Longbow)** | La sensation du tir à l'arc en VR |
| **Moss** | Le joueur géant penché sur un monde miniature (la maquette) |
| **How to Fish** | La roulette : la roue en bois, la mise posée sur la table, visible |
| **Gamble With Your Friends** | La borne de blackjack : terminal de mise à pavé, gros boutons, croupier en face |
| **PlateUp!** | L'esprit du coop : dans la même base, chacun prend un rôle (l'arc, les singes, la récolte) |

#### Pourquoi c'est un bon jeu
- **Il se prend en main sans doc :** le tutoriel joue le niveau 1, chaque consigne est montrée par une flèche dorée, dans le décor.
- **Il s'appuie sur des codes connus :** tower defense, fusion, coffre à raretés, couleurs des ballons de Bloons.
- **Il hérite de la culture du jeu vidéo :** paliers de difficulté, boss qui se divisent, mode infini, trophées, classement, casino, et le « dunk » des singes récolteurs.
- **Il crée des décisions :** défendre à l'arc ou récolter avant que ça pourrisse ; fusionner tout de suite ou garder deux singes ; miser ses bananes ou les économiser.

=== B | L'univers | Trois lieux, un style low-poly fait maison, et un son à chaque geste. | n-cabane-et-bananeraie.png

## Univers et lieux

L'univers de Bloons TD est assumé : des singes, des ballons colorés, un chemin qui serpente. Le joueur incarne **Quincy**, l'archer de la cabane. Il n'y a pas de scénario à embranchements : l'histoire tient en trois tableaux au début du tutoriel (la cabane, les ballons qui arrivent, ce qu'est un tower defense).

{{PLACES}}

| Lieu | Ce qu'on y voit | Ce qu'on y fait |
|---|---|---|
| **Le menu** | Une scène vivante : le titre, Quincy et les singes qui font coucou, des ballons à éclater | CONTINUER, NOUVELLE PARTIE (à confirmer), OPTIONS, REJOINDRE une partie en coop |
| **La cabane (hub)** | Cabane ronde en rondins, toit de chaume, lustre en roue de charrette, lanternes, guirlande de ballons, tapis rond, armoire à trophées | Poser et fusionner les singes sur la maquette, ouvrir le coffre, lancer les vagues, jouer aux fléchettes |
| **La bananeraie** | Derrière la grande porte : terrasse en planches, abri en rondins, 1 à 3 bananiers et leurs étals, panier, casino, panneau du classement | Récolter les bananes, améliorer les arbres, acheter des récolteurs, miser au casino |
| **La carte** | Prairie façon Monkey Lane : chemin de dalles qui se croise deux fois, fleurs, champignons, palmiers, montagnes, deux portails en pierre | Défendre à l'arc depuis l'estrade, améliorer l'arc |

!![Le menu principal : Quincy, les singes, des ballons à éclater.](menu-principal.webp) | ![Vue d'ensemble : la cabane, sa terrasse et la bananeraie derrière.](n-cabane-et-bananeraie.png)

!![La bananeraie : les étals et les bananiers à gauche, l'abri au fond, le panier à droite.](n-bananeraie-vue.png) | ![Depuis la bananeraie : la cabane et sa porte « CABANE ».](n-cabane-exterieur.png)

## Direction artistique

- **Style :** low-poly coloré et lisible, sans textures réalistes, des formes rondes et des couleurs saturées. La cabane, le paysage, le coffre et la roulette sont **faits maison par scripts Blender** (`Blender/cabane.py`, `coffre.py`, `roulette.py`) : un modèle se régénère en une commande.
- **Typographie :** **Bangers** pour les titres (esprit BD), **Oswald Bold** pour les textes. Tout texte est écrit sur un objet du décor : ardoise, enseigne, plaque, pancarte.
- **Lumière :** soleil de fin d'après-midi, poussières dorées dans les rayons des fenêtres, flammes qui vacillent dans les lanternes ; ciel d'orage pendant les vagues de boss.

<div class="cards c4">
<div class="card" style="border-top-color:#8a5a1c"><h5>BOIS CHAUD</h5><p>La cabane, l'estrade, les meubles.</p></div>
<div class="card" style="border-top-color:#5d9440"><h5>VERT PRAIRIE</h5><p>La carte, le paysage, « possible ».</p></div>
<div class="card" style="border-top-color:#e3a72f"><h5>OR</h5><p>L'interface : titres, cadres, flèche du tutoriel, action proposée.</p></div>
<div class="card" style="border-top-color:#2f3b33"><h5>ARDOISE ET CRAIE</h5><p>Tous les tableaux où on lit.</p></div>
</div>

#### Les codes couleur
- **Les ballons** changent de couleur à chaque couche perdue : on lit leur solidité d'un coup d'œil.
- **Les raretés** vont du gris au blanc (voir la section Fusion).
- **Rouge = interdit, vert = possible, or = action proposée.** Le fantôme de pose, les plaques de prix et le cadre du tutoriel suivent tous cette règle.

!![Les murs en rondins et la guirlande de ballons.](n-murs-guirlande.png) | ![Les montagnes low-poly à l'horizon.](n-horizon.png)

#### Droits
L'univers appartient à Ninja Kiwi. Le projet reste un exercice d'école, un fan-game non commercial. Il reprend des sons et des musiques de Bloons TD 6 (leur source est notée dans les journaux) : **en l'état, le jeu n'est pas candidat aux Pégases**. Une diffusion demanderait de renommer le jeu et de changer l'habillage (singes, ballons, nom, sons), ce qui reste possible : toute la mécanique est indépendante de la marque.

## Audio et vibrations

Le jeu était muet à la fin de la semaine 1 : c'était son plus gros manque. Depuis, **chaque action a son image, son son et sa vibration**.

| Famille | Ce qu'on entend |
|---|---|
| **Musiques** | Trois ambiances en fondu : la préparation, la vague, le boss (tant qu'un boss est en vie). Jouées en streaming |
| **Arc** | La corde qui se tend, le tir, le rebond ; Quincy parle de temps en temps quand une flèche touche (1 fois sur 8, jamais deux fois en 10 s), et toujours quand un boss éclate |
| **Ballons** | Le « pop » (plusieurs variantes, 3 au plus à la fois), plus grave pour un boss |
| **Vagues** | Annonce de boss, entrée du boss, entrée du dirigeable, vie perdue, jingle de vague gagnée, de vague perdue, de victoire finale |
| **Singes et coffre** | Pose, fusion, ouverture du coffre, « tic » de la roulette, singe obtenu, fermeture |
| **Économie** | Banane vendue (plus aiguë pour une dorée), achat refusé |
| **Interface** | Clic de bouton, « ding » du tutoriel à chaque étape, whoosh de téléportation, klaxon du plein centre aux fléchettes |

**Les vibrations ont un motif par action** (`Haptics`), pour reconnaître ce qui se passe sans regarder : la tension de l'arc qui monte dans la main, un tic sec quand la flèche touche, deux battements de cœur à l'annonce d'un boss, un grondement dans les deux mains quand un ballon passe. Les volumes (musique, sons) et les vibrations se règlent dans les options.

=== C | Les règles | Singes, arc, ballons, vagues, bananes : tous les chiffres du jeu. | x-boss.png

## Déroulé d'une partie

Une partie alterne **deux temps** :

- **La préparation (au hub) :** rien n'attaque. On gère les singes, les bananes, le coffre, puis on appuie sur **LANCER**.
- **L'attaque :** la vague suit son chemin. On peut rester au hub (la maquette montre la carte en direct, ballons compris) ou aller sur la carte avec **SE TP** pour tirer à l'arc.

>! **Le monde qu'on quitte continue de tourner.** Les bananes tombent et pourrissent pendant qu'on défend ; les ballons avancent pendant qu'on récolte. C'est ce qui crée la tension du jeu : on ne peut pas être partout.

- **20 vies par vague**, remises au maximum à chaque vague. Un ballon qui atteint la sortie retire **autant de vies que ses couches restantes**.
- **Perdre une vague ne fait pas tout recommencer :** on la rejoue, singes et bananes gardés.
- **Éclater un ballon ne rapporte rien :** seule la vague finie paie, pour pousser à défendre proprement.
- **Victoire à la vague 100** (4 dirigeables rouges), puis mode infini.
- On démarre avec **0 banane et aucun singe** : la vague 1 se gagne à l'arc, puis le tutoriel offre le premier singe.

## Les singes

On les prend dans la **bibliothèque**, un meuble courbe avec une ligne par type et une colonne par rareté. On les pose sur la maquette avec un rayon de pose : le fantôme est **vert** si c'est possible, **rouge** sinon, et un cercle montre la portée.

| Type | Effet | Portée | Tir toutes les… | Perce | Cibles | Déblocage |
|---|---|---|---|---|---|---|
| **Classique** | Tire sur le ballon le plus avancé | 6 m | 1 s | 1 | 1 | offert |
| **Boomerang** | Touche 2 ballons par lancer | 6 m | 1 s | 1 | 2 | 150 |
| **Punaise** | Touche 4 ballons tout près : il garde un virage | 2,5 m | 0,7 s | 1 | 4 | 200 |
| **Glace** | Ralentit tous les ballons à portée (onde de froid) | 6 m | 1,5 s | 0 | tous | 300 |
| **Canon** | Explosion autour de la cible | 5 m | 1,5 s | 1 | zone | 400 |
| **Colle** | Colle un ballon : très lent pendant 3 s | 6 m | 1 s | 0 | 1 | 500 |
| **Sniper** (le Tireur) | Tire sur toute la carte, gros dégâts, **voit les camouflés** | 100 m | 2 s | 3 | 1 | 600 |

**Ce que gagne un singe à chaque rareté :** +0,3 m de portée (+0,15 m pour la Punaise), +20 % de cadence, et +1 couche de perce toutes les 2 raretés. **Dès le violet, il voit les ballons camouflés.**

#### Règles de pose
- jamais sur le chemin, et à 0,95 m au moins d'un autre singe ;
- **60 singes au plus** sur le plateau (le tableau affiche « Singes : n / 60 ») : au-delà, il faut fusionner ;
- on peut reprendre un singe posé ; **VIDER** (10 bananes) range tout le plateau.

!![La bibliothèque : une ligne par type, une colonne par rareté.](n-bibliotheque.png) | ![La maquette : les singes posés sur la carte en miniature.](n-maquette.png)

Les singes posés sont **animés** : ils se tournent vers leur cible, font un geste propre à leur type et lancent de vrais projectiles 3D. Le bouton A (en visant un singe) affiche sa fiche.

## Fusion et coffre

### La fusion
**Deux singes identiques (même type, même rareté) fusionnent en un singe de la rareté suivante.** On pose le second sur le premier : un fantôme **doré** montre le résultat, avec un halo qui pulse.

{{RARITIES}}

![La fusion : le fantôme doré annonce le singe de la rareté suivante.](fusion-doree-plateau.webp)

### Le coffre
Le coffre se paie en bananes : **40 + 15 par vague vaincue**. Il s'ouvre en roulette façon caisse à butin, avec un « tic » à chaque singe qui passe, et donne **un singe**. Son type est tiré parmi les types débloqués ; sa rareté dépend de la vague :

| Rareté | Gris | Vert | Bleu | Violet | Jaune | Rouge |
|---|---|---|---|---|---|---|
| **Poids de base** | 50 | 28 | 14 | 6 | 2 | 0,9 |
| **Possible dès la vague** | 0 | 0 | 3 | 7 | 12 | 20 |

**Les chances sont affichées sur un tableau au mur**, à côté du coffre : pas de hasard caché.

!![Le coffre, son prix, et le tableau de ses chances au mur.](n-coffre-porte.png) | ![La roulette du coffre : les singes défilent, puis ralentissent.](jeu-coffre-roulette.png) | ![Le singe tiré sort du coffre, monte et grossit.](jeu-coffre-singe.png)

## Quincy et l'arc

- **Tir physique :** la force dépend du recul de la main. Il y a 0,3 s entre deux tirs, pour ne pas « spammer ».
- **L'arc n'existe que sur la carte :** au hub, on a les mains libres.
- **Les flèches de Quincy percent le blindage et touchent les camouflés :** le joueur peut toujours s'en sortir lui-même.

Le **pupitre ARC**, sur l'estrade, vend six améliorations qui se cumulent :

| Amélioration | Effet | Paliers et prix |
|---|---|---|
| **Perforante** | Couches percées par ballon touché : 1 → 6 | 100, 200, 400, 700, 1 200 |
| **Transperçante** | Ballons traversés par flèche : 1 → 8 | 150, 300, 500, 800, 1 300 |
| **Tir triple** | 2 flèches de plus, à ± 8° | 500 |
| **Explosion** | Onde au contact : 3 à 8 ballons, rayon 1 à 3 m | 150, 300, 500, 800, 1 300 |
| **Rebond** | La flèche file vers le ballon le plus proche (6 m) : 1 à 5 rebonds | 200, 400, 700, 1 100 |
| **Givrante** | Chaque ballon touché est ralenti (70 %, 55 %, puis 40 % de sa vitesse) pendant 1,5 à 3 s | 150, 350, 650 |

!![L'estrade de la carte : le pupitre ARC, le tableau de la vague, LANCER / HUB, la caisse.](n-carte-estrade.png) | ![Le pupitre ARC et ses six améliorations.](n-pupitre-arc.png) | ![Le tir explosif : les ballons autour de l'impact éclatent.](jeu-tir-explosif.png)

## Les ballons

| Sorte | Taille | Vitesse | Particularité |
|---|---|---|---|
| **Normal** | 0,9 | × 1 | Perd une couche par point de perce ; sa couleur change à chaque couche |
| **Rapide** | 0,65 | × 1,7 | Petit et vif |
| **Blindé** | 1 | × 0,8 | Une couche coûte **2 de perce** : il faut l'arc, un singe Bleu ou plus, ou un Sniper. Ne peut être ni gelé ni collé |
| **Cœur** | 0,9 | × 1 | Regagne une couche toutes les 2 s |
| **Camouflé** (normal ou rapide) | — | — | Kaki à taches sombres : **seuls le Sniper et les singes Violet et plus le voient** ; les flèches le touchent toujours |
| **Boss (MOAB)** | 1,8 | × 0,6 | Très solide, hélice qui tourne ; **en éclatant, lâche 4 ballons normaux** |
| **Dirigeable rouge (BFB)** | 2,2 | × 0,35 | Le boss final : blindé, insensible au ralentissement ; **en éclatant, lâche 2 boss** |

![Tous les ballons : normal, rapide, blindé, cœur, MOAB et dirigeable rouge.](jeu-ballons-tous.png)

!![En pleine vague : des blindés à rivets, des cœurs, des normaux très solides (noirs).](w-283-a.png) | ![Des camouflés à taches, des blindés, et le MOAB qui arrive.](x-camo.png)

**Les boss se divisent**, comme dans Bloons TD : les petits ballons repartent de l'endroit où le boss a éclaté, un peu espacés sur la piste, avec une onde de choc. Leurs couches suivent la solidité du boss (un cinquième de ses couches, entre 2 et 6 ; le dirigeable lâche des boss au quart de ses couches). **Éclater un boss n'est donc pas la fin du danger.**

!![Le boss (MOAB) sur la piste.](maxens-boss-avant.png) | ![Juste après : 4 ballons normaux à sa place.](maxens-boss-divise.png)

>! **Chaque ballon spécial donne une raison de construire.** Le blindé demande de fusionner jusqu'au Bleu, le camouflé jusqu'au Violet ou d'acheter un Sniper, le cœur demande de la cadence, le boss de la perce. Quand une nouvelle sorte arrive, **le tableau de la vague prévient et dit quoi faire**.

## Les 100 vagues

Les vagues sont calculées par **une seule formule** (`WaveBook.Make`) : chaque sorte de ballon arrive à son palier, puis son nombre et ses couches montent doucement. Tout se règle au même endroit, en quelques minutes.

{{WAVES}}
<div class="cap">La difficulté de chaque vague, mesurée en couches de ballon à percer. Les quatre pics sont les vagues à dirigeable.</div>

| Vague | Nouveauté | Composition |
|---|---|---|
| 1 | Ballons normaux seulement (tutoriel) | 8 normaux à 1 couche |
| 2 | Un 2e groupe plus solide | 11 × 1 couche + 5 × 2 couches |
| 3 | **Rapides** | 12 + 6 normaux, 3 rapides |
| 6 | **Blindés** (message d'alerte) | 15 + 7 normaux, 5 rapides, 2 blindés |
| 10 | **Cœurs** (une vague sur deux) et **camouflés** | 19 + 8 normaux, 8 rapides, 4 blindés, 2 cœurs, 2 camouflés |
| 12 | **Premier boss**, puis un toutes les 4 vagues | … + 1 boss à 22 couches |
| 20 | **Rapides camouflés** | 28 + 11 normaux, 16 rapides, 11 blindés, 4 cœurs, 5 + 1 camouflés, 1 boss |
| 25 | **Dirigeable rouge** | … + 1 dirigeable à 117 couches |
| 40 | Un boss à chaque vague | 46 + 18 normaux, 32 rapides, 24 blindés, 9 cœurs, 12 + 6 camouflés, 2 boss |
| 50 | 2 dirigeables | 55 + 21 normaux, 40 rapides, 31 blindés, 12 cœurs, 15 + 8 camouflés, 3 boss, 2 dirigeables |
| 75 | 3 dirigeables | 77 + 30 normaux, 60 rapides, 48 blindés, 23 + 14 camouflés, 5 boss, 3 dirigeables |
| **100** | **Finale : 4 dirigeables → victoire** | 100 + 38 normaux, 80 rapides, 64 blindés, 24 cœurs, 32 + 21 camouflés, 6 boss, 4 dirigeables à 230 couches |
| 101 et + | Mode infini : la formule continue | un dirigeable de plus toutes les 25 vagues |

### Les vagues de boss ont leur mise en scène
1. **Pendant la préparation**, le tableau prévient : « VAGUE DE BOSS ! » ou « DIRIGEABLE ROUGE EN APPROCHE ! ».
2. **Au lancement**, un son d'annonce, deux battements de cœur dans les manettes, et **l'orage arrive** : nuages, ciel qui s'assombrit.
3. **Quand le boss entre sur la piste**, son entrée (plus grosse pour le dirigeable), « LE BOSS ARRIVE ! », et l'orage éclate : pluie, brume, éclairs doux et tonnerre.
4. **La musique de boss** remplace celle de la vague tant qu'un boss est en vie.
5. **La vague finie**, le ciel se dégage. Dans la cabane, il ne pleut pas : on voit la pluie par la fenêtre et on l'entend, étouffée.

!![Sur l'estrade : « VAGUE DE BOSS ! », la pluie commence.](w-278-c.png) | ![« LE BOSS ARRIVE ! » : l'orage éclate.](x-boss-estrade.png)

!![La carte sous l'orage, vue de haut : brume et pluie.](w-283-b.png) | ![Le MOAB entre sur la piste, entouré de blindés et de camouflés.](x-boss.png)

## La bananeraie

Derrière la grande porte de la cabane : **l'économie du jeu, faite à la main.**

### Les bananes
- une banane tombe toutes les **8 s** au départ, et vaut **5** ;
- elle **pourrit en 15 s** : elle brunit, se ratatine, sent mauvais, puis disparaît ;
- **tenue en main**, par le joueur ou par un singe, elle ne pourrit pas ;
- on la **lance dans le panier** pour la vendre : un « +5 » s'envole, la caisse saute ;
- **banane dorée :** 1 sur 25 (4 %) tombe dorée et brillante. Elle vaut **10 fois plus** et pourrit **2 fois moins vite**. C'est la petite surprise qui fait courir au pied de l'arbre.

!![Les trois bananiers et leurs étals.](n-trois-bananiers.png) | ![Une banane normale et une banane dorée.](maxens-banane-doree.png)

### Le comptoir BANANIER
Sous l'abri. Les améliorations valent pour tous les arbres ; le prix est multiplié par 1,6 à chaque niveau, et le bouton disparaît au niveau maximum.

| Amélioration | Effet par niveau (10 niveaux) | 1er prix |
|---|---|---|
| **Production** | − 0,6 s entre deux bananes (8 s → 2,6 s) | 60 |
| **Fraîcheur** | + 3 s avant de pourrir (15 s → 42 s) | 40 |
| **Valeur** | + 3 bananes par banane (5 → 32) | 80 |
| **+ 1 arbre** | 2e bananier, puis 3e | 500, puis 1 500 |

### Les singes récolteurs
Le comptoir RÉCOLTEUR vend jusqu'à **4 singes** (150, 300, 450, 600), améliorables sur 5 niveaux en **vitesse** (0,25 → 1,65 m/s), **cadence** (pause de 3 s → 0,6 s) et **rendement** (ils gardent une commission : 60 % → 100 % payé).

- Chaque singe va à **l'arbre qui a le plus de bananes en attente** (2 singes au plus par arbre) et file d'abord vers une **banane dorée**.
- Il prend 2 bananes par trajet quand c'est possible et les lance dans le panier. **Il rate 1 fois sur 4** (il boude, puis va la rechercher) et **fait un dunk 1 fois sur 5**.
- Au maximum, ils ramènent environ **75 % de la production** des 3 arbres : le joueur doit encore ramasser lui-même.
- On peut les attraper et les lancer, pour rire : ils atterrissent, sont sonnés, puis retournent au travail.

!![Sous l'abri : les comptoirs BANANIER et RÉCOLTEUR, le panier au premier plan.](n-bananeraie-abri.png) | ![Un singe récolteur au travail devant les étals.](n-recolteurs.png)

## Économie

{{ECONOMY}}

#### Repères d'équilibrage
- 1 bananier sans amélioration ≈ **37 bananes par minute** si tout est ramassé ;
- le 1er coffre (40) ≈ la récompense de la vague 1 (30) + 2 bananes ;
- le Boomerang se débloque vers la vague 3, le Sniper vers la vague 10 ;
- une caisse affiche le total au hub, dans la bananeraie et sur la carte : on voit ses bananes partout.

## Les à-côtés : casino, trophées, classement, fléchettes

### Le casino (ouvre à la vague 5)
Au début, la roulette et le blackjack sont **fermés par des barrières de chantier**, avec une pancarte « CASINO FERMÉ · ouvre à la vague 5 (2 / 5) ». À la vague 5, les barrières s'enfoncent dans le sol et deux tableaux expliquent comment jouer. **Pourquoi :** on apprend d'abord la boucle sans parier ses premières bananes, et le casino devient une récompense.

- **La roulette**, en 3 étapes écrites sur l'ardoise : **choisir sa couleur** sur le tapis (ROUGE × 2, NOIR × 2, VERT × 36), **miser** avec les jetons (+1, +10, +100, TOUT), puis **faire tourner la roue à la main**. Le numéro est tiré au lâcher : le geste fait le spectacle, pas la triche.
- **La borne de blackjack :** une partie contre un singe croupier à chapeau. On tape sa mise sur un terminal à pavé (ou MIN, 1/4, 1/2, MAX), puis 4 gros boutons : DOUBLER, RESTER, CARTE, JOUER. Le croupier tire jusqu'à 16 ; gagné × 2, blackjack × 2,5, égalité = mise rendue.

!![La roulette ouverte : la roue, le tapis, les jetons et l'ardoise des 3 étapes.](n-roulette.png) | ![La borne de blackjack et son croupier.](n-blackjack.png) | ![Avant la vague 5 : roulette et blackjack fermés par des barrières.](casino-ferme.png)

### Trophées, classement, fléchettes
- **Les trophées :** une armoire dans la cabane. Quatre paliers pour les vagues vaincues (10, 50, 100, 200) et quatre pour les bananes gagnées (1 000, 5 000, 15 000, 50 000). Tant qu'il n'est pas gagné, un trophée est sombre et sa plaque montre où on en est (« 37 / 50 »). Gagné, il prend sa couleur (bronze, argent, or, diamant arc-en-ciel), et une copie apparaît devant le joueur avec une fanfare.
- **Le classement :** un panneau sur la terrasse, en deux colonnes : **ICI** (les scores du casque) et **MONDE** (une base en ligne). Chaque casque a un nom, qu'on change avec un clavier en cubes.
- **Les fléchettes :** au mur de la cabane, 3 fléchettes par manche, 5 à 50 points selon l'anneau, meilleur score affiché. En plein centre : un klaxon.

!![L'armoire à trophées : le premier est gagné, les autres attendent.](n-trophees.png) | ![Un trophée gagné apparaît devant le joueur.](trophee-succes.webp) | ![Le classement : ICI et MONDE.](n-classement.png)

## Le coop (bonus)

Le jeu est **complet en solo**. Le coop est un bonus, ajouté en fin de semaine 2 : jusqu'à 4 joueurs dans la même partie, casques et PC mélangés.

- **Ouvrir :** depuis la pause, en pleine partie. On choisit un mot de passe, le panneau affiche un **code de partie** à 6 caractères.
- **Rejoindre :** depuis le menu principal, avec le code et le mot de passe.
- **Ce qui est partagé :** la bourse, la bibliothèque, les singes posés, les vagues, les ballons, les flèches, la bananeraie entière (mêmes bananes, mêmes récolteurs, même comptoir), le coffre, les fléchettes, la roulette. On voit la tête, les mains et le singe tenu par les autres.
- **Ce qui reste à chacun :** son arc et ses améliorations, sa borne de blackjack, son tutoriel, sa sauvegarde.

>! **Un choix assumé.** La consigne classe le « multijoueur en ligne » parmi les choses à éviter. Nous l'avons gardé en POURRAIT parce que le jeu tient sans lui, et que dans la même base chacun prend naturellement un rôle : l'un tire à l'arc, l'autre pose les singes, le troisième récolte.

!![Le menu pause et son bouton COOP.](pause-coop.png) | ![Un singe pris dans la bibliothèque : en coop, les autres joueurs le voient dans la main.](maxens-singe-en-main-aura.webp)

=== D | L'expérience | Ce que le joueur vit : apprendre, se repérer, comprendre, rester à l'aise. | cap-tuto-tableau.png

## Tutoriel intégré

Le niveau 1 **est** le tutoriel : 23 étapes, de la première flèche à la première fusion.

- **Une consigne à la fois**, écrite sur des **tableaux du décor** (suspendu dans la cabane, sur poteaux dehors), jamais collée au visage.
- **Une flèche dorée** pulse au-dessus de ce qu'il faut toucher. Si la cible est loin, une **flèche au sol**, aux pieds du joueur, montre la direction ; la porte et la case de la bibliothèque ont un **contour doré**.
- **Les grands moments ont leur annonce**, posée dans le décor à 2 m : « PRÊT AU COMBAT ! », « VICTOIRE ! », « BIEN POSÉ ! », « FUSION RÉUSSIE ! »…
- **Le ton est celui de Bloons :** joyeux, direct, une action par tableau, le geste ou le bouton en MAJUSCULES. Les consignes de l'arc changent entre le casque et le PC.

#### Les garde-fous
- **Seule l'action demandée est permise.** Toute autre est refusée : le tableau tremble, son cadre rougit, et « SUIS LA FLÈCHE DORÉE ! » apparaît (le tableau est peut-être derrière soi).
- **Un échauffement avant la vague 1 :** 3 ballons d'entraînement flottent au-dessus du chemin. Une flèche trop peu tendue tombe, et le tableau dit « TENDS PLUS FORT ! ».
- **Vague 1 stricte :** un seul ballon qui passe et c'est raté. « DOMMAGE ! », puis **REFAIRE** ou **CONTINUER** : on ne bloque personne.
- **PASSER** saute tout le tutoriel (le premier singe est quand même offert).

!![Le tableau du tutoriel, suspendu au-dessus de la maquette.](cap-tuto-tableau.png) | ![L'échauffement : 3 ballons flottent au-dessus du chemin.](tuto-echauffement.png) | ![La flèche dorée au-dessus du bouton LANCER, et la consigne sur son tableau.](tuto-fleche-annonce.png)

| # | Étape | Lieu | Ce que fait le joueur | Passe à la suite quand… |
|---|---|---|---|---|
| 1-3 | L'univers et le genre | Cabane | Lit : la cabane, les ballons, ce qu'est un tower defense | SUIVANT |
| 4 | En route | Cabane | Appuie sur SE TP | il arrive sur la carte |
| 5 | Ton arc | Carte | Lit où est l'arc (main gauche au casque) | SUIVANT |
| 6 | Échauffement | Carte | Éclate 3 ballons d'entraînement (« 1 / 3 BALLONS ») | 3 ballons éclatés |
| 7 | Vague 1 | Carte | Appuie sur LANCER | la vague part |
| 8 | Pas un seul ne passe | Carte | Éclate les 8 ballons sans en laisser passer | vague gagnée, ou CONTINUER |
| 9 | Améliore ton arc | Carte | Lit : les boutons du pupitre ARC | SUIVANT |
| 10 | Bravo | Carte | Reçoit son premier singe, appuie sur HUB | il est au hub |
| 11 | Tes bananes | Cabane | Lit : les bananes sont l'argent, la caisse les compte | SUIVANT |
| 12-13 | La bibliothèque | Cabane | Lit : types en lignes, raretés en colonnes ; les plaques débloquent un type | SUIVANT |
| 14-15 | Ton premier singe | Cabane | Le prend (contour doré sur sa case), le pose sur le plateau | il est posé |
| 16 | Les bananes | Bananeraie | Passe la porte, lance une banane dans le panier | il a gagné des bananes |
| 17-18 | Comptoir, récolteurs | Bananeraie | Lit : PRODUCTION, FRAÎCHEUR, VALEUR ; les singes qui ramassent pour lui | SUIVANT |
| 19 | Le coffre | Cabane | Ouvre le coffre, **offert** : il donne un Classique gris | coffre ouvert |
| 20 | La fusion | Cabane | Pose le nouveau singe **sur** le premier | un singe vert existe |
| 21-22 | Trophées, fléchettes | Cabane | Lit (la flèche montre l'armoire, puis la cible) | SUIVANT |
| 23 | À toi de jouer | Cabane | Lance la vague 2 | la vague part : fin |
| + 2 | Le casino | Bananeraie | À la vague 5 : « LE CASINO OUVRE ! », puis comment jouer | ne bloque rien |

## Level design

### La carte

{{MAP}}

### La cabane

{{CABIN}}

### La bananeraie
- **La terrasse :** 16 × 8,4 m, fermée par une barrière. On y arrive par une allée depuis la porte (la même porte, dans les deux sens ; un panneau dit où elle mène).
- **À gauche,** trois places de bananier, chacune avec son étal.
- **À droite,** le panier et la caisse, puis le casino (roulette et blackjack) et le panneau du classement.
- **Au fond, sous l'abri,** les comptoirs BANANIER et RÉCOLTEUR.

![La porte de la cabane et son panneau : on appuie dessus, elle s'ouvre, un voile noir couvre le passage.](maxens-porte-panneau.png)

## Interface et feedback

### Une interface dans le décor
Il n'y a **aucun texte collé à l'écran**. Tout est écrit sur un objet, à 1 ou 2 m du joueur :
- **les ardoises :** la vague, les vies, les singes posés, la caisse, les chances du coffre, la roulette, le tutoriel ;
- **les enseignes** (BIBLIOTHÈQUE, ARC, BANANIER, RÉCOLTEUR, ROULETTE) disent à quoi sert chaque meuble ;
- **les plaques de prix :** dorées si on peut payer, grises sinon ;
- **les panneaux de porte** indiquent où elle mène ;
- le menu, la pause et les options sont eux aussi des panneaux posés dans le monde.

### Affordances

| Ce qu'on voit | Ce que ça veut dire |
|---|---|
| Un gros bouton rond, qui grossit quand on le vise | Il s'enfonce |
| Le rayon de la main qui se colore | L'objet visé est utilisable |
| La porte qui s'entrouvre et frémit quand on la vise | On peut l'ouvrir en appuyant dessus |
| Une banane jaune / brune / dorée et brillante | Mûre / pourrie / rare (× 10) |
| Un fantôme vert / rouge / doré | Pose possible / interdite / fusion |
| Une plaque de prix dorée / grise | On peut payer / pas encore |
| Des barrières rouges et blanches | Fermé pour l'instant (casino) |

### Le feedback de chaque action

| Action | Image | Vibration | Son |
|---|---|---|---|
| Appuyer sur un bouton | Le bouton s'enfonce | Moyenne | Clic |
| Action refusée | Le tableau tremble, cadre rouge | Très faible | Alerte |
| Bander l'arc, tirer | La corde se tend, traînée dorée | Monte avec la tension, coup sec au tir | Corde, puis tir |
| Flèche qui touche | Le ballon change de couleur, puis éclate | Tic sec dans la main de l'arc | Pop ; Quincy de temps en temps |
| Boss qui éclate | Onde de choc, les petits ballons sortent | Grosse secousse, deux mains | Pop grave, voix de Quincy |
| Ballon qui passe | Les vies baissent (rouges sous 5) | Grondement, deux mains | Vie perdue |
| Vague de boss | « VAGUE DE BOSS ! », orage | Deux battements de cœur | Annonce, entrée, tonnerre, musique |
| Vague gagnée / perdue | Message sur le tableau | Trois tics qui montent / un long et sourd | Jingle de victoire / de défaite |
| Vendre une banane | « +5 » qui flotte, la caisse saute | Deux tics (trois pour une dorée) | Collecte (plus aiguë pour une dorée) |
| Poser / fusionner | Fantôme, halo doré | Forte | Pose / montée de niveau |
| Ouvrir le coffre | Couvercle, roulette, le singe sort | — | Ouverture, tics, singe obtenu |
| Téléportation, porte | Voile noir fondu | — | Whoosh |

## Contrôles

| Action | Casque (Meta Quest) | Mode PC |
|---|---|---|
| Se déplacer | Téléportation : stick vers l'avant, viser, relâcher | ZQSD |
| Tourner | Rotation par crans de 45° (stick) | Souris |
| Prendre un singe, une banane | Viser (rayon) ou toucher, puis serrer le grip | Clic maintenu |
| Poser / lancer | Relâcher le grip | Relâcher le clic |
| Appuyer sur un bouton | Le toucher, ou le viser et appuyer sur la gâchette | Clic |
| Tirer à l'arc | Main droite sur la corde, grip, reculer, lâcher | Clic droit maintenu, puis relâcher |
| Faire tourner la roulette | Attraper le plateau, le lancer | Clic maintenu et glisser |
| Fiche d'un singe | Bouton A en visant le singe | Touche A |
| Ouvrir la porte | Appuyer dessus de la main, ou la viser et appuyer sur la gâchette | Clic |
| Pause | Bouton Menu de la manette | Échap |

La page **COMMANDES** des options rappelle ces touches, au casque comme au clavier.

## Confort

>! **Non négociable.** Chaque idée a été vérifiée contre ces règles avant d'entrer dans le jeu.

- **Déplacement :** téléportation, limitée aux zones de jeu, avec un **voile noir fondu** à chaque saut et à chaque porte. Pas de saut. La marche au stick existe en **option, coupée par défaut** : c'est le joueur qui choisit.
- **Rotation :** par crans de 45°, toujours.
- **Caméra :** jamais bougée à la place du joueur, pas d'accélération, pas de secousse, horizon toujours droit. L'orage n'a ni flash ni secousse.
- **Interface :** dans le décor, à 1-2 m. Les annonces apparaissent une fois devant le joueur et **ne suivent pas la tête**.
- **Portée :** tout est à portée de bras (cercle de 2,8 m). Les singes et les bananes s'attrapent de loin, jusqu'à 6 m : on ne ramasse pas au sol en boucle.
- **Sessions :** vagues courtes, pause naturelle entre deux vagues, pause à tout moment, sauvegarde automatique.
- **Technique :** 72 images par seconde, plafonnées et tenues de façon constante au casque (compteur au poignet en build de test) ; 1 unité Unity = 1 m.

## Menu, pause et sauvegarde

- **Menu principal :** CONTINUER (reprend la sauvegarde), NOUVELLE PARTIE (il faut appuyer deux fois : elle efface la sauvegarde), OPTIONS, REJOINDRE (coop).
- **Options :** volume de la musique et des sons (0 à 10), vibrations, déplacement (téléportation ou stick), page COMMANDES.
- **Pause :** bouton Menu de la manette ou Échap ; on y retrouve les options et le bouton COOP.
- **Sauvegarde automatique :** vague, bananes, singes, améliorations. Le classement continue la même ligne.

!![Les options : musique, sons, vibrations, déplacement.](menu-options.webp) | ![NOUVELLE PARTIE demande une confirmation.](menu-confirmation.webp)

=== E | Le projet | Ce qui est dans le jeu, ce qu'on a coupé, comment on a travaillé, et ce qu'on en retient. | n-bananeraie-abri.png

## Périmètre

Le périmètre range chaque idée selon son importance, pour savoir quoi faire d'abord et quoi couper si le temps manque :

- **DOIT :** sans ça, il n'y a pas de jeu. C'est fait en premier.
- **DEVRAIT :** important, mais le jeu tient sans. C'est fait ensuite.
- **POURRAIT :** un bonus, s'il reste du temps. C'est coupé en premier.
- **NE FERA PAS :** écarté dès le départ, pour ne pas se disperser.

✅ gardé : dans le jeu · ✂ coupé

| DOIT | DEVRAIT | POURRAIT | NE FERA PAS |
|---|---|---|---|
| ✅ 1 carte, 1 chemin, 100 vagues + finale | ✅ Fusion et 8 raretés | ✅ 6 améliorations de l'arc | Plusieurs cartes |
| ✅ Quincy à l'arc (tir physique) | ✅ Coffre et sa roulette | ✅ Bananes dorées, 2e et 3e bananiers | Méta-progression entre les parties |
| ✅ Singes posés à la main | ✅ Bananes qui pourrissent, singes récolteurs | ✅ Roulette, blackjack, fléchettes | Achats réels, gacha complet |
| ✅ Bananes → panier → déblocages | ✅ Sons, musiques, vibrations par action | ✅ Orage sur les vagues de boss (pluie, brume, tonnerre) | Multijoueur compétitif |
| ✅ Tutoriel intégré (23 étapes) | ✅ Boss qui se divisent, vagues annoncées | ✅ Classement en ligne | Scénario, monde ouvert |
| ✅ Confort : téléportation, crans, voile noir, 72 fps | ✅ Ballons cœur, blindés, camouflés | ✅ Coop à 4 par Internet | Déplacement continu imposé |
| ✅ Victoire et défaite annoncées (tableau, jingle, vibration) | ✅ Menu, pause, options, sauvegarde | ✂ Niveau Casino à part | |
| | ✅ Trophées, classement local | ✂ Vagues de nuit | |
| | ✅ Décor soigné, singes animés | | |
| | ✂ Combo de vagues parfaites | | |
| | ✂ Bombes à lancer à la main | | |
| | ✂ Pluie de flèches de Quincy | | |

>! **Ce qu'on a coupé, et pourquoi.** Le 10 novembre, au gel des nouveautés, trois idées de la semaine 2 ont été fermées proprement (« not planned ») : le combo de vagues parfaites, les bombes à lancer et la pluie de flèches. Aucune n'était nécessaire à la boucle, et chacune demandait des tests au casque que nous n'avions plus le temps de faire. Le niveau Casino séparé est resté dans sa branche : la roulette et le blackjack tiennent dans la bananeraie.

## Tests et retours

Nous avons appliqué le **test du silence** : donner le jeu sans rien dire, noter les blocages, corriger le jeu plutôt qu'expliquer.

| Ce qu'on a observé | Ce qu'on a changé |
|---|---|
| **Les testeurs ne jouaient pas au tower defense** : les vagues étaient trop faciles, ils n'avaient aucune raison de poser et de fusionner des singes (23 oct.) | Vagues durcies (plus de ballons, de couches, plus serrés, boss dès la 12), **ballons camouflés** dès la vague 10, Punaise ramenée à 2,5 m |
| **Le tir à l'arc n'était pas intuitif pour tout le monde** | Tutoriel passé de 17 à 23 étapes : où est l'arc, comment tendre, un échauffement sur 3 ballons, « TENDS PLUS FORT ! » |
| **Les textes du tutoriel ne donnaient pas envie**, et on ne voyait pas toujours le tableau trembler | Textes réécrits sur le ton de Bloons ; annonces des grands moments ; « SUIS LA FLÈCHE DORÉE ! » quand une action est refusée |
| **Les ballons blindés étaient invincibles sans le dire** : l'arc de base et les singes Gris ou Vert ne leur faisaient rien | Les flèches percent le blindage ; un message prévient avant la première vague de blindés |
| **Le casino distrayait** au début : on pariait ses premières bananes | Casino fermé par des barrières jusqu'à la vague 5 |
| **La flèche du tutoriel traversait les meubles**, les ballons d'entraînement étaient cachés par un tableau | Flèche redessinée, contours dorés sur la porte et la bibliothèque, ballons au-dessus du chemin, tableau déplacé |
| **Le jeu était muet** (fin S1) | Sons, musiques et un motif de vibration par action (S2) |
| **Dans le build, le menu était tout rose** | Un matériau URP commun pour les objets créés en jeu |
| **Le hub n'était pas adapté à la VR** : meubles trop loin, textes qui flottaient (S1) | Tout en cercle à 2,8 m du joueur ; tout texte sur un support du décor |

## Technique

### Architecture
- **Trois scènes :** `Menu`, `Hub` (cabane et bananeraie) et `Labyrinthe` (la carte), ces deux dernières chargées ensemble. Il n'y a qu'un seul joueur : changer de niveau, c'est le déplacer. Le monde qu'on quitte continue de tourner.
- **Scènes générées par code :** le menu `SAE > Générer le prototype` reconstruit les scènes à partir des scripts et des modèles Blender. **Jamais de conflit de scène** entre nous trois.
- **Données centralisées :** `MonkeyData` (singes), `WaveBook` (les 100 vagues), `BowUpgrades` (l'arc), `Economy` et `GameState` (bananes, inventaire, singes posés), `SaveGame`, `Sounds`. Équilibrer le jeu, c'est changer un chiffre à un seul endroit.
- **Interactions :** une interface `IPressable` pour tous les boutons, qu'on touche (`HandPress`), qu'on vise (`RayPress`) ou qu'on clique (`DesktopPlayer`). Le tutoriel filtre ces appuis.
- **Tutoriel :** une machine à états (`Tutorial`), des tableaux abonnés à ses événements, une flèche (`TutorialRunner`) et des cibles que chaque objet pose lui-même (`TutorialTarget`).
- **Coop :** Netcode for GameObjects et Unity Relay (WebSocket sécurisé : passe le Wi-Fi de l'IUT). L'hôte fait tourner la partie ; les invités demandent (RPC) et reçoivent l'état (NetworkVariable, NetworkList).
- **Classement en ligne :** une petite API PHP et une base MySQL sur l'espace web de l'IUT.

### Optimisation pour le Quest
>! **72 images par seconde, constantes.** Le jeu est plafonné à 72 fps, la fréquence de l'écran du casque : aller au-delà ne servirait à rien. Au casque, il les atteint et les tient de façon constante, y compris pendant les vagues chargées.

Chaque modification de performance est notée dans `docs/opti.md` (problème, ce qui a été fait, avant / après).

<div class="cards c3">
<div class="card"><h5>RENDU</h5><p>Pas de post-traitement au casque, fovéation dynamique, qualité mobile, vue à 250 m, ombres coupées sur ballons et projectiles.</p></div>
<div class="card"><h5>MODÈLES</h5><p>Cabane : 97 700 → 68 300 triangles (rondins, sol en une dalle texturée, palmiers). Mesh LOD automatique, textures à 1 024 px.</p></div>
<div class="card"><h5>CODE</h5><p>Pools de ballons, projectiles et sons ; décor fusionné par couleur ; aura des singes en un seul maillage ; ciblage sans allocation.</p></div>
</div>

### Mode PC
Un mode clavier-souris permet de développer et de tester sans casque. Il sert à la **démo à plat** de l'oral, et un build Windows est publié (v1.0-pc).

## Production

{{TIMELINE}}

### Organisation
- **Suivi :** un tableau GitHub Project (À faire / En cours / En relecture / Fait), une issue par tâche avec sa priorité (DOIT, DEVRAIT, POURRAIT), un jalon par semaine.
- **Git :** `main` toujours stable, une branche courte par tâche, une Pull Request relue par tâche. En chiffres : **plus de 400 commits et 217 issues et Pull Requests**.
- **Journaux :** un journal d'équipe et un par personne, avec le tableau des usages de l'IA (outil, pour quoi, gardé ou jeté). Les demandes faites à l'IA sont gardées dans `prompts/`.

| Qui | Ce qu'il a porté |
|---|---|
| **Dylan** | Hub, carte, singes, tutoriel (base et finitions), menu, sauvegarde, classement, météo, coop réseau, blackjack, optimisations, suivi GitHub, intégration |
| **Maxens** | Bananeraie, bananes dorées, récolteurs, coffre, arc de Quincy, ballons et boss, sons, musiques et vibrations, roulette, tutoriel complet, casino fermé, coop partagé (bananeraie, coffre, fléchettes) |
| **Nicolas** | Améliorations de l'arc, première roulette, armoire à trophées |

### Risques

| Risque | Parade |
|---|---|
| Chute de fps aux vagues hautes (centaines de ballons, orage) | Pools, maillages fusionnés, pluie réduite au casque : les 72 fps sont tenus |
| Équilibrage des 100 vagues | Une formule et des données centralisées : on règle en quelques minutes |
| Coop ajouté tard (fin de semaine 2) | Le jeu est complet en solo ; le coop reste un bonus |
| Mode développeur du casque qui saute | Partage de connexion, date et heure automatiques |
| Droits de l'univers Bloons | Habillage remplaçable pour une diffusion |

## Bilan

!![5 oct. : trois prototypes réunis, en cubes.](integration-hub-1.webp) | ![7 oct. : la première cabane.](cabane-v1.webp) | ![Aujourd'hui.](n-cabane-interieur.png)

<div class="cards c3">
<div class="card"><h5>CE QUI A MARCHÉ</h5><p><b>Prouver le geste d'abord.</b> L'arc, la pose et le panier marchaient en cubes gris dès la semaine 1. <b>Les scènes générées par code</b> : trois personnes, zéro conflit de scène. <b>Les données au même endroit</b> : durcir 100 vagues a pris une soirée.</p></div>
<div class="card"><h5>CE QUI A ÉTÉ DUR</h5><p><b>Tester au casque chaque jour</b> : les tests arrivaient souvent par lots, après plusieurs changements, au lieu d'un test par changement. <b>Le périmètre</b> : le coop et le casino sont arrivés le dernier jour de la semaine 2, contre notre propre règle. <b>Le son</b>, attaqué trop tard.</p></div>
<div class="card"><h5>CE QU'ON REFERAIT AUTREMENT</h5><p>Faire tester par d'autres <b>dès la semaine 1</b> : c'est seulement le test du 23 octobre qui a montré que les joueurs ne posaient presque pas de singes, parce que les vagues étaient trop faciles. Couper plus tôt. Mesurer les fps au casque avant d'optimiser.</p></div>
</div>

<div class="end">MERCI, ET BON TIR !</div>
