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
- un **disque lumineux au sol**, sous les pieds ;
- un petit **battement** de hauteur (une ligne de calcul par image, pas de simulation) ;
- le maillage est **partagé** entre toutes les auras de même couleur, et il utilise le matériau des lignes du jeu (aucun nouveau shader).

Une aura = 60 triangles, 1 objet, 0 particule (avant : jusqu'à 86 particules et 2 systèmes).

**Avant / après.** *À mesurer.* Au casque : fps face à la bibliothèque, avant (`main` du 21 oct.) et après (cette branche). Dans le Profiler : *Batches* et temps CPU de `ParticleSystem.Update`.

| Mesure | Avant | Après |
|---|---|---|
| fps au casque, face à la bibliothèque | ? | ? |
| fps au casque, carte pendant une vague | ? | ? |
| Batches (Profiler, hub) | ? | ? |
| Particules vivantes (hub) | ~ 56 × 80 | 0 |

**Est-ce que ça a vraiment marché ?** *Pas encore vérifié.* On s'attend à un vrai gain au hub. À vérifier aussi : que l'aura reste lisible (la rareté se reconnaît toujours à sa couleur), et que l'arc-en-ciel, en dégradé de teintes autour du tube, se distingue bien.

**Suggestions.**
- Si les *Batches* restent hauts : activer le **GPU Instancing** sur le matériau, ou fusionner les auras de la bibliothèque (elles ne bougent pas).
- Dans la bibliothèque, les auras pourraient ne battre que pour le singe que la main survole.
- L'ancien prefab `Prefabs/Aura.prefab` ne sert plus. On peut le supprimer, avec le champ `auraPrefab` de `MonkeyVisuals`, une fois l'aura validée au casque.
- Prochaines pistes à mesurer : la pluie de l'orage (`Map/Weather.cs`, jusqu'à 1 000 gouttes), les ombres de la lumière du soleil, et les textes 3D (un TextMesh = un appel de dessin).
