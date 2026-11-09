# Compila Brasa en Windows con el csc.exe de .NET Framework (equivalente a build.sh)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
New-Item -ItemType Directory -Force build | Out-Null

$comun = @('/target:winexe', '/codepage:65001', '/optimize+', '/win32icon:res\brasa.ico', '/nowarn:618')
$res = @('/resource:res\lt-emblema.png,lt-emblema.png', '/resource:res\logo.png,logo.png', '/resource:res\brasa.ico,brasa.ico')
foreach ($c in 'verde', 'ambar', 'rojo', 'gris') { $res += "/resource:res\bandeja-$c.ico,bandeja-$c.ico" }

$panel = 'Info', 'Idioma', 'Textos', 'Comun', 'Controles', 'Terminos', 'Servicios', 'Migracion', 'Panel', 'ProgramaPanel' | ForEach-Object { "src\$_.cs" }
& $csc /nologo /out:build\Brasa.exe @comun @res @panel
if ($LASTEXITCODE) { throw "Fallo al compilar Brasa.exe" }

(Get-Content src\Info.cs -Raw -Encoding UTF8).Replace('Panel del servidor local Brasa', 'Programa para poner Brasa en tu PC') |
    Set-Content build\InfoInst.cs -Encoding UTF8
$extra = '/resource:build\Brasa.exe,Brasa.exe', '/resource:payload\scripts\instalar.ps1,instalar.ps1',
         '/resource:payload\scripts\config.ps1,config.ps1', '/resource:payload\www\index.php,index.php'
$inst = @('build\InfoInst.cs') + ('Idioma', 'Textos', 'Comun', 'Controles', 'Terminos', 'Servicios', 'Instalador' | ForEach-Object { "src\$_.cs" })
& $csc /nologo /out:build\Brasa-Instalador.exe @comun @res @extra @inst
if ($LASTEXITCODE) { throw "Fallo al compilar Brasa-Instalador.exe" }

Get-ChildItem build\*.exe | Select-Object Name, Length, LastWriteTime
