using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Brasa
{
    class Instalador : FormOscuro
    {
        const int PasosTotales = 11;
        enum Pagina { Terminos, Opciones, Progreso, Fin }
        Pagina pagina;

        // cabecera
        Label lblTitulo, lblSub;
        // páginas
        readonly Control[] pTerminos, pOpciones, pProgreso, pFin;
        CheckBox chkAcepto;
        TextBox txtCarpeta;
        NumericUpDown numWeb, numDb;
        CheckBox chkEscritorio, chkWindows, chkAbrir;
        Label lblAviso, lblPaso, lblFin;
        ProgressBar barra;
        RichTextBox detalles;
        // pie
        BotonR btnSiguiente, btnAtras;

        int paso;
        bool instalando, fallo;
        readonly StringBuilder todo = new StringBuilder();
        string carpetaFinal;
        /// true si se ha cerrado para volver a abrirse en otro idioma
        public bool CambioIdioma { get; private set; }

        public Instalador()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Tema.Fuente(9.5f);
            Text = L.T("Instalar Brasa");
            Icon = Recursos.Icono("brasa.ico", 32);
            BackColor = Tema.Fondo;
            ForeColor = Tema.Texto;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(620, 560);

            // ---------- Cabecera ----------
            Controls.Add(new Logo { Location = new Point(24, 20), Size = new Size(56, 56) });
            lblTitulo = Tema.Etiqueta(L.T("Instalar Brasa"), 18f, Tema.Texto, FontStyle.Bold);
            lblTitulo.Location = new Point(90, 16);
            lblSub = Tema.Etiqueta("", 9f, Tema.Tenue);
            lblSub.Location = new Point(93, 52);
            Controls.AddRange(new Control[] { lblTitulo, lblSub });
            Controls.Add(new LogoLT { Location = new Point(546, 22), Size = new Size(50, 50) });
            Controls.Add(new Separador { Bounds = new Rectangle(0, 94, 620, 1) });

            // ---------- Página 1: términos ----------
            var tTer = Tema.Seccion(L.T("TÉRMINOS Y CONDICIONES"), 24, 110);
            var cmbIdioma = L.Selector(c => { L.Poner(c); CambioIdioma = true; Close(); });
            cmbIdioma.SetBounds(476, 104, 120, 24);
            var cajaTer = Tema.CajaTexto(new Rectangle(24, 132, 572, 290));
            cajaTer.Font = Tema.Fuente(9f);
            cajaTer.ForeColor = Tema.Texto;
            Terminos.Pintar(cajaTer);
            chkAcepto = Tema.Casilla(L.T("He leído y acepto los términos y condiciones"));
            chkAcepto.Location = new Point(24, 436);
            chkAcepto.CheckedChanged += (o, e) => ActualizarBotones();
            pTerminos = new Control[] { tTer, cmbIdioma, cajaTer, chkAcepto };

            // ---------- Página 2: opciones ----------
            var l1 = Tema.Seccion(L.T("CARPETA DE INSTALACIÓN"), 24, 112);
            txtCarpeta = new TextBox { Location = new Point(24, 134), Width = 456, Text = @"C:\Brasa", BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, BorderStyle = BorderStyle.FixedSingle, Font = Tema.Fuente(10.5f) };
            txtCarpeta.TextChanged += (o, e) => Comprobar();
            var btnCambiar = Tema.Boton1(L.T("Cambiar..."));
            btnCambiar.SetBounds(490, 132, 106, 30);
            btnCambiar.Click += (o, e) => ElegirCarpeta();

            var l2 = Tema.Seccion(L.T("PUERTOS"), 24, 182);
            var lw = Tema.Etiqueta("Web", 9.5f, Tema.Texto); lw.Location = new Point(24, 208);
            numWeb = Numero(80, new Point(64, 205));
            var ld = Tema.Etiqueta("MariaDB", 9.5f, Tema.Texto); ld.Location = new Point(170, 208);
            numDb = Numero(3306, new Point(234, 205));
            var lp = Tema.Etiqueta(L.T("(los habituales para desarrollo local)"), 8.5f, Tema.Tenue); lp.Location = new Point(330, 209);

            var l3 = Tema.Seccion(L.T("OPCIONES"), 24, 250);
            chkEscritorio = Tema.Casilla(L.T("Acceso directo en el escritorio")); chkEscritorio.Location = new Point(24, 272); chkEscritorio.Checked = true;
            chkWindows = Tema.Casilla(L.T("Arrancar Brasa con Windows")); chkWindows.Location = new Point(24, 298); chkWindows.Checked = true;
            chkAbrir = Tema.Casilla(L.T("Abrir Brasa al terminar")); chkAbrir.Location = new Point(24, 324); chkAbrir.Checked = true;

            lblAviso = Tema.Etiqueta("", 9f, Tema.Ambar);
            lblAviso.AutoSize = false;
            lblAviso.Bounds = new Rectangle(24, 360, 572, 70);
            pOpciones = new Control[] { l1, txtCarpeta, btnCambiar, l2, lw, numWeb, ld, numDb, lp, l3, chkEscritorio, chkWindows, chkAbrir, lblAviso };

            // ---------- Página 3: progreso ----------
            lblPaso = Tema.Etiqueta("", 11f, Tema.Texto, FontStyle.Bold);
            lblPaso.AutoSize = false;
            lblPaso.AutoEllipsis = true;
            lblPaso.Bounds = new Rectangle(24, 112, 572, 26);
            barra = new ProgressBar { Bounds = new Rectangle(24, 144, 572, 8), Maximum = PasosTotales, Style = ProgressBarStyle.Continuous };
            detalles = Tema.CajaTexto(new Rectangle(24, 166, 572, 290));
            pProgreso = new Control[] { lblPaso, barra, detalles };

            // ---------- Página 4: fin ----------
            lblFin = Tema.Etiqueta("", 10f, Tema.Texto);
            lblFin.AutoSize = false;
            lblFin.Bounds = new Rectangle(24, 112, 572, 190);
            var tarjetaDiscord = new Tarjeta { Bounds = new Rectangle(24, 316, 572, 96) };
            var td1 = Tema.Etiqueta(L.T("¿Dudas o sugerencias?"), 11f, Tema.Texto, FontStyle.Bold); td1.Location = new Point(18, 16);
            var td2 = Tema.Etiqueta(L.T("Pásate por el Discord de LiteScripts.\nEs gratis: no se garantiza respuesta, pero lo intentamos."), 8.5f, Tema.Tenue); td2.Location = new Point(19, 42);
            var bd = Tema.Boton2(L.T("Unirme al Discord"), EstiloBoton.Discord); bd.SetBounds(400, 28, 156, 40);
            bd.Click += (o, e) => Enlaces.Abrir(Enlaces.Discord);
            tarjetaDiscord.Controls.AddRange(new Control[] { td1, td2, bd });
            pFin = new Control[] { lblFin, tarjetaDiscord };

            foreach (var c in pTerminos.Concat(pOpciones).Concat(pProgreso).Concat(pFin)) Controls.Add(c);

            // ---------- Pie: LiteScripts + navegación ----------
            Controls.Add(new Separador { Bounds = new Rectangle(0, 474, 620, 1) });
            var lt = new LogoLT() { Location = new Point(24, 490), Size = new Size(48, 48) };
            var ltN = Tema.Etiqueta("LiteScripts", 10.5f, Tema.Azul, FontStyle.Bold); ltN.Location = new Point(80, 490);
            var ltT = Tema.Etiqueta(L.T("Gratis · Desarrollado por hotbambi"), 8.5f, Tema.Tenue); ltT.Location = new Point(81, 514);
            var btnDiscord = Tema.Boton2("Discord", EstiloBoton.Discord);
            btnDiscord.SetBounds(278, 494, 92, 38);
            btnDiscord.Click += (o, e) => Enlaces.Abrir(Enlaces.Discord);
            btnAtras = Tema.Boton1(L.T("Cancelar"));
            btnAtras.SetBounds(380, 494, 104, 38);
            btnAtras.Click += (o, e) => Atras();
            btnSiguiente = Tema.Boton1(L.T("Siguiente"), true);
            btnSiguiente.SetBounds(492, 494, 104, 38);
            btnSiguiente.Click += (o, e) => Adelante();
            Controls.AddRange(new Control[] { lt, ltN, ltT, btnDiscord, btnAtras, btnSiguiente });
            AcceptButton = btnSiguiente;

            ResumeLayout(false);
            PerformLayout();

            Ir(Pagina.Terminos);
            Shown += (o, e) => { var x = ExistenteInstalado(); if (x != null) txtCarpeta.Text = x; };
            FormClosing += (o, e) =>
            {
                if (instalando && MessageBox.Show(L.T("La instalación está en marcha. ¿Cancelarla?"), "Brasa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    e.Cancel = true;
            };
        }

        NumericUpDown Numero(int v, Point p)
        {
            var n = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = v, Location = p, Width = 80, BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, BorderStyle = BorderStyle.FixedSingle };
            n.ValueChanged += (o, e) => Comprobar();
            return n;
        }

        // ================= Navegación =================

        void Ir(Pagina p)
        {
            pagina = p;
            foreach (var c in pTerminos) c.Visible = p == Pagina.Terminos;
            foreach (var c in pOpciones) c.Visible = p == Pagina.Opciones;
            foreach (var c in pProgreso) c.Visible = p == Pagina.Progreso;
            foreach (var c in pFin) c.Visible = p == Pagina.Fin;
            switch (p)
            {
                case Pagina.Terminos:
                    lblTitulo.Text = L.T("Instalar Brasa");
                    lblSub.Text = L.T("Tu servidor local: Apache, PHP 8.4, MariaDB y phpMyAdmin.\nPaso 1 de 3 · Para instalar tienes que aceptar los términos.");
                    break;
                case Pagina.Opciones:
                    lblTitulo.Text = L.T("¿Dónde lo instalo?");
                    lblSub.Text = L.T("Paso 2 de 3 · Se descargan unos 150 MB de las webs oficiales.");
                    Comprobar();
                    break;
                case Pagina.Progreso:
                    lblTitulo.Text = L.T("Instalando...");
                    lblSub.Text = L.T("Paso 3 de 3 · No cierres esta ventana.");
                    break;
                case Pagina.Fin:
                    lblTitulo.Text = L.T("¡Listo!");
                    lblSub.Text = L.T("Brasa está instalado.");
                    break;
            }
            ActualizarBotones();
        }

        void ActualizarBotones()
        {
            switch (pagina)
            {
                case Pagina.Terminos:
                    btnAtras.Text = L.T("Cancelar"); btnAtras.Enabled = true;
                    btnSiguiente.Text = L.T("Siguiente"); btnSiguiente.Enabled = chkAcepto.Checked;
                    break;
                case Pagina.Opciones:
                    btnAtras.Text = L.T("Atrás"); btnAtras.Enabled = true;
                    btnSiguiente.Text = L.T("Instalar"); btnSiguiente.Enabled = ProblemaCarpeta(txtCarpeta.Text) == null;
                    break;
                case Pagina.Progreso:
                    btnAtras.Text = fallo ? L.T("Copiar registro") : L.T("Cancelar"); btnAtras.Enabled = !instalando;
                    btnSiguiente.Text = fallo ? L.T("Reintentar") : L.T("Instalando..."); btnSiguiente.Enabled = !instalando;
                    break;
                case Pagina.Fin:
                    btnAtras.Text = L.T("Cerrar"); btnAtras.Enabled = true;
                    btnSiguiente.Text = chkAbrir.Checked ? L.T("Abrir Brasa") : L.T("Cerrar"); btnSiguiente.Enabled = true;
                    break;
            }
        }

        void Adelante()
        {
            switch (pagina)
            {
                case Pagina.Terminos: if (chkAcepto.Checked) Ir(Pagina.Opciones); break;
                case Pagina.Opciones: Instalar(); break;
                case Pagina.Progreso: if (fallo) Ir(Pagina.Opciones); break;
                case Pagina.Fin:
                    if (chkAbrir.Checked)
                        try { Process.Start(new ProcessStartInfo(Path.Combine(carpetaFinal, "Brasa.exe")) { UseShellExecute = true, WorkingDirectory = carpetaFinal }); } catch { }
                    Close();
                    break;
            }
        }

        void Atras()
        {
            switch (pagina)
            {
                case Pagina.Opciones: Ir(Pagina.Terminos); break;
                case Pagina.Progreso:
                    if (fallo)
                    {
                        try { lock (todo) Clipboard.SetText(todo.ToString()); btnAtras.Text = L.T("¡Copiado!"); } catch { }
                        return;
                    }
                    Close();
                    break;
                default: Close(); break;
            }
        }

        // ================= Comprobaciones =================

        static string ExistenteInstalado()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Brasa"))
                {
                    var r = k == null ? null : k.GetValue("InstallLocation") as string;
                    return r != null && Directory.Exists(r) ? r : null;
                }
            }
            catch { return null; }
        }

        void ElegirCarpeta()
        {
            using (var d = new FolderBrowserDialog { Description = L.T("Elige dónde instalar Brasa (mejor una ruta corta, por ejemplo C:\\Brasa)") })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                var r = d.SelectedPath;
                if (!r.TrimEnd('\\').EndsWith("Brasa", StringComparison.OrdinalIgnoreCase)) r = Path.Combine(r, "Brasa");
                txtCarpeta.Text = r;
            }
        }

        static string ProblemaCarpeta(string r)
        {
            if (string.IsNullOrWhiteSpace(r)) return L.T("Escribe una carpeta.");
            try { r = Path.GetFullPath(r.Trim()); } catch { return L.T("Esa ruta no es válida."); }
            if (Regex.IsMatch(r, @"[^\x20-\x7E]")) return L.T("La ruta tiene tildes, eñes u otros caracteres raros: Apache falla con eso. Usa algo como C:\\Brasa.");
            if (r.IndexOf("OneDrive", StringComparison.OrdinalIgnoreCase) >= 0) return L.T("No la pongas dentro de OneDrive (el Escritorio o Documentos suelen estarlo): da fallos. Usa C:\\Brasa.");
            if (r.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase)) return L.T("Esa es una carpeta temporal. Usa C:\\Brasa.");
            if (r.TrimEnd('\\').Length <= 2) return L.T("Elige una carpeta, no la raíz del disco.");
            var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (pf.Length > 0 && r.StartsWith(pf, StringComparison.OrdinalIgnoreCase))
                return L.T("Mejor fuera de Archivos de programa (Windows no deja escribir ahí sin permisos). Usa C:\\Brasa.");
            return null;
        }

        void Comprobar()
        {
            if (pagina != Pagina.Opciones) return;
            var problema = ProblemaCarpeta(txtCarpeta.Text);
            ActualizarBotones();
            if (problema != null) { lblAviso.ForeColor = Tema.Rojo; lblAviso.Text = problema; return; }
            lblAviso.Text = "";
            int w = (int)numWeb.Value, d = (int)numDb.Value;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var avisos = new StringBuilder();
                try
                {
                    if (Motor.Escuchando(w)) avisos.AppendLine(L.F("El puerto {0} está ocupado por {1}.", w, Corto(Motor.QuienUsa(w))));
                    if (Motor.Escuchando(d)) avisos.AppendLine(L.F("El puerto {0} está ocupado por {1}.", d, Corto(Motor.QuienUsa(d))));
                    if (avisos.Length > 0) avisos.Append(L.T("Puedes instalar igualmente, pero cierra ese programa antes de encender Brasa. Si es otro servidor local, desde Brasa podrás migrar tus datos."));
                }
                catch { }
                try { BeginInvoke(new Action(() => { if (pagina == Pagina.Opciones && ProblemaCarpeta(txtCarpeta.Text) == null) { lblAviso.ForeColor = Tema.Ambar; lblAviso.Text = avisos.ToString(); } })); } catch { }
            });
        }

        static string Corto(string s)
        {
            int i = s.IndexOf(" — ");
            return i > 0 ? s.Substring(0, i) : s;
        }

        // ================= Instalación =================

        void Instalar()
        {
            carpetaFinal = Path.GetFullPath(txtCarpeta.Text.Trim());
            instalando = true; fallo = false; paso = 0;
            lock (todo) todo.Clear();
            detalles.Clear();
            barra.Value = 0;
            lblPaso.ForeColor = Tema.Texto;
            Ir(Pagina.Progreso);
            Paso(L.T("Preparando..."));

            bool escritorio = chkEscritorio.Checked, conWindows = chkWindows.Checked;
            int pWeb = (int)numWeb.Value, pDb = (int)numDb.Value;
            var carpeta = carpetaFinal;

            new Thread(() =>
            {
                string error = null;
                try { error = Trabajo(carpeta, pWeb, pDb, escritorio, conWindows); }
                catch (Exception ex) { error = ex.Message; }
                try { BeginInvoke(new Action(() => Acabado(error))); } catch { }
            }) { IsBackground = true }.Start();
        }

        string Trabajo(string carpeta, int pWeb, int pDb, bool escritorio, bool conWindows)
        {
            Directory.CreateDirectory(carpeta);
            var exe = Path.Combine(carpeta, "Brasa.exe");

            foreach (var p in Process.GetProcessesByName("Brasa"))
            {
                var r = Motor.RutaDe(p.Id);
                if (r != null && string.Equals(r, exe, StringComparison.OrdinalIgnoreCase))
                    return L.T("Brasa está abierto. Ciérralo (icono junto al reloj → Salir) y pulsa Reintentar.");
            }

            Linea(L.F("Copiando archivos a {0}", carpeta));
            Recursos.Extraer("Brasa.exe", exe);
            Recursos.Extraer("instalar.ps1", Path.Combine(carpeta, @"scripts\instalar.ps1"));
            var cfgRuta = Path.Combine(carpeta, @"scripts\config.ps1");
            Recursos.Extraer("config.ps1", cfgRuta);
            var cfg = File.ReadAllText(cfgRuta);
            cfg = Regex.Replace(cfg, @"(?m)^\$PuertoWeb\s*=\s*\d+", "$PuertoWeb = " + pWeb);
            cfg = Regex.Replace(cfg, @"(?m)^\$PuertoDB\s*=\s*\d+", "$PuertoDB  = " + pDb);
            File.WriteAllText(cfgRuta, cfg, new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(carpeta, "TERMINOS.txt"), Terminos.Texto, new UTF8Encoding(true));
            var www = Path.Combine(carpeta, "www");
            Directory.CreateDirectory(www);
            if (!File.Exists(Path.Combine(www, "index.php")) && !File.Exists(Path.Combine(www, "index.html")))
                Recursos.Extraer("index.php", Path.Combine(www, "index.php"));

            Linea("");
            var psi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -File \"" + Path.Combine(carpeta, @"scripts\instalar.ps1") + "\"")
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                WorkingDirectory = carpeta,
            };
            psi.EnvironmentVariables["BRASA_IDIOMA"] = L.Codigo;   // idioma de los mensajes del script
            // el script cambia la consola a UTF-8 (para tildes, acentos y umlauts)
            psi.StandardOutputEncoding = new UTF8Encoding(false); psi.StandardErrorEncoding = new UTF8Encoding(false);

            int codigo;
            using (var p = Process.Start(psi))
            {
                p.OutputDataReceived += (o, e) => { if (e.Data != null) Salida(e.Data, false); };
                p.ErrorDataReceived += (o, e) => { if (e.Data != null) Salida(e.Data, true); };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                codigo = p.ExitCode;
            }
            if (codigo != 0) return L.T("Algo ha fallado al instalar los componentes (mira los detalles).");

            // comprobación final: que esté TODO
            foreach (var f in new[] { @"apache\bin\httpd.exe", @"php\php8apache2_4.dll", @"mariadb\bin\mariadbd.exe", @"datos\my.ini", @"phpmyadmin\index.php" })
                if (!File.Exists(Path.Combine(carpeta, f))) return L.F("Falta {0} después de instalar. Pulsa Reintentar.", f);

            Paso(L.T("Creando accesos directos..."));
            var ini = Path.Combine(carpeta, "brasa.ini");
            if (!File.Exists(ini)) File.WriteAllText(ini, "AutoEncender=1\r\nVigilante=1\r\nCopiaDiaria=1\r\n");
            var aj = new Ajustes(ini);
            aj.PonerTexto("TerminosAceptados", Terminos.Version);   // aceptados en el paso 1
            aj.PonerTexto("Idioma", L.Codigo);
            aj.Guardar();

            try
            {
                Sistema.CrearAcceso(Sistema.AccesoMenuInicio, exe, "", L.T("Brasa - Servidor local"));
                if (escritorio) Sistema.CrearAcceso(Sistema.AccesoEscritorio, exe, "", L.T("Brasa - Servidor local"));
                else if (File.Exists(Sistema.AccesoEscritorio)) File.Delete(Sistema.AccesoEscritorio);
            }
            catch (Exception ex) { Linea(L.F("Aviso: no se pudo crear un acceso directo ({0})", ex.Message), true); }

            Sistema.PonerInicioConWindows(conWindows, exe);
            Sistema.RegistrarDesinstalador(carpeta, exe, Terminos.Version);
            Linea(L.T("Accesos directos creados. Brasa aparece en el menú Inicio y en Aplicaciones instaladas."));
            return null;
        }

        void Salida(string linea, bool error)
        {
            var l = linea.TrimEnd();
            if (l.StartsWith("==> "))
            {
                var t = l.Substring(4);
                if (!t.EndsWith("...") && !t.EndsWith(")")) t += "...";
                Paso(t);
            }
            Linea(l, error || l.Contains("ERROR"));
        }

        void Paso(string texto)
        {
            try
            {
                BeginInvoke(new Action(() =>
                {
                    paso = Math.Min(paso + 1, PasosTotales);
                    barra.Value = paso;
                    lblPaso.Text = texto;
                }));
            }
            catch { }
        }

        void Linea(string texto, bool error = false)
        {
            lock (todo) todo.AppendLine(texto);
            try
            {
                BeginInvoke(new Action(() =>
                {
                    detalles.SelectionStart = detalles.TextLength;
                    detalles.SelectionColor = error ? Tema.Rojo : (texto.StartsWith("==>") ? Tema.Texto : Tema.Tenue);
                    detalles.AppendText(texto + "\n");
                    detalles.ScrollToCaret();
                }));
            }
            catch { }
        }

        void Acabado(string error)
        {
            instalando = false;
            if (error == null)
            {
                fallo = false;
                barra.Value = PasosTotales;
                lblFin.Text =
                    (chkEscritorio.Checked ? L.T("Lo tienes en el menú Inicio y en el escritorio.") : L.T("Lo tienes en el menú Inicio.")) + "\n\n" +
                    "•  " + L.T("Tus proyectos van en") + "   " + Path.Combine(carpetaFinal, "www") + "\n" +
                    "•  Web:   http://localhost" + (numWeb.Value == 80 ? "" : ":" + numWeb.Value) + "/\n" +
                    "•  " + L.T("Base de datos:   localhost · usuario root · sin contraseña") + "\n" +
                    "•  " + L.T("¿Tienes datos en XAMPP? En Brasa pulsa «Migrar desde XAMPP».") + "\n\n" +
                    L.T("Al cerrar Brasa con la X sigue funcionando junto al reloj.");
                Ir(Pagina.Fin);
            }
            else
            {
                fallo = true;
                lblPaso.Text = error;
                lblPaso.ForeColor = Tema.Rojo;
                Linea("");
                Linea("ERROR: " + error, true);
                ActualizarBotones();
            }
        }

        /// El de una instalación anterior si la hay; si no, el de Windows
        static string IdiomaInicial()
        {
            try
            {
                var x = ExistenteInstalado();
                var ini = x == null ? null : Path.Combine(x, "brasa.ini");
                if (ini != null && File.Exists(ini))
                {
                    var c = new Ajustes(ini).Texto("Idioma", "");
                    if (Array.IndexOf(L.Codigos, c) >= 0) return c;
                }
            }
            catch { }
            return L.DelSistema();
        }

        [STAThread]
        static void Main()
        {
            Sistema.ActivarDpi();
            Enlaces.ActivarTls();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            L.Poner(IdiomaInicial());
            Instalador f;
            do { f = new Instalador(); Application.Run(f); } while (f.CambioIdioma);   // cambiar idioma = volver a abrir la ventana
        }
    }
}
