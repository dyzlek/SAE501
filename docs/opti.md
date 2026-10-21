# Journal des optimisations

Le jeu doit tenir **72 images par seconde sur le Meta Quest** (règle de confort de la SAÉ, issue [#35](https://github.com/dyzlek/SAE501/issues/35)). Ce fichier garde une trace de **chaque modification faite pour les performances**. C'est utile pour l'oral (volet technique, Davide Di Pierro) et pour la note « assets 3D et optimisation » (Ilyasse Lojdi).

## Comment noter une optimisation
Une entrée par modification, **la plus récente en haut**, avec toujours les mêmes parties :

1. **Le problème** : ce qui coûtait cher, et comment on l'a vu (fps, Profiler, Frame Debugger, intuition).
2. **Ce qui a été fait** : le changement, les fichiers, la branche ou la PR.
3. **Avant / après** : des chiffres mesurés de la même façon (même scène, même endroit, même vague). Sans mesure, écrire « à mesurer » et ne pas conclure.
4. **Est-ce que ça a vraiment marché ?** Oui, non ou partiellement, avec le gain réel, et ce qu'on a perdu en visuel ou en jeu.
5. **Suggestions** : ce qu'on pourrait faire ensuite sur le même sujet.

### Comment mesurer (toujours pareil)
- **Au casque :** le compteur de fps au poignet gauche (`Player/FpsCounter.cs`, vert à partir de 72), dans un build de développement. On note les fps au même endroit, en regardant dans la même direction.
- **Dans l'éditeur :** *Window → Analysis → Profiler* (CPU et Rendering : *Batches*, *SetPass calls*, *Triangles*) et *Frame Debugger* (le détail des appels de dessin). Les chiffres de l'éditeur ne sont pas ceux du casque, mais ils montrent si on va dans le bon sens.
- **Endroits de test :** (1) au hub, face à la bibliothèque ; (2) sur la carte, au point d'arrivée, pendant une vague.

---

## Limite de 60 singes posés
**Mer. 21 oct. 2026** · Dylan (avec Claude) · branche `feat/opti-aura`

**Le problème.** On pouvait poser autant de singes qu'on voulait, et chaque singe coûte à chaque image : son modèle animé, son aura, la recherche d'une cible et ses projectiles. Il existe en plus **en double**, parce qu'il est visible sur la carte et en miniature sur le plateau du hub. En fin de partie, le nombre de singes pouvait grimper sans limite et faire tomber les fps.

**Ce qui a été fait.** `Placement.MaxPlaced = 60` dans `Core/Placement.cs`. Plateau plein, le fantôme devient rouge et un texte « PLATEAU PLEIN (60) · Fusionne tes singes ! » s'affiche au-dessus (`Core/PlacementSurface.cs`). On peut toujours **fusionner**, puisque ça ne fait pas un singe de plus, et reprendre un singe posé. La limite devient un choix de jeu : il vaut mieux fusionner que d'empiler.

**Avant / après.** *À mesurer.* Il faudra noter les fps au casque, sur la carte pendant une vague, avec 60 singes posés (en bac à sable). C'est le pire cas maintenant garanti.

| Mesure | Avant | Après |
|---|---|---|
| Singes posés au maximum | illimité | 60 |
| fps au casque, 60 singes, vague en cours | ? | ? |

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
| fps au casque, face à la bibliothèque | ? | ? |
| fps au casque, carte pendant une vague | ? | ? |
| Batches (Profiler, hub) | ? | ? |
| Particules vivantes (hub) | ~ 56 × 80 | 0 |

**Premier test dans Unity (Dylan, 21 oct.).** Seule la colonne arc-en-ciel avait son aura. C'était un bug de cache : les maillages sont gardés dans un dictionnaire statique, et sans rechargement du domaine (*Enter Play Mode* rapide), ceux de la partie d'avant étaient détruits mais toujours renvoyés. L'arc-en-ciel était reconstruit, parce que son test vérifiait le maillage détruit. Corrigé : le cache est vidé au lancement, et un maillage détruit est refait. Dylan trouvait aussi l'aura trop simple (un cône plat) : v2 avec des langues de flamme irrégulières, un cœur blanc et une rotation lente, toujours en un seul maillage.

**Est-ce que ça a vraiment marché ?** *Pas encore mesuré.* On s'attend à un vrai gain au hub. À vérifier aussi : que l'aura reste lisible (la rareté se reconnaît toujours à sa couleur), et que l'arc-en-ciel, en dégradé de teintes autour du tube, se distingue bien.

**Suggestions.**
- Si les *Batches* restent hauts : activer le **GPU Instancing** sur le matériau, ou fusionner les auras de la bibliothèque (elles ne bougent pas).
- Dans la bibliothèque, les auras pourraient ne battre que pour le singe que la main survole.
- L'ancien prefab `Prefabs/Aura.prefab` ne sert plus. On peut le supprimer, avec le champ `auraPrefab` de `MonkeyVisuals`, une fois l'aura validée au casque.
- Prochaines pistes à mesurer : la pluie de l'orage (`Map/Weather.cs`, jusqu'à 1 000 gouttes), les ombres de la lumière du soleil, et les textes 3D (un TextMesh = un appel de dessin).
