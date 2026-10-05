# Journal de bord — Général (équipe)

_Vue d'ensemble : décisions, jalons, blocages communs. Le détail de chacun est dans son journal perso. Entrée la plus récente en haut._

| Journal perso | |
|---|---|
| [Dylan](dylan.md) · [Maxens](maxens.md) · [Nicolas](nicolas.md) | ce que chacun a fait, ses blocages, **son usage de l'IA** |

## Décisions
| Date | Décision | Pourquoi |
|---|---|---|
| 2026-10-05 | Idée retenue : tower defense VR univers Bloons (Quincy à l'arc + singes posés à la main) | Gestes VR forts (arc, saisir/poser, ramasser) |
| 2026-10-05 | Le plateau du hub = la carte en miniature, **en direct** (ballons, singes, joueurs). Placement libre des singes, sauf sur la piste et hors carte | Même vision au hub et sur la carte ; prépare un 2e joueur visible |
| 2026-10-05 | Hub et carte dans **une seule scène** (deux zones, téléportation) | Pour que la carte tourne pendant qu'on est au hub |
| 2026-10-05 | Prototype d'abord au clavier/souris (pas de casque dispo), VR ensuite | Valider les mécaniques sans attendre le matériel |
| 2026-10-05 | Un seul projet Unity en **6000.6** ; intégration des 3 prototypes (hub, coffres, bananier) sur `feat/integration` avant `main` | 3 projets séparés et 2 versions d'Unity ne pouvaient pas cohabiter dans `Unity/` |
| 2026-10-05 | Git : `main` stable + une branche par tâche + PR relue | Éviter de casser le build commun |
| _à trancher_ | Nom du jeu · assets Bloons ou maison · périmètre définitif | Avant le GDD v1 (ven. 9 oct.) |

## Jalons
- [ ] Ven. 9 oct. — GDD v1 + prototype gris jouable
- [ ] Ven. 23 oct. — un autre groupe teste le jeu
- [ ] Ven. 13 nov. — rendu + oral

## Semaine 1 · 5-9 oct. — PROUVER
**Lun. 5 oct.**
- Fait :
  - lancement de la SAÉ, dépôt GitHub, brouillon du GDD v0 (idée TD Bloons) ;
  - composition du groupe envoyée à Antoine ;
  - trois prototypes au clavier et à la souris : hub, plateau et carte (Dylan), coffres (Nicolas), bananier (Maxens) ;
  - tests croisés entre nous ;
  - intégration dans un seul projet Unity 6000.6 (`feat/integration`), avec un hub en cercle autour du joueur (plateau, bibliothèque, bananier et panier, coffre).
- Bloque : pas de casque disponible aujourd'hui, donc rien de testé en VR.
- Prochaine étape :
  - au coffre, faire sortir un singe de la rareté gagnée, qui rejoint la bibliothèque ;
  - rendre le plateau plus grand et alléger les textes ;
  - réserver les casques et choisir l'outil de suivi ;
  - GDD v1 pour vendredi.
