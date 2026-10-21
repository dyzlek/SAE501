-- Les tables du classement en ligne de Bloons VR (à exécuter une fois dans phpMyAdmin, onglet SQL).

-- Les noms réservés : un nom appartient au premier casque qui l'a pris (device = identifiant tiré au hasard
-- par le jeu au premier lancement, gardé sur le casque). Un autre casque ne peut pas l'utiliser.
CREATE TABLE IF NOT EXISTS bloons_names (
    name       VARCHAR(12) NOT NULL PRIMARY KEY,      -- en majuscules : A-Z, 0-9 et « - », 3 à 12 caractères
    device     CHAR(32)    NOT NULL,
    created_at DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_device (device)
) DEFAULT CHARSET = utf8mb4;

-- Une ligne par PARTIE : le jeu renvoie la même partie (game_id) à chaque vague gagnée, et la ligne est mise à jour.
CREATE TABLE IF NOT EXISTS bloons_scores (
    id         INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    game_id    CHAR(32)     NOT NULL UNIQUE,          -- identifiant tiré au hasard par le jeu pour chaque partie
    device     CHAR(32)     NOT NULL,                 -- le casque qui a joué
    name       VARCHAR(12)  NOT NULL,                 -- son nom au moment de la partie
    wave       SMALLINT UNSIGNED NOT NULL DEFAULT 0,  -- la plus haute vague gagnée
    bananas    INT UNSIGNED NOT NULL DEFAULT 0,       -- les bananes gagnées pendant la partie
    created_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_rank (wave DESC, bananas DESC)
) DEFAULT CHARSET = utf8mb4;
