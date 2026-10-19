# MVP de la semaine 2 · 19-23 oct. 2026 — « Construire »

_Objectif du jalon : **jouable du début à la fin, même moche**. Vendredi 23 oct., un autre groupe teste le jeu (test du silence, #36). Ce document dit où on en est, ce qui vient d'être ajouté, et **tout ce qu'on peut encore faire**, trié par priorité._

## 1. Ce qu'est le MVP (la définition de « fini » pour vendredi)
Un testeur qui n'a jamais vu le jeu, sans aucune explication, doit pouvoir :
1. mettre le casque, comprendre ce qu'il faut faire (tutoriel de 17 étapes) ;
2. tirer à l'arc, poser un singe, récolter des bananes, ouvrir un coffre ;
3. **entendre** ce qu'il fait (sons, vibrations) ;
4. gagner ou perdre une vague et **comprendre ce qui se passe** ;
5. aller jusqu'à une **fin** (victoire), puis **rejouer** sans relancer l'application ;
6. rester à 72 fps minimum, sans nausée.

Ce qui manquait au 19 oct. (d'après le GDD, partie 13, et les issues ouvertes) : **les sons**, **l'écran de victoire / défaite**, le **rejouer**, l'**équilibrage**, le **test au casque** de tout ce qui a été fait le 9 oct.

## 2. Ajouté dans cette branche (`feat/mvp-s2`)
Compilé sans erreur (0 erreur, 0 avertissement) ; **pas encore testé dans Unity ni au casque**.

| Ajout | Fichiers | Ce que ça fait | Issue |
|---|---|---|---|
| **Sons du jeu** | [Sfx.cs](../Unity/Assets/_Project/Scripts/Core/Sfx.cs) | 14 sons fabriqués par le code (pas de fichier audio, pas de licence) : ballon éclaté, tir de l'arc, bouton, refus, pièce, pose d'un singe, coffre, vague lancée / gagnée / perdue, ballon qui s'échappe, étape du tutoriel, porte, victoire. Les sons viennent de l'endroit où ça se passe (3D). 12 haut-parleurs réutilisés : aucune création d'objet pendant le jeu | #34 |
| **Musique d'ambiance** | [Sfx.cs](../Unity/Assets/_Project/Scripts/Core/Sfx.cs) | Une boucle de 14 s de notes pincées (gamme pentatonique), très douce (volume 0,1) | POURRAIT |
| **Sons branchés sur le jeu** | `Balloon`, `Bow`, `HandPress`, `RayPress`, `Economy`, `MonkeyToken`, `ChestController`, `PortalDoor`, `Tutorial`, `WaveSpawner` | Une ligne `Sfx.Play(...)` à chaque endroit où il y avait déjà une vibration, plus l'argent (gagné / dépensé) | #34 |
| **Écran de victoire et de vague perdue** | [EndScreen.cs](../Unity/Assets/_Project/Scripts/Map/EndScreen.cs) | Un panneau dans le décor de la carte, à 2,2 m devant le point d'arrivée. **Victoire** (dernière vague gagnée) : « VICTOIRE ! » avec deux boutons REJOUER et CONTINUER (mode infini). **Vague perdue** : bannière de 5 s « Rien n'est perdu, relance-la avec LANCER ». Se construit tout seul : **pas besoin de régénérer les scènes** | #31 |
| **Rejouer** | [GameRestart.cs](../Unity/Assets/_Project/Scripts/Core/GameRestart.cs), `GameState.Reset`, `BowUpgrades.Reset`, `Tutorial.Restart`, `Levels.Restart` | Remet à zéro l'argent, l'inventaire, les vagues gagnées, l'arc, le tutoriel et le niveau, puis recharge la scène Hub. Le tutoriel recommence : pratique pour enchaîner les testeurs | #31 |

**Comment tester :**
- Les sons : lancer le jeu (PC ou casque), tirer, appuyer sur un bouton, déposer une banane. Pour baisser ou couper la musique, changer `MusicVolume` dans `Sfx.cs`.
- L'écran de fin : en mode Play, dans la scène `Labyrinthe`, sélectionner l'objet **Ecran de fin**, clic droit sur le composant → **Tester la victoire** ou **Tester la vague perdue**.
- Rejouer : appuyer sur REJOUER du panneau de victoire (ou le déclencher avec le menu ci-dessus) ; on doit retrouver la cabane et le tutoriel, sans argent ni singe.

**À surveiller au premier test :**
- Position du panneau de fin : il flotte à 2,2 m devant le point d'arrivée de la carte, à 1,5 m de haut ; il peut recouvrir un élément du décor ou être derrière un arbre → ajuster `Distance` et `Height` dans `EndScreen.cs`.
- Après REJOUER : vérifier que le joueur apparaît bien dans la cabane, que la carte se recharge et que le bananier et les récolteurs repartent de zéro.
- Les sons répétés (ballons éclatés en rafale) : le volume doit rester supportable ; au besoin, baisser le volume de `Sound.Pop`.

## 3. Tout ce qu'on peut encore faire

_Les numéros sont les issues du [tableau](https://github.com/users/dyzlek/projects/2). Effort : **S** < 1 h · **M** une demi-journée · **L** une journée ou plus. Beaucoup d'issues ouvertes ont déjà été traitées le 8-9 oct. : elles attendent un **test au casque** avant d'être fermées._

### 3.1 DOIT — sans ça, pas de jalon
| Tâche | Issue | Effort | Remarque |
|---|---|---|---|
| **Test au casque de tout ce qui n'a pas encore été essayé** (tutoriel complet, 100 vagues, PASSER, porte, bananeraie, roulette, nouveaux sons, écran de fin) | #14, #37 | M | Régénérer les scènes avant : SAE > Générer le prototype, bac à sable décoché. À faire **avant tout le reste** |
| **Build casque de la semaine** (le vendredi, un build sur le Quest) | #37, #13 | M | Mode développeur + débogage USB déjà vus le 9 oct. |
| **Mesurer les fps** sur le Quest (≥ 72) avec le compteur au poignet, optimiser si besoin | #35 | M | La cabane fait ~108 000 triangles : à surveiller |
| **Fusionner la PR #128 (vagues vraiment difficiles)** et relire #124 / #126 déjà mergées | #127 | S | À équilibrer en jouant |
| **Vague 100 = trop long pour un test** : décider d'une fin de test (voir 4.1) | #31 | S | Un testeur ne jouera pas 100 vagues en 20 min |
| **Argent compréhensible** : prix et coûts lisibles dans le décor | #63 | M | |
| **Bananes visibles aussi sur la carte** | #64 | M | |
| **Fusion : visuel « fusion possible »** à la place du cercle rouge | #57 | M | |
| **Retirer le texte flottant au-dessus du coffre** | #72 | S | |
| **Expliquer les touches** (fiche du singe en VR, B pour lancer) | #54 | S | Une plaque dans le décor |
| **Bloquer la téléportation hors de la carte, sur le plateau, dans les murs** | #81, #67 | S | Vu bon au casque le 8 oct. : re-tester puis fermer |
| **Spawn, saut coupé, arc sans spam** | #65, #55, #60 | S | Idem : vérifier puis fermer |
| **Soigner les 2 premières minutes** (consignes une à une, rien de bloquant) | #39 | M | Test du silence pour le repérer |
| **Test du silence par un autre groupe** (vendredi 23) | #36 | M | Voir la check-list en partie 5 |
| **Réserver les casques** sur Moodle pour les créneaux de la semaine | #25 | S | |
| **Jouer en continu** : chaque jour, au moins un essai au casque | — | — | Règle du cahier des charges |

### 3.2 DEVRAIT — si le DOIT est vert
| Tâche | Issue | Effort |
|---|---|---|
| Vraies **animations des singes posés** et projectiles 3D | #105 | L |
| **Chemin des ballons** inspiré des cartes de Bloons TD | #102 | M |
| **Fusion 3 → 1** fiable au casque (le jeu fusionne par 2 aujourd'hui : mettre le GDD et l'issue d'accord) | #33 | M |
| **Vie des ballons** plus claire (couches, cœurs qui se régénèrent) | #80 | M |
| **Étincelles de la flèche explosive**, **autre couleur pour le tracé de la flèche** | #78, #79 | S |
| **Ne pas annoncer le contenu de la vague** (la surprise fait partie du jeu) | #70 | S |
| **Fiche des singes** mieux présentée ; **cacher le nom** quand on pose le singe | #71, #53 | S |
| **Carte à taille réelle** : enlever les textes au-dessus des singes | #59 | S |
| **Plus de bananes dans le coffre** ; **roulette plus « boisée »** | #56, #51 | S |
| **Bananes qui pourrissent** : animation | #75 | M |
| **Bananier dehors** avec porte animée | #68 | L |
| **Singes qui se débloquent avec les bananes** (fait le 8 oct. : vérifier puis fermer) | #52 | S |
| **Visuels** : hub (murs), plateforme d'arrivée sur la carte, objets, lumière | #66, #62, #58, #73, #77 | M chacun |
| **Équilibrage** : jouer 20 vagues, noter les vagues trop dures ou trop faciles, régler `WaveBook` et les prix | — | M |

### 3.3 POURRAIT — seulement si tout le reste est fini
| Tâche | Issue | Effort |
|---|---|---|
| **Vrais fichiers audio** à la place des sons fabriqués (bruitages libres de droits) | #34 | M |
| **Bouton muet / volume de la musique** dans le décor (une plaque à la cabane) | — | S |
| **Musique différente** hub / carte / combat de boss | — | M |
| **Singe récolteur** qui porte les bananes devant lui | #76 | M |
| **Lancer les singes récolteurs** pour s'amuser ; **physique des bananes** (paniers) | #27, #29 | M |
| **Bouton assis / debout** | #50 | S |
| **Décor extérieur** : montagnes, champignons | #61 | M |
| **Cible de fléchettes jouable** (easter egg) ; **mini-jeu de la roulette** | #74, #30 | M |
| **Casino et banque** à la place du bananier | #69 | L (hors périmètre) |

### 3.4 NE FERA PAS (rappel du GDD)
Plusieurs cartes · méta-progression ou sauvegarde · multijoueur · scénario · déplacement continu · achats réels. **En S3, plus aucune nouvelle fonction** (gel le lundi 9 nov., #38).

## 4. Idées nouvelles (pas encore dans le GDD ni sur le tableau)
À valider en équipe avant de créer les issues. Chacune est petite.

1. **Fin de partie pour un test (S, DOIT)** : 100 vagues ne se jouent pas en une séance. Proposer une victoire plus courte pour le test (par exemple vague 15 ou 20, `WaveSpawner` lit déjà `LastWrittenWave`), et garder le mode infini derrière CONTINUER. À trancher avec Antoine : le GDD annonce 100 vagues.
2. **Statistiques de fin (S)** : sur le panneau de victoire, afficher le nombre de ballons éclatés et de singes posés (de simples compteurs, rien de sauvegardé).
3. **Panneau de fin aussi au hub (S)** : si la victoire arrive pendant qu'on est dans la cabane, le panneau est sur la carte et personne ne le voit. Soit une phrase sur le tableau du hub, soit le même panneau là-bas.
4. **Pause entre deux vagues (S)** : une plaque « Respire, enlève ton casque si besoin » toutes les 5 vagues (confort, sessions courtes).
5. **Retour sonore du tutoriel (S)** : une voix ou un son différent pour « étape réussie » et « raté » (le `Ding` actuel est le même partout).
6. **Aide « bloqué ? » (M)** : si le joueur ne fait rien pendant 30 s au tutoriel, la flèche dorée clignote plus fort (test du silence).
7. **Écran de chargement du rejouer (S)** : le voile noir `ScreenCurtain` avant de recharger la scène, pour ne pas avoir de coupure sèche dans le casque.
8. **Fiche « Comment jouer » pour l'autre groupe (S)** : une page pour la personne qui fait tester (ne rien dire, noter les blocages, durée), voir partie 5.
9. **Vidéo à plat et captures (M, DOIT, #42)** : enregistrer pendant les tests de S2 ; ça sert pour l'oral et le GDD.
10. **Vérification des journaux (S)** : chacun relit son journal et son tableau « IA » avant vendredi (note de gestion de projet).

## 5. Plan de la semaine

| Jour | Objectif | Qui peut le prendre |
|---|---|---|
| **Lun. 19** (visio 1 h) | Trier ce document, valider les idées de la partie 4, créer les issues manquantes, répartir | Tout le monde |
| **Mar. 20** | **Test au casque** de la branche `feat/mvp-s2` + tout le 9 oct. ; corriger les bugs bloquants ; régler le point 4.1 | Dylan (casque), Maxens et Nicolas (relire / tester) |
| **Mer. 21** | Lisibilité (#63, #64, #57, #72, #54) et équilibrage sur 20 vagues | À répartir |
| **Jeu. 22** | Build casque, fps (#35), test du silence **en interne** et correction ; réserver les casques | Tout le monde |
| **Ven. 23** | **Un autre groupe teste** (#36) ; build du jour ; bilan visio 30 min (« qu'avez-vous produit ? ») | Tout le monde |

### Check-list du test du silence (vendredi)
- [ ] Le testeur met le casque, **on ne dit rien** (ni règles, ni gestes).
- [ ] On note : où il hésite, ce qu'il essaie en premier, ce qu'il rate, ce qu'il dit.
- [ ] On chronomètre : temps pour finir le tutoriel, pour finir la première vague.
- [ ] On demande ensuite : « Qu'as-tu compris ? Qu'est-ce qui t'a gêné ? Pourquoi c'est en VR ? »
- [ ] On vérifie : pas de nausée, sons audibles, panneau de fin compris, REJOUER marche.
- [ ] On corrige le jeu d'après les blocages, on ne corrige pas le testeur.

## 6. Règles à garder en tête
- **Test de l'écran** : ce qu'on ajoute doit servir un geste VR (arc, saisir, poser, enfoncer), pas un menu.
- **Confort** : rien de collé au visage, pas de secousse, pas de son trop fort, 72 fps.
- **Simple, c'est bien** : fini et court vaut mieux qu'ambitieux et à moitié fait. On coupe la semaine 2, on ne rajoute pas.
- **Journal et prompts** à jour dans chaque commit (règles n°1 et n°2), PR relue par un autre membre, jamais de commit direct sur `main`.
