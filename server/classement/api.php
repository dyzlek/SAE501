<?php
// API du classement en ligne de Bloons VR (SAÉ 5D.01). Deux appels, tout en JSON :
//   GET  api.php            -> les 10 meilleures parties : {"scores":[{"name":"DYL","wave":42,"bananas":5300}, ...]}
//   POST api.php            -> enregistre (ou met à jour) une partie. Corps : game_id, name, wave, bananas, key
//                              (formulaire classique, comme l'envoie UnityWebRequest.Post). Réponse : {"ok":true}
// Toutes les valeurs sont vérifiées, et la base n'est touchée que par des requêtes préparées (pas d'injection SQL).
// Une partie ne peut que MONTER : un envoi avec une vague plus basse que celle déjà enregistrée ne la fait pas baisser.

header('Content-Type: application/json; charset=utf-8');

if (!file_exists(__DIR__ . '/config.php')) {
    http_response_code(500);
    exit(json_encode(['error' => 'config.php manquant : copier config.example.php et le remplir']));
}
require __DIR__ . '/config.php';

const TOP = 10;
const MAX_WAVE = 10000;          // au-delà, ce n'est pas une vraie partie
const MAX_BANANAS = 100000000;

function fail(int $code, string $message): void
{
    http_response_code($code);
    exit(json_encode(['error' => $message]));
}

try {
    $db = new PDO('mysql:host=' . DB_HOST . ';dbname=' . DB_NAME . ';charset=utf8mb4', DB_USER, DB_PASS,
        [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC]);
} catch (PDOException $e) {
    fail(500, 'base indisponible');   // on ne renvoie jamais le détail (il peut contenir l'identifiant)
}

if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    $rows = $db->query('SELECT name, wave, bananas FROM bloons_scores ORDER BY wave DESC, bananas DESC, updated_at ASC LIMIT ' . TOP)->fetchAll();
    foreach ($rows as &$r) { $r['wave'] = (int)$r['wave']; $r['bananas'] = (int)$r['bananas']; }
    exit(json_encode(['scores' => $rows]));
}

if ($_SERVER['REQUEST_METHOD'] !== 'POST') fail(405, 'GET ou POST seulement');

// --- Vérification de ce qu'envoie le jeu ---
if (($_POST['key'] ?? '') !== GAME_KEY) fail(403, 'clé du jeu invalide');
$gameId = $_POST['game_id'] ?? '';
$name = strtoupper($_POST['name'] ?? '');
$wave = filter_var($_POST['wave'] ?? null, FILTER_VALIDATE_INT, ['options' => ['min_range' => 0, 'max_range' => MAX_WAVE]]);
$bananas = filter_var($_POST['bananas'] ?? null, FILTER_VALIDATE_INT, ['options' => ['min_range' => 0, 'max_range' => MAX_BANANAS]]);
if (!preg_match('/^[0-9a-f]{32}$/', $gameId)) fail(400, 'game_id invalide');
if (!preg_match('/^[A-Z]{3}$/', $name)) fail(400, 'nom invalide (3 lettres)');
if ($wave === false || $bananas === false) fail(400, 'score invalide');

// Nouvelle partie : on l'ajoute ; partie connue : on garde le meilleur (une partie ne redescend pas)
$sql = 'INSERT INTO bloons_scores (game_id, name, wave, bananas) VALUES (:g, :n, :w, :b)
        ON DUPLICATE KEY UPDATE name = VALUES(name), wave = GREATEST(wave, VALUES(wave)), bananas = GREATEST(bananas, VALUES(bananas))';
$db->prepare($sql)->execute([':g' => $gameId, ':n' => $name, ':w' => $wave, ':b' => $bananas]);
exit(json_encode(['ok' => true]));
