using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Brasa
{
    static class ProgramaPanel
    {
        const string NombreMutex = "Local\\Brasa_Panel_Unico";
        const string EventoMostrar = "Local\\Brasa_Panel_Mostrar";
        const string EventoSalir = "Local\\Brasa_Panel_Salir";

        [STAThread]
        static int Main(string[] args)
        {
            Sistema.ActivarDpi();
            Enlaces.ActivarTls();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string exe = Application.ExecutablePath;
            string baseDir = Path.GetDirectoryName(exe);

            var ajustes = new Ajustes(Path.Combine(baseDir, "brasa.ini"));
            L.Cargar(ajustes);
            if (args.Contains("--desinstalar")) return Desinstalar(exe, baseDir);

            bool nuevo;
            using (var mutex = new Mutex(true, NombreMutex, out nuevo))
            {
                if (!nuevo)
                {
                    // Ya está abierto: le pedimos que se muestre
                    try { using (var ev = EventWaitHandle.OpenExisting(EventoMostrar)) ev.Set(); } catch { }
                    return 0;
                }

                if (!VentanaAceptar.Comprobar(ajustes)) return 0;   // sin aceptar los términos no se usa
                var motor = new Motor(baseDir);
                Ventana panel = null;

                var mostrar = new EventWaitHandle(false, EventResetMode.AutoReset, EventoMostrar);
                var salir = new EventWaitHandle(false, EventResetMode.AutoReset, EventoSalir);
                var hilo = new Thread(() =>
                {
                    var hs = new WaitHandle[] { mostrar, salir };
                    while (true)
                    {
                        int i = WaitHandle.WaitAny(hs);
                        var p = panel;
                        if (p == null) continue;
                        if (i == 0) p.MostrarDesdeFuera();
                        else { p.SalirDesdeFuera(); return; }
                    }
                }) { IsBackground = true };
                hilo.Start();

                // cambiar de idioma cierra la ventana y abre otra nueva con los textos traducidos
                bool minimizado = args.Contains("--minimizado"), reabierta = false;
                do
                {
                    panel = new Ventana(motor, ajustes, exe, minimizado, reabierta);
                    Application.Run(panel);
                    minimizado = false; reabierta = true;
                } while (panel.CambioIdioma);
                GC.KeepAlive(mutex);
            }
            return 0;
        }

        static int Desinstalar(string exe, string baseDir)
        {
            var r = MessageBox.Show(L.T("¿Quieres desinstalar Brasa?"), L.T("Desinstalar Brasa"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return 1;

            var conservar = MessageBox.Show(
                L.T("¿Conservar tus bases de datos, la carpeta www y las copias de seguridad?") + "\n\n" +
                L.F("Sí: se quedan en {0}\nNo: se borra TODO", baseDir),
                L.T("Desinstalar Brasa"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (conservar == DialogResult.Cancel) return 1;

            // 1. cerrar el panel si está abierto (apaga los servidores de forma limpia)
            try { using (var ev = EventWaitHandle.OpenExisting(EventoSalir)) ev.Set(); } catch { }
            var limite = DateTime.Now.AddSeconds(60);
            while (DateTime.Now < limite)
            {
                bool hay = Process.GetProcessesByName("Brasa").Any(p => p.Id != Process.GetCurrentProcess().Id);
                if (!hay) break;
                Thread.Sleep(500);
            }

            // 2. por si acaso, apagar lo que siga encendido
            try
            {
                var motor = new Motor(baseDir);
                motor.Parar(motor.Apache);
                motor.Parar(motor.MariaDB);
            }
            catch { }

            // 3. accesos directos y registro
            foreach (var lnk in new[] { Sistema.AccesoEscritorio, Sistema.AccesoMenuInicio })
                try { if (File.Exists(lnk)) File.Delete(lnk); } catch { }
            Sistema.QuitarRegistro();

            // 4. borrar archivos cuando este programa se haya cerrado
            string orden;
            if (conservar == DialogResult.Yes)
            {
                var borrar = new[] { "apache", "php", "mariadb", "phpmyadmin", "scripts", "tmp" };
                orden = string.Join(" & ", borrar.Select(d => "rmdir /s /q \"" + Path.Combine(baseDir, d) + "\""))
                      + " & del /f /q \"" + exe + "\" \"" + Path.Combine(baseDir, "brasa.ini") + "\"";
            }
            else orden = "rmdir /s /q \"" + baseDir + "\"";

            Process.Start(new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 3 >nul & " + orden)
            {
                CreateNoWindow = true, UseShellExecute = false, WorkingDirectory = Path.GetTempPath(),
            });

            MessageBox.Show(conservar == DialogResult.Yes
                ? L.T("Brasa desinstalado. Tus datos siguen en:") + "\n" + baseDir
                : L.T("Brasa desinstalado por completo."), "Brasa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
    }
}
