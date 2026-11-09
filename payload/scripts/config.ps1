# ============================================================
#  Ajustes del servidor local
#  Si cambias algo aquí, vuelve a ejecutar instalar.bat
# ============================================================

$PuertoWeb = 80      # http://localhost  (igual que XAMPP)
$PuertoDB  = 3306    # MariaDB           (igual que XAMPP)

$PhpRama   = '8.4'   # Versión de PHP
$MariaRama = '11.4'  # Versión de MariaDB (LTS)

# ---- Carpetas (no hace falta tocar) ----
$Base      = Split-Path -Parent $PSScriptRoot
$DirApache = Join-Path $Base 'apache'
$DirPhp    = Join-Path $Base 'php'
$DirMaria  = Join-Path $Base 'mariadb'
$DirPma    = Join-Path $Base 'phpmyadmin'
$DirWww    = Join-Path $Base 'www'
$DirDatos  = Join-Path $Base 'datos'
$DirTmp    = Join-Path $Base 'tmp'

# ---- Idioma de los mensajes (el instalador lo pasa en BRASA_IDIOMA: es, en, fr, de) ----
try { [Console]::OutputEncoding = New-Object Text.UTF8Encoding $false } catch { }
$Idioma = "$env:BRASA_IDIOMA".ToLower()
if ($Idioma -notin 'es', 'en', 'fr', 'de') { $Idioma = 'es' }
$Textos = @{
    'cabecera'    = @{ es = ' Brasa - instalando componentes'; en = ' Brasa - installing components'; fr = ' Brasa - installation des composants'; de = ' Brasa - Komponenten werden installiert' }
    'carpeta'     = @{ es = ' Carpeta: {0}'; en = ' Folder: {0}'; fr = ' Dossier : {0}'; de = ' Ordner: {0}' }
    'ruta.rara'   = @{ es = 'La ruta tiene tildes, eñes u otros caracteres raros. Apache puede fallar.'; en = 'The path has accents or other special characters. Apache may fail.'; fr = 'Le chemin contient des accents ou d''autres caractères spéciaux. Apache peut échouer.'; de = 'Der Pfad enthält Umlaute oder andere Sonderzeichen. Apache kann scheitern.' }
    'ruta.consejo'= @{ es = 'Recomendado: vuelve a instalar en una ruta como C:\Brasa.'; en = 'Recommended: reinstall to a path like C:\Brasa.'; fr = 'Conseillé : réinstallez dans un chemin comme C:\Brasa.'; de = 'Empfohlen: In einen Pfad wie C:\Brasa neu installieren.' }
    'continuando' = @{ es = 'Continuando igualmente...'; en = 'Continuing anyway...'; fr = 'On continue quand même...'; de = 'Fahre trotzdem fort...' }
    'zip.dentro'  = @{ es = 'Lo estás ejecutando DENTRO de un zip o de una carpeta temporal. Así no funciona.'; en = 'You are running it INSIDE a zip or a temporary folder. It won''t work like that.'; fr = 'Vous l''exécutez DANS un zip ou un dossier temporaire. Cela ne fonctionnera pas.'; de = 'Du führst es INNERHALB einer ZIP-Datei oder eines temporären Ordners aus. So funktioniert es nicht.' }
    'zip.consejo' = @{ es = 'Elige una carpeta normal, por ejemplo C:\Brasa.'; en = 'Choose a normal folder, for example C:\Brasa.'; fr = 'Choisissez un dossier normal, par exemple C:\Brasa.'; de = 'Wähle einen normalen Ordner, zum Beispiel C:\Brasa.' }
    'paso.vc'     = @{ es = 'Comprobando Visual C++ (si Windows pide permiso, pulsa Sí)'; en = 'Checking Visual C++ (if Windows asks for permission, click Yes)'; fr = 'Vérification de Visual C++ (si Windows demande l''autorisation, cliquez sur Oui)'; de = 'Prüfe Visual C++ (wenn Windows um Erlaubnis fragt, klicke auf Ja)' }
    'ya.instalado'= @{ es = 'Ya instalado ({0})'; en = 'Already installed ({0})'; fr = 'Déjà installé ({0})'; de = 'Bereits installiert ({0})' }
    'vc.descarga' = @{ es = 'No se pudo descargar Visual C++.'; en = 'Could not download Visual C++.'; fr = 'Impossible de télécharger Visual C++.'; de = 'Visual C++ konnte nicht heruntergeladen werden.' }
    'vc.instalando'=@{ es = 'Instalando... (Windows te pedirá permiso, pulsa Sí)'; en = 'Installing... (Windows will ask for permission, click Yes)'; fr = 'Installation... (Windows va demander l''autorisation, cliquez sur Oui)'; de = 'Installiere... (Windows fragt um Erlaubnis, klicke auf Ja)' }
    'vc.fallo'    = @{ es = 'Visual C++ falló (código {0}).'; en = 'Visual C++ failed (code {0}).'; fr = 'Visual C++ a échoué (code {0}).'; de = 'Visual C++ ist fehlgeschlagen (Code {0}).' }
    'instalado'   = @{ es = 'Instalado'; en = 'Installed'; fr = 'Installé'; de = 'Installiert' }
    'ya.descargado'=@{ es = 'Ya estaba descargado'; en = 'Already downloaded'; fr = 'Déjà téléchargé'; de = 'Bereits heruntergeladen' }
    'descargando' = @{ es = 'Descargando {0}'; en = 'Downloading {0}'; fr = 'Téléchargement de {0}'; de = 'Lade {0} herunter' }
    'descargado'  = @{ es = 'Descargado'; en = 'Downloaded'; fr = 'Téléchargé'; de = 'Heruntergeladen' }
    'apache.fallo'= @{ es = 'No se pudo descargar Apache. Bájalo a mano de apachelounge.com (win64 VS17) y descomprime la carpeta Apache24 aquí con el nombre "apache".'; en = 'Could not download Apache. Download it manually from apachelounge.com (win64 VS17) and extract the Apache24 folder here, named "apache".'; fr = 'Impossible de télécharger Apache. Téléchargez-le à la main sur apachelounge.com (win64 VS17) et décompressez le dossier Apache24 ici sous le nom « apache ».'; de = 'Apache konnte nicht heruntergeladen werden. Lade es manuell von apachelounge.com (win64 VS17) herunter und entpacke den Ordner Apache24 hierher mit dem Namen „apache“.' }
    'no.responde' = @{ es = 'No responde: {0}'; en = 'Not responding: {0}'; fr = 'Ne répond pas : {0}'; de = 'Antwortet nicht: {0}' }
    'php.fallo'   = @{ es = 'No se pudo descargar PHP {0}. Bájalo a mano (versión Thread Safe x64 zip) y descomprímelo en la carpeta php.'; en = 'Could not download PHP {0}. Download it manually (Thread Safe x64 zip) and extract it into the php folder.'; fr = 'Impossible de télécharger PHP {0}. Téléchargez-le à la main (version Thread Safe x64 zip) et décompressez-le dans le dossier php.'; de = 'PHP {0} konnte nicht heruntergeladen werden. Lade es manuell herunter (Thread Safe x64 zip) und entpacke es in den Ordner php.' }
    'maria.descargando' = @{ es = 'Descargando MariaDB {0} (unos 100 MB, paciencia)'; en = 'Downloading MariaDB {0} (about 100 MB, please be patient)'; fr = 'Téléchargement de MariaDB {0} (environ 100 Mo, patience)'; de = 'Lade MariaDB {0} herunter (etwa 100 MB, bitte Geduld)' }
    'desde'       = @{ es = '  desde {0}'; en = '  from {0}'; fr = '  depuis {0}'; de = '  von {0}' }
    'maria.fallo' = @{ es = 'No se pudo descargar MariaDB.'; en = 'Could not download MariaDB.'; fr = 'Impossible de télécharger MariaDB.'; de = 'MariaDB konnte nicht heruntergeladen werden.' }
    'pma.fallo'   = @{ es = 'No se pudo descargar phpMyAdmin.'; en = 'Could not download phpMyAdmin.'; fr = 'Impossible de télécharger phpMyAdmin.'; de = 'phpMyAdmin konnte nicht heruntergeladen werden.' }
    'paso.apache' = @{ es = 'Configurando Apache'; en = 'Configuring Apache'; fr = 'Configuration d''Apache'; de = 'Konfiguriere Apache' }
    'paso.php'    = @{ es = 'Configurando PHP'; en = 'Configuring PHP'; fr = 'Configuration de PHP'; de = 'Konfiguriere PHP' }
    'hecho'       = @{ es = 'Hecho'; en = 'Done'; fr = 'Terminé'; de = 'Fertig' }
    'paso.db'     = @{ es = 'Preparando la base de datos'; en = 'Preparing the database'; fr = 'Préparation de la base de données'; de = 'Bereite die Datenbank vor' }
    'db.existe'   = @{ es = 'Ya existía (tus datos se conservan)'; en = 'Already existed (your data is kept)'; fr = 'Existait déjà (vos données sont conservées)'; de = 'War bereits vorhanden (deine Daten bleiben erhalten)' }
    'db.incompleta'=@{ es = 'La carpeta "datos" existe pero está incompleta. Si no tiene nada importante, bórrala y vuelve a instalar.'; en = 'The "datos" folder exists but is incomplete. If it has nothing important, delete it and install again.'; fr = 'Le dossier « datos » existe mais est incomplet. S''il ne contient rien d''important, supprimez-le et réinstallez.'; de = 'Der Ordner „datos“ existiert, ist aber unvollständig. Wenn nichts Wichtiges darin ist, lösche ihn und installiere erneut.' }
    'db.fallo'    = @{ es = 'No se pudo crear la base de datos.'; en = 'Could not create the database.'; fr = 'Impossible de créer la base de données.'; de = 'Die Datenbank konnte nicht erstellt werden.' }
    'db.creada'   = @{ es = 'Creada (usuario root, sin contraseña, como XAMPP)'; en = 'Created (user root, no password, like XAMPP)'; fr = 'Créée (utilisateur root, sans mot de passe, comme XAMPP)'; de = 'Erstellt (Benutzer root, kein Passwort, wie bei XAMPP)' }
    'paso.check'  = @{ es = 'Comprobando la configuración de Apache'; en = 'Checking the Apache configuration'; fr = 'Vérification de la configuration d''Apache'; de = 'Prüfe die Apache-Konfiguration' }
    'apache.dir'  = @{ es = 'Carpeta apache existe: {0}'; en = 'apache folder exists: {0}'; fr = 'Le dossier apache existe : {0}'; de = 'Ordner apache vorhanden: {0}' }
    'apache.conf' = @{ es = 'La configuración de Apache tiene un error (mira el mensaje de arriba).'; en = 'The Apache configuration has an error (see the message above).'; fr = 'La configuration d''Apache contient une erreur (voir le message ci-dessus).'; de = 'Die Apache-Konfiguration enthält einen Fehler (siehe Meldung oben).' }
    'fin'         = @{ es = ' Componentes instalados'; en = ' Components installed'; fr = ' Composants installés'; de = ' Komponenten installiert' }
    'copiar'      = @{ es = 'Pulsa «Copiar registro» y pásaselo a Claude.'; en = 'Click "Copy log" and pass it to Claude.'; fr = 'Cliquez sur « Copier le log » et transmettez-le à Claude.'; de = 'Klicke auf „Log kopieren“ und gib es an Claude weiter.' }
    'zip.sin'     = @{ es = 'El zip {0} no tiene la carpeta esperada ({1}).'; en = 'The zip {0} doesn''t contain the expected folder ({1}).'; fr = 'Le zip {0} ne contient pas le dossier attendu ({1}).'; de = 'Die ZIP-Datei {0} enthält nicht den erwarteten Ordner ({1}).' }
}
function T($clave) {
    $e = $Textos[$clave]
    if (-not $e) { return $clave }
    if ($e[$Idioma]) { return $e[$Idioma] }
    return $e['es']
}

