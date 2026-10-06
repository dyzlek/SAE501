# Prompts de Maxens

_Les demandes les plus utiles faites à l'IA, **reformulées et corrigées** (orthographe, clarté, structure : contexte, objectif, contraintes). Le sens est conservé. La plus récente est en haut. Le détail de ce qui a été gardé ou jeté est dans [mon journal](../docs/journal/maxens.md)._

## Mar. 6 oct. 2026

### Séparer l'arc de Quincy
> D'accord pour ta proposition : sépare l'arc de Quincy par un script Blender, en travaillant sur une copie du fichier. L'arc doit devenir un objet à part avec son propre squelette (poignée, branches, corde qu'on peut tirer), exporté en FBX, et les bras et les mains du personnage doivent en être indépendants.

### Analyse du rig de Quincy
> Notre personnage sera Quincy, et son arme sera l'arc. Pour l'instant, analyse uniquement son rig, sans rien modifier : vérifie qu'il est bien construit, et en particulier que les bras et les mains sont correctement séparés de l'arc.

### Retour à la version précédente du hub
> Après essai, l'établi et les étagères tournantes surchargent l'espace autour du joueur : on ne distingue plus clairement les éléments. Annule cette dernière version et remets le hub tel qu'il était avant.
>
> Consigne dans le journal et dans le message de commit la raison de ce retour en arrière (interface trop surchargée), pour garder une trace de la décision.

### Prototype : établi et étagères tournantes
> Je valide ta recommandation de combiner les pistes 1 et 2 : un établi à portée de main pour le plateau et les boutons, et des étagères tournantes pour la bibliothèque. Réalise un premier prototype sur ma branche.

### Analyse : un hub inadapté à la VR
> Dans la version actuelle, les boutons et la bibliothèque sont hors de portée des mains : le hub n'est pas adapté à la VR. Ajoute ce problème dans la partie « Bloque » de mon journal, avec une capture qui l'illustre.
>
> Propose ensuite les solutions possibles, de la plus simple (réorganiser les bibliothèques) à la plus profonde (changer complètement le fonctionnement), avec leurs avantages, leurs limites et ta recommandation.

### Test : un hub où l'on ne se déplace pas
> Je veux tester un hub où le joueur reste immobile au centre et fait tout en tournant la tête et en saisissant les objets. Le schéma joint montre la disposition voulue : tous les éléments répartis autour de lui, accessibles du regard, avec une portée d'interaction assez longue.
>
> Dans la même modification :
> - supprime les textes affichés au-dessus des singes ;
> - agrandis le plateau, car on distingue mal les différents singes posés dessus.

### Respect des cours de développement
> Avant d'aller plus loin : as-tu lu nos supports de cours ? Le code doit suivre les pratiques qui y sont enseignées.
>
> Installe l'outil nécessaire pour lire les PDF, relis les supports, puis reprends le code déjà écrit pour qu'il s'y conforme.

### Corrections après mon premier test
> J'ai testé la version avec les singes en 3D. Plusieurs problèmes :
> - un singe posé sur le plateau redevient un cube ;
> - l'aura convient mal aux modèles larges et bas (Canon, Tireur) ;
> - le Canon est tourné vers le mur ;
> - quand j'arrête le Play, la bibliothèque repasse en cubes : pourquoi ?
>
> Côté dépôt : annule les modifications parasites qu'Unity a faites à l'ouverture du projet, ne committe pas le package MCP et travaille uniquement sur ma branche. Ajoute mes deux captures dans la partie « Fait » de mon journal, et consigne-y chaque changement à venir.

### Les singes en 3D dans la bibliothèque, avec une aura
> Dans la scène, la bibliothèque affiche encore des cubes : remplace-les par les modèles 3D de mes singes. Aujourd'hui, la rareté est indiquée par la couleur du cube ; elle devra l'être par une aura autour du singe, dans l'esprit des auras de Dragon Ball.

### Intégrer tous mes modèles 3D au dépôt
> Mes modèles 3D sont dans `Semestre-5/SAE/SAE-dispositif-interectif/Asset`. Ajoute-les tous au projet Unity et pousse-les sur GitHub. Pour le bananier, utilise la version du dossier « Grand tronc ».

### Une branche de test personnelle
> Laisse de côté le bananier agrandi pour l'instant. Crée une branche `feat/prototype-maxens` à partir du `main` à jour, pour que je puisse faire mes propres tests (placer l'arbre, etc.). Pousse-la, puis mets à jour mon journal et mes prompts.

### Agrandir le bananier
> Le tronc du bananier est trop court : agrandis-le.

## Lun. 5 oct. 2026

_(à compléter)_
