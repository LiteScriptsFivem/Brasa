# Instala Apache + PHP + MariaDB + phpMyAdmin en esta carpeta (sin Docker, sin XAMPP)
$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
. "$PSScriptRoot\config.ps1"

$UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36'
$DirDesc = Join-Path $DirTmp 'descargas'

function LeerPagina($url) {
    (Invoke-WebRequest -Uri $url -UseBasicParsing -UserAgent $UA).Content
}

function Descargar($url, $destino, $referer) {
    $h = @{}
    if ($referer) { $h['Referer'] = $referer }
    for ($i = 1; $i -le 3; $i++) {
        try {
            Invoke-WebRequest -Uri $url -OutFile $destino -UseBasicParsing -UserAgent $UA -Headers $h
            if ((Get-Item $destino).Length -gt 200000) { return $true }
        } catch { }
        Start-Sleep -Seconds 2
    }
    if (Test-Path $destino) { Remove-Item $destino -Force }
    return $false
}

function Extraer($zip, $destino) {
    New-Item -ItemType Directory -Force -Path $destino | Out-Null
    if (Get-Command tar.exe -ErrorAction SilentlyContinue) {
        & tar.exe -xf $zip -C $destino
        if ($LASTEXITCODE -eq 0) { return }
    }
    Expand-Archive -Path $zip -DestinationPath $destino -Force
}

# Extrae un zip que trae una carpeta dentro y la mueve a $final
function ExtraerCarpeta($zip, $final, $patronCarpeta) {
    $tmp = Join-Path $DirTmp ('x_' + [IO.Path]::GetRandomFileName())
    Extraer $zip $tmp
    $dentro = Get-ChildItem $tmp -Directory | Where-Object { $_.Name -like $patronCarpeta } | Select-Object -First 1
    if (-not $dentro) { throw "El zip $zip no tiene la carpeta esperada ($patronCarpeta)." }
    Move-Item $dentro.FullName $final
    Remove-Item $tmp -Recurse -Force
}

