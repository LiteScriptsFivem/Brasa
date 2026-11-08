using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Brasa
{
    /// Pasa bases de datos y archivos de XAMPP a Brasa
    class MigradorXampp
    {
        readonly Motor motor;
        readonly Action<string, bool> salida;
        void log(string m, bool error = false) { salida(m, error); }
        public string Carpeta;

        static readonly string[] BasesSistema = { "information_schema", "mysql", "performance_schema", "sys", "phpmyadmin", "test" };
        static readonly string[] DefectoHtdocs = { "dashboard", "img", "webalizer", "xampp", "applications.html", "bitnami.css", "favicon.ico" };

        public MigradorXampp(Motor motor, string carpeta, Action<string, bool> log)
        {
            this.motor = motor; salida = log; Carpeta = carpeta;
        }

        public static string Buscar()
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                try
                {
                    if (d.DriveType != DriveType.Fixed) continue;
                    var c = Path.Combine(d.Name, "xampp");
                    if (EsXampp(c)) return c;
                }
                catch { }
            }
            return null;
        }

        public static bool EsXampp(string c)
        {
            return !string.IsNullOrEmpty(c) && File.Exists(Path.Combine(c, @"mysql\bin\mysqld.exe"));
        }

        string Bin(string exe) { return Path.Combine(Carpeta, @"mysql\bin", exe); }
        string MyIni { get { return Bin("my.ini"); } }

        int PuertoXampp()
        {
            try
            {
                bool enMysqld = false;
                foreach (var l in File.ReadAllLines(MyIni))
                {
                    var t = l.Trim();
                    if (t.StartsWith("[")) enMysqld = t.Equals("[mysqld]", StringComparison.OrdinalIgnoreCase);
                    var m = Regex.Match(t, @"^port\s*=\s*(\d+)");
                    if (enMysqld && m.Success) return int.Parse(m.Groups[1].Value);
                }
            }
            catch { }
            return 3306;
        }

        static int PuertoLibre()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int p = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return p;
        }

        List<Process> ProcesosXampp()
        {
            var r = new List<Process>();
            foreach (var n in new[] { "mysqld", "mariadbd" })
                foreach (var p in Process.GetProcessesByName(n))
                {
                    var ruta = Motor.RutaDe(p.Id);
                    if (ruta != null && ruta.StartsWith(Carpeta + "\\", StringComparison.OrdinalIgnoreCase)) r.Add(p); else p.Dispose();
                }
            return r;
        }

        string Cliente(int puerto, string sql, out string error)
        {
            var psi = new ProcessStartInfo(Bin("mysql.exe"), "-u root --host=127.0.0.1 --port=" + puerto + " -N -B -e \"" + sql + "\"")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            using (var p = Process.Start(psi))
            {
                var salida = p.StandardOutput.ReadToEnd();
                error = p.StandardError.ReadToEnd().Trim();
                p.WaitForExit(30000);
                if (p.ExitCode != 0 && error.Length == 0) error = "código " + p.ExitCode;
                if (p.ExitCode == 0) error = "";
                return salida;
            }
        }

        /// Hace todo el proceso. Devuelve true si ha ido bien.
        public bool Migrar(bool bases, bool archivos)
        {
            bool ok = true;
            if (bases) ok &= MigrarBases();
            if (archivos) ok &= CopiarHtdocs();
            log(ok ? "Migración terminada. Ya puedes desinstalar XAMPP si quieres." : "La migración ha terminado con avisos (mira arriba).", !ok);
            return ok;
        }

        bool MigrarBases()
        {
            if (!File.Exists(Bin("mysqldump.exe")) || !File.Exists(Bin("mysql.exe")))
            { log("No encuentro mysqldump.exe en XAMPP.", true); return false; }

            // 1. copia de seguridad de Brasa antes de tocar nada
            if (motor.MariaDB.Estado == Estado.Encendido) motor.CopiaSeguridad("antes de migrar");

            // 2. poner en marcha el MySQL de XAMPP
            Process temporal = null;
            int puerto;
            bool yaEncendido = ProcesosXampp().Count > 0;
            if (yaEncendido)
            {
                puerto = PuertoXampp();
                log("El MySQL de XAMPP ya está encendido (puerto " + puerto + "). Lo uso.");
            }
            else
            {
                puerto = PuertoLibre();
                log("Arrancando el MySQL de XAMPP en modo lectura (puerto temporal " + puerto + ")...");
                var psi = new ProcessStartInfo(Bin("mysqld.exe"),
                    "--defaults-file=\"" + MyIni + "\" --port=" + puerto + " --bind-address=127.0.0.1 --skip-grant-tables --skip-slave-start")
                { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.Combine(Carpeta, "mysql") };
                temporal = Process.Start(psi);
                var lim = DateTime.Now.AddSeconds(40);
                while (DateTime.Now < lim && !Motor.Escuchando(puerto) && !temporal.HasExited) Thread.Sleep(400);
                if (!Motor.Escuchando(puerto))
                {
                    log("El MySQL de XAMPP no arranca. Puede que sus datos estén dañados (un fallo típico de XAMPP).", true);
                    var err = Path.Combine(Carpeta, @"mysql\data\mysql_error.log");
                    if (File.Exists(err))
                        try
                        {
                            using (var fs = new FileStream(err, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var sr = new StreamReader(fs))
                            {
                                var ls = sr.ReadToEnd().Split(new[] { '\n' }).Where(x => x.Trim().Length > 0).ToList();
                                foreach (var x in ls.Skip(Math.Max(0, ls.Count - 6))) log("   " + x.TrimEnd(), true);
                            }
                        }
                        catch { }
                    try { if (!temporal.HasExited) temporal.Kill(); } catch { }
                    return false;
                }
            }

            string archivo = null;
            try
            {
                // 3. qué bases hay
                string error;
                var lista = Cliente(puerto, "SHOW DATABASES", out error);
                if (error.Length > 0)
                {
                    log("No puedo leer las bases de datos de XAMPP: " + error, true);
                    if (yaEncendido) log("Si root tiene contraseña: para MySQL en el panel de XAMPP (Stop) y pulsa Migrar otra vez.", true);
                    return false;
                }
                var bases = lista.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).Where(x => x.Length > 0 && !BasesSistema.Contains(x.ToLowerInvariant())).ToList();
                if (bases.Count == 0) { log("XAMPP no tiene bases de datos tuyas. Nada que pasar."); return true; }
                log("Bases encontradas: " + string.Join(", ", bases));

                // 4. exportar
                Directory.CreateDirectory(motor.DirBackups);
                archivo = Path.Combine(motor.DirBackups, "xampp-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".sql");
                log("Exportando desde XAMPP...");
                var args = "-u root --host=127.0.0.1 --port=" + puerto +
                           " --routines --events --triggers --single-transaction --default-character-set=utf8mb4" +
                           " --result-file=\"" + archivo + "\" --databases " + string.Join(" ", bases.Select(b => "\"" + b + "\""));
                var e2 = Motor.Ejecutar(Bin("mysqldump.exe"), args, 30 * 60 * 1000);
                if (e2.Length > 0) { log("Error al exportar: " + e2, true); return false; }
                log("Exportado: " + Path.GetFileName(archivo) + " (" + new FileInfo(archivo).Length / 1024 + " KB). Se guarda en Copias.");
            }
            finally
            {
                // 5. apagar el MySQL de XAMPP (si no, choca con el de Brasa)
                var admin = Bin("mysqladmin.exe");
                if (File.Exists(admin)) Motor.Ejecutar(admin, "-u root --host=127.0.0.1 --port=" + puerto + " shutdown", 20000);
                var lim = DateTime.Now.AddSeconds(20);
                while (DateTime.Now < lim && ProcesosXampp().Count > 0) Thread.Sleep(300);
                foreach (var p in ProcesosXampp()) using (p) { try { p.Kill(); } catch { } }
                if (temporal != null) temporal.Dispose();
                log("MySQL de XAMPP apagado.");
            }

            // 6. importar en Brasa
            if (motor.MariaDB.Estado != Estado.Encendido)
            {
                log("Encendiendo MariaDB de Brasa...");
                if (!motor.Arrancar(motor.MariaDB)) return false;
            }
            return motor.ImportarCompleto(archivo);
        }

        bool CopiarHtdocs()
        {
            var origen = Path.Combine(Carpeta, "htdocs");
            if (!Directory.Exists(origen)) { log("XAMPP no tiene carpeta htdocs.", true); return false; }
            Directory.CreateDirectory(motor.DirWww);
            log("Copiando tus proyectos de htdocs a www (no se sobrescribe nada que ya exista)...");
            int copiados = 0, saltados = 0;
            foreach (var entrada in Directory.GetFileSystemEntries(origen))
            {
                var nombre = Path.GetFileName(entrada);
                if (DefectoHtdocs.Contains(nombre.ToLowerInvariant())) continue;
                if (nombre.Equals("index.php", StringComparison.OrdinalIgnoreCase))
                {
                    // el index.php de XAMPP solo redirige a su dashboard
                    try { if (File.ReadAllText(entrada).Contains("/dashboard/")) continue; } catch { }
                }
                Copiar(entrada, Path.Combine(motor.DirWww, nombre), ref copiados, ref saltados);
            }
            log("Archivos copiados: " + copiados + (saltados > 0 ? " (" + saltados + " ya existían y no se han tocado)" : "") + ".");
            return true;
        }

        void Copiar(string desde, string hasta, ref int copiados, ref int saltados)
        {
            if (Directory.Exists(desde))
            {
                Directory.CreateDirectory(hasta);
                foreach (var e in Directory.GetFileSystemEntries(desde))
                    Copiar(e, Path.Combine(hasta, Path.GetFileName(e)), ref copiados, ref saltados);
                return;
            }
            if (File.Exists(hasta)) { saltados++; return; }
            try { File.Copy(desde, hasta); copiados++; }
            catch (Exception ex) { log("No se pudo copiar " + desde + ": " + ex.Message, true); }
        }
    }

    /// Ventana "Migrar desde XAMPP"
    class VentanaMigrar : FormOscuro
    {
        readonly Motor motor;
        readonly TextBox txt;
        readonly CheckBox chkBases, chkArchivos;
        readonly BotonR btnMigrar, btnCerrar;
        readonly RichTextBox caja;
        readonly ProgressBar barra;
        bool trabajando;
        public event Action Terminado;

        public VentanaMigrar(Motor motor)
        {
            this.motor = motor;
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Tema.Fuente(9.5f);
            Text = "Migrar desde XAMPP";
            Icon = Recursos.Icono("brasa.ico", 32);
            BackColor = Tema.Fondo;
            ForeColor = Tema.Texto;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 500);

            var t = Tema.Etiqueta("Migrar desde XAMPP a Brasa", 14f, Tema.Texto, FontStyle.Bold);
            t.Location = new Point(20, 16);
            var s = Tema.Etiqueta("Copia tus bases de datos y tus proyectos. XAMPP no se borra ni se modifica.", 9f, Tema.Tenue);
            s.Location = new Point(22, 48);

            var l1 = Tema.Seccion("CARPETA DE XAMPP", 20, 84);
            txt = new TextBox { Location = new Point(20, 106), Width = 410, BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, BorderStyle = BorderStyle.FixedSingle, Font = Tema.Fuente(10.5f) };
            txt.Text = MigradorXampp.Buscar() ?? @"C:\xampp";
            var cambiar = Tema.Boton1("Cambiar...");
            cambiar.SetBounds(440, 104, 100, 30);
            cambiar.Click += (o, e) =>
            {
                using (var d = new FolderBrowserDialog { Description = "Elige la carpeta de XAMPP (la que tiene mysql y htdocs)" })
                    if (d.ShowDialog(this) == DialogResult.OK) txt.Text = d.SelectedPath;
            };

            chkBases = Tema.Casilla("Bases de datos (todas las tuyas)"); chkBases.Location = new Point(20, 148); chkBases.Checked = true;
            chkArchivos = Tema.Casilla("Proyectos de htdocs → carpeta www"); chkArchivos.Location = new Point(20, 174); chkArchivos.Checked = true;

            var nota = Tema.Etiqueta("Antes de empezar, Brasa hace una copia de seguridad. Si en Brasa ya existe una base con el mismo nombre, sus tablas se sustituyen por las de XAMPP.", 8.5f, Tema.Ambar);
            nota.AutoSize = false;
            nota.Bounds = new Rectangle(20, 204, 520, 36);
            var marca = Tema.Etiqueta("XAMPP pertenece a sus autores. Brasa no está afiliado ni respaldado por ellos: solo lee tus propios datos para copiarlos.", 8f, Tema.Tenue);
            marca.AutoSize = false;
            marca.Bounds = new Rectangle(20, 240, 520, 30);

            barra = new ProgressBar { Location = new Point(20, 274), Size = new Size(520, 6), Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 0, Visible = false };
            caja = Tema.CajaTexto(new Rectangle(20, 286, 520, 156));

            btnCerrar = Tema.Boton1("Cerrar");
            btnCerrar.SetBounds(320, 452, 100, 36);
            btnCerrar.Click += (o, e) => Close();
            btnMigrar = Tema.Boton1("Migrar", true);
            btnMigrar.SetBounds(430, 452, 110, 36);
            btnMigrar.Click += (o, e) => Empezar();

            Controls.AddRange(new Control[] { t, s, l1, txt, cambiar, chkBases, chkArchivos, nota, marca, barra, caja, btnCerrar, btnMigrar });
            ResumeLayout(false);
            PerformLayout();

            Escribir(MigradorXampp.EsXampp(txt.Text) ? "XAMPP encontrado en " + txt.Text + "." : "No encuentro XAMPP. Elige su carpeta con Cambiar...", !MigradorXampp.EsXampp(txt.Text));
            FormClosing += (o, e) => { if (trabajando) { e.Cancel = true; Escribir("Espera a que termine la migración.", true); } };
        }

        void Escribir(string m, bool error = false)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => Escribir(m, error))); return; }
            caja.SelectionStart = caja.TextLength;
            caja.SelectionColor = error ? Tema.Rojo : Tema.Texto;
            caja.AppendText(m + "\n");
            caja.ScrollToCaret();
        }

        void Empezar()
        {
            var carpeta = txt.Text.Trim().TrimEnd('\\');
            if (!MigradorXampp.EsXampp(carpeta)) { Escribir("En esa carpeta no hay un XAMPP (falta mysql\\bin\\mysqld.exe).", true); return; }
            if (!chkBases.Checked && !chkArchivos.Checked) return;
            if (!motor.Instalado) { Escribir("Brasa no está instalado del todo. Ejecuta el instalador antes.", true); return; }

            trabajando = true;
            motor.Migrando = true;
            btnMigrar.Enabled = btnCerrar.Enabled = false;
            btnMigrar.Text = "Migrando...";
            barra.Visible = true; barra.MarqueeAnimationSpeed = 30;
            bool bases = chkBases.Checked, archivos = chkArchivos.Checked;
            Action<string, bool> registro = (m, e) => Escribir(m, e);
            motor.Registro += registro;

            new Thread(() =>
            {
                bool ok = false;
                try { ok = new MigradorXampp(motor, carpeta, (m, e) => Escribir(m, e)).Migrar(bases, archivos); }
                catch (Exception ex) { Escribir("Error: " + ex.Message, true); }
                finally { motor.Migrando = false; motor.Registro -= registro; }
                BeginInvoke(new Action(() =>
                {
                    trabajando = false;
                    barra.MarqueeAnimationSpeed = 0; barra.Visible = false;
                    btnCerrar.Enabled = true;
                    btnMigrar.Enabled = true;
                    btnMigrar.Text = ok ? "Hecho" : "Reintentar";
                    var h = Terminado; if (h != null) h();
                }));
            }) { IsBackground = true }.Start();
        }
    }
}
