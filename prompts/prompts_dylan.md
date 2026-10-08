# Prompts de Dylan

_Les demandes les plus utiles faites à l'IA (Claude Code), **reformulées et corrigées** (orthographe, clarté). Le sens est conservé. La plus récente est en haut. Le détail de ce qui a été gardé ou jeté est dans [mon journal](../docs/journal/dylan.md)._

## Jeu. 8 oct. 2026

### Arrivée sur la carte et animations des singes
> Améliore cet endroit (l'arrivée sur la carte, voir la capture) et le portail, et ajoute des animations pour chaque singe.

### Portails et bac à sable
> Ajoute un portail au début et à la fin du chemin pour montrer le sens. Je voudrais aussi commencer les animations des différents singes : passe tous les singes en débloqués et gratuits, juste pour les voir.

### Chemin des ballons
> Améliore le chemin des ballons, en t'inspirant des vraies cartes du jeu (Bloons TD).
> Suite : rien n'a changé ? Je veux quelque chose de stylé, comme cette carte de Bloons (chemin de dalles qui se croise, herbe, fleurs). Le sol doit être comme l'herbe de base, avec du décor.
> Suite : juste ces trucs (les buissons) sont bizarres.
> Suite : j'aimerais qu'on puisse mettre les singes deux par deux, comme sur ma capture : singes plus serrés, et plus de place dans les boucles.

### Fiche du singe et coffre
> Quand on appuie sur A, je veux que les informations du singe soient plus lisibles, pas juste des infos données comme ça : à gauche, on voit le singe avec son aura, etc. Et quand on ouvre le coffre, ça doit respecter la DA globale.

### Retours du test au casque (PR #95 à #97)
> Le saut, le point d'apparition, la téléportation sous le plateau, le déblocage des singes avec des bananes et les bananes qui pourrissent, c'est bon. L'arc ne se spamme plus, mais je voudrais 0,3 s entre deux tirs. Sur la carte, on peut encore se téléporter dans les arbres. La traînée marche, mais en VR on ne peut pas prendre les fléchettes de loin pour les lancer sur la cible du hub. Beaucoup d'éléments du décor se chevauchent (voir les captures) : je veux des montagnes vraiment plus belles, et le reste un peu plus réaliste. J'aimerais aussi que les flammes soient un peu animées. Je n'ai pas de sensation de lag, mais il faudrait afficher le nombre de fps en VR pour vérifier.

## Mer. 7 oct. 2026

### Urgences casque, singes, bananes, décor
> Fais : enlever le saut en VR (#55), arc sans spam (#60), point d'apparition (#65), téléportation sous le plateau (#67), téléportation hors de la carte (#81), débloquer les singes avec des bananes (#52), traînée derrière ce qu'on lance (#28), animation des bananes qui pourrissent (#75), champignons et montagnes, murs du hub (#61, #66), lumière (#77).

### Singes récolteurs à lancer, fléchettes, coffre, caisse sur la carte
> Suite : on ne peut toujours pas prendre les fléchettes ; et quand je lance le singe, il doit atterrir au plus proche de l'endroit où il peut marcher (là où on se téléporte en VR), pas au point de départ.
>
> Suite : je ne peux pas prendre les singes ni jouer aux fléchettes ; il faut aussi que ça marche en mode PC (sans VR).
>
> Reformule « Lancer les singes pour les poser » (#27) : on peut prendre les singes récolteurs (avec une petite animation) et les jeter un peu partout pour rigoler, et fais-le. Fais aussi : le récolteur porte les bananes devant lui (#76), la cible de fléchettes jouable (#74), plus de bananes visibles dans le coffre (#56) et voir combien de bananes on a, aussi sur la carte (#64).

### Arc, bananes lancées, vague, portée
> C'est tout bon : ajoute la capture dans mon journal. Puis fais : « Flèche explosive : effet d'étincelles », « Vraie physique des bananes : on les lance dans des paniers (sans poignée) », « Tracé de la flèche : autre couleur que blanc (voir la trajectoire qu'elle aura) », « Ne pas annoncer le contenu de la vague », et afficher la portée quand on s'apprête à poser un singe sur le plateau. Mets-moi comme responsable.

### Textes du coffre et des singes, visuel de fusion
> Dans une autre branche, fais : « Enlever le texte au-dessus du coffre », « Carte en taille réelle : enlever les textes au-dessus des singes » et « Fusion : remplacer le cercle rouge par un visuel "fusion possible" ».

### Ranger le tableau GitHub
> Vérifie tout ce que j'ai fait sur le tableau GitHub et corrige ce qui ne va pas.
>
> Suite : il y a des objectifs de la semaine et des petites modifications au même niveau, ça n'a pas de sens : sépare-les (chantiers et tâches).

### Point de fin de journée
> Mets à jour le README et le journal général, et liste ce qu'il faut faire en urgence demain.

### Critiques de fin de journée et envoi sur main
> Pousse sur `main`, et ajoute la critique de Maxens et la mienne (listes de remarques de fin de journée).
>
> Suite : crée les issues GitHub de toutes ces remarques, et fais le reste.

### Téléportation sur la carte
> Il y a des bugs de téléportation, surtout sur la carte (là où il y a l'arc) : je ne peux pas me téléporter n'importe où, ce qui n'est pas cool. Corrige-les.
>
> Suite : je pense qu'il y a vraiment un bug : en bas, il y a l'indicateur pour se téléporter à un endroit, mais quand je lâche, on ne se téléporte pas.
>
> Suite : vraiment tout est buggé en VR (par exemple quand on tourne), patche tout, et corrige aussi ce qui ne va pas dans les menus SAE.

### Mes captures et tout sur main
> Mets mes captures dans mon journal et pousse tout sur `main`.

### Captures de l'arc dans le journal de Nicolas
> Mets à jour le journal de Nicolas avec ses captures : le tableau des améliorations de l'arc (pour le bêta-test), la prévisualisation du tir, le tir triple et le tir explosif. Ajoute aussi `docs/organisation-github`.

### Revenir au hub pendant une vague
> Quand je lance la vague, j'aimerais pouvoir quand même revenir au hub depuis l'autre niveau, etc.
>
> Suite (mon choix) : la vague continue pendant que je suis au hub.

### Branche fusion avec le travail de Maxens et Nicolas
> Dans une branche `fusion`, ajoute ce que Maxens et Nicolas ont fait de nouveau.

### Rendre les deux scènes belles
> Rends les deux scènes belles, sans toucher encore au labyrinthe.
>
> Suite (précision) : j'aimerais des rayons de soleil, et un peu plus de végétation autour du hub et du labyrinthe.
>
> Suite : ce n'est pas mal, mais dans la maison c'est trop artificiel et abusé.
>
> Suite : ça traverse encore, et ce n'est pas encore centré.
>
> Suite : toujours pas centré, et la lumière aux fenêtres n'est pas réaliste, donc améliore. Essaie de rendre le jeu vraiment beau avec la lumière, je te laisse faire.

### Zone de téléportation du hub et deux vraies scènes
> Fais maintenant « limiter la zone de téléportation dans le hub » et « séparer vraiment les deux niveaux : le hub et le labyrinthe ».
>
> Suite (mes choix) : deux vraies scènes ; dans le hub, se téléporter seulement au centre.
>
> Suite : regarde pourquoi je vois seulement une prairie et des montagnes au loin (capture).
>
> Suite : je veux vraiment deux scènes, au sens où on le demande de base (un vrai changement de scène).

### Organiser le projet sur GitHub
> Aide-moi à mettre en place le planning et l'organisation du projet sur GitHub, car nous serons notés sur l'organisation. Ne touche à rien dans le dossier du projet (une autre IA est en train de modifier Unity) : travaille seulement sur GitHub et dans une autre branche.
>
> Suite : n'envoie aucune notification par mail. Fais en sorte que chaque IA qui travaille sur le projet mette à jour le suivi GitHub automatiquement.

### Bilan du jour et plan de demain
> Ajoute ce qu'il faudrait faire demain et mets à jour ce qu'on a fait dans le GENERAL.md : chercher des assets de boutons ou les faire ; prendre les singes et les jeter ; faire un indicateur avec un effet de traînée ; ajouter la physique sur les bananes (faire des paniers, et donc enlever la poignée).

### Fusionner le coffre de Maxens et lancer les vagues en VR
> Fusionne `feat/coffre` (le coffre de Maxens) avec ce que j'ai fait, et corrige le fait que je ne peux pas faire apparaître les ballons simplement en VR. Attends avant de mettre ça sur `main`.

### Fiches des singes, textures qui se chevauchent, chute dans le vide
> Améliore les fiches des singes (il faut aussi régler le fait qu'elles soient cachées par les meubles), corrige les textures qui se chevauchent selon l'angle (par exemple les bûches du mur sur la fenêtre), et le fait que quand on tombe dans le vide hors de la carte, rien ne nous remet en place.

### La critique de Nicolas dans son journal
> Ajoute dans le journal de Nicolas sa critique (texte de la bibliothèque, collision du singe récolteur, fiche du singe cachée par la bibliothèque, textures qui se chevauchent, herbe de la carte, chute dans le vide, bouton LANCER à griser, singes en T-pose, arc, portée et stats des singes sur le plateau), avec ses 2 photos.

### Corriger une partie des critiques
> Pour l'instant, corrige seulement ces points : les objets qu'on peut faire voler avec la banane ; les objets qui se superposent (les caisses rentrent dans l'armoire) ; le petit singe récolteur qui rentre dans les objets ; l'affichage des écrans, pas beau même s'il est lisible ; le texte qui flotte dans le vide (il faudrait une pancarte, et le mot « Bibliothèque » tourne vers le joueur) ; les textes trop simples (bibliothèque et récolteur) ; les panneaux et certains objets pas beaux ; les bornes d'amélioration pas belles et les boutons jaunes du récolteur. Pour le meuble du récolteur, enlever seulement le bouton quand il est au maximum, et permettre d'avoir plusieurs singes ; pareil pour le bananier. Refaire le bananier et toute sa zone, les boutons LANCER et JOUER (horribles), et enlever le tonneau.
>
> Suite (après mon test) : certains meubles sont trop collés ; les boutons sont trop gros, pas beaux, et ils dépassent ; on voit mal les textes de la bibliothèque ; la pancarte au-dessus des améliorations flotte ; on ne comprend pas à quoi servent certains boutons ; remplace JOUER par « SE TP ».
>
> Suite : il manque juste à enlever le « +0 » qui s'affiche et à améliorer la lisibilité du texte.

### Ma critique et les objectifs du jour, dans une branche fix-all
> Dans une branche `fix-all`, mets d'abord ma critique (mon fichier Word « analyse critique ») et les objectifs qu'on s'est fixés aujourd'hui : analyse critique et bugs (tout le monde, Dylan corrige) ; coffre : changer l'asset, améliorer l'animation, voir les singes (Maxens) ; limiter la zone de téléportation dans le hub (Dylan) ; améliorer l'arc (Nicolas) ; séparer vraiment les deux niveaux, le hub et le labyrinthe (Dylan) ; améliorer le labyrinthe (Dylan) ; mettre les assets des ballons MOAB (Maxens).
>
> Suite : ajoute aussi la critique de Maxens (bibliothèque, récolteur, bananier, boutons LANCER/JOUER, caisses, tonneau, carte et montagnes).

### Journaux à jour avec mes captures
> Mets à jour mon journal et le GENERAL.md (avec mes 3 captures du hub et de la carte).

### Fusionner la cabane et l'arc de Maxens
> Fusionne ce que j'ai fait dans `test/cabane` avec la dernière branche de Maxens.
>
> Suite : c'est bien `feat/arc-quincy` sur GitHub (prends sa dernière version).

## Mar. 6 oct. 2026

### Même DA partout : coffre, boutons, montagnes, lisibilité
> J'aimerais que le coffre corresponde au style et que les boutons soient plus intégrés au décor. Ajoute des montagnes et d'autres éléments autour de la maison pour la déco : je veux que tout soit dans la même DA. Améliore aussi la lisibilité de certains éléments.

### Cabane : fais encore mieux
> Fais encore mieux, dépasse-toi.

### Cabane plus serrée et vraiment belle
> J'aimerais encore un peu moins d'espace. Tu peux changer certains éléments, comme les bibliothèques. Je veux que ce soit adapté à la VR, mais vraiment beau, semi-réaliste comme dans le vrai jeu (Bloons TD 6). Surprends-moi.

### Hub dans une cabane en bois (branche de test)
> Dans une branche de test, j'aimerais que tout ce qu'il y a dans le hub soit mis dans une sorte de cabane en bois fermée, beaucoup plus serrée, avec un tapis au sol. Essaie de tout rapprocher pour que ce soit adapté à la VR.

### Panier à côté du plateau, le récolteur va chercher partout
> J'aimerais vraiment que le panier soit à côté du plateau où on pose les singes. Et quand on prend une banane et qu'on la pose ailleurs, le singe va la chercher.

### Récolteur plus lent et plus drôle
> J'aimerais qu'il soit beaucoup plus lent de base, que le panier soit plus loin, vers le plateau, et quelque chose de drôle : parfois il dunke, parfois il rate, etc.

### Retouches du singe récolteur
> Parfait, sauf un truc : écarte le panier des autres objets. Sur le singe, grossis simplement son visage et enlève sa fléchette.

### Singe récolteur de bananes
> J'aimerais pouvoir acheter le service d'un singe qui ramasse les bananes et les met dans le panier. Fais-le gratuit pour le moment, ses améliorations aussi (vitesse de déplacement, etc.). Fais toutes les animations : il marche vers une banane, la porte, se rapproche du panier et la jette dedans. Utilise le singe classique.

### Prix de « Vider » affiché, singe tenu de côté (PC) ou dans la main (VR)
> J'aimerais que le prix pour vider soit affiché. Et quand on prend un singe, en mode PC il doit se mettre sur le côté, et en VR directement dans la main.

### « Vider » payant, plus de texte sur le plateau
> Appuyer sur « Vider » doit coûter des bananes. J'aimerais aussi qu'il n'y ait plus de texte sur le singe quand je l'ai posé sur le plateau.

### Le singe vient vers nous
> J'aimerais que le singe vienne vers nous quand on le prend de loin.

### Prendre les singes de loin (ça ne marche pas)
> J'aimerais pouvoir prendre les singes de loin, car ça ne marche pas.

### Viser vraiment le singe pour interagir
> Corrige juste ça : je veux qu'il faille pointer sur le singe pour interagir (sur mes captures, le rayon est bleu et interagit alors qu'il passe à côté).

### Prendre les singes et les bananes de loin (2 m)
> J'aimerais qu'en mode PC, et aussi en VR si ce n'est pas déjà le cas, je puisse prendre les singes et le reste de loin, à environ 2 m.

### La VR dans le travail de Maxens, avec un mode PC
> Maintenant, prends ce qu'il a fait (commit 1cc56b1) et implémente la VR dedans. Ajoute juste une option pour jouer en VR ou non, pour tester vite fait.

### Plus adapté à la VR : vies, rayon et interactions de loin
> Ce n'est pas encore adapté à la VR. D'abord, j'aimerais que la vie se réinitialise à chaque niveau. Ensuite, le rayon n'est pas sur les manettes : j'aimerais pouvoir poser les singes sur la carte de loin, et qu'un rayon lumineux parte devant quand on vise, pour appuyer sur les boutons, récupérer les bananes de loin, etc.

### Textes en VR et boutons à distance
> Je peux me déplacer, mais tous les textes s'affichent mal, et peu de choses sont adaptées à la VR. J'aimerais aussi pouvoir interagir avec les boutons un peu à distance. Je te laisse faire.

### Chantier E : la VR, les gestes de base au casque
> Maintenant, mettons la VR :
> - le joueur VR : rig XR avec téléportation et rotation par crans, et le simulateur XR pour continuer à tester sans casque ;
> - prendre un singe sur l'étagère avec la main (XR Grab) ;
> - le lâcher au-dessus du plateau, ce qui le pose, avec l'aperçu vert ou rouge sous la main ;
> - le lâcher sur un singe identique, ce qui les fusionne, avec le halo blanc ;
> - les bananes : les prendre et les lâcher dans le panier (le prefab est déjà prêt pour la VR). Pour le confort, on évite de ramasser au sol en boucle : les bananes tombent sur un plateau à hauteur de main ;
> - les boutons (Jouer, lancer la vague, améliorations) s'enfoncent avec la main ;
> - un premier build sur le casque, au plus tard en début d'après-midi.

### Branche pour la VR et schéma du hub
> Je vais m'occuper de l'ajout de la VR dans une branche `feat/ajout-vr`. Avant ça, regarde ce que j'ai fait et ajoute l'image de mon schéma du hub.

### Guide de bonnes pratiques tiré des cours
> Récupère toutes les informations importantes de mes supports de cours (comment bien structurer son code, etc.) et mets-les soit dans le CLAUDE.md, soit dans un autre fichier .md que tu liras quand tu coderas dans le projet.

### Fiche du singe sur un bouton
> J'aimerais avoir les infos seulement quand j'appuie sur un bouton, par exemple A.

### Vrai système de vagues et fiche des singes
> Maintenant, j'aimerais que tu fasses un vrai système de vagues, et qu'on puisse voir les caractéristiques des singes, etc.

### Singe de départ et chances par type de singe
> Il faudrait qu'on puisse avoir un singe Classique dès le départ. Mais j'aimerais aussi ajouter des probabilités de drop des personnages : au début, seulement le Classique, puis petit à petit les autres, etc.

### Chantier B : un vrai inventaire
> - La bibliothèque démarre vide. Chaque case affiche le nombre de singes possédés, et une case vide est grisée.
> - Le coffre ajoute le singe gagné à l'inventaire. Le singe sort du coffre avec la couleur de sa rareté, puis va se ranger sur l'étagère.
> - Poser un singe le retire de l'inventaire, le reprendre l'y remet, et fusionner en consomme 2 pour en créer 1.
>
> J'aimerais aussi qu'une aura de la couleur de sa rareté soit autour de lui.

### Corriger ma deuxième analyse critique
> Maintenant, essaie de résoudre tout ce que j'ai relevé.

### Deuxième analyse critique (à noter)
> Ajoute que j'ai refait une analyse critique :
> - il faut un bouton pour lancer la vague manuellement ;
> - si on perd une vague, on ne recommence pas de zéro, mais à partir de cette vague ;
> - à chaque vague, il y aura un ou plusieurs boss (des ballons plus gros) ;
> - les raretés plus élevées du coffre se débloqueront plus tard, par exemple après la vague 20, etc. ;
> - on ne peut pas obtenir les singes blancs et arc-en-ciel dans le coffre ;
> - le nombre de vies restantes et la vague actuelle ne doivent pas être affichés à l'écran, car une interface à l'écran en VR donne la nausée.

### Hub en vrai cercle, d'après mon schéma
> C'est plus espacé, mais pas assez. J'ai dessiné une idée : que tout suive vraiment un cercle. Recule le plateau et reprends mon schéma (avec plus d'espace) : le plateau devant avec JOUER à côté, les deux bibliothèques sur les côtés, le coffre, le bananier au fond avec le panier à côté, et une zone où tombent les bananes.

### Analyse critique du hub (à noter, pas encore à corriger)
> Note mon analyse critique, je te dirai quand tout corriger :
> - le hub est trop petit : on est serré, les éléments sont les uns devant les autres, c'est difficile de circuler et d'utiliser les objets ;
> - le coffre doit devenir plus cher en fonction des vagues vaincues, mais aussi meilleur : plus de singes, et de meilleur niveau ;
> - je veux voir les probabilités de drop ;
> - les bananes ne doivent pas traverser le bananier, sinon elles restent cachées à des endroits inaccessibles.

Suite : « Ne pousse pas, et corrige tout ce que j'ai relevé pour le moment. »

### Économie unique, sans interface à l'écran
> On est le 6. Pour commencer, il faudrait agrandir l'asset du palmier. Ensuite, réalise quelque chose de stylé pour le chantier A (une seule économie avec de vrais coûts) :
> - une seule bourse, `GameState.Money` : on supprime le `Wallet` du coffre et le pont entre les deux ;
> - le coffre coûte de l'argent, avec un prix qui augmente ;
> - améliorer le bananier coûte de l'argent, avec un panneau d'amélioration près de l'arbre et 3 boutons à appuyer ;
> - ce qui rapporte : les bananes et chaque vague finie, pas les ballons éclatés ;
> - l'argent est affiché dans le décor, près du panier.
>
> Je ne veux pas d'interface à l'écran pour la suite, parce qu'en VR ça donne le vertige : dans le hub, on doit pouvoir voir l'argent total et le reste directement dans le décor.

## Lun. 5 oct. 2026

### Plan du lendemain
> Planifie ce qu'il faut faire demain. Objectif du prototype : pouvoir y jouer normalement avec les mécaniques de base (poser des singes, les fusionner en VR, obtenir des singes, améliorer le bananier, récupérer des bananes…). Il faut relier tout ce qu'on a fait : un vrai système d'économie, un vrai inventaire, une meilleure lisibilité, un meilleur système de vagues, et commencer l'arme.

Suite : « Ajoute dans le journal général ce qu'il faut faire aujourd'hui : les points à harmoniser (raretés, prix, sens de "drop") et les chantiers A, B, D et E. »

### Fusion dans main
> Tu peux mettre l'intégration directement sur `main`.

### Corriger les défauts du hub intégré
> Avant de proposer la fusion dans `main`, corrige les trois défauts visibles sur mes captures : la bibliothèque de gauche vue de travers, le coffre qui n'est pas tourné vers le joueur, et le panier collé au bananier.

### Un seul menu
> On ne garde que le hub : je ne veux plus que le menu « SAE » dans Unity. Enlève aussi les scènes de test de Nicolas et de Maxens.

### Hub en cercle avec le bananier et le coffre
> Dans le hub, les éléments importants doivent être disposés en cercle autour du joueur. Ajoute le bananier (avec le panier à côté) et le coffre à ce que j'ai fait, de façon ordonnée, en rond. Je te laisse décider de la disposition.

### Prompts, consignes et README
> Analyse tous les prompts que je t'ai donnés, garde les plus pertinents, reformule-les et corrige-les, puis mets-les dans `prompts/prompts_dylan.md`. Ajoute dans le `CLAUDE.md` que l'IA doit faire la même chose pour chacun de nous trois. Rédige aussi le `README.md` avec la répartition actuelle des rôles et le journal commun du jour.

### Retours de Maxens dans son journal
> Avant de faire des modifications, ajoute dans le journal de Maxens ses commentaires sur nos prototypes (texte copié depuis Discord).

### Fusion des trois projets
> J'aimerais fusionner tous les projets ensemble. Avant de le faire, explique-moi comment on pourrait s'y prendre.

Suite : « Une branche d'intégration, c'est une bonne idée. On part sur Unity 6000.6. »

### Analyse des prototypes de mes camarades
> Ajoute dans mon journal que j'ai analysé les réalisations de mes camarades.
> - **Nicolas** : l'idée de la roulette qui défile pour afficher la rareté est bonne, mais l'animation du coffre fait trop maladroite. Quand la rareté gagnée est sélectionnée, j'aimerais que le singe sorte du coffre avec la couleur de cette rareté.
> - **Maxens** : rien à redire, le prototype est simple et efficace.

### Tester les prototypes des autres
> Lance le prototype de Nicolas, puis celui de Maxens, pour que je puisse les tester et en faire une analyse critique, sans toucher à ma version.

### Retour vers la bibliothèque en étagères
> Note dans mon journal que la disposition en cercle avec 7 socles était une proposition alternative, non retenue. Reviens à la bibliothèque d'avant, agrandie de 1,1, en restant facile d'accès pour un petit joueur en VR (deux meubles, pas collés). Le plateau incliné me plaît, mais son support en bois le traverse : corrige-le. Attends avant de pousser.

### Corriger les points de mon analyse critique
> Corrige tous les problèmes de mon analyse critique :
> - en VR, les éléments doivent être disposés autour du joueur, à son point d'apparition, et inclinés vers lui ;
> - la bibliothèque est peu lisible, il faut une alternative ;
> - pour fusionner, l'aperçu doit montrer qu'on ne peut pas poser, et un halo blanc doit apparaître sur le singe déjà posé ;
> - on doit pouvoir prendre et déplacer les singes directement sur la carte.

### Plateau = la carte en direct, placement libre
> Je veux que le plateau et la carte montrent vraiment la même chose : si un deuxième joueur est sur la carte, on doit le voir sur le plateau. Et le placement des singes doit être libre, tant qu'on ne les pose pas hors de la carte ou sur le chemin.

### Premier prototype (sans VR)
> Je commence le prototype. Je veux des cubes de la même famille, de niveaux différents, avec les couleurs de rareté du coffre (gris, vert, bleu, violet, jaune, rouge, arc-en-ciel, blanc). Fais une piste en labyrinthe. À gauche, une bibliothèque où je vois tous mes singes, à tous les niveaux (il n'y a pas encore de coffres). Devant moi, un plateau façon échiquier, assez grand. À droite, un cube qui m'envoie directement sur la carte : le hub et la carte sont deux niveaux différents, et dans le hub on peut seulement voir et modifier certaines choses. Ça doit rester un prototype : je veux les mécaniques, pas encore la VR (pas de casque disponible aujourd'hui).

### Tags et captures d'écran
> Ajoute des tags (aux ballons, etc.), pousse cette version sur une branche, et fais une première capture d'écran pour montrer à quoi ça ressemble.

Suite : le mode démo Windows créé pour les captures a été retiré à ma demande.

### Organisation Git
> Pousse sur une autre branche. Comment tu organiserais les branches, toi ?

### Journal de bord
> Mets en place un journal de bord : un journal général et un journal pour chacun de nous.
> Chaque modification du projet faite par l'IA doit être notée dans le journal. On travaille à trois avec Claude : Dylan, Maxens et Nicolas.

### Idée du jeu
> Voici notre idée de jeu : un tower defense dans l'univers de Bloons, avec des coffres pour débloquer les singes, un bananier, la fusion des singes (2 identiques donnent le niveau suivant), Quincy jouable à l'arc et des ballons de plusieurs catégories.

### Lancement de la SAÉ
> Analyse et résume les consignes et les conseils du support de lancement, pour qu'à chaque nouvelle session l'IA relise l'objectif de la SAÉ. Prépare aussi le dépôt GitHub.
