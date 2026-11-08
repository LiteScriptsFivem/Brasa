#!/bin/bash
set -e
cd "$(dirname "$0")"
mkdir -p build
API=/usr/lib/mono/4.7.2-api
REFS="-nostdlib -r:$API/mscorlib.dll -r:$API/System.dll -r:$API/System.Windows.Forms.dll -r:$API/System.Drawing.dll -r:$API/System.Core.dll"
COMUN="-target:winexe -codepage:utf8 -optimize+ -win32icon:res/brasa.ico -nowarn:618"
RES="-resource:res/lt-emblema.png,lt-emblema.png -resource:res/logo.png,logo.png -resource:res/brasa.ico,brasa.ico"
for c in verde ambar rojo gris; do RES="$RES -resource:res/bandeja-$c.ico,bandeja-$c.ico"; done
mcs $COMUN $REFS $RES -out:build/Brasa.exe src/Info.cs src/Comun.cs src/Controles.cs src/Terminos.cs src/Servicios.cs src/Migracion.cs src/Panel.cs src/ProgramaPanel.cs
sed 's/Panel del servidor local Brasa/Programa para poner Brasa en tu PC/' src/Info.cs > build/InfoInst.cs
mcs $COMUN $REFS $RES \
  -resource:build/Brasa.exe,Brasa.exe \
  -resource:payload/scripts/instalar.ps1,instalar.ps1 \
  -resource:payload/scripts/config.ps1,config.ps1 \
  -resource:payload/www/index.php,index.php \
  -out:build/Brasa-Instalador.exe build/InfoInst.cs src/Comun.cs src/Controles.cs src/Terminos.cs src/Servicios.cs src/Instalador.cs
ls -la build/*.exe