try {
    Write-Host '=============================================' -ForegroundColor Cyan
    Write-Host ' Brasa - instalando componentes' -ForegroundColor Cyan
    Write-Host ' Apache + PHP + MariaDB + phpMyAdmin' -ForegroundColor Cyan
    Write-Host '=============================================' -ForegroundColor Cyan
    Write-Host " Carpeta: $Base"

    if ($Base -match '[^\x00-\x7F]') {
        Aviso 'La ruta tiene tildes, enes u otros caracteres raros. Apache puede fallar.'
        Aviso 'Recomendado: mueve la carpeta a C:\servidor y ejecuta instalar.bat desde ahi.'
        Aviso 'Continuando igualmente...'
    }

    if ($Base -match '\\AppData\\Local\\Temp\\' -or $Base -match '\.zip') {
        Fallo 'Lo estas ejecutando DENTRO del zip. Asi no funciona.'
        Aviso 'Clic derecho en el zip -> "Extraer todo..." -> elige C:\'
        Aviso 'Luego abre C:\servidor y haz doble clic en instalar.bat'
        exit 1
    }

    New-Item -ItemType Directory -Force -Path $DirTmp, $DirDesc, $DirWww, (Join-Path $DirTmp 'pma') | Out-Null

    # ---------- 1. Visual C++ (lo necesitan Apache y PHP) ----------
    Paso 'Comprobando Visual C++ Redistributable'
    $vc = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64' -ErrorAction SilentlyContinue
    if ($vc -and $vc.Installed -eq 1 -and $vc.Minor -ge 40) {
        Ok "Ya instalado ($($vc.Version))"
    } else {
        $vcExe = Join-Path $DirDesc 'vc_redist.x64.exe'
        if (-not (Descargar 'https://aka.ms/vs/17/release/vc_redist.x64.exe' $vcExe $null)) { throw 'No se pudo descargar Visual C++.' }
        Aviso 'Instalando... (Windows te pedira permiso, pulsa Si)'
        $p = Start-Process $vcExe -ArgumentList '/install', '/passive', '/norestart' -Wait -PassThru
        if ($p.ExitCode -notin 0, 1638, 3010) { throw "Visual C++ fallo (codigo $($p.ExitCode))." }
        Ok 'Instalado'
    }

    # ---------- 2. Apache ----------
    Paso 'Apache'
    if (Test-Path "$DirApache\bin\httpd.exe") {
        Ok 'Ya estaba descargado'
    } else {
        $nombre = $null
        try {
            $html = LeerPagina 'https://www.apachelounge.com/download/'
            $m = [regex]::Matches($html, 'httpd-2\.4\.\d+-\d+-win64-VS17\.zip') | ForEach-Object { $_.Value } | Sort-Object -Unique -Descending
            if ($m) { $nombre = @($m)[0] }
        } catch { }
        $candidatos = @()
        if ($nombre) { $candidatos += $nombre }
        $candidatos += 'httpd-2.4.66-251206-win64-VS17.zip'   # reserva
        $zip = Join-Path $DirDesc 'apache.zip'
        $ok = $false
        foreach ($n in ($candidatos | Select-Object -Unique)) {
            Aviso "Descargando $n"
            if (Descargar "https://www.apachelounge.com/download/VS17/binaries/$n" $zip 'https://www.apachelounge.com/download/') { $ok = $true; break }
        }
        if (-not $ok) { throw 'No se pudo descargar Apache. Bajalo a mano de apachelounge.com (win64 VS17) y descomprime la carpeta Apache24 aqui con el nombre "apache".' }
        ExtraerCarpeta $zip $DirApache 'Apache24*'
        Ok 'Descargado'
    }
    New-Item -ItemType Directory -Force -Path "$DirApache\logs", (Join-Path $Base 'backups') | Out-Null
    $confOrig = "$DirApache\conf\httpd.conf.original"
    if (-not (Test-Path $confOrig)) { Copy-Item "$DirApache\conf\httpd.conf" $confOrig }

    # ---------- 3. PHP ----------
    Paso "PHP $PhpRama"
    if (Test-Path "$DirPhp\php8apache2_4.dll") {
        Ok 'Ya estaba descargado'
    } else {
        $patron = 'php-' + [regex]::Escape($PhpRama) + '\.(\d+)-Win32-(vs1[67])-x64\.zip'
        $fuentes = @(
            'https://downloads.php.net/~windows/releases/',
            'https://www.php.net/~windows/releases/',
            'https://windows.php.net/downloads/releases/',
            'https://downloads.php.net/~windows/releases/archives/',
            'https://windows.php.net/downloads/releases/archives/'
        )
        $zip = Join-Path $DirDesc 'php.zip'
        $ok = $false
        foreach ($f in $fuentes) {
            try { $html = LeerPagina $f } catch { Aviso "No responde: $f"; continue }
            $m = [regex]::Matches($html, $patron) | Sort-Object { [int]$_.Groups[1].Value } -Descending | Select-Object -First 1
            if (-not $m) { continue }
            $n = $m.Value
            Aviso "Descargando $n"
            if (Descargar ($f + $n) $zip $null) { $ok = $true; break }
        }
        if (-not $ok) { throw "No se pudo descargar PHP $PhpRama. Bajalo a mano (version Thread Safe x64 zip) y descomprimelo en la carpeta php." }
        Extraer $zip $DirPhp
        Ok 'Descargado'
    }

    # ---------- 4. MariaDB ----------
    Paso "MariaDB $MariaRama"
    if (Test-Path "$DirMaria\bin\mariadbd.exe") {
        Ok 'Ya estaba descargado'
    } else {
        $versiones = @()
        try {
            $html = LeerPagina 'https://archive.mariadb.org/'
            $patron = 'mariadb-' + [regex]::Escape($MariaRama) + '\.(\d+)/'
            $versiones = [regex]::Matches($html, $patron) | ForEach-Object { [int]$_.Groups[1].Value } | Sort-Object -Unique -Descending | Select-Object -First 3
        } catch { }
        $candidatos = @($versiones | ForEach-Object { "$MariaRama.$_" })
        if ($MariaRama -eq '11.4') { $candidatos += '11.4.4' }   # reserva
        $zip = Join-Path $DirDesc 'mariadb.zip'
        $ok = $false
        foreach ($v in ($candidatos | Select-Object -Unique)) {
            Aviso "Descargando MariaDB $v (unos 100 MB, paciencia)"
            $urls = @(
                "https://archive.mariadb.org/mariadb-$v/winx64-packages/mariadb-$v-winx64.zip",
                "https://downloads.mariadb.org/rest-api/mariadb/$v/mariadb-$v-winx64.zip",
                "https://mirror.mariadb.org/mariadb-$v/winx64-packages/mariadb-$v-winx64.zip",
                "https://ftp.nluug.nl/db/mariadb/mariadb-$v/winx64-packages/mariadb-$v-winx64.zip"
            )
            foreach ($u in $urls) {
                Aviso "  desde $(([uri]$u).Host)"
                if (Descargar $u $zip $null) { $ok = $true; break }
            }
            if ($ok) { break }
        }
        if (-not $ok) { throw 'No se pudo descargar MariaDB.' }
        ExtraerCarpeta $zip $DirMaria 'mariadb-*'
        Ok 'Descargado'
    }

    # ---------- 5. phpMyAdmin ----------
    Paso 'phpMyAdmin'
    if (Test-Path "$DirPma\index.php") {
        Ok 'Ya estaba descargado'
    } else {
        $zip = Join-Path $DirDesc 'pma.zip'
        if (-not (Descargar 'https://www.phpmyadmin.net/downloads/phpMyAdmin-latest-all-languages.zip' $zip $null)) { throw 'No se pudo descargar phpMyAdmin.' }
        ExtraerCarpeta $zip $DirPma 'phpMyAdmin-*'
        Ok 'Descargado'
    }

    # ---------- 6. Configurar Apache ----------
    Paso 'Configurando Apache'
    $apF  = $DirApache -replace '\\', '/'
    $phpF = $DirPhp    -replace '\\', '/'
    $wwwF = $DirWww    -replace '\\', '/'
    $pmaF = $DirPma    -replace '\\', '/'
    $conf = Get-Content -Raw $confOrig
    # Ruta escrita directamente (sin la variable SRVROOT)
    $conf = $conf -replace '(?m)^Define SRVROOT .*$', ''
    $conf = $conf -replace '(?m)^ServerRoot .*$', ('ServerRoot "' + $apF + '"')
    $conf = $conf.Replace('${SRVROOT}', $apF)
    # Algunas versiones traen la ruta fija: c:/Apache24, C:/Apache24-64...
    $conf = $conf -replace '(?i)[a-z]:/Apache24[^/"\s]*', $apF
    $conf = $conf.Replace("$apF/htdocs", $wwwF)
    $conf = $conf -replace '(?m)^Listen \d+\s*$', "Listen 127.0.0.1:$PuertoWeb"
    $conf = $conf -replace '(?m)^#?ServerName .*$', "ServerName localhost:$PuertoWeb"
    $conf = $conf -replace '(?m)^#(LoadModule rewrite_module .*)$', '$1'
    $conf = $conf -replace '(?m)^(\s*)DirectoryIndex index\.html\s*$', '$1DirectoryIndex index.php index.html'
    $conf = $conf.Replace('${SRVROOT}/htdocs', $wwwF)
    $extra = @'

# ---------- Servidor local (generado por instalar.ps1) ----------
LoadModule php_module "@PHP@/php8apache2_4.dll"
PHPIniDir "@PHP@"
<FilesMatch "\.php$">
    SetHandler application/x-httpd-php
</FilesMatch>

<Directory "@WWW@">
    Options Indexes FollowSymLinks
    AllowOverride All
    Require all granted
</Directory>

Alias /phpmyadmin "@PMA@"
<Directory "@PMA@">
    AllowOverride All
    Require all granted
</Directory>
'@
    $conf += $extra.Replace('@PHP@', $phpF).Replace('@WWW@', $wwwF).Replace('@PMA@', $pmaF)
    Escribir "$DirApache\conf\httpd.conf" $conf
    Ok 'Hecho'

    # ---------- 7. Configurar PHP ----------
    Paso 'Configurando PHP'
    $ini = Get-Content -Raw "$DirPhp\php.ini-development"
    $ini = $ini -replace '(?m)^;\s*extension_dir\s*=\s*"ext"\s*$', ('extension_dir = "' + $phpF + '/ext"')
    foreach ($e in 'mysqli', 'pdo_mysql', 'mbstring', 'openssl', 'curl', 'gd', 'intl', 'zip', 'fileinfo', 'exif', 'sodium') {
        $ini = $ini -replace ('(?m)^;extension=' + $e + '\s*$'), "extension=$e"
    }
    $ini = $ini -replace '(?m)^;date\.timezone\s*=.*$', 'date.timezone = Europe/Madrid'
    $ini = $ini -replace '(?m)^upload_max_filesize\s*=.*$', 'upload_max_filesize = 128M'
    $ini = $ini -replace '(?m)^post_max_size\s*=.*$', 'post_max_size = 128M'
    Escribir "$DirPhp\php.ini" $ini
    Ok 'Hecho'

    # ---------- 8. Base de datos ----------
    Paso 'Preparando la base de datos'
    $myIni = Join-Path $DirDatos 'my.ini'
    if (Test-Path $myIni) {
        Ok 'Ya existia (tus datos se conservan)'
    } else {
        if (Test-Path $DirDatos) { throw "La carpeta 'datos' existe pero esta incompleta. Si no tiene nada importante, borrala y vuelve a ejecutar instalar.bat." }
        $installDb = @('mariadb-install-db.exe', 'mysql_install_db.exe') | ForEach-Object { Join-Path "$DirMaria\bin" $_ } | Where-Object { Test-Path $_ } | Select-Object -First 1
        & $installDb "--datadir=$DirDatos" "--port=$PuertoDB"
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path $myIni)) { throw 'No se pudo crear la base de datos.' }
        Ok 'Creada (usuario root, sin contrasena, como XAMPP)'
    }
    $my = Get-Content -Raw $myIni
    $my = $my -replace '(?m)^port\s*=\s*\d+', "port=$PuertoDB"
    $my = $my -replace '(?m)^datadir\s*=.*$', ('datadir=' + ($DirDatos -replace '\\', '/'))
    $my = $my -replace '(?m)^plugin-dir\s*=.*$', ('plugin-dir=' + ($DirMaria -replace '\\', '/') + '/lib/plugin')
    if ($my -notmatch '(?m)^bind-address') { $my = $my -replace '(?m)^\[mysqld\]\s*$', "[mysqld]`r`nbind-address=127.0.0.1" }
    Escribir $myIni $my

    # ---------- 9. phpMyAdmin ----------
    $chars = [char[]]'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
    $secreto = -join (1..32 | ForEach-Object { $chars | Get-Random })
    $pmaConf = @'
