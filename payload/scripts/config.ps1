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
