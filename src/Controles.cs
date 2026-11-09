using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Brasa
{
    enum EstiloBoton { Normal, Principal, Discord, Contorno }

    /// Botón con esquinas redondeadas, hover suave y estilos de color
    class BotonR : Control, IButtonControl
    {
        EstiloBoton estilo;
        bool encima, pulsado;
        public DialogResult DialogResult { get; set; }

        public EstiloBoton Estilo { get { return estilo; } set { estilo = value; Invalidate(); } }

        public BotonR()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            ForeColor = Tema.Texto;
            Cursor = Cursors.Hand;
            TabStop = true;
            Font = Tema.Fuente(9.5f);
        }

        public void NotifyDefault(bool value) { }
        public void PerformClick() { if (Enabled && Visible) OnClick(EventArgs.Empty); }

        protected override void OnClick(EventArgs e)
        {
            if (DialogResult != DialogResult.None)
            {
                var f = FindForm();
                if (f != null) f.DialogResult = DialogResult;
            }
            base.OnClick(e);
        }

        protected override void OnMouseEnter(EventArgs e) { encima = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { encima = false; pulsado = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { pulsado = true; Focus(); Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pulsado = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) PerformClick();
            base.OnKeyUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fondo, borde = Color.Empty, texto;
            switch (estilo)
            {
                case EstiloBoton.Principal:
                    fondo = pulsado ? Tema.AcentoOscuro : encima ? Tema.AcentoHover : Tema.Acento;
                    texto = Color.White; break;
                case EstiloBoton.Discord:
                    fondo = pulsado ? Color.FromArgb(71, 82, 196) : encima ? Color.FromArgb(108, 120, 245) : Tema.Discord;
                    texto = Color.White; break;
                case EstiloBoton.Contorno:
                    fondo = pulsado ? Color.FromArgb(70, Tema.Acento) : encima ? Color.FromArgb(40, Tema.Acento) : Color.FromArgb(14, Tema.Acento);
                    borde = Tema.Acento; texto = Tema.AcentoHover; break;
                default:
                    fondo = pulsado ? Tema.Borde : encima ? Tema.BotonHover : Tema.Boton;
                    texto = Tema.Texto; break;
            }
            if (!Enabled)
            {
                fondo = Color.FromArgb(Math.Min(fondo.A, (byte)90), fondo);
                borde = borde.IsEmpty ? borde : Color.FromArgb(90, borde);
                texto = Color.FromArgb(110, 115, 125);
            }

            int radio = Math.Max(4, Math.Min(Height / 4, 9));
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var p = Tema.Redondeado(r, radio))
            {
                using (var b = new SolidBrush(fondo)) g.FillPath(b, p);
                if (!borde.IsEmpty) using (var pen = new Pen(borde, 1.2f)) g.DrawPath(pen, p);
                if (Focused && ShowFocusCues)
                    using (var pen = new Pen(Color.FromArgb(150, 255, 255, 255), 1f) { DashStyle = DashStyle.Dot })
                    using (var p2 = Tema.Redondeado(Rectangle.Inflate(r, -3, -3), Math.Max(2, radio - 2)))
                        g.DrawPath(pen, p2);
            }
            var f = estilo == EstiloBoton.Principal || estilo == EstiloBoton.Discord ? new Font(Font, FontStyle.Bold) : Font;
            TextRenderer.DrawText(g, Text, f, r, texto,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            if (f != Font) f.Dispose();
        }
    }

    /// Enlaces de LiteScripts
    static class Enlaces
    {
        public const string Discord = "https://discord.gg/ffSndEucDF";
        public const string Tienda = "https://lite-scripts.com/";

        public static void Abrir(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { MessageBox.Show(L.T("No se pudo abrir el navegador.") + "\n" + url, "Brasa"); }
        }

        public static void ActivarTls()
        {
            try { ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; } catch { }
        }

    }

    /// Emblema de LiteScripts (integrado en el programa). Clic = abre la tienda.
    class LogoLT : Control
    {
        readonly Image img = Recursos.Imagen("lt-emblema.png");

        public LogoLT()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Click += (s, e) => Enlaces.Abrir(Enlaces.Tienda);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (img == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var p = Tema.Redondeado(r, Math.Max(4, Width / 5)))
            {
                g.SetClip(p);
                g.DrawImage(img, new Rectangle(0, 0, Width, Height));
                g.ResetClip();
                using (var pen = new Pen(Tema.Borde)) g.DrawPath(pen, p);
            }
        }
    }

    /// Términos que hay que aceptar para usar Brasa
    class VentanaAceptar : FormOscuro
    {
        public VentanaAceptar()
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Tema.Fuente(9.5f);
            Text = "Brasa — " + L.T("Términos y condiciones");
            Icon = Recursos.Icono("brasa.ico", 32);
            BackColor = Tema.Fondo;
            ForeColor = Tema.Texto;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(600, 560);

            Controls.Add(new Logo { Location = new Point(20, 18), Size = new Size(50, 50) });
            var t = Tema.Etiqueta(L.T("Antes de empezar"), 16f, Tema.Texto, FontStyle.Bold);
            t.Location = new Point(80, 14);
            var s = Tema.Etiqueta(L.T("Para usar Brasa tienes que aceptar los términos y condiciones."), 9f, Tema.Tenue);
            s.Location = new Point(82, 46);
            Controls.Add(new LogoLT { Location = new Point(530, 18), Size = new Size(50, 50) });

            var caja = Tema.CajaTexto(new Rectangle(20, 86, 560, 360));
            caja.Font = Tema.Fuente(9f);
            caja.ForeColor = Tema.Texto;
            Terminos.Pintar(caja);

            var chk = Tema.Casilla(L.T("He leído y acepto los términos y condiciones"));
            chk.Location = new Point(20, 460);

            var no = Tema.Boton1(L.T("No acepto"));
            no.SetBounds(360, 504, 104, 38);
            no.DialogResult = DialogResult.Cancel;
            var si = Tema.Boton1(L.T("Aceptar"), true);
            si.SetBounds(476, 504, 104, 38);
            si.Enabled = false;
            si.DialogResult = DialogResult.OK;
            chk.CheckedChanged += (o, e) => si.Enabled = chk.Checked;
            var d = Tema.Boton2("Discord", EstiloBoton.Discord);
            d.SetBounds(20, 504, 104, 38);
            d.Click += (o, e) => Enlaces.Abrir(Enlaces.Discord);

            Controls.AddRange(new Control[] { t, s, caja, chk, no, si, d });
            CancelButton = no;
        }

        /// true si ya estaban aceptados o los acepta ahora
        public static bool Comprobar(Ajustes ajustes)
        {
            if (ajustes.Texto("TerminosAceptados", "") == Terminos.Version) return true;
            using (var v = new VentanaAceptar())
            {
                if (v.ShowDialog() != DialogResult.OK) return false;
            }
            ajustes.PonerTexto("TerminosAceptados", Terminos.Version);
            try { ajustes.Guardar(); } catch { }
            return true;
        }
    }

    /// Ventana sencilla para mostrar un texto largo (términos)
    class VentanaTexto : FormOscuro
    {
        public VentanaTexto(string titulo, string texto)
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Tema.Fuente(9.5f);
            Text = titulo;
            Icon = Recursos.Icono("brasa.ico", 32);
            BackColor = Tema.Fondo;
            ForeColor = Tema.Texto;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 520);
            var caja = Tema.CajaTexto(new Rectangle(16, 16, 528, 440));
            caja.ForeColor = Tema.Texto;
            caja.Font = Tema.Fuente(9.5f);
            if (texto == Terminos.Texto) Terminos.Pintar(caja); else caja.Text = texto;
            var ok = Tema.Boton1(L.T("Cerrar"), true);
            ok.SetBounds(444, 470, 100, 36);
            ok.DialogResult = DialogResult.OK;
            Controls.AddRange(new Control[] { caja, ok });
            AcceptButton = ok;
            CancelButton = ok;
        }
    }

    /// Línea separadora fina
    class Separador : Control
    {
        public Separador() { BackColor = Tema.Borde; Height = 1; }
    }
}
