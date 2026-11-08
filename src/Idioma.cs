using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Brasa
{
    /// Idioma de la interfaz. El texto original (español) es la clave de las traducciones:
    /// L.T("Encender todo") devuelve "Start all" en inglés. Si falta una traducción, se queda en español.
    static partial class L
    {
        public static readonly string[] Codigos = { "es", "en", "fr", "de" };
        public static readonly string[] Nombres = { "Español", "English", "Français", "Deutsch" };

        static int indice;   // 0 = es, 1 = en, 2 = fr, 3 = de
        public static string Codigo { get { return Codigos[indice]; } }

        static readonly Dictionary<string, string[]> Tabla = new Dictionary<string, string[]>(StringComparer.Ordinal);

        static void E(string es, string en, string fr, string de) { Tabla[es] = new[] { en, fr, de }; }

        static L() { Cargar(); }

        public static void Poner(string codigo)
        {
            int i = Array.IndexOf(Codigos, (codigo ?? "").Trim().ToLowerInvariant());
            indice = i >= 0 ? i : 0;
        }

        /// Idioma de Windows si es uno de los nuestros; si no, inglés
        public static string DelSistema()
        {
            try
            {
                var c = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
                return Array.IndexOf(Codigos, c) >= 0 ? c : "en";
            }
            catch { return "en"; }
        }

        /// Lee "Idioma=" de brasa.ini (o usa el de Windows)
        public static void Cargar(Ajustes a)
        {
            var c = a == null ? "" : a.Texto("Idioma", "");
            Poner(Array.IndexOf(Codigos, c) >= 0 ? c : DelSistema());
        }

        public static string T(string es)
        {
            if (indice == 0 || es == null) return es;
            string[] t;
            return Tabla.TryGetValue(es, out t) && !string.IsNullOrEmpty(t[indice - 1]) ? t[indice - 1] : es;
        }

        public static string F(string es, params object[] args)
        {
            return string.Format(T(es), args);
        }

        /// Desplegable de idioma con el estilo oscuro de Brasa
        public static ComboBox Selector(Action<string> alCambiar)
        {
            var c = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat,
                BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, Font = Tema.Fuente(9f), Cursor = Cursors.Hand,
            };
            c.Items.AddRange(Nombres);
            c.SelectedIndex = indice;
            c.SelectedIndexChanged += (s, e) => { if (c.SelectedIndex >= 0 && c.SelectedIndex != indice) alCambiar(Codigos[c.SelectedIndex]); };
            return c;
        }

        /// Vuelve a marcar el idioma actual (si se cancela el cambio)
        public static void Restaurar(ComboBox c) { c.SelectedIndex = indice; }
    }
}
