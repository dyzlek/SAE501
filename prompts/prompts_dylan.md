# Prompts de Dylan

_Les demandes les plus utiles faites à l'IA (Claude Code), **reformulées et corrigées** (orthographe, clarté). Le sens est conservé. La plus récente est en haut. Le détail de ce qui a été gardé ou jeté est dans [mon journal](../docs/journal/dylan.md)._

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
