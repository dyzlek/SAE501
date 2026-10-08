# Prompts de Nicolas

_Les demandes les plus utiles faites à l'IA, **reformulées et corrigées** (orthographe, clarté). Le sens est conservé. La plus récente est en haut. Le détail de ce qui a été gardé ou jeté est dans [mon journal](../docs/journal/nicolas.md)._

## Jeu. 8 oct. 2026

**Roulette de casino.** Crée un système de roulette qui reprend le fonctionnement et les multiplicateurs de la roulette d'un casino classique.
- Pour le bêta-test, place-la à l'opposé du pupitre d'amélioration de l'arc, pour pouvoir l'essayer facilement.
- Pour miser, il y aura des boutons pour ajouter ou retirer des bananes ; on peut miser à tout moment, à partir d'une banane.
- Toujours pour le bêta-test, un bouton garantit à 100 % la mise placée, pour vérifier que les récompenses fonctionnent vraiment.

Suite : le pupitre avec PARI et NUM prend trop de place pour rien, retire-le ; garde seulement MISE +, MISE -, LANCER et 100 %. Pour choisir son numéro et sa couleur, agrandis la roulette et mets-la sur le côté du pupitre, pas derrière. On clique simplement sur la case voulue : une lueur apparaît autour pour confirmer le choix. Si ce n'est pas la bonne, on clique sur une autre, qui la remplace. La lueur reste jusqu'à ce qu'on lance la roue.

Suite : pas de numéro, on parie uniquement sur le noir, le rouge ou le vert. Ajoute un bouton pour tout miser. Dès le début, la mise est à 1. Le bouton pour miser plus passe de +1 à +10, et celui pour miser moins de -1 à -10. En m'inspirant du jeu *How to Fish* : à côté de la roulette, une zone montre des bananes selon la mise (de 1 à 25 : 1 banane ; de 25 à 50 : 2 bananes). Au maximum 6 bananes, pour ne pas surcharger la zone : adapte les paliers à partir de mes valeurs pour 3, 4, 5 et 6 bananes.

Suite : retire les numéros de la roulette. Un scénario rend les boutons et la conservation des mises pénibles : pour le test, j'ai tout misé sur le vert avec le 100 %, ce qui m'a donné énormément de bananes (environ 1000), puis j'ai retout misé et tout perdu. Comme la mise précédente reste affichée et qu'elle était immense (2000), il fallait cliquer pendant des minutes pour miser seulement 20. Donc, quel que soit le résultat, gagné ou perdu, la mise suivante sera de 1 par défaut.

## Mer. 7 oct. 2026

**Refonte de l'arc.** Que penses-tu de ma refonte de l'arc pour notre projet ?
Pour l'instant, la flèche transperce tout : dès qu'elle touche une cible, elle doit être détruite. C'est pour préparer les améliorations. Place les boutons pour les tester sur la carte, entre l'herbe et la plateforme grise ; pour le bêta-test, les améliorations sont gratuites, mais il faut déjà prévoir les prix.
- **Flèche perforante**, 5 paliers de plus en plus chers : elle traverse plusieurs ballons et plusieurs couches (certains ballons, comme les bleus, en ont plusieurs).
- **Tir triple** : trois flèches partent en même temps, à l'horizontale.
- **Flèche explosive**, 5 paliers : au contact d'un ballon, une onde éclate ceux autour. Au palier 1, elle touche le ballon visé et ses deux voisins (3 en tout) et perce une couche ; chaque palier augmente le rayon et les couches percées.

Retire le système de « dégâts » : la perforation des couches, ce sont les dégâts. Ajoute aussi une prévisualisation de la trajectoire quand on bande l'arc, et un petit effet de vitesse sur la flèche.

Suite : d'accord pour convertir les dégâts des singes en perforation et pour que la couche blindée coûte 2 de perforation ; les améliorations se cumulent. Récupère le `main` à jour, remplace mon dossier local par celui-ci, puis code les améliorations ; on créera la branche `feat/arc-upgrade` une fois que j'aurai validé.

Suite : ça marche, mais je me suis trompé. « Transperçante » n'est pas la même chose que « perforante » : la perforante, ce sont les « dégâts » (les couches percées), et la transperçante, c'est le fait de toucher plusieurs ballons d'un coup, comme un tir collatéral. Ajoute une amélioration de plus pour ça.

## Lun. 5 oct. 2026

_(à compléter)_
