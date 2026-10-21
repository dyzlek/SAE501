-- La table du classement en ligne de Bloons VR (à exécuter une fois dans phpMyAdmin, onglet SQL).
-- Une ligne par PARTIE : le jeu renvoie la même partie (game_id) à chaque vague gagnée, et la ligne est mise à jour.
CREATE TABLE IF NOT EXISTS bloons_scores (
    id         INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    game_id    CHAR(32)     NOT NULL UNIQUE,          -- identifiant tiré au hasard par le jeu pour chaque partie
    name       CHAR(3)      NOT NULL,                 -- 3 lettres, façon borne d'arcade (AAA à ZZZ)
    wave       SMALLINT UNSIGNED NOT NULL DEFAULT 0,  -- la plus haute vague gagnée
    bananas    INT UNSIGNED NOT NULL DEFAULT 0,       -- les bananes gagnées pendant la partie
    created_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_rank (wave DESC, bananas DESC)
) DEFAULT CHARSET = utf8mb4;
