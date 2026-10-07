# Prompts de Maxens

_Les demandes les plus utiles faites à l'IA, **reformulées et corrigées** (orthographe, clarté, structure : contexte, objectif, contraintes). Le sens est conservé. La plus récente est en haut. Le détail de ce qui a été gardé ou jeté est dans [mon journal](../docs/journal/maxens.md)._

## Mer. 7 oct. 2026

### Roulette des singes et ouverture du coffre
> Restons sur la branche du coffre. Dans la roulette, je veux voir les singes avec leur aura à la place des carrés de couleur. Le coffre ne doit plus bouger dans la scène ; quand on appuie pour l'ouvrir, il fait un petit « boing », s'ouvre et dégage une petite aura dorée. Dis-moi ce que tu en penses.

### Un coffre dans le style de la cabane
> Récupère le `main` à jour. Ce matin, je m'occupe du coffre (changer l'asset, améliorer l'animation, voir les singes en sortir) puis des assets des ballons MOAB. On commence par le coffre : l'asset actuel ne correspond pas à la direction artistique du reste (la cabane). Refais-en un propre ; on verra l'animation et le reste ensuite.

## Mar. 6 oct. 2026

### Mise à jour finale
> Pousse le travail sur ma branche, et mets bien à jour mes prompts utiles et ce que j'ai fait, avec des captures.

### Tenue de l'arc
> C'est parfait. Deux améliorations : quand on tient les manettes, les mains sont paume vers le bas, ce qui n'est pas naturel ; il faudrait les incliner un peu, comme sur la capture. Ensuite, la main gauche devrait tenir l'arc sur le côté : aujourd'hui elle est au milieu, et la flèche passe à travers. Enfin, la main droite devrait être accrochée à la corde : il reste un espace entre les deux.

### Les mains doivent se voir se fermer
> Voici une capture main ouverte et une capture main fermée : on ne voit presque aucune différence.

### Tir VR et mains
> Les mains ne se ferment pas, et l'arc ne rend pas bien. Surtout : en mode PC le tir fonctionne, mais pas en VR. Quand je clique, la flèche apparaît puis disparaît. Comment as-tu conçu le système de tir en VR ?

### Retour aux mains simples
> Finalement, je préférais les mains simples : les nouvelles collent moins au style du jeu. Reprends la main de Quincy d'avant, avec une fermeture légère quand on appuie sur les boutons.

### Refaire les mains
> Les mains se ferment de façon étrange : il faudrait sans doute les refaire. Autre problème : quand on prend un singe, il se retrouve à l'intérieur de la main.

### Corrections de l'arc et mains de Quincy
> Au casque, l'arc est beaucoup trop gros et je n'arrive pas à tirer : c'est sans doute lié aux commandes. Corrige ces deux points.
>
> Je veux aussi que les mains affichées au hub soient celles de Quincy, correctement riggées si ce n'est pas déjà le cas.

### Le système de l'arc
> Récupère le `main` à jour et crée une nouvelle branche pour l'arme. Au hub, le joueur n'a que ses mains ; l'arc n'apparaît dans ses mains qu'une fois téléporté sur la carte. Pour l'instant, réalise uniquement le système de tir : tendre l'arc, encocher la flèche, l'animation de la corde et de la flèche, le tir.

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
