<?php
// API du classement en ligne de Bloons VR (SAÉ 5D.01). Tout en JSON.
//   GET  api.php                         -> les 10 meilleures parties : {"scores":[{"name":"DYLAN","wave":42,"bananas":5300}, ...]}
//   GET  api.php?check=NOM&device=ID     -> {"state":"free"} (libre), "mine" (déjà à ce casque) ou "taken" (à un autre casque)
//   POST api.php  action=claim           -> réserve le nom pour ce casque (key, device, name). {"ok":true} ou 409 {"error":"taken"}
//   POST api.php  action=score           -> enregistre (ou met à jour) une partie (key, device, game_id, name, wave, bananas).
//                                           Refusé si le nom n'appartient pas à ce casque.
// Un nom appartient au premier casque qui le réserve : personne d'autre ne peut écrire sous ce nom.
// Toutes les valeurs sont vérifiées, et la base n'est touchée que par des requêtes préparées (pas d'injection SQL).
// Une partie ne peut que MONTER : un envoi avec une vague plus basse ne la fait pas baisser.

header('Content-Type: application/json; charset=utf-8');

if (!file_exists(__DIR__ . '/config.php')) {
    http_response_code(500);
    exit(json_encode(['error' => 'config.php manquant : copier config.example.php et le remplir']));
}
require __DIR__ . '/config.php';

const TOP = 10;
const MAX_WAVE = 10000;          // au-delà, ce n'est pas une vraie partie
const MAX_BANANAS = 100000000;
const NAME_PATTERN = '/^[A-Z0-9-]{3,12}$/';
const ID_PATTERN = '/^[0-9a-f]{32}$/';

function fail(int $code, string $message): void
{
    http_response_code($code);
    exit(json_encode(['error' => $message]));
}

// Le nom, en majuscules, vérifié ; sinon la requête s'arrête
function readName(string $raw): string
{
    $name = strtoupper(trim($raw));
    if (!preg_match(NAME_PATTERN, $name)) fail(400, 'nom invalide (3 à 12 caractères : lettres, chiffres, -)');
    return $name;
}

function readId(string $raw, string $what): string
{
    if (!preg_match(ID_PATTERN, $raw)) fail(400, "$what invalide");
    return $raw;
}

// À qui appartient ce nom : null (libre) ou l'identifiant du casque
function ownerOf(PDO $db, string $name): ?string
{
    $q = $db->prepare('SELECT device FROM bloons_names WHERE name = :n');
    $q->execute([':n' => $name]);
    $owner = $q->fetchColumn();
    return $owner === false ? null : $owner;
}

try {
    $db = new PDO('mysql:host=' . DB_HOST . ';dbname=' . DB_NAME . ';charset=utf8mb4', DB_USER, DB_PASS,
        [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC]);
} catch (PDOException $e) {
    fail(500, 'base indisponible');   // on ne renvoie jamais le détail (il peut contenir l'identifiant)
}

// ---------------- Lecture ----------------
if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    if (isset($_GET['check'])) {
        $name = readName($_GET['check']);
        $device = readId($_GET['device'] ?? '', 'device');
        $owner = ownerOf($db, $name);
        exit(json_encode(['state' => $owner === null ? 'free' : ($owner === $device ? 'mine' : 'taken')]));
    }
    $rows = $db->query('SELECT name, wave, bananas FROM bloons_scores ORDER BY wave DESC, bananas DESC, updated_at ASC LIMIT ' . TOP)->fetchAll();
    foreach ($rows as &$r) { $r['wave'] = (int)$r['wave']; $r['bananas'] = (int)$r['bananas']; }
    exit(json_encode(['scores' => $rows]));
}

if ($_SERVER['REQUEST_METHOD'] !== 'POST') fail(405, 'GET ou POST seulement');

// ---------------- Écriture : toujours la clé du jeu et l'identifiant du casque ----------------
if (($_POST['key'] ?? '') !== GAME_KEY) fail(403, 'clé du jeu invalide');
$device = readId($_POST['device'] ?? '', 'device');
$name = readName($_POST['name'] ?? '');
$action = $_POST['action'] ?? 'score';

if ($action === 'claim') {
    $owner = ownerOf($db, $name);
    if ($owner !== null && $owner !== $device) fail(409, 'taken');
    if ($owner === null) {
        // INSERT IGNORE : si deux casques réservent le même nom au même instant, un seul gagne (clé primaire)
        $db->prepare('INSERT IGNORE INTO bloons_names (name, device) VALUES (:n, :d)')->execute([':n' => $name, ':d' => $device]);
        if (ownerOf($db, $name) !== $device) fail(409, 'taken');
    }
    exit(json_encode(['ok' => true]));
}

if ($action !== 'score') fail(400, 'action inconnue');

// Un score n'est accepté que sous un nom réservé par ce casque
if (ownerOf($db, $name) !== $device) fail(403, 'ce nom appartient à un autre casque');
$gameId = readId($_POST['game_id'] ?? '', 'game_id');
$wave = filter_var($_POST['wave'] ?? null, FILTER_VALIDATE_INT, ['options' => ['min_range' => 0, 'max_range' => MAX_WAVE]]);
$bananas = filter_var($_POST['bananas'] ?? null, FILTER_VALIDATE_INT, ['options' => ['min_range' => 0, 'max_range' => MAX_BANANAS]]);
if ($wave === false || $bananas === false) fail(400, 'score invalide');

// Nouvelle partie : on l'ajoute ; partie connue (même casque) : on garde le meilleur, et le nom actuel
$sql = 'INSERT INTO bloons_scores (game_id, device, name, wave, bananas) VALUES (:g, :d, :n, :w, :b)
        ON DUPLICATE KEY UPDATE name = IF(device = VALUES(device), VALUES(name), name),
                                wave = IF(device = VALUES(device), GREATEST(wave, VALUES(wave)), wave),
                                bananas = IF(device = VALUES(device), GREATEST(bananas, VALUES(bananas)), bananas)';
$db->prepare($sql)->execute([':g' => $gameId, ':d' => $device, ':n' => $name, ':w' => $wave, ':b' => $bananas]);
exit(json_encode(['ok' => true]));