<?php
$cfg['blowfish_secret'] = '@SECRET@';
$i = 1;
$cfg['Servers'][$i]['host'] = '127.0.0.1';
$cfg['Servers'][$i]['port'] = '@PORT@';
$cfg['Servers'][$i]['auth_type'] = 'config';
$cfg['Servers'][$i]['user'] = 'root';
$cfg['Servers'][$i]['password'] = '';
$cfg['Servers'][$i]['AllowNoPassword'] = true;
$cfg['TempDir'] = '@TMP@';
'@
    Escribir "$DirPma\config.inc.php" $pmaConf.Replace('@SECRET@', $secreto).Replace('@PORT@', "$PuertoDB").Replace('@TMP@', ((Join-Path $DirTmp 'pma') -replace '\\', '/'))

    # ---------- 10. Comprobacion ----------
    Paso 'Comprobando la configuracion de Apache'
    $env:PATH = "$DirPhp;$env:PATH"
    & "$DirApache\bin\httpd.exe" -t
    if ($LASTEXITCODE -ne 0) {
        Aviso "Carpeta apache existe: $(Test-Path $DirApache)"
        Get-Content "$DirApache\conf\httpd.conf" | Select-String -Pattern '^(ServerRoot|DocumentRoot|Listen|LoadModule php|PHPIniDir)' | ForEach-Object { Aviso $_.Line }
        throw 'La configuracion de Apache tiene un error (mira el mensaje de arriba).' }

    Remove-Item $DirDesc -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host ''
    Write-Host '=============================================' -ForegroundColor Green
    Write-Host ' Componentes instalados' -ForegroundColor Green
    Write-Host '=============================================' -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ''
    Fallo "ERROR: $($_.Exception.Message)"
    Fallo 'Pulsa 'Copiar registro' y pasaselo a Claude.'
    exit 1
}
