-- À exécuter UNE fois dans phpMyAdmin (onglet SQL) si la table bloons_scores a été créée avec la première version
-- (noms en 3 lettres, sans colonne « device »). schema.sql ne la modifie pas : « CREATE TABLE IF NOT EXISTS »
-- ne touche pas une table qui existe déjà. Sans cette mise à jour, chaque nouveau score est refusé par la base.
ALTER TABLE bloons_scores
    ADD COLUMN device CHAR(32) NOT NULL DEFAULT '' AFTER game_id,
    MODIFY name VARCHAR(12) NOT NULL;
