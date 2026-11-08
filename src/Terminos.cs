using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Brasa
{
    static class Terminos
    {
        public const string Version = "1.2.0";

        /// Escribe los términos con los títulos en negrita
        public static void Pintar(RichTextBox caja)
        {
            caja.Clear();
            var normal = caja.Font;
            var negrita = new Font(normal, FontStyle.Bold); // la caja la sigue usando: no se libera
            {
                bool primera = true;
                foreach (var linea in Texto.Replace("\r\n", "\n").Split('\n'))
                {
                    bool titulo = primera || Regex.IsMatch(linea, @"^\d+\. [A-ZÁÉÍÓÚÑ ,]+$");
                    caja.SelectionStart = caja.TextLength;
                    caja.SelectionFont = titulo ? negrita : normal;
                    caja.SelectionColor = titulo ? Tema.Acento : Tema.Texto;
                    caja.AppendText(linea + "\n");
                    primera = false;
                }
                caja.SelectionStart = 0;
                caja.ScrollToCaret();
            }
        }
        public const string Fecha = "8 de noviembre de 2026";

        public const string Texto =
"BRASA — TÉRMINOS Y CONDICIONES DE USO\r\n" +
"Versión " + Version + " · " + Fecha + "\r\n" +
"Publicado por LiteScripts · Desarrollado por hotbambi\r\n" +
"\r\n" +
"Al instalar o usar Brasa aceptas estos términos. Si no estás de acuerdo, no lo instales.\r\n" +
"\r\n" +
"1. GRATIS\r\n" +
"Brasa es gratuito. Puedes usarlo y compartirlo sin coste. No está permitido venderlo ni distribuirlo como si fuera tuyo ni modificado haciéndolo pasar por el original.\r\n" +
"\r\n" +
"2. SIN GARANTÍA\r\n" +
"Brasa se ofrece \"tal cual\", sin garantías de ningún tipo, ni expresas ni implícitas. No se garantiza que funcione sin errores, sin interrupciones ni que sirva para un fin concreto.\r\n" +
"\r\n" +
"3. SIN RESPONSABILIDAD\r\n" +
"Ni LiteScripts ni hotbambi se hacen responsables de ningún daño, directo o indirecto, que pueda causar el uso de Brasa o la imposibilidad de usarlo. Esto incluye, entre otros: pérdida o corrupción de datos o bases de datos, fallos de servidores (incluidos servidores de juego como FiveM), pérdidas económicas, interrupciones de servicio o problemas en tu equipo.\r\n" +
"Tú eres el único responsable de tus datos. Haz copias de seguridad con regularidad.\r\n" +
"\r\n" +
"4. PROGRAMAS DE TERCEROS\r\n" +
"Brasa descarga desde sus webs oficiales Apache HTTP Server, PHP, MariaDB, phpMyAdmin y, si hace falta, Microsoft Visual C++ Redistributable. Cada uno pertenece a sus autores y se rige por su propia licencia (Apache License 2.0, PHP License, GNU GPL v2 y licencia de Microsoft). Brasa no está afiliado a ninguno de ellos ni responde de su funcionamiento.\r\n" +
"\r\n" +
"5. SOLO PARA USO LOCAL\r\n" +
"Brasa está pensado para desarrollo y pruebas en tu propio ordenador. Por defecto, la base de datos usa el usuario root sin contraseña y solo acepta conexiones desde el propio equipo. No lo uses para publicar servicios en internet ni para datos sensibles sin configurarlo tú mismo de forma segura.\r\n" +
"\r\n" +
"6. MIGRACIÓN, IMPORTACIÓN Y CAMBIOS EN TUS DATOS\r\n" +
"Brasa incluye funciones para importar datos desde otros servidores locales instalados en tu equipo, importar archivos SQL y copiar archivos. Estas funciones solo leen datos que ya son tuyos y que tú eliges; no copian, modifican ni redistribuyen el software de terceros. Pueden modificar los datos de Brasa: antes de usarlas, Brasa intenta hacer una copia de seguridad, pero no se garantiza el resultado. Úsalas bajo tu responsabilidad y solo con datos que tengas derecho a usar.\r\n" +
"\r\n" +
"7. MARCAS\r\n" +
"Los nombres de otros programas y servicios que aparecen en Brasa o en estos términos pertenecen a sus respectivos dueños. Se mencionan únicamente para identificar el software con el que Brasa funciona o es compatible. Brasa, LiteScripts y hotbambi no están afiliados, patrocinados ni respaldados por ninguno de ellos.\r\n" +
"\r\n" +
"8. SOPORTE\r\n" +
"Puedes escribirnos en el Discord de LiteScripts (discord.gg/ffSndEucDF). Al ser un programa gratuito, no se garantiza respuesta ni ningún plazo para darla.\r\n" +
"\r\n" +
"9. CAMBIOS EN ESTOS TÉRMINOS\r\n" +
"Estos términos pueden cambiar en futuras versiones. Seguir usando Brasa después de un cambio implica aceptar los nuevos términos.\r\n" +
"\r\n" +
"LiteScripts · lite-scripts.com\r\n";
    }
}
