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
| `GET api.php` | Renvoie les 10 meilleures parties en JSON : nom (3 lettres), vague, bananes. |
| `POST api.php` (`key`, `game_id`, `name`, `wave`, `bananas`) | Ajoute la partie, ou la met à jour. Une partie ne peut que monter. |

**Sécurité :**
- Toutes les valeurs sont vérifiées : 3 lettres, nombres bornés, identifiant de 32 caractères.
- Les requêtes sont préparées, donc pas d'injection SQL.
- Le mot de passe n'est que dans `config.php`, sur le serveur.
- La « clé du jeu » filtre seulement les envois au hasard. Elle est dans le build, donc un tricheur motivé pourrait envoyer un faux score : pour un projet étudiant, c'est accepté.