function Paso($t)  { Write-Host ''; Write-Host "==> $t" -ForegroundColor Cyan }
function Ok($t)    { Write-Host "    $t" -ForegroundColor Green }
function Aviso($t) { Write-Host "    $t" -ForegroundColor Yellow }
function Fallo($t) { Write-Host "    $t" -ForegroundColor Red }

function Escribir($ruta, $texto) {
    [IO.File]::WriteAllText($ruta, $texto, (New-Object Text.UTF8Encoding $false))
}

function ProcesoEnPuerto($puerto) {
    $c = Get-NetTCPConnection -LocalPort $puerto -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $c) { return $null }
    $p = Get-Process -Id $c.OwningProcess -ErrorAction SilentlyContinue
    if ($p) { return $p }
    return [pscustomobject]@{ Id = $c.OwningProcess; ProcessName = 'desconocido'; Path = '' }
}

function NuestrosProcesos($nombre, $carpeta) {
    Get-Process $nombre -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith($carpeta, [StringComparison]::OrdinalIgnoreCase) }
}

function ExplicarPuerto($puerto) {
    $p = ProcesoEnPuerto $puerto
    if (-not $p) { return }
    Fallo "El puerto $puerto lo esta usando: $($p.ProcessName) (PID $($p.Id)) $($p.Path)"
    switch -Regex ($p.ProcessName) {
        '^(httpd|mysqld|mariadbd)$' { Aviso 'Parece XAMPP u otro servidor. Cierralo (Stop en el panel de XAMPP) y vuelve a probar.' }
        '^System$'                  { Aviso 'Es Windows (IIS o "Servicio de publicacion World Wide Web"). Desactivalo o cambia el puerto en scripts\config.ps1.' }
        '(?i)skype|teams'           { Aviso 'Cierra Skype/Teams o cambia el puerto en scripts\config.ps1.' }
        default                     { Aviso 'Cierra ese programa o cambia el puerto en scripts\config.ps1 y ejecuta instalar.bat otra vez.' }
    }
}
