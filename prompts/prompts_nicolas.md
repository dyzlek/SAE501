# Prompts de Nicolas

_Les demandes les plus utiles faites à l'IA, **reformulées et corrigées** (orthographe, clarté). Le sens est conservé. La plus récente est en haut. Le détail de ce qui a été gardé ou jeté est dans [mon journal](../docs/journal/nicolas.md)._

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
