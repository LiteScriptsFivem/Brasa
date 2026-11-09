using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Brasa
{
    static class Terminos
    {
        public const string Version = "1.2.1";

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
                    bool titulo = primera || Regex.IsMatch(linea, @"^\d+\. [\p{Lu} ,'’\-]+$");
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
        /// Términos en el idioma actual (la versión en español es la que vale si hay dudas)
        public static string Texto
        {
            get
            {
                switch (L.Codigo)
                {
                    case "en": return TextoEn;
                    case "fr": return TextoFr;
                    case "de": return TextoDe;
                    default: return TextoEs;
                }
            }
        }

        public const string Fecha = "8 de noviembre de 2026";

        public const string TextoEs =
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
    


        public const string TextoEn =
"BRASA — TERMS AND CONDITIONS OF USE\r\n" +
"Version " + Version + " · November 8, 2026\r\n" +
"Published by LiteScripts · Developed by hotbambi\r\n" +
"\r\n" +
"By installing or using Brasa you accept these terms. If you do not agree, do not install it.\r\n" +
"This is a translation; if there is any discrepancy, the Spanish version prevails.\r\n" +
"\r\n" +
"1. FREE\r\n" +
"Brasa is free. You may use it and share it at no cost. You may not sell it, distribute it as your own, or distribute a modified version passing it off as the original.\r\n" +
"\r\n" +
"2. NO WARRANTY\r\n" +
"Brasa is provided \"as is\", without warranties of any kind, express or implied. It is not guaranteed to work without errors or interruptions, or to be fit for any particular purpose.\r\n" +
"\r\n" +
"3. NO LIABILITY\r\n" +
"Neither LiteScripts nor hotbambi are liable for any damage, direct or indirect, that may result from using Brasa or from being unable to use it. This includes, among others: loss or corruption of data or databases, server failures (including game servers such as FiveM), financial losses, service interruptions or problems with your computer.\r\n" +
"You alone are responsible for your data. Make backups regularly.\r\n" +
"\r\n" +
"4. THIRD-PARTY SOFTWARE\r\n" +
"Brasa downloads Apache HTTP Server, PHP, MariaDB, phpMyAdmin and, if needed, Microsoft Visual C++ Redistributable from their official websites. Each belongs to its authors and is governed by its own license (Apache License 2.0, PHP License, GNU GPL v2 and Microsoft license). Brasa is not affiliated with any of them and is not responsible for how they work.\r\n" +
"\r\n" +
"5. LOCAL USE ONLY\r\n" +
"Brasa is intended for development and testing on your own computer. By default, the database uses the root user without a password and only accepts connections from the computer itself. Do not use it to publish services on the internet or for sensitive data without securing it yourself.\r\n" +
"\r\n" +
"6. MIGRATION, IMPORTING AND CHANGES TO YOUR DATA\r\n" +
"Brasa includes features to import data from other local servers installed on your computer, import SQL files and copy files. These features only read data that is already yours and that you choose; they do not copy, modify or redistribute third-party software. They may modify Brasa's data: before using them, Brasa tries to make a backup, but the result is not guaranteed. Use them at your own risk and only with data you have the right to use.\r\n" +
"\r\n" +
"7. TRADEMARKS\r\n" +
"The names of other programs and services that appear in Brasa or in these terms belong to their respective owners. They are mentioned only to identify the software Brasa works with or is compatible with. Brasa, LiteScripts and hotbambi are not affiliated with, sponsored or endorsed by any of them.\r\n" +
"\r\n" +
"8. SUPPORT\r\n" +
"You can write to us on the LiteScripts Discord (discord.gg/ffSndEucDF). As this is a free program, there is no guarantee of a reply or of any response time.\r\n" +
"\r\n" +
"9. CHANGES TO THESE TERMS\r\n" +
"These terms may change in future versions. Continuing to use Brasa after a change means you accept the new terms.\r\n" +
"\r\n" +
"LiteScripts · lite-scripts.com\r\n";

        public const string TextoFr =
"BRASA — CONDITIONS GÉNÉRALES D'UTILISATION\r\n" +
"Version " + Version + " · 8 novembre 2026\r\n" +
"Publié par LiteScripts · Développé par hotbambi\r\n" +
"\r\n" +
"En installant ou en utilisant Brasa, vous acceptez ces conditions. Si vous n'êtes pas d'accord, ne l'installez pas.\r\n" +
"Ceci est une traduction ; en cas de divergence, la version espagnole prévaut.\r\n" +
"\r\n" +
"1. GRATUIT\r\n" +
"Brasa est gratuit. Vous pouvez l'utiliser et le partager sans frais. Il est interdit de le vendre, de le distribuer comme s'il était le vôtre ou d'en distribuer une version modifiée en la faisant passer pour l'original.\r\n" +
"\r\n" +
"2. AUCUNE GARANTIE\r\n" +
"Brasa est fourni « tel quel », sans garantie d'aucune sorte, expresse ou implicite. Il n'est pas garanti qu'il fonctionne sans erreurs ni interruptions, ni qu'il convienne à un usage particulier.\r\n" +
"\r\n" +
"3. AUCUNE RESPONSABILITÉ\r\n" +
"Ni LiteScripts ni hotbambi ne sont responsables d'aucun dommage, direct ou indirect, pouvant résulter de l'utilisation de Brasa ou de l'impossibilité de l'utiliser. Cela inclut notamment : la perte ou la corruption de données ou de bases de données, les pannes de serveurs (y compris les serveurs de jeu comme FiveM), les pertes financières, les interruptions de service ou les problèmes sur votre ordinateur.\r\n" +
"Vous êtes seul responsable de vos données. Faites des sauvegardes régulièrement.\r\n" +
"\r\n" +
"4. LOGICIELS TIERS\r\n" +
"Brasa télécharge depuis leurs sites officiels Apache HTTP Server, PHP, MariaDB, phpMyAdmin et, si nécessaire, Microsoft Visual C++ Redistributable. Chacun appartient à ses auteurs et est régi par sa propre licence (Apache License 2.0, PHP License, GNU GPL v2 et licence Microsoft). Brasa n'est affilié à aucun d'entre eux et ne répond pas de leur fonctionnement.\r\n" +
"\r\n" +
"5. USAGE LOCAL UNIQUEMENT\r\n" +
"Brasa est conçu pour le développement et les tests sur votre propre ordinateur. Par défaut, la base de données utilise l'utilisateur root sans mot de passe et n'accepte que les connexions depuis l'ordinateur lui-même. Ne l'utilisez pas pour publier des services sur internet ni pour des données sensibles sans le sécuriser vous-même.\r\n" +
"\r\n" +
"6. MIGRATION, IMPORTATION ET MODIFICATIONS DE VOS DONNÉES\r\n" +
"Brasa inclut des fonctions pour importer des données depuis d'autres serveurs locaux installés sur votre ordinateur, importer des fichiers SQL et copier des fichiers. Ces fonctions lisent uniquement des données qui vous appartiennent déjà et que vous choisissez ; elles ne copient, ne modifient ni ne redistribuent les logiciels tiers. Elles peuvent modifier les données de Brasa : avant de les utiliser, Brasa tente de faire une sauvegarde, mais le résultat n'est pas garanti. Utilisez-les sous votre responsabilité et uniquement avec des données que vous avez le droit d'utiliser.\r\n" +
"\r\n" +
"7. MARQUES\r\n" +
"Les noms d'autres programmes et services qui apparaissent dans Brasa ou dans ces conditions appartiennent à leurs propriétaires respectifs. Ils sont mentionnés uniquement pour identifier les logiciels avec lesquels Brasa fonctionne ou est compatible. Brasa, LiteScripts et hotbambi ne sont ni affiliés à, ni sponsorisés, ni approuvés par aucun d'entre eux.\r\n" +
"\r\n" +
"8. ASSISTANCE\r\n" +
"Vous pouvez nous écrire sur le Discord de LiteScripts (discord.gg/ffSndEucDF). Comme il s'agit d'un programme gratuit, aucune réponse ni aucun délai de réponse ne sont garantis.\r\n" +
"\r\n" +
"9. MODIFICATIONS DE CES CONDITIONS\r\n" +
"Ces conditions peuvent changer dans les futures versions. Continuer à utiliser Brasa après une modification implique l'acceptation des nouvelles conditions.\r\n" +
"\r\n" +
"LiteScripts · lite-scripts.com\r\n";

        public const string TextoDe =
"BRASA — NUTZUNGSBEDINGUNGEN\r\n" +
"Version " + Version + " · 8. November 2026\r\n" +
"Veröffentlicht von LiteScripts · Entwickelt von hotbambi\r\n" +
"\r\n" +
"Mit der Installation oder Nutzung von Brasa akzeptierst du diese Bedingungen. Wenn du nicht einverstanden bist, installiere es nicht.\r\n" +
"Dies ist eine Übersetzung; bei Abweichungen gilt die spanische Fassung.\r\n" +
"\r\n" +
"1. KOSTENLOS\r\n" +
"Brasa ist kostenlos. Du darfst es kostenlos nutzen und weitergeben. Es ist nicht erlaubt, es zu verkaufen, als dein eigenes auszugeben oder eine veränderte Version als das Original zu verbreiten.\r\n" +
"\r\n" +
"2. KEINE GEWÄHRLEISTUNG\r\n" +
"Brasa wird „wie besehen“ bereitgestellt, ohne jegliche ausdrückliche oder stillschweigende Gewährleistung. Es wird nicht garantiert, dass es fehlerfrei oder ohne Unterbrechungen funktioniert oder für einen bestimmten Zweck geeignet ist.\r\n" +
"\r\n" +
"3. KEINE HAFTUNG\r\n" +
"Weder LiteScripts noch hotbambi haften für direkte oder indirekte Schäden, die durch die Nutzung von Brasa oder die Unmöglichkeit seiner Nutzung entstehen. Dazu gehören unter anderem: Verlust oder Beschädigung von Daten oder Datenbanken, Serverausfälle (einschließlich Spieleserver wie FiveM), finanzielle Verluste, Dienstunterbrechungen oder Probleme mit deinem Computer.\r\n" +
"Du allein bist für deine Daten verantwortlich. Erstelle regelmäßig Sicherungen.\r\n" +
"\r\n" +
"4. SOFTWARE VON DRITTEN\r\n" +
"Brasa lädt Apache HTTP Server, PHP, MariaDB, phpMyAdmin und bei Bedarf Microsoft Visual C++ Redistributable von deren offiziellen Websites herunter. Jedes davon gehört seinen Autoren und unterliegt seiner eigenen Lizenz (Apache License 2.0, PHP License, GNU GPL v2 und Microsoft-Lizenz). Brasa ist mit keinem davon verbunden und haftet nicht für deren Funktion.\r\n" +
"\r\n" +
"5. NUR FÜR LOKALE NUTZUNG\r\n" +
"Brasa ist für Entwicklung und Tests auf deinem eigenen Computer gedacht. Standardmäßig verwendet die Datenbank den Benutzer root ohne Passwort und akzeptiert nur Verbindungen vom eigenen Computer. Verwende es nicht, um Dienste im Internet zu veröffentlichen oder für sensible Daten, ohne es selbst abzusichern.\r\n" +
"\r\n" +
"6. MIGRATION, IMPORT UND ÄNDERUNGEN AN DEINEN DATEN\r\n" +
"Brasa enthält Funktionen, um Daten aus anderen auf deinem Computer installierten lokalen Servern zu importieren, SQL-Dateien zu importieren und Dateien zu kopieren. Diese Funktionen lesen nur Daten, die bereits dir gehören und die du auswählst; sie kopieren, verändern oder verbreiten keine Software von Dritten. Sie können die Daten von Brasa verändern: Vorher versucht Brasa, eine Sicherung zu erstellen, aber das Ergebnis ist nicht garantiert. Die Nutzung erfolgt auf eigene Verantwortung und nur mit Daten, die du verwenden darfst.\r\n" +
"\r\n" +
"7. MARKEN\r\n" +
"Die Namen anderer Programme und Dienste, die in Brasa oder in diesen Bedingungen erscheinen, gehören ihren jeweiligen Inhabern. Sie werden nur genannt, um die Software zu bezeichnen, mit der Brasa funktioniert oder kompatibel ist. Brasa, LiteScripts und hotbambi sind mit keinem davon verbunden und werden von keinem gesponsert oder unterstützt.\r\n" +
"\r\n" +
"8. SUPPORT\r\n" +
"Du kannst uns im Discord von LiteScripts schreiben (discord.gg/ffSndEucDF). Da es ein kostenloses Programm ist, gibt es keine Garantie für eine Antwort oder eine Antwortfrist.\r\n" +
"\r\n" +
"9. ÄNDERUNGEN DIESER BEDINGUNGEN\r\n" +
"Diese Bedingungen können sich in zukünftigen Versionen ändern. Wer Brasa nach einer Änderung weiter nutzt, akzeptiert die neuen Bedingungen.\r\n" +
"\r\n" +
"LiteScripts · lite-scripts.com\r\n";
    }
}
