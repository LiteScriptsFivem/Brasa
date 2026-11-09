using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Brasa
{
    static class Tema
    {
        public static readonly Color Fondo = Color.FromArgb(20, 22, 27);
        public static readonly Color Tarjeta = Color.FromArgb(29, 32, 39);
        public static readonly Color Borde = Color.FromArgb(42, 46, 55);
        public static readonly Color Texto = Color.FromArgb(232, 234, 238);
        public static readonly Color Tenue = Color.FromArgb(154, 160, 171);
        public static readonly Color Acento = Color.FromArgb(238, 116, 38);
        public static readonly Color AcentoHover = Color.FromArgb(250, 142, 70);
        public static readonly Color AcentoOscuro = Color.FromArgb(206, 96, 26);
        public static readonly Color Azul = Color.FromArgb(104, 168, 218);
        public static readonly Color Discord = Color.FromArgb(88, 101, 242);
        public static readonly Color Boton = Color.FromArgb(40, 44, 53);
        public static readonly Color BotonHover = Color.FromArgb(52, 57, 68);
        public static readonly Color Verde = Color.FromArgb(34, 197, 94);
        public static readonly Color Ambar = Color.FromArgb(245, 158, 11);
        public static readonly Color Rojo = Color.FromArgb(239, 68, 68);
        public static readonly Color Gris = Color.FromArgb(120, 125, 135);

        public static Font Fuente(float pt, FontStyle st = FontStyle.Regular)
        {
            return new Font("Segoe UI", pt, st);
        }

        public static BotonR Boton1(string texto, bool principal = false)
        {
            return new BotonR { Text = texto, Estilo = principal ? EstiloBoton.Principal : EstiloBoton.Normal };
        }

        public static BotonR Boton2(string texto, EstiloBoton estilo)
        {
            return new BotonR { Text = texto, Estilo = estilo };
        }

        public static RichTextBox CajaTexto(Rectangle r)
        {
            return new RichTextBox
            {
                Bounds = r, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Tarjeta, ForeColor = Tenue, Font = new Font("Consolas", 8.5f),
                ScrollBars = RichTextBoxScrollBars.Vertical, DetectUrls = false,
            };
        }

        public static Label Seccion(string texto, int x, int y)
        {
            var l = Etiqueta(texto, 8f, Tenue, FontStyle.Bold);
            l.Location = new Point(x, y);
            return l;
        }

        public static Label Etiqueta(string texto, float pt, Color c, FontStyle st = FontStyle.Regular)
        {
            var l = new Label();
            l.Text = texto;
            l.Font = Fuente(pt, st);
            l.ForeColor = c;
            l.BackColor = Color.Transparent;
            l.AutoSize = true;
            return l;
        }

        public static CheckBox Casilla(string texto)
        {
            var c = new CheckBox();
            c.Text = texto;
            c.ForeColor = Texto;
            c.Font = Fuente(9.5f);
            c.AutoSize = true;
            c.BackColor = Color.Transparent;
            c.Cursor = Cursors.Hand;
            return c;
        }

        public static GraphicsPath Redondeado(Rectangle r, int radio)
        {
            var p = new GraphicsPath();
            int d = radio * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    /// Panel con esquinas redondeadas y borde fino
    class Tarjeta : Panel
    {
        public Tarjeta()
        {
            DoubleBuffered = true;
            BackColor = Tema.Fondo;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var p = Tema.Redondeado(r, 10))
            using (var b = new SolidBrush(Tema.Tarjeta))
            using (var pen = new Pen(Tema.Borde))
            {
                e.Graphics.FillPath(b, p);
                e.Graphics.DrawPath(pen, p);
            }
            base.OnPaint(e);
        }
    }

    /// Círculo de color (indicador de estado)
    class Punto : Control
    {
        Color color = Tema.Gris;
        public Color Color { get { return color; } set { color = value; Invalidate(); } }
        public Punto()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int s = Math.Min(Width, Height) - 1;
            using (var halo = new SolidBrush(Color.FromArgb(60, color)))
                e.Graphics.FillEllipse(halo, 0, 0, s, s);
            int m = s / 4;
            using (var b = new SolidBrush(color))
                e.Graphics.FillEllipse(b, m, m, s - 2 * m, s - 2 * m);
        }
    }

    /// Imagen del logo escalada con buena calidad
    class Logo : Control
    {
        Image img;
        public Logo()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            img = Recursos.Imagen("logo.png");
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (img == null) return;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(img, new Rectangle(0, 0, Width, Height));
        }
    }

    static class Recursos
    {
        public static Stream Flujo(string nombre)
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var n in asm.GetManifestResourceNames())
                if (n.EndsWith(nombre, StringComparison.OrdinalIgnoreCase))
                    return asm.GetManifestResourceStream(n);
            return null;
        }

        public static Image Imagen(string nombre)
        {
            var s = Flujo(nombre);
            return s == null ? null : Image.FromStream(s);
        }

        public static Icon Icono(string nombre, int tam)
        {
            var s = Flujo(nombre);
            if (s == null) return SystemIcons.Application;
            return new Icon(s, tam, tam);
        }

        public static void Extraer(string nombre, string destino)
        {
            using (var s = Flujo(nombre))
            {
                if (s == null) throw new Exception(L.T("Falta el recurso interno ") + nombre);
                Directory.CreateDirectory(Path.GetDirectoryName(destino));
                using (var f = File.Create(destino)) s.CopyTo(f);
            }
        }
    }

    /// Archivo de ajustes sencillo clave=valor
    class Ajustes
    {
        readonly string ruta;
        readonly Dictionary<string, string> datos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Ajustes(string ruta)
        {
            this.ruta = ruta;
            if (File.Exists(ruta))
                foreach (var linea in File.ReadAllLines(ruta))
                {
                    int i = linea.IndexOf('=');
                    if (i > 0) datos[linea.Substring(0, i).Trim()] = linea.Substring(i + 1).Trim();
                }
        }

        public bool Bool(string k, bool def)
        {
            string v;
            return datos.TryGetValue(k, out v) ? v == "1" : def;
        }
        public void PonerBool(string k, bool v) { datos[k] = v ? "1" : "0"; }
        public string Texto(string k, string def)
        {
            string v;
            return datos.TryGetValue(k, out v) ? v : def;
        }
        public void PonerTexto(string k, string v) { datos[k] = v; }

        public void Guardar()
        {
            var sb = new StringBuilder();
            foreach (var kv in datos) sb.AppendLine(kv.Key + "=" + kv.Value);
            File.WriteAllText(ruta, sb.ToString());
        }
    }

    static class Sistema
    {
        public const string NombreApp = "Brasa";
        const string ClaveRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ClaveDesinstalar = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Brasa";

        public static bool InicioConWindows
        {
            get
            {
                using (var k = Registry.CurrentUser.OpenSubKey(ClaveRun))
                    return k != null && k.GetValue(NombreApp) != null;
            }
        }

        public static void PonerInicioConWindows(bool activar, string exe)
        {
            using (var k = Registry.CurrentUser.CreateSubKey(ClaveRun))
            {
                if (activar) k.SetValue(NombreApp, "\"" + exe + "\" --minimizado");
                else if (k.GetValue(NombreApp) != null) k.DeleteValue(NombreApp);
            }
        }

        public static void RegistrarDesinstalador(string carpeta, string exe, string version)
        {
            using (var k = Registry.CurrentUser.CreateSubKey(ClaveDesinstalar))
            {
                k.SetValue("DisplayName", L.T("Brasa - Servidor local"));
                k.SetValue("DisplayIcon", exe + ",0");
                k.SetValue("DisplayVersion", version);
                k.SetValue("Publisher", "LiteScripts");
                k.SetValue("URLInfoAbout", Enlaces.Tienda);
                k.SetValue("HelpLink", Enlaces.Discord);
                k.SetValue("InstallLocation", carpeta);
                k.SetValue("UninstallString", "\"" + exe + "\" --desinstalar");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                k.SetValue("EstimatedSize", 400000, RegistryValueKind.DWord);
            }
        }

        public static void QuitarRegistro()
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(ClaveDesinstalar, false); } catch { }
            try { PonerInicioConWindows(false, ""); } catch { }
        }

        public static string AccesoEscritorio
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Brasa.lnk"); }
        }

        public static string AccesoMenuInicio
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Brasa.lnk"); }
        }

        /// Crea un acceso directo .lnk usando WScript.Shell
        public static void CrearAcceso(string lnk, string destino, string args, string descripcion)
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            object sh = Activator.CreateInstance(t);
            object a = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, sh, new object[] { lnk });
            Type at = a.GetType();
            at.InvokeMember("TargetPath", BindingFlags.SetProperty, null, a, new object[] { destino });
            at.InvokeMember("Arguments", BindingFlags.SetProperty, null, a, new object[] { args ?? "" });
            at.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, a, new object[] { Path.GetDirectoryName(destino) });
            at.InvokeMember("IconLocation", BindingFlags.SetProperty, null, a, new object[] { destino + ",0" });
            at.InvokeMember("Description", BindingFlags.SetProperty, null, a, new object[] { descripcion });
            at.InvokeMember("Save", BindingFlags.InvokeMethod, null, a, null);
            Marshal.FinalReleaseComObject(a);
            Marshal.FinalReleaseComObject(sh);
        }

        /// Windows 11 esconde los iconos nuevos en la flecha ^. Esto lo saca a la barra.
        public static void PromocionarIconoBandeja(string exe)
        {
            try
            {
                using (var raiz = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings", true))
                {
                    if (raiz == null) return;
                    foreach (var n in raiz.GetSubKeyNames())
                        using (var k = raiz.OpenSubKey(n, true))
                        {
                            var ruta = k == null ? null : k.GetValue("ExecutablePath") as string;
                            if (ruta != null && string.Equals(Path.GetFullPath(ruta), Path.GetFullPath(exe), StringComparison.OrdinalIgnoreCase))
                                k.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                        }
                }
            }
            catch { }
        }

        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        public static void ActivarDpi()
        {
            try { if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware(); } catch { }
        }
    }

    /// Ventana con la barra de título oscura (Windows 10/11), a juego con el resto
    class FormOscuro : Form
    {
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int atributo, ref int valor, int tam);

        static int Bgr(Color c) { return c.R | (c.G << 8) | (c.B << 16); }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int si = 1;
                if (DwmSetWindowAttribute(Handle, 20, ref si, 4) != 0)      // modo oscuro (Win10 20H1+ / Win11)
                    DwmSetWindowAttribute(Handle, 19, ref si, 4);           // modo oscuro (Win10 antiguos)
                int fondo = Bgr(Tema.Fondo), borde = Bgr(Tema.Borde), texto = Bgr(Tema.Texto);
                DwmSetWindowAttribute(Handle, 35, ref fondo, 4);            // color de la barra (Win11)
                DwmSetWindowAttribute(Handle, 34, ref borde, 4);            // color del borde (Win11)
                DwmSetWindowAttribute(Handle, 36, ref texto, 4);            // color del título (Win11)
            }
            catch { }
        }
    }
}
