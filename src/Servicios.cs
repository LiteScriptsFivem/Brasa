using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Brasa
{
    enum Estado { Apagado, Arrancando, Encendido, Parando, Error }

    /// Datos de un servicio (Apache o MariaDB) y cómo está ahora mismo
    class Servicio
    {
        public string Nombre;
        public string Carpeta;      // carpeta raíz del programa (para reconocer sus procesos)
        public string Exe;
        public string NombreProceso;
        public int Puerto;
        public string Version = "";
        public string Log;          // archivo de registro principal

        public volatile bool Deseado;     // el usuario lo quiere encendido (para el vigilante)
        public volatile bool Ocupado;     // hay una operación en marcha
        public Estado Estado = Estado.Apagado;
        public string Detalle = "";
        public int[] Pids = new int[0];
        public DateTime? Inicio;
        public long MemoriaMB;
        public readonly List<DateTime> Reinicios = new List<DateTime>();
        public int FallosPuerto;          // comprobaciones seguidas sin ver el puerto (evita parpadeos)
    }

    /// Lógica de arranque, parada y estado. No toca la interfaz.
    class Motor
    {
        public readonly string Base;
        public readonly Servicio Apache, MariaDB;
        public readonly string DirPhp, DirDatos, DirBackups, DirWww, DirMaria;
        public string VersionPhp = "";
        public event Action<string, bool> Registro;   // (mensaje, esError)
        public volatile bool Migrando;                  // pausa el vigilante durante la migración

        public Motor(string baseDir)
        {
            Base = baseDir;
            DirPhp = Path.Combine(Base, "php");
            DirDatos = Path.Combine(Base, "datos");
            DirBackups = Path.Combine(Base, "backups");
            DirWww = Path.Combine(Base, "www");
            DirMaria = Path.Combine(Base, "mariadb");

            int pWeb = 80, pDb = 3306;
            var cfg = Path.Combine(Base, @"scripts\config.ps1");
            if (File.Exists(cfg))
            {
                var t = File.ReadAllText(cfg);
                var m = Regex.Match(t, @"\$PuertoWeb\s*=\s*(\d+)"); if (m.Success) pWeb = int.Parse(m.Groups[1].Value);
                m = Regex.Match(t, @"\$PuertoDB\s*=\s*(\d+)"); if (m.Success) pDb = int.Parse(m.Groups[1].Value);
            }

            Apache = new Servicio
            {
                Nombre = "Apache",
                Carpeta = Path.Combine(Base, "apache"),
                Exe = Path.Combine(Base, @"apache\bin\httpd.exe"),
                NombreProceso = "httpd",
                Puerto = pWeb,
                Log = Path.Combine(Base, @"apache\logs\error.log"),
            };
            MariaDB = new Servicio
            {
                Nombre = "MariaDB",
                Carpeta = DirMaria,
                Exe = Path.Combine(DirMaria, @"bin\mariadbd.exe"),
                NombreProceso = "mariadbd",
                Puerto = pDb,
            };

            Apache.Version = VersionDe(Apache.Exe);
            MariaDB.Version = VersionDe(MariaDB.Exe);
            VersionPhp = VersionDe(Path.Combine(DirPhp, "php.exe"));
        }

        static string VersionDe(string exe)
        {
            try
            {
                if (!File.Exists(exe)) return "";
                var v = FileVersionInfo.GetVersionInfo(exe);
                var s = v.ProductVersion ?? v.FileVersion ?? "";
                var m = Regex.Match(s, @"\d+\.\d+\.\d+");
                return m.Success ? m.Value : s;
            }
            catch { return ""; }
        }

        public bool Instalado { get { return File.Exists(Apache.Exe) && File.Exists(MariaDB.Exe) && File.Exists(Path.Combine(DirDatos, "my.ini")); } }

        void Log(string m, bool error = false)
        {
            var h = Registro;
            if (h != null) h(m, error);
        }

        // ---------------- Procesos y puertos ----------------

        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(int acceso, bool heredar, int pid);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr h, int flags, StringBuilder ruta, ref int tam);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

        public static string RutaDe(int pid)
        {
            IntPtr h = IntPtr.Zero;
            try
            {
                h = OpenProcess(0x1000, false, pid); // PROCESS_QUERY_LIMITED_INFORMATION
                if (h == IntPtr.Zero) return null;
                var sb = new StringBuilder(1024);
                int tam = sb.Capacity;
                return QueryFullProcessImageName(h, 0, sb, ref tam) ? sb.ToString() : null;
            }
            catch { return null; }
            finally { if (h != IntPtr.Zero) CloseHandle(h); }
        }

        public List<Process> Procesos(Servicio s)
        {
            var lista = new List<Process>();
            foreach (var p in Process.GetProcessesByName(s.NombreProceso))
            {
                var r = RutaDe(p.Id);
                if (r != null && r.StartsWith(s.Carpeta + "\\", StringComparison.OrdinalIgnoreCase)) lista.Add(p);
                else p.Dispose();
            }
            return lista;
        }

        public static bool Escuchando(int puerto)
        {
            // La tabla de puertos de Windows a veces falla o llega incompleta: si no aparece, se prueba a conectar
            try { if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(e => e.Port == puerto)) return true; }
            catch { }
            return Responde(puerto);
        }

        static bool Responde(int puerto)
        {
            try
            {
                using (var c = new System.Net.Sockets.TcpClient())
                {
                    var r = c.BeginConnect(System.Net.IPAddress.Loopback, puerto, null, null);
                    if (!r.AsyncWaitHandle.WaitOne(300)) return false;
                    c.EndConnect(r);
                    return true;
                }
            }
            catch { return false; }
        }

        /// Quién está usando un puerto (para explicar conflictos)
        public static string QuienUsa(int puerto)
        {
            try
            {
                var psi = new ProcessStartInfo("netstat.exe", "-ano -p TCP") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using (var p = Process.Start(psi))
                {
                    var salida = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(5000);
                    foreach (var linea in salida.Split(new[] { '\n' }))
                    {
                        var c = linea.Split(new[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        if (c.Length >= 5 && c[3] == "LISTENING" && c[1].EndsWith(":" + puerto))
                        {
                            int pid = int.Parse(c[4]);
                            if (pid == 4) return L.T("Windows (IIS / servicio web del sistema)");
                            try
                            {
                                using (var pr = Process.GetProcessById(pid))
                                {
                                    var ruta = RutaDe(pid);
                                    string n = pr.ProcessName;
                                    if (ruta != null && ruta.IndexOf("xampp", StringComparison.OrdinalIgnoreCase) >= 0) n += L.T(" de XAMPP");
                                    return n + " (PID " + pid + ")" + (ruta != null ? " — " + ruta : "");
                                }
                            }
                            catch { return "PID " + pid; }
                        }
                    }
                }
            }
            catch { }
            return L.T("otro programa");
        }

        /// Actualiza estado, PIDs, memoria, tiempo encendido
        public void Refrescar(Servicio s)
        {
            var procs = Procesos(s);
            try
            {
                s.Pids = procs.Select(p => p.Id).ToArray();
                long mem = 0; DateTime? ini = null;
                foreach (var p in procs)
                {
                    try { mem += p.WorkingSet64; } catch { }
                    try { var t = p.StartTime; if (ini == null || t < ini) ini = t; } catch { }
                }
                s.MemoriaMB = mem / (1024 * 1024);
                s.Inicio = ini;

                if (s.Ocupado) return; // el estado lo pone la operación en curso
                if (procs.Count == 0) { s.FallosPuerto = 0; if (s.Estado != Estado.Error) s.Estado = Estado.Apagado; }
                else if (Escuchando(s.Puerto)) { s.FallosPuerto = 0; s.Estado = Estado.Encendido; }
                else if (s.Estado == Estado.Encendido && ++s.FallosPuerto < 4) { }   // un fallo suelto no lo baja a "Arrancando"
                else s.Estado = Estado.Arrancando;
            }
            finally { foreach (var p in procs) p.Dispose(); }
        }

        // ---------------- Arrancar ----------------

        public bool Arrancar(Servicio s)
        {
            if (s.Ocupado) return false;
            s.Ocupado = true;
            try
            {
                if (Procesos(s).Count > 0 && Escuchando(s.Puerto)) { s.Estado = Estado.Encendido; s.Deseado = true; return true; }
                if (!File.Exists(s.Exe)) { Fallo(s, L.F("No está instalado: falta {0}", s.Exe)); return false; }

                if (Escuchando(s.Puerto))
                {
                    Fallo(s, L.F("El puerto {0} está ocupado por {1}. Ciérralo y vuelve a intentarlo.", s.Puerto, QuienUsa(s.Puerto)));
                    return false;
                }

                s.Estado = Estado.Arrancando; s.Detalle = "";
                Log(L.F("Encendiendo {0}...", s.Nombre));

                var psi = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
                psi.FileName = s.Exe;
                if (s == Apache)
                {
                    Directory.CreateDirectory(Path.Combine(s.Carpeta, "logs"));
                    psi.WorkingDirectory = s.Carpeta;
                    psi.EnvironmentVariables["PATH"] = DirPhp + ";" + Environment.GetEnvironmentVariable("PATH");
                    psi.EnvironmentVariables["PHPRC"] = DirPhp;
                }
                else
                {
                    psi.WorkingDirectory = Path.Combine(DirMaria, "bin");
                    psi.Arguments = "--defaults-file=\"" + Path.Combine(DirDatos, "my.ini") + "\"";
                }

                Process p;
                try { p = Process.Start(psi); }
                catch (Exception ex) { Fallo(s, L.T("No se pudo lanzar: ") + ex.Message); return false; }

                var limite = DateTime.Now.AddSeconds(s == MariaDB ? 40 : 25);
                while (DateTime.Now < limite)
                {
                    if (Escuchando(s.Puerto)) break;
                    if (p.HasExited) break;
                    Thread.Sleep(400);
                }

                if (Escuchando(s.Puerto) && Procesos(s).Count > 0)
                {
                    s.Estado = Estado.Encendido; s.Deseado = true; s.Detalle = "";
                    Log(L.F("{0} encendido en el puerto {1}.", s.Nombre, s.Puerto));
                    return true;
                }

                string motivo = p.HasExited ? L.F("{0} se ha cerrado nada más arrancar (código {1}).", s.Nombre, p.ExitCode) : L.F("{0} no responde en el puerto {1}.", s.Nombre, s.Puerto);
                if (!p.HasExited) MatarProcesos(s);
                Fallo(s, motivo);
                foreach (var l in UltimasLineas(s, 8)) Log("   " + l, true);
                return false;
            }
            finally { s.Ocupado = false; }
        }

        void Fallo(Servicio s, string msg)
        {
            s.Estado = Estado.Error;
            s.Detalle = msg;
            Log(msg, true);
        }

        public IEnumerable<string> UltimasLineas(Servicio s, int n)
        {
            string archivo = s.Log;
            if (s == MariaDB)
            {
                try
                {
                    archivo = new DirectoryInfo(DirDatos).GetFiles("*.err").OrderByDescending(f => f.LastWriteTime).Select(f => f.FullName).FirstOrDefault();
                }
                catch { archivo = null; }
            }
            if (archivo == null || !File.Exists(archivo)) return new string[0];
            try
            {
                using (var fs = new FileStream(archivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var sr = new StreamReader(fs))
                {
                    var lineas = sr.ReadToEnd().Split(new[] { '\n' }).Select(l => l.TrimEnd('\r')).Where(l => l.Trim().Length > 0).ToList();
                    return lineas.Skip(Math.Max(0, lineas.Count - n)).ToList();
                }
            }
            catch { return new string[0]; }
        }

        public string ArchivoLog(Servicio s)
        {
            if (s == Apache) return s.Log;
            try { return new DirectoryInfo(DirDatos).GetFiles("*.err").OrderByDescending(f => f.LastWriteTime).Select(f => f.FullName).FirstOrDefault(); }
            catch { return null; }
        }

        // ---------------- Parar ----------------

        public void Parar(Servicio s)
        {
            s.Deseado = false;
            if (s.Ocupado) return;
            s.Ocupado = true;
            try
            {
                if (Procesos(s).Count == 0) { s.Estado = Estado.Apagado; s.Detalle = ""; return; }
                s.Estado = Estado.Parando;
                Log(L.F("Apagando {0}...", s.Nombre));

                if (s == MariaDB)
                {
                    // apagado limpio: así no se corrompen las tablas (el gran fallo de XAMPP)
                    var admin = new[] { "mariadb-admin.exe", "mysqladmin.exe" }.Select(x => Path.Combine(DirMaria, "bin", x)).FirstOrDefault(File.Exists);
                    if (admin != null)
                    {
                        Ejecutar(admin, "-u root --host=127.0.0.1 --port=" + s.Puerto + " shutdown", 20000);
                        var lim = DateTime.Now.AddSeconds(30);
                        while (DateTime.Now < lim && Procesos(s).Count > 0) Thread.Sleep(300);
                    }
                    if (Procesos(s).Count > 0)
                    {
                        Log(L.T("MariaDB no respondió al apagado normal (¿root tiene contraseña?). Forzando cierre."), true);
                        MatarProcesos(s);
                    }
                }
                else MatarProcesos(s);

                // esperar a que Windows suelte el puerto, para que un "Reiniciar" no lo vea ocupado
                var libre = DateTime.Now.AddSeconds(5);
                while (DateTime.Now < libre && Escuchando(s.Puerto)) Thread.Sleep(250);

                s.Estado = Estado.Apagado; s.Detalle = ""; s.FallosPuerto = 0;
                Log(L.F("{0} apagado.", s.Nombre));
            }
            finally { s.Ocupado = false; }
        }

        void MatarProcesos(Servicio s)
        {
            foreach (var p in Procesos(s))
                using (p) { try { p.Kill(); p.WaitForExit(5000); } catch { } }
        }

        // ---------------- Utilidades de base de datos ----------------

        public static string Ejecutar(string exe, string args, int espera, string entrada = null)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = entrada != null,
            };
            using (var p = Process.Start(psi))
            {
                var err = new StringBuilder();
                p.ErrorDataReceived += (o, e) => { if (e.Data != null) lock (err) err.AppendLine(e.Data); };
                p.BeginErrorReadLine();
                if (entrada != null) { p.StandardInput.Write(entrada); p.StandardInput.Close(); }
                var salida = p.StandardOutput.ReadToEnd();
                if (!p.WaitForExit(espera)) { try { p.Kill(); } catch { } return "TIEMPO AGOTADO"; }
                p.WaitForExit();
                lock (err) { return p.ExitCode == 0 ? "" : (err.ToString().Trim() + " " + salida.Trim()).Trim(); }
            }
        }

        string Herramienta(params string[] nombres)
        {
            return nombres.Select(x => Path.Combine(DirMaria, "bin", x)).FirstOrDefault(File.Exists);
        }

        /// Copia de seguridad de todas las bases de datos. Devuelve la ruta o null.
        public string CopiaSeguridad(string motivo)
        {
            if (MariaDB.Estado != Estado.Encendido) { Log(L.T("Para hacer una copia, MariaDB tiene que estar encendido."), true); return null; }
            var dump = Herramienta("mariadb-dump.exe", "mysqldump.exe");
            if (dump == null) { Log(L.F("No encuentro {0}.", "mariadb-dump.exe"), true); return null; }
            Directory.CreateDirectory(DirBackups);
            var archivo = Path.Combine(DirBackups, "copia-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".sql");
            Log(L.T("Haciendo copia de seguridad") + (motivo != null ? " (" + motivo + ")" : "") + "...");
            var err = Ejecutar(dump, "-u root --host=127.0.0.1 --port=" + MariaDB.Puerto +
                " --all-databases --routines --events --triggers --single-transaction --default-character-set=utf8mb4" +
                " --result-file=\"" + archivo + "\"", 15 * 60 * 1000);
            if (err.Length > 0 || !File.Exists(archivo))
            {
                Log(L.T("La copia ha fallado: ") + err, true);
                try { File.Delete(archivo); } catch { }
                return null;
            }
            // conservar solo las 15 más recientes
            foreach (var viejo in new DirectoryInfo(DirBackups).GetFiles("copia-*.sql").OrderByDescending(f => f.Name).Skip(15))
                try { viejo.Delete(); } catch { }
            Log(L.T("Copia guardada: ") + Path.GetFileName(archivo) + " (" + (new FileInfo(archivo).Length / 1024) + " KB)");
            return archivo;
        }

        public bool HayCopiaDeHoy()
        {
            if (!Directory.Exists(DirBackups)) return false;
            return Directory.GetFiles(DirBackups, "copia-" + DateTime.Now.ToString("yyyy-MM-dd") + "_*.sql").Length > 0;
        }

        /// Importa un .sql que ya trae sus CREATE DATABASE / USE (copias completas, migración)
        public bool ImportarCompleto(string archivo)
        {
            if (MariaDB.Estado != Estado.Encendido) { Log(L.T("Para importar, MariaDB tiene que estar encendido."), true); return false; }
            var cli = Herramienta("mariadb.exe", "mysql.exe");
            if (cli == null) { Log(L.F("No encuentro {0}.", "mariadb.exe"), true); return false; }
            Log(L.F("Importando {0} en Brasa... (puede tardar)", Path.GetFileName(archivo)));
            var err = Ejecutar(cli, "-u root --host=127.0.0.1 --port=" + MariaDB.Puerto + " --default-character-set=utf8mb4", 60 * 60 * 1000,
                "SOURCE " + archivo.Replace('\\', '/') + "\n");
            if (err.Length > 0) { Log(L.T("Error al importar: ") + err, true); return false; }
            Log(L.T("Bases de datos importadas en Brasa."));
            return true;
        }

        /// ¿El .sql trae su propio CREATE DATABASE? (copia completa)
        public static bool TraeSuBase(string archivo)
        {
            try
            {
                using (var sr = new StreamReader(archivo))
                {
                    char[] buf = new char[200000];
                    int n = sr.Read(buf, 0, buf.Length);
                    var ini = new string(buf, 0, n);
                    return Regex.IsMatch(ini, @"(?im)^CREATE DATABASE ");
                }
            }
            catch { return false; }
        }

        /// Importa un .sql en la base indicada (la crea si no existe)
        public bool Importar(string archivo, string baseDatos)
        {
            if (MariaDB.Estado != Estado.Encendido) { Log(L.T("Para importar, MariaDB tiene que estar encendido."), true); return false; }
            var cli = Herramienta("mariadb.exe", "mysql.exe");
            if (cli == null) { Log(L.F("No encuentro {0}.", "mariadb.exe"), true); return false; }
            var nombre = baseDatos.Replace("`", "``");
            Log(L.F("Importando {0} en '{1}'... (puede tardar)", Path.GetFileName(archivo), baseDatos));
            var sql = "CREATE DATABASE IF NOT EXISTS `" + nombre + "` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;\n" +
                      "USE `" + nombre + "`;\n" +
                      "SOURCE " + archivo.Replace('\\', '/') + "\n";
            var err = Ejecutar(cli, "-u root --host=127.0.0.1 --port=" + MariaDB.Puerto + " --default-character-set=utf8mb4", 60 * 60 * 1000, sql);
            if (err.Length > 0) { Log(L.T("Error al importar: ") + err, true); return false; }
            Log(L.F("Importación terminada en '{0}'.", baseDatos));
            return true;
        }
    }
}
