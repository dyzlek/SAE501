# Journal de bord — Nicolas

_Entrée la plus récente en haut. Une entrée par jour travaillé. Toute aide de l'IA est notée ici (outil, pour quoi, gardé/jeté)._

## Mer. 7 oct. 2026
**Mon analyse critique du jeu** _(transmise par Dylan, qui corrige sur la branche `fix-all` ; mise en forme par l'IA)_
1. **Bibliothèque :** le texte « Bibliothèque » suit la caméra au lieu de rester fixe.
2. **Singe récolteur :** par moments, il a un bug de collision avec la table du panier et passe à travers son bord gauche.
3. **Fiche du singe (touche A) :** la commode (la bibliothèque) cache les informations. Il faudrait un genre de « z-index » pour que la fiche passe devant (photo 1).
4. **Textures qui se chevauchent :** selon l'angle de vue, une texture passe par-dessus une autre ; par exemple, les bûches du mur débordent un peu sur la fenêtre (photo 2).
5. **Herbe de la carte :** en mode FPS, hors du hub, la texture de l'herbe est très moche.
6. **Chute dans le vide :** en mode FPS, si on s'aventure trop loin, on traverse le sol et on tombe sans être remis en place : on est bloqué. Il faudrait revenir au hub, ou à une position proche d'avant la chute.
7. **Bouton LANCER :** après avoir lancé la vague puis être revenu au hub, le bouton n'a pas changé, ce qui donne envie de recliquer dessus. Le griser une fois la vague lancée éviterait ça.
8. **Singes tenus en main :** ils sont encore en T-pose ; il faudrait changer leur design.
9. **Arc :** l'arc et son animation de tir ne rendent pas très bien, en mode PC comme en VR.
10. **Plateau 3D :** quand on pose un singe, on ne voit pas sa portée, ni un rappel de ses dégâts, de ses tirs par seconde, etc.

_Corrigé par Dylan (avec l'IA) sur `fix-all`, à tester : points 3 (fiche par-dessus le décor, restylée), 4 (bûches et fenêtres) et 6 (retour au point d'arrivée après une chute)._

_Note : les deux photos ont été prises avant les corrections de Dylan sur `fix-all` (anciennes polices, ancien comptoir), donc le point 1 est peut-être déjà réglé._

![Photo 1 : la fiche du singe (touche A) en partie cachée par la bibliothèque](../captures/nicolas-critique-1.webp)

![Photo 2 : les bûches du mur qui débordent sur la fenêtre, près de la caisse et du comptoir du récolteur](../captures/nicolas-critique-2.webp)

**Refonte de l'arc et améliorations** _(en test local, pas encore de branche : `feat/arc-upgrade` sera créée si je valide)_
- **Fait :** la flèche est détruite dès qu'elle n'a plus rien à percer (sans amélioration : une couche, puis elle disparaît). Le système de « dégâts » est retiré du jeu : tout passe par la **perforation** (couches percées), singes compris. Le ballon blindé coûte 2 de perforation par couche. Quatre améliorations, cumulables, gratuites pour le bêta-test (les vrais prix sont prêts) : **perforante** (couches percées par ballon, c'est-à-dire les « dégâts », 5 paliers), **transperçante** (nombre de ballons traversés, tir collatéral, 5 paliers), **tir triple**, **explosive** (5 paliers). Le pupitre « ARC » est sur l'estrade de la carte, au bord de l'herbe. En plus : la courbe de visée pendant la tension, et une traînée de vitesse derrière la flèche.
- **À tester :** au casque et en mode PC ; l'équilibrage (le blindé et le dirigeable sont intouchables à l'arc sans perforation, les singes percent moins qu'avant aux raretés impaires).

### IA
| Outil | Pour quoi | Gardé / jeté |
|---|---|---|
| Claude Code | Avis sur ma refonte de l'arc (points à trancher : dégâts des singes, blindé, cumul, périmètre) et proposition de paliers et de prix | Gardé |
| Claude Code | Code de la refonte : `BowUpgrades`, `BowUpgradeButton`, `Shockwave` (nouveaux), `Arrow`, `Bow`, `DesktopArcher`, `Balloon` (`Hit` → `Pop`), `Tower`, `MonkeyData` (`Damage` → `Pierce`), `MonkeyInfoCard`, pupitre « ARC » dans `PrototypeGenerator` ; scène régénérée | À valider (test) |
| Claude Code | Correction de ma demande : perforante = couches par ballon, nouvelle amélioration « transperçante » = nombre de ballons traversés ; pupitre à 4 boutons | À valider (test) |

## Lun. 5 oct. 2026
- **Fait :** création du projet Unity (6000.3.8f1, URP) dans `Unity/` et du prototype « ouverture de coffres » (money, 7 raretés, probabilités, roulette façon CS, animation du coffre).
- **Bloque :** projet pas encore ouvert dans Unity : il reste à installer les packages (menu SAE501 > 1) puis créer la scène (menu SAE501 > 2) et tester.
- **Demain :** tester le proto à l'écran, régler les probabilités, valider avec Dylan et Maxens.

### IA
| Outil | Pour quoi | Gardé / jeté |
|---|---|---|
| Claude Code | Squelette du projet Unity (réglages URP copiés du template Unity 6, manifest des packages) | Gardé |
| Claude Code | Scripts C# du coffre : `Wallet`, `ChestOdds` (probabilités + gel à 10 coffres), `ChestController`, `RouletteView` (roulette façon CS), `DesktopInteractor`, `ChestDebugPanel` | Gardé (à relire et tester) |
| Claude Code | Script Editor `Sae501SetupMenu` : installation des packages et création de la scène de test en un clic | Gardé (à tester) |
| Claude Code | Ajout de `*.glb` aux fichiers Git LFS (`.gitattributes`) | Gardé |
| Claude Code | Retouches UI : sol 30 m, joueur (ZQSD + souris, bonhomme basique), texte « [E] Ouvrir le coffre » qui suit la caméra, roulette qui n'apparaît qu'à l'ouverture et disparaît 3 s après le résultat, menu bêta caché (F1). Scripts `PlayerController`, `Billboard`, `ChestPrompt`, retouche de `ChestController`, `RouletteView`, `DesktopInteractor`, `ChestDebugPanel` | Gardé (à tester) |
| Claude Code | Corrections : le coffre se referme (animation à l'envers) quand la roulette disparaît ; probabilités qui changent à chaque ouverture (1er coffre limité à Bleu, ensuite toutes raretés, LGBT qui monte jusqu'au gel à 10) ; suppression des paliers manuels et du bouton « Palier suivant » | Gardé (à tester) |
| Claude Code | Déblocage progressif des raretés (Violet au coffre 2, Jaune au 4, Rouge au 6, LGBT au 8) avec montée en puissance sur 3 coffres, LGBT jusqu'au gel à 10 ; remplace la limite « 1er coffre = Bleu max » | Gardé (à tester) |
