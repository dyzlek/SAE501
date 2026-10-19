# MVP de la semaine 2 · 19-23 oct. 2026 — « Construire »

_Objectif du jalon : **jouable du début à la fin, même moche**. Vendredi 23 oct., un autre groupe teste le jeu (test du silence, #36). Ce document dit où on en est, ce qui vient d'être ajouté, et **tout ce qu'on peut encore faire**, trié par priorité._

## 0. Le principe du jeu (précisé le 19 oct.)
**Il n'y a pas de fin.** Les vagues sont **infinies** et le but est d'**aller le plus loin possible** (le record). Quand on perd une vague, on **reste sur la même vague** : on la relance, rien n'est perdu (singes et argent gardés). Il n'y a donc ni écran de victoire ni « game over » : à la place, des **paliers** qui récompensent et un **record** à battre.

Conséquences à répercuter dans le [GDD](GDD.md) (il parle encore de « 100 vagues » et de « victoire ») : parties 2, 3, 7, 13 et 16 ; à faire dans une PR à part, avec le `.docx` (Word bloque le fichier tant qu'il est ouvert).

## 1. Ce qu'est le MVP (la définition de « fini » pour vendredi)
Un testeur qui n'a jamais vu le jeu, sans aucune explication, doit pouvoir :
1. mettre le casque, comprendre ce qu'il faut faire (tutoriel de 17 étapes) ;
2. tirer à l'arc, poser un singe, récolter des bananes, ouvrir un coffre ;
3. **entendre** ce qu'il fait (sons, vibrations) ;
4. gagner ou perdre une vague et **comprendre ce qui se passe** ;
5. avoir **envie de recommencer pour battre son record**, sans relancer l'application ;
6. rester à 72 fps minimum, sans nausée.

## 2. Ajouté dans cette branche (`feat/mvp-s2`)
Compilé sans erreur ; **pas encore testé dans Unity ni au casque**.

| Ajout | Fichiers | Ce que ça fait | Issue |
|---|---|---|---|
| **Vagues infinies** | [WaveData.cs](../Unity/Assets/_Project/Scripts/Map/WaveData.cs) (`WaveBook.Make`), [WaveSpawner.cs](../Unity/Assets/_Project/Scripts/Map/WaveSpawner.cs) | Plus de liste de 100 vagues ni de victoire : la formule calcule n'importe quelle vague (`WaveBook.Make(w)`). Plus aucun `/ 100` sur les tableaux | #31 |
| **Vagues spéciales** | `WaveBook.Special` | Toutes les 10 vagues, un groupe à thème s'ajoute à la vague : **RUSH** (nuée de ballons rapides), **BLINDAGE** (colonne de blindés), **COEURS** (ballons qui se regonflent). Les thèmes tournent | nouveau |
| **Paliers** | `WaveSpawner`, [EndScreen.cs](../Unity/Assets/_Project/Scripts/Map/EndScreen.cs) | Toutes les 10 vagues gagnées : bonus de bananes (50 × le numéro du palier : 50, 100, 150…), fanfare et panneau « PALIER 20 ! » dans le décor | nouveau |
| **Vague parfaite** | `WaveSpawner` | Aucun ballon passé (les vies sont restées pleines) : la récompense de la vague augmente de 50 %, le tableau écrit « PARFAITE » | nouveau |
| **Record** | `GameState.BestWave`, [WaveBoard.cs](../Unity/Assets/_Project/Scripts/Hub/WaveBoard.cs) | La plus haute vague gagnée est affichée sur le tableau de la vague, au hub et sur la carte. Pas de sauvegarde entre deux lancements (c'est dans « NE FERA PAS ») | nouveau |
| **Vague perdue** | `EndScreen` | Bannière de 5 s dans le décor : « rien n'est perdu, relance-la » (pas pendant le tutoriel, qui a son tableau) | #31 |
| **Sons et musique** | [Sfx.cs](../Unity/Assets/_Project/Scripts/Core/Sfx.cs) | 14 sons fabriqués par le code (pas de fichier audio) branchés sur le ballon éclaté, l'arc, les boutons, l'argent, la pose des singes, le coffre, la porte, le tutoriel et les vagues, plus une musique d'ambiance très douce | #34 |
| **Nouvelle partie** | [GameRestart.cs](../Unity/Assets/_Project/Scripts/Core/GameRestart.cs) | Remet tout à zéro (argent, inventaire, arc, tutoriel, record) et recharge la cabane. **Pas encore de bouton dans le jeu** : à lancer par le menu clic droit de `EndScreen` pour enchaîner les testeurs (voir 3.1) | nouveau |

**Comment tester :**
- Sons : lancer le jeu, tirer, appuyer sur un bouton, déposer une banane. `MusicVolume` dans `Sfx.cs` règle la musique.
- Panneau d'annonce : en mode Play, scène `Labyrinthe`, objet **Ecran de fin**, clic droit sur le composant → **Tester le palier** / **Tester la vague perdue** / **Tester REJOUER**.
- Vagues : `WaveBook.Make(10)`, `Make(20)`… donnent les groupes ; pour jouer vite une vague spéciale, mettre `Wave` à 10 (à ajouter à `WaveShortcut` si besoin).

**À surveiller au premier test :**
- Position du panneau : 2,2 m devant le point d'arrivée de la carte, à 1,5 m de haut ; ajuster `Distance` et `Height` si un arbre le cache.
- **Performance en infini** : plus la vague monte, plus il y a de ballons en même temps (≈ 70 à la vague 100, davantage après). À mesurer sur le Quest (#35) ; si ça ralentit, plafonner le nombre de ballons à l'écran.
- Équilibrage des vagues spéciales (RUSH 30 rapides à 2 couches dès la vague 10 : peut-être trop dur) ; bonus de palier et vague parfaite à ajuster en jouant.

## 3. Tout ce qu'on peut encore faire

_Les numéros sont les issues du [tableau](https://github.com/users/dyzlek/projects/2). Effort : **S** < 1 h · **M** une demi-journée · **L** une journée ou plus. Beaucoup d'issues ouvertes ont déjà été traitées le 8-9 oct. : elles attendent un **test au casque** avant d'être fermées._

### 3.1 DOIT — sans ça, pas de jalon
| Tâche | Issue | Effort | Remarque |
|---|---|---|---|
| **Test au casque de tout ce qui n'a pas encore été essayé** (tutoriel complet, vagues infinies, PASSER, porte, bananeraie, roulette, sons, panneau d'annonce) | #14, #37 | M | Régénérer les scènes avant : SAE > Générer le prototype, bac à sable décoché. À faire **avant tout le reste** |
| **Build casque de la semaine** (le vendredi) | #37, #13 | M | |
| **Mesurer les fps** (≥ 72) avec le compteur au poignet, **surtout aux grosses vagues** ; plafonner les ballons si besoin | #35 | M | |
| **Fusionner la PR #128 (vagues vraiment difficiles)** : elle modifie `WaveBook.Make`, elle **va entrer en conflit** avec cette branche → la refaire sur la nouvelle formule | #127 | S | |
| **Bouton « NOUVELLE PARTIE »** dans le décor (plaque au hub, avec confirmation OUI / NON comme PASSER) | #31 | S | `GameRestart.Now()` existe déjà |
| **Équilibrer l'infini** : jouer jusqu'à la vague 30, noter où ça casse (trop facile, trop dur, argent qui s'accumule) | #127 | M | Régler `WaveBook`, les prix, les bonus |
| **Argent compréhensible**, **bananes visibles sur la carte**, **visuel « fusion possible »**, **texte du coffre**, **explication des touches** | #63, #64, #57, #72, #54 | M | |
| **Téléportation, spawn, saut, spam de l'arc** : re-tester au casque puis fermer | #81, #67, #65, #55, #60 | S | |
| **Soigner les 2 premières minutes** | #39 | M | Test du silence |
| **Test du silence par un autre groupe** (vendredi 23) | #36 | M | Check-list en partie 5 |
| **Réserver les casques** sur Moodle | #25 | S | |

### 3.2 Mécaniques à ajouter — idées classées par ce qu'elles apportent
_Chacune est notée : **VR** = elle utilise un geste du corps (elle passe le test de l'écran) · **Infini** = elle donne envie d'aller plus loin. Rien n'est dans le GDD : à valider en équipe avant de créer les issues, **S2 est la dernière semaine où l'on ajoute**._

**A. Rendre les vagues plus intéressantes (le cœur de l'infini)**
| Idée | VR / Infini | Effort | Détail |
|---|---|---|---|
| **Boss qui se divise** | Infini | M | Le boss éclate en ballons plus petits quand on le touche (comme le MOAB de Bloons). Les dirigeables « pondent » des ballons rapides |
| **Ballons camouflés** | Infini | M | Invisibles de loin / seulement visibles dans la lumière d'un singe « vigie » : donne un rôle à un nouveau singe |
| **Ballons à ressort / qui sautent des virages** | Infini | M | Ils raccourcissent le chemin : il faut viser plus tôt |
| **Choix de vague (deux portes)** | Infini | M | Avant une vague, deux boutons : « facile : +20 » ou « difficile : +60 » ; le joueur choisit son risque |
| **Chrono de vague / ballons qui accélèrent** | Infini | S | À partir de la vague 50, les ballons vont un peu plus vite à chaque palier |
| **Modificateurs de palier** | Infini | M | Brouillard sur la carte, nuit (lumière rouge), pluie : change l'ambiance et la lisibilité |
| **Série de vagues parfaites (combo)** | Infini | S | Un compteur « ×3 » sur le tableau, qui multiplie les bananes gagnées ; se perd au premier ballon passé |
| **Annonce de la vague spéciale** | Infini | S | Les cloches ou une musique différente 3 s avant (sans dire le contenu, #70) |

**B. Gestes VR (l'arc et les mains)**
| Idée | VR / Infini | Effort | Détail |
|---|---|---|---|
| **Bombes et objets à lancer à la main** | VR | M | Une caisse de « bombes de glace », « bombes collantes » ou « piège à clous » à saisir et jeter sur la piste (geste de lancer comme les fléchettes et les bananes, la physique existe déjà) |
| **Points faibles des gros ballons** | VR, Infini | M | Une cible dorée sur le dos du boss : toucher dedans fait x3 dégâts ; récompense la précision |
| **Tir chargé parfait** | VR | S | Tendre l'arc à fond **et** retenir 0,5 s : la flèche brille et perce plus ; sinon un tir rapide moins fort |
| **Coup de poing / main qui éclate** | VR | S | Taper un ballon qui passe près de la plateforme avec la main éclate une couche (utile en dernier recours) |
| **Pouvoir de Quincy : pluie de flèches** | VR | M | Lever l'arc au-dessus de la tête 1 s déclenche une salve sur toute la piste ; recharge de 60 s |
| **Vibrations différentes par action** | VR | S | Un motif court / long / double selon ce qu'on fait (déjà des `Buzz`, il manque de la variété) |
| **Se pencher pour voir sous les ballons** | VR | S | Un indice caché derrière un décor, juste pour récompenser l'exploration |

**C. Singes, économie, progression pendant la partie**
| Idée | VR / Infini | Effort | Détail |
|---|---|---|---|
| **Singe actif : tape-pour-booster** | VR | M | Toucher un singe posé avec la main le fait tirer deux fois plus vite pendant 5 s |
| **Singe vigie / détecteur** | Infini | M | Révèle les ballons camouflés (voir A) |
| **Singe banquier** | Infini | M | Ajoute des intérêts : +5 % de bananes à chaque palier (récompense de patienter) |
| **Vendre un singe** (en le jetant dans le panier) | VR | S | Rend la moitié du prix ; sert aux paliers durs |
| **Améliorer un singe en lui apportant un autre** | VR | M | Poser un singe sur un autre l'améliore (en plus de la fusion) |
| **Bananes dorées rares** | Infini | S | Une banane dorée sur un arbre toutes les 10 min vaut 50 : encourage à regarder dehors |
| **Boutique à rotation** | Infini | M | Une étagère propose 3 singes à prix réduit, qui change à chaque palier |
| **Amélioration d'arc supplémentaire** | Infini | S | Ex. « flèches qui rebondissent » (3 niveaux) |

**D. Hub, records, ambiance, confort**
| Idée | VR / Infini | Effort | Détail |
|---|---|---|---|
| **Mur des records** | Infini | S | Un tableau au hub : meilleure vague, nombre de ballons éclatés, bananes gagnées (statistiques de la partie, rien de sauvegardé) |
| **Trophées dans la cabane** | Infini | M | Une étagère : un trophée apparaît aux paliers 10, 20, 30… (3D simples) |
| **Confettis et ralenti au palier** | Infini | S | Particules et 1 s de ralenti sur le dernier ballon d'un palier |
| **Musique qui monte avec la vague** | Infini | M | Plus de notes / un tempo plus rapide aux vagues spéciales (`Sfx` génère déjà la musique) |
| **Sons par type de ballon** | Infini | S | Un « bong » grave pour le blindé, un « ding » pour le rapide, un « glou » pour le cœur |
| **Bouton muet / volume** | confort | S | Une plaque dans la cabane |
| **Gaucher** | confort | S | Arc dans la main droite (option au hub) |
| **Assis / debout** | confort | S | Déjà dans les issues (#50) |
| **Pause naturelle** | confort | S | Une plaque « Respire » toutes les 5 vagues ; le jeu n'avance pas tant qu'on n'appuie pas sur LANCER (c'est déjà le cas) |
| **Diffusion à plat avec le record** | démo | M | La vue diffusée au casque montre aussi le tableau du record (pour l'oral) |
| **Difficulté choisie** | Infini | M | Une plaque « facile / normal / difficile » qui règle les vies et la croissance de `WaveBook` |

**Mes 5 conseils pour S2** (le plus de plaisir pour le moins de travail, et ça tient dans le périmètre) :
1. **Bouton NOUVELLE PARTIE** + **mur des records** : ferme la boucle « essayer, perdre, recommencer, battre son record » (S + S).
2. **Bombes de glace à lancer** : un geste VR neuf qui réutilise la physique des fléchettes (M).
3. **Boss qui se divise** : le ballon est l'ennemi principal, ça rend l'infini bien plus varié (M).
4. **Points faibles + tir chargé parfait** : plus de maîtrise de l'arc, qui est le geste central (M + S).
5. **Sons par type de ballon** + **confettis au palier** : le jeu paraît fini sans nouvelle règle (S + S).

### 3.3 DEVRAIT / POURRAIT déjà sur le tableau
| Tâche | Issue | Effort |
|---|---|---|
| Animations des singes posés et projectiles 3D | #105 | L |
| Chemin des ballons inspiré des cartes de Bloons TD | #102 | M |
| Fusion 3 → 1 fiable au casque (le jeu fusionne par 2 : accorder GDD et issue) | #33 | M |
| Vie des ballons plus claire ; étincelles de la flèche explosive ; tracé de la flèche d'une autre couleur | #80, #78, #79 | S à M |
| Ne pas annoncer le contenu de la vague | #70 | S |
| Fiche des singes ; cacher le nom à la pose ; textes sur la carte | #71, #53, #59 | S |
| Plus de bananes dans le coffre ; roulette « boisée » ; animation des bananes qui pourrissent | #56, #51, #75 | S à M |
| Bananier dehors avec porte animée | #68 | L |
| Visuels : murs du hub, plateforme d'arrivée, objets, lumière, décor extérieur | #66, #62, #58, #73, #77, #61 | M chacun |
| Singe récolteur qui porte les bananes ; lancer les récolteurs ; physique des bananes | #76, #27, #29 | M |
| Vrais fichiers audio à la place des sons fabriqués | #34 | M |
| Cible de fléchettes jouable ; mini-jeu de la roulette ; casino (hors périmètre) | #74, #30, #69 | M à L |

### 3.4 NE FERA PAS (rappel du GDD)
Plusieurs cartes · méta-progression ou **sauvegarde entre deux parties** · multijoueur · scénario · déplacement continu · achats réels. **En S3, plus aucune nouvelle fonction** (gel le lundi 9 nov., #38).

## 4. Questions à trancher lundi (visio avec Antoine)
1. **Vagues infinies sans fin** : est-ce OK pour le cahier des charges (« une fin » dans le support de lancement : « 1-2 niveaux + une fin ») ? Si on veut garder une fin, une victoire à une vague donnée avec le mode infini derrière (ce que j'avais fait au début) reste possible en 10 lignes.
2. Un **record sans sauvegarde** (perdu quand on quitte le jeu) suffit-il pour la démo ?
3. Quelles **mécaniques** de la partie 3.2 on garde (maximum 3 cette semaine) ?

## 5. Plan de la semaine

| Jour | Objectif | Qui peut le prendre |
|---|---|---|
| **Lun. 19** (visio 1 h) | Trancher la partie 4, choisir les mécaniques, créer les issues, répartir | Tout le monde |
| **Mar. 20** | **Test au casque** de `feat/mvp-s2` + tout le 9 oct. ; corriger les bugs bloquants ; refaire #128 sur la nouvelle formule | Dylan (casque), Maxens et Nicolas (relire / tester) |
| **Mer. 21** | Mécaniques choisies + lisibilité (#63, #64, #57, #72, #54) | À répartir |
| **Jeu. 22** | Build casque, fps (#35), équilibrage jusqu'à la vague 30, test du silence **en interne** | Tout le monde |
| **Ven. 23** | **Un autre groupe teste** (#36) ; build du jour ; bilan visio 30 min | Tout le monde |

### Check-list du test du silence (vendredi)
- [ ] Le testeur met le casque, **on ne dit rien** (ni règles, ni gestes).
- [ ] On note : où il hésite, ce qu'il essaie en premier, ce qu'il rate, ce qu'il dit.
- [ ] On chronomètre : temps pour finir le tutoriel, pour finir la première vague, **jusqu'à quelle vague il va**.
- [ ] On demande ensuite : « Qu'as-tu compris ? Qu'est-ce qui t'a gêné ? Pourquoi c'est en VR ? Tu veux refaire une partie ? »
- [ ] On vérifie : pas de nausée, sons audibles, panneau d'annonce compris.
- [ ] On corrige le jeu d'après les blocages, on ne corrige pas le testeur.

## 6. Règles à garder en tête
- **Test de l'écran** : ce qu'on ajoute doit servir un geste VR (arc, saisir, lancer, poser, enfoncer), pas un menu.
- **Confort** : rien de collé au visage, pas de secousse, pas de son trop fort, 72 fps.
- **Simple, c'est bien** : fini et court vaut mieux qu'ambitieux et à moitié fait. On coupe la semaine 2, on ne rajoute pas.
- **Journal et prompts** à jour dans chaque commit (règles n°1 et n°2), PR relue par un autre membre, jamais de commit direct sur `main`.
