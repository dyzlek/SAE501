# Classement en ligne de Bloons VR

Un petit serveur PHP + MySQL qui garde les 10 meilleures parties de tous les casques. Le jeu l'appelle depuis `Unity/Assets/_Project/Scripts/Core/Leaderboard.cs`. Sans réseau, le jeu continue de marcher : le classement local reste sur le casque.

## Installer (une seule fois)
1. **La table :** dans phpMyAdmin ([web-mmi2.iutbeziers.fr/my](http://web-mmi2.iutbeziers.fr/my/)), ouvrir sa base, onglet **SQL**, coller le contenu de `schema.sql`, puis Exécuter.
2. **Les fichiers :** créer un dossier `bloons` dans son espace web (`~dylan.belledent`), puis y copier `api.php` et `config.example.php`.
3. **La configuration :** sur le serveur, renommer `config.example.php` en `config.php` et remplir la base, l'identifiant et le mot de passe MySQL. **Ce fichier ne va jamais sur GitHub** (il est dans le `.gitignore`).
4. **Vérifier :** ouvrir [web-mmi2.iutbeziers.fr/~dylan.belledent/bloons/api.php](https://web-mmi2.iutbeziers.fr/~dylan.belledent/bloons/api.php) dans un navigateur. On doit voir `{"scores":[]}`.

L'adresse est écrite dans `Leaderboard.Url`. Si le dossier change, il faut la changer aussi. Le casque n'accepte que le **HTTPS**.

## Ce que fait l'API (`api.php`)
| Appel | Effet |
|---|---|
| `GET api.php` | Renvoie les 10 meilleures parties en JSON : nom, vague, bananes. |
| `GET api.php?check=NOM&device=ID` | Dit si le nom est `free` (libre), `mine` (déjà à ce casque) ou `taken` (pris par un autre casque). |
| `POST` `action=claim` (`key`, `device`, `name`) | Réserve le nom pour ce casque. Répond 409 s'il est déjà pris. |
| `POST` `action=score` (`key`, `device`, `game_id`, `name`, `wave`, `bananas`) | Ajoute la partie, ou la met à jour. Refusé si le nom n'appartient pas à ce casque. Une partie ne peut que monter. |

## Noms uniques
- Au premier lancement, le jeu tire au hasard un **identifiant caché** pour le casque, et le garde sur le casque.
- Le joueur reçoit un nom par défaut, `SINGE-` suivi de 4 chiffres.
- Le **premier casque** qui prend un nom le garde (table `bloons_names`). Un autre casque ne peut ni le réserver, ni envoyer un score sous ce nom.
- Un nom fait de 3 à 12 caractères : lettres, chiffres et « - ».
- **Limite :** si on désinstalle le jeu, le casque perd son identifiant, et donc son nom. Sur les casques partagés de l'IUT, un nom est réservé **par casque**, pas par personne.

## Sécurité
- Toutes les valeurs sont vérifiées, et les requêtes sont préparées : pas d'injection SQL.
- Le mot de passe n'est que dans `config.php`, sur le serveur.
- La « clé du jeu » filtre seulement les envois au hasard. Elle est dans le build, donc un tricheur motivé pourrait envoyer un faux score : pour un projet étudiant, c'est accepté.
