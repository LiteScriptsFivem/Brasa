<?php
if (isset($_GET['info'])) { phpinfo(); exit; }
// Página de inicio de Brasa. Puedes borrarla o sustituirla por tu proyecto.
$dbOk = false; $dbInfo = '';
try {
    $pdo = new PDO('mysql:host=127.0.0.1;charset=utf8mb4', 'root', '', [PDO::ATTR_TIMEOUT => 2]);
    $dbInfo = $pdo->query('SELECT VERSION()')->fetchColumn();
    $dbs = $pdo->query('SHOW DATABASES')->fetchAll(PDO::FETCH_COLUMN);
    $dbs = array_values(array_diff($dbs, ['information_schema', 'mysql', 'performance_schema', 'sys']));
    $dbOk = true;
} catch (Throwable $e) {
    $dbInfo = $e->getMessage();
    $dbs = [];
}
$proyectos = array_filter(glob(__DIR__ . '/*'), 'is_dir');
$e = fn($s) => htmlspecialchars((string)$s, ENT_QUOTES, 'UTF-8');
?>
<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Brasa · localhost</title>
<style>
  :root { --bg:#14161b; --card:#1d2027; --line:#2a2e37; --txt:#e8eaee; --dim:#9aa0ab; --acc:#ee7426; --azul:#68a8da; --ok:#22c55e; --bad:#ef4444; }
  * { box-sizing:border-box }
  body { margin:0; background:var(--bg); color:var(--txt); font:15px/1.5 "Segoe UI",system-ui,sans-serif }
  .wrap { max-width:880px; margin:0 auto; padding:40px 20px }
  header { display:flex; align-items:center; gap:14px; margin-bottom:28px }
  .logo { width:44px; height:44px; border-radius:11px; background:linear-gradient(#2e333a,#1c1f24); border:2px solid var(--acc) }
  footer { margin-top:36px; color:var(--dim); font-size:13px } footer a { color:var(--azul) }
  h1 { margin:0; font-size:24px } .sub { color:var(--dim); font-size:14px }
  .grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(240px,1fr)); gap:14px; margin-bottom:28px }
  .card { background:var(--card); border:1px solid var(--line); border-radius:12px; padding:16px 18px }
  .card b { display:block; font-size:13px; color:var(--dim); font-weight:500; margin-bottom:4px }
  .ok { color:var(--ok) } .bad { color:var(--bad) }
  h2 { font-size:15px; color:var(--dim); font-weight:600; margin:0 0 10px }
  ul { list-style:none; padding:0; margin:0 0 28px; display:grid; grid-template-columns:repeat(auto-fill,minmax(200px,1fr)); gap:8px }
  li a { display:block; background:var(--card); border:1px solid var(--line); border-radius:10px; padding:10px 14px; color:var(--txt); text-decoration:none }
  li a:hover { border-color:var(--acc) }
  .vacio { color:var(--dim) }
  code { background:#262a33; padding:2px 6px; border-radius:6px }
</style>
</head>
<body>
<div class="wrap">
  <header>
    <div class="logo"></div>
    <div><h1>Brasa está funcionando</h1><div class="sub">Tus archivos van en la carpeta <code>www</code></div></div>
  </header>

  <div class="grid">
    <div class="card"><b>PHP</b><span class="ok">● <?= $e(PHP_VERSION) ?></span></div>
    <div class="card"><b>MariaDB</b>
      <?php if ($dbOk): ?><span class="ok">● <?= $e($dbInfo) ?></span>
      <?php else: ?><span class="bad">● Sin conexión</span><?php endif; ?>
    </div>
    <div class="card"><b>Herramientas</b><a href="/phpmyadmin" style="color:var(--acc)">phpMyAdmin</a> · <a href="?info" style="color:var(--acc)">phpinfo()</a></div>
  </div>

  <h2>Proyectos en www</h2>
  <?php if ($proyectos): ?>
    <ul><?php foreach ($proyectos as $p): $n = basename($p); ?><li><a href="/<?= rawurlencode($n) ?>/"><?= $e($n) ?></a></li><?php endforeach; ?></ul>
  <?php else: ?><p class="vacio">Todavía no hay ninguna carpeta. Crea una dentro de <code>www</code> y aparecerá aquí.</p><?php endif; ?>

  <h2>Bases de datos</h2>
  <?php if ($dbs): ?>
    <ul><?php foreach ($dbs as $d): ?><li><a href="/phpmyadmin/index.php?route=/database/structure&db=<?= rawurlencode($d) ?>"><?= $e($d) ?></a></li><?php endforeach; ?></ul>
  <?php else: ?><p class="vacio">No hay bases de datos todavía.</p><?php endif; ?>
  <footer>Brasa · gratis · por <a href="https://lite-scripts.com/">LiteScripts</a> · <a href="https://discord.gg/ffSndEucDF">Discord</a></footer>
</div>
</body>
</html>
