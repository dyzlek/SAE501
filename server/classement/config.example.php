<?php
// Configuration du classement en ligne de Bloons VR.
// Copier ce fichier en « config.php » (dans le même dossier, SUR LE SERVEUR) et le remplir.
// config.php n'est jamais envoyé sur GitHub (.gitignore) : le mot de passe reste sur le serveur.

// Base MySQL / MariaDB de l'IUT (celle visible dans phpMyAdmin)
define('DB_HOST', 'localhost');   // « Serveur » affiché en haut de phpMyAdmin
define('DB_NAME', '');            // nom de la base (colonne de gauche de phpMyAdmin)
define('DB_USER', '');            // identifiant MySQL
define('DB_PASS', '');            // mot de passe MySQL : à écrire uniquement sur le serveur

// Clé partagée avec le jeu (Leaderboard.Key dans Unity) : un score sans cette clé est refusé.
// Ce n'est pas une vraie sécurité (elle est dans le build), juste un filtre contre les envois au hasard.
define('GAME_KEY', 'bloons-vr-sae501');
