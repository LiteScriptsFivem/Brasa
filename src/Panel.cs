using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Brasa
{
    class Ventana : FormOscuro
    {
        readonly Motor motor;
        readonly Ajustes ajustes;
        readonly string exe;
        readonly NotifyIcon bandeja = new NotifyIcon();
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        ToolStripMenuItem miEstado;
        readonly System.Windows.Forms.Timer reloj = new System.Windows.Forms.Timer();
        readonly Icon icoVerde, icoAmbar, icoRojo, icoGris;

        bool permitirMostrar;
        bool saliendo;
        int operaciones;

        Punto puntoGlobal; Label lblGlobal;
        readonly FilaServicio filaApache, filaMaria;
        BotonR btnTodo, btnApagarTodo, btnReiniciar;
        CheckBox chkWindows, chkAuto, chkVigilante, chkCopia;
        RichTextBox registro;
        string ultimoEstadoBandeja = "";
        VentanaMigrar ventanaMigrar;

        class FilaServicio
        {
            public Servicio S;
            public Tarjeta Caja;
            public Punto Punto;
            public Label Nombre, Version, Estado, Detalle;
            public BotonR Accion, Log;
        }

        public Ventana(Motor motor, Ajustes ajustes, string exe, bool minimizado)
        {
            this.motor = motor;
            this.ajustes = ajustes;
            this.exe = exe;
            permitirMostrar = !minimizado;

            int ti = SystemInformation.SmallIconSize.Width;
            icoVerde = Recursos.Icono("bandeja-verde.ico", ti);
            icoAmbar = Recursos.Icono("bandeja-ambar.ico", ti);
            icoRojo = Recursos.Icono("bandeja-rojo.ico", ti);
            icoGris = Recursos.Icono("bandeja-gris.ico", ti);

            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Tema.Fuente(9f);
            Text = "Brasa — Servidor local";
            Icon = Recursos.Icono("brasa.ico", 32);
            BackColor = Tema.Fondo;
            ForeColor = Tema.Texto;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(880, 560);

            // ================= Cabecera =================
            Controls.Add(new Logo { Location = new Point(22, 16), Size = new Size(50, 50) });
            var titulo = Tema.Etiqueta("Brasa", 19f, Tema.Texto, FontStyle.Bold);
            titulo.Location = new Point(80, 10);
            Controls.Add(titulo);
            var sub = Tema.Etiqueta("Servidor local  ·  Apache  ·  PHP  ·  MariaDB  ·  by LiteScripts", 9f, Tema.Tenue);
            sub.Location = new Point(83, 48);
            Controls.Add(sub);

            var pill = new Tarjeta { Location = new Point(660, 24), Size = new Size(200, 36) };
            puntoGlobal = new Punto { Location = new Point(12, 9), Size = new Size(18, 18) };
            lblGlobal = Tema.Etiqueta("Comprobando...", 9.5f, Tema.Texto, FontStyle.Bold);
            lblGlobal.AutoSize = false;
            lblGlobal.TextAlign = ContentAlignment.MiddleLeft;
            lblGlobal.Bounds = new Rectangle(36, 7, 156, 22);
            pill.Controls.AddRange(new Control[] { puntoGlobal, lblGlobal });
            Controls.Add(pill);

            Controls.Add(new Separador { Bounds = new Rectangle(0, 82, 880, 1) });

            // ================= Columna izquierda =================
            Controls.Add(Tema.Seccion("SERVICIOS", 22, 98));
            filaApache = CrearFila(motor.Apache, "Apache + PHP", 120);
            filaMaria = CrearFila(motor.MariaDB, "MariaDB", 216);

            btnTodo = Tema.Boton1("Encender todo", true);
            btnTodo.SetBounds(20, 316, 136, 42);
            btnTodo.Click += (s, e) => EncenderTodo(false);
            btnApagarTodo = Tema.Boton1("Apagar todo");
            btnApagarTodo.SetBounds(162, 316, 136, 42);
            btnApagarTodo.Click += (s, e) => ApagarTodo(null);
            btnReiniciar = Tema.Boton1("Reiniciar");
            btnReiniciar.SetBounds(304, 316, 136, 42);
            btnReiniciar.Click += (s, e) => ApagarTodo(() => EncenderTodo(false));
            Controls.AddRange(new Control[] { btnTodo, btnApagarTodo, btnReiniciar });

            Controls.Add(Tema.Seccion("OPCIONES", 22, 376));
            chkWindows = Opcion("Arrancar con Windows", 20, 398, Sistema.InicioConWindows);
            chkAuto = Opcion("Encender servidores al abrir", 20, 424, ajustes.Bool("AutoEncender", true));
            chkVigilante = Opcion("Vigilante (reinicia si se cae)", 232, 398, ajustes.Bool("Vigilante", true));
            chkCopia = Opcion("Copia de seguridad diaria", 232, 424, ajustes.Bool("CopiaDiaria", true));
            chkWindows.CheckedChanged += (s, e) =>
            {
                try { Sistema.PonerInicioConWindows(chkWindows.Checked, exe); }
                catch (Exception ex) { Escribir("No se pudo cambiar el inicio con Windows: " + ex.Message, true); }
            };
            chkAuto.CheckedChanged += (s, e) => GuardarAjuste("AutoEncender", chkAuto.Checked);
            chkVigilante.CheckedChanged += (s, e) => GuardarAjuste("Vigilante", chkVigilante.Checked);
            chkCopia.CheckedChanged += (s, e) => GuardarAjuste("CopiaDiaria", chkCopia.Checked);

            // ================= Columna derecha =================
            Controls.Add(Tema.Seccion("ACCESOS RÁPIDOS", 462, 98));
            Acceso("Abrir web", 0, 0, (s, e) => AbrirWeb(""));
            Acceso("phpMyAdmin", 1, 0, (s, e) => AbrirWeb("phpmyadmin/"));
            Acceso("Carpeta www", 0, 1, (s, e) => AbrirCarpeta(motor.DirWww));
            Acceso("Ver copias", 1, 1, (s, e) => AbrirCarpeta(motor.DirBackups));
            Acceso("Copia de seguridad", 0, 2, (s, e) => HacerCopia());
            Acceso("Importar .sql", 1, 2, (s, e) => ImportarSql());
            var migrar = Tema.Boton2("Migrar desde XAMPP", EstiloBoton.Contorno);
            migrar.SetBounds(460, 246, 400, 36);
            migrar.Click += (s, e) => AbrirMigracion();
            Controls.Add(migrar);

            Controls.Add(Tema.Seccion("ACTIVIDAD", 462, 300));
            registro = Tema.CajaTexto(new Rectangle(460, 322, 400, 128));
            Controls.Add(registro);

            // ================= Pie LiteScripts =================
            var pie = new Tarjeta { Location = new Point(20, 470), Size = new Size(840, 74) };
            var lt = new LogoLT() { Location = new Point(14, 12), Size = new Size(50, 50) };
            var ltNombre = Tema.Etiqueta("LiteScripts", 11f, Tema.Azul, FontStyle.Bold);
            ltNombre.Location = new Point(74, 10);
            var ltTexto = Tema.Etiqueta("Brasa es gratis  ·  Desarrollado por hotbambi  ·  v" + Terminos.Version, 8.5f, Tema.Tenue);
            ltTexto.Location = new Point(76, 33);
            var terminos = new LinkLabel
            {
                Text = "Términos y condiciones", AutoSize = true, Location = new Point(76, 51), Font = Tema.Fuente(8.5f),
                LinkColor = Tema.Acento, ActiveLinkColor = Tema.AcentoHover, VisitedLinkColor = Tema.Acento, BackColor = Color.Transparent,
                LinkBehavior = LinkBehavior.HoverUnderline,
            };
            terminos.LinkClicked += (s, e) => { using (var v = new VentanaTexto("Términos y condiciones", Terminos.Texto)) v.ShowDialog(this); };
            var discord = Tema.Boton2("Discord", EstiloBoton.Discord);
            discord.SetBounds(560, 18, 128, 38);
            discord.Click += (s, e) => Enlaces.Abrir(Enlaces.Discord);
            var tienda = Tema.Boton2("Tienda", EstiloBoton.Principal);
            tienda.SetBounds(698, 18, 128, 38);
            tienda.Click += (s, e) => Enlaces.Abrir(Enlaces.Tienda);
            pie.Controls.AddRange(new Control[] { lt, ltNombre, ltTexto, terminos, discord, tienda });
            Controls.Add(pie);

            ResumeLayout(false);
            PerformLayout();

            // ================= Bandeja =================
            ConstruirMenu();
            bandeja.Icon = icoGris;
            bandeja.Text = "Brasa";
            bandeja.ContextMenuStrip = menu;
            bandeja.Visible = true;
            bandeja.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) Mostrar(); };
            bandeja.BalloonTipClicked += (s, e) => Mostrar();

            motor.Registro += (m, err) => { if (IsHandleCreated && !IsDisposed) try { BeginInvoke(new Action(() => Escribir(m, err))); } catch { } };

            reloj.Interval = 1500;
            reloj.Tick += (s, e) => Tic();
            Load += (s, e) => Inicio();
        }

        // ================= Construcción =================

        FilaServicio CrearFila(Servicio s, string nombre, int y)
        {
            var f = new FilaServicio { S = s };
            f.Caja = new Tarjeta { Location = new Point(20, y), Size = new Size(420, 86) };
            f.Punto = new Punto { Location = new Point(14, 15), Size = new Size(22, 22) };
            f.Nombre = Tema.Etiqueta(nombre, 11.5f, Tema.Texto, FontStyle.Bold);
            f.Nombre.Location = new Point(42, 12);
            f.Version = Tema.Etiqueta("", 8.5f, Tema.Tenue);
            f.Version.Location = new Point(170, 17);
            f.Estado = Tema.Etiqueta("—", 9f, Tema.Texto);
            f.Estado.AutoSize = false;
            f.Estado.AutoEllipsis = true;
            f.Estado.Bounds = new Rectangle(43, 40, 230, 18);
            f.Detalle = Tema.Etiqueta("", 8.5f, Tema.Rojo);
            f.Detalle.AutoSize = false;
            f.Detalle.AutoEllipsis = true;
            f.Detalle.Bounds = new Rectangle(43, 61, 365, 18);

            f.Log = Tema.Boton1("Log");
            f.Log.SetBounds(276, 24, 48, 34);
            f.Log.Click += (o, e) => VerLog(s);
            f.Accion = Tema.Boton1("Encender", true);
            f.Accion.SetBounds(330, 24, 78, 34);
            f.Accion.Click += (o, e) => AlternarServicio(s);

            f.Caja.Controls.AddRange(new Control[] { f.Punto, f.Nombre, f.Version, f.Estado, f.Detalle, f.Log, f.Accion });
            Controls.Add(f.Caja);

            if (s == motor.Apache)
                f.Version.Text = (s.Version.Length > 0 ? s.Version : "") + (motor.VersionPhp.Length > 0 ? "  ·  PHP " + motor.VersionPhp : "");
            else
                f.Version.Text = s.Version.Length > 0 ? s.Version : "";
            return f;
        }

        void Acceso(string texto, int col, int fila, EventHandler accion)
        {
            var b = Tema.Boton1(texto);
            b.SetBounds(460 + col * 205, 120 + fila * 42, 195, 36);
            b.Click += accion;
            Controls.Add(b);
        }

        CheckBox Opcion(string texto, int x, int y, bool valor)
        {
            var c = Tema.Casilla(texto);
            c.Font = Tema.Fuente(9f);
            c.Location = new Point(x, y);
            c.Checked = valor;
            Controls.Add(c);
            return c;
        }

        void ConstruirMenu()
        {
            menu.ShowImageMargin = false;
            menu.Font = Tema.Fuente(9.5f);
            miEstado = new ToolStripMenuItem("Comprobando...") { Enabled = false };
            menu.Items.Add(miEstado);
            menu.Items.Add(new ToolStripSeparator());
            var abrir = new ToolStripMenuItem("Abrir Brasa", null, (s, e) => Mostrar());
            abrir.Font = new Font(menu.Font, FontStyle.Bold);
            menu.Items.Add(abrir);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Encender todo", null, (s, e) => EncenderTodo(false));
            menu.Items.Add("Apagar todo", null, (s, e) => ApagarTodo(null));
            menu.Items.Add("Reiniciar", null, (s, e) => ApagarTodo(() => EncenderTodo(false)));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Abrir web", null, (s, e) => AbrirWeb(""));
            menu.Items.Add("phpMyAdmin", null, (s, e) => AbrirWeb("phpmyadmin/"));
            menu.Items.Add("Carpeta www", null, (s, e) => AbrirCarpeta(motor.DirWww));
            menu.Items.Add("Copia de seguridad ahora", null, (s, e) => HacerCopia());
            menu.Items.Add("Migrar desde XAMPP...", null, (s, e) => AbrirMigracion());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Discord de LiteScripts", null, (s, e) => Enlaces.Abrir(Enlaces.Discord));
            menu.Items.Add("Tienda LiteScripts", null, (s, e) => Enlaces.Abrir(Enlaces.Tienda));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Salir (apaga el servidor)", null, (s, e) => Salir(true));
        }

        // ================= Ciclo de vida =================

        protected override void SetVisibleCore(bool value)
        {
            if (!permitirMostrar)
            {
                value = false;
                if (!IsHandleCreated) CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        void Inicio()
        {
            foreach (var f in new[] { filaApache, filaMaria })
            {
                f.Version.Left = f.Nombre.Right + 6;
                f.Version.Top = f.Nombre.Top + (f.Nombre.Height - f.Version.Height) / 2 + 1;
            }

            Escribir("Brasa listo. Al cerrar con la X sigue funcionando junto al reloj.");
            motor.Refrescar(motor.Apache);
            motor.Refrescar(motor.MariaDB);
            if (motor.Apache.Estado == Estado.Encendido) motor.Apache.Deseado = true;
            if (motor.MariaDB.Estado == Estado.Encendido) motor.MariaDB.Deseado = true;
            Pintar();
            reloj.Start();

            var t = new System.Windows.Forms.Timer { Interval = 4000 };
            t.Tick += (s, e) => { t.Stop(); t.Dispose(); Sistema.PromocionarIconoBandeja(exe); };
            t.Start();

            if (!motor.Instalado)
            {
                Escribir("Brasa no está instalado del todo en esta carpeta. Abre Brasa-Instalador.exe otra vez.", true);
                return;
            }
            if (ajustes.Bool("AutoEncender", true)) EncenderTodo(true);
        }

        void Mostrar()
        {
            permitirMostrar = true;
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
        }

        public void MostrarDesdeFuera() { if (IsHandleCreated) BeginInvoke(new Action(Mostrar)); }
        public void SalirDesdeFuera() { if (IsHandleCreated) BeginInvoke(new Action(() => Salir(false))); }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (saliendo) { base.OnFormClosing(e); return; }

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                if (!ajustes.Bool("AvisoBandeja", false))
                {
                    bandeja.ShowBalloonTip(4000, "Brasa sigue funcionando", "Lo tienes aquí, junto al reloj. Clic para abrirlo; clic derecho para el menú.", ToolTipIcon.Info);
                    GuardarAjuste("AvisoBandeja", true);
                }
                return;
            }

            // Windows se apaga: apagar MariaDB limpio para no perder datos
            saliendo = true;
            reloj.Stop();
            try { motor.Parar(motor.Apache); motor.Parar(motor.MariaDB); } catch { }
            bandeja.Visible = false;
            base.OnFormClosing(e);
        }

        void Salir(bool preguntar)
        {
            if (saliendo) return;
            if (motor.Migrando) { Mostrar(); Escribir("Espera a que termine la migración antes de salir.", true); return; }
            bool hayAlgo = motor.Apache.Estado != Estado.Apagado || motor.MariaDB.Estado != Estado.Apagado;
            if (preguntar && hayAlgo)
            {
                var r = MessageBox.Show("Se apagarán Apache y MariaDB y Brasa se cerrará.\n\n¿Salir?", "Brasa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return;
            }
            saliendo = true;
            reloj.Stop();
            Escribir("Apagando todo para salir...");
            bandeja.Text = "Brasa — apagando...";
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { motor.Parar(motor.Apache); motor.Parar(motor.MariaDB); } catch { }
                BeginInvoke(new Action(() =>
                {
                    bandeja.Visible = false;
                    bandeja.Dispose();
                    Application.Exit();
                }));
            });
        }

        // ================= Acciones =================

        void EnSegundoPlano(Action trabajo, Action despues = null)
        {
            Interlocked.Increment(ref operaciones);
            Pintar();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { trabajo(); }
                catch (Exception ex) { try { BeginInvoke(new Action(() => Escribir("Error: " + ex.Message, true))); } catch { } }
                finally
                {
                    Interlocked.Decrement(ref operaciones);
                    try { BeginInvoke(new Action(() => { Pintar(); if (despues != null) despues(); })); } catch { }
                }
            });
        }

        void EncenderTodo(bool silencioso)
        {
            if (!motor.Instalado) { Escribir("Brasa no está instalado del todo. Abre Brasa-Instalador.exe otra vez.", true); return; }
            EnSegundoPlano(() =>
            {
                bool okDb = motor.Arrancar(motor.MariaDB);
                bool okWeb = motor.Arrancar(motor.Apache);
                if (okDb && ajustes.Bool("CopiaDiaria", true) && !motor.HayCopiaDeHoy())
                    motor.CopiaSeguridad("automática diaria");
                BeginInvoke(new Action(() =>
                {
                    if (!(okDb && okWeb))
                        bandeja.ShowBalloonTip(5000, "Brasa", "Algo no ha arrancado. Abre Brasa para ver el motivo.", ToolTipIcon.Warning);
                    else if (!Visible)
                        bandeja.ShowBalloonTip(3000, "Brasa", "Servidor encendido en " + UrlBase(), ToolTipIcon.Info);
                }));
            });
        }

        void ApagarTodo(Action despues)
        {
            EnSegundoPlano(() => { motor.Parar(motor.Apache); motor.Parar(motor.MariaDB); }, despues);
        }

        void AlternarServicio(Servicio s)
        {
            if (s.Estado == Estado.Encendido || s.Estado == Estado.Arrancando) EnSegundoPlano(() => motor.Parar(s));
            else if (!motor.Instalado) Escribir("Brasa no está instalado del todo. Abre Brasa-Instalador.exe otra vez.", true);
            else EnSegundoPlano(() => motor.Arrancar(s));
        }

        string UrlBase()
        {
            return motor.Apache.Puerto == 80 ? "http://localhost/" : "http://localhost:" + motor.Apache.Puerto + "/";
        }

        void AbrirWeb(string ruta)
        {
            if (motor.Apache.Estado != Estado.Encendido) Escribir("Ojo: Apache está apagado, la página no cargará.", true);
            Enlaces.Abrir(UrlBase() + ruta);
        }

        void AbrirCarpeta(string ruta)
        {
            try
            {
                Directory.CreateDirectory(ruta);
                Process.Start("explorer.exe", "\"" + ruta + "\"");
            }
            catch (Exception ex) { Escribir("No se pudo abrir la carpeta: " + ex.Message, true); }
        }

        void VerLog(Servicio s)
        {
            var archivo = motor.ArchivoLog(s);
            if (archivo == null || !File.Exists(archivo)) { Escribir("Todavía no hay registro de " + s.Nombre + ".", true); return; }
            try { Process.Start("notepad.exe", "\"" + archivo + "\""); }
            catch (Exception ex) { Escribir(ex.Message, true); }
        }

        void HacerCopia()
        {
            EnSegundoPlano(() =>
            {
                var f = motor.CopiaSeguridad(null);
                if (f != null) BeginInvoke(new Action(() => bandeja.ShowBalloonTip(3000, "Copia de seguridad hecha", Path.GetFileName(f), ToolTipIcon.Info)));
            });
        }

        void ImportarSql()
        {
            if (motor.MariaDB.Estado != Estado.Encendido) { Escribir("Enciende MariaDB antes de importar.", true); return; }
            string archivo;
            using (var d = new OpenFileDialog { Filter = "Archivos SQL (*.sql)|*.sql|Todos los archivos|*.*", Title = "Elige el .sql que quieres importar" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                archivo = d.FileName;
            }
            if (Motor.TraeSuBase(archivo))
            {
                Escribir("Este .sql ya indica sus bases de datos: se importa tal cual.");
                EnSegundoPlano(() => motor.ImportarCompleto(archivo));
                return;
            }
            var nombre = PedirTexto("Importar .sql", "¿En qué base de datos lo meto? Si no existe, la creo.", Path.GetFileNameWithoutExtension(archivo));
            if (string.IsNullOrWhiteSpace(nombre)) return;
            EnSegundoPlano(() => motor.Importar(archivo, nombre.Trim()));
        }

        void AbrirMigracion()
        {
            Mostrar();
            if (ventanaMigrar != null && !ventanaMigrar.IsDisposed) { ventanaMigrar.Activate(); return; }
            ventanaMigrar = new VentanaMigrar(motor);
            ventanaMigrar.Terminado += () => { motor.Refrescar(motor.MariaDB); Pintar(); };
            ventanaMigrar.Show(this);
        }

        string PedirTexto(string titulo, string pregunta, string valor)
        {
            using (var f = new FormOscuro())
            {
                f.AutoScaleDimensions = new SizeF(96F, 96F);
                f.AutoScaleMode = AutoScaleMode.Dpi;
                f.Font = Tema.Fuente(9.5f);
                f.Text = titulo;
                f.Icon = Icon;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MinimizeBox = f.MaximizeBox = false;
                f.StartPosition = FormStartPosition.CenterParent;
                f.BackColor = Tema.Fondo;
                f.ClientSize = new Size(420, 140);
                var l = Tema.Etiqueta(pregunta, 9.5f, Tema.Texto);
                l.MaximumSize = new Size(390, 0);
                l.Location = new Point(15, 14);
                var t = new TextBox { Text = valor, Location = new Point(15, 50), Width = 390, BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, BorderStyle = BorderStyle.FixedSingle, Font = Tema.Fuente(10.5f) };
                var ok = Tema.Boton1("Importar", true);
                ok.SetBounds(215, 92, 92, 34);
                ok.DialogResult = DialogResult.OK;
                var no = Tema.Boton1("Cancelar");
                no.SetBounds(313, 92, 92, 34);
                no.DialogResult = DialogResult.Cancel;
                f.Controls.AddRange(new Control[] { l, t, ok, no });
                f.AcceptButton = ok;
                f.CancelButton = no;
                return f.ShowDialog(this) == DialogResult.OK ? t.Text : null;
            }
        }

        void GuardarAjuste(string k, bool v)
        {
            ajustes.PonerBool(k, v);
            try { ajustes.Guardar(); } catch (Exception ex) { Escribir("No se pudo guardar el ajuste: " + ex.Message, true); }
        }

        // ================= Estado y vigilante =================

        void Tic()
        {
            motor.Refrescar(motor.Apache);
            motor.Refrescar(motor.MariaDB);
            if (chkVigilante.Checked && operaciones == 0 && !saliendo && !motor.Migrando)
            {
                Vigilar(motor.MariaDB);
                Vigilar(motor.Apache);
            }
            Pintar();
        }

        void Vigilar(Servicio s)
        {
            if (!s.Deseado || s.Ocupado || s.Estado != Estado.Apagado) return;
            s.Reinicios.RemoveAll(t => (DateTime.Now - t).TotalMinutes > 5);
            if (s.Reinicios.Count >= 3)
            {
                s.Deseado = false;
                Escribir(s.Nombre + " se ha caído 3 veces en 5 minutos. El vigilante se rinde: mira el log.", true);
                bandeja.ShowBalloonTip(6000, "Brasa", s.Nombre + " se cae una y otra vez. Abre Brasa para ver el motivo.", ToolTipIcon.Error);
                return;
            }
            s.Reinicios.Add(DateTime.Now);
            Escribir(s.Nombre + " se ha caído. El vigilante lo vuelve a encender...", true);
            EnSegundoPlano(() => motor.Arrancar(s));
        }

        void Pintar()
        {
            PintarFila(filaApache);
            PintarFila(filaMaria);

            var a = motor.Apache.Estado; var m = motor.MariaDB.Estado;
            string texto; Color color; Icon ico;
            if (a == Estado.Encendido && m == Estado.Encendido) { texto = "Todo encendido"; color = Tema.Verde; ico = icoVerde; }
            else if (a == Estado.Error || m == Estado.Error) { texto = "Hay un problema"; color = Tema.Rojo; ico = icoRojo; }
            else if (operaciones > 0 || motor.Migrando || a == Estado.Arrancando || m == Estado.Arrancando || a == Estado.Parando || m == Estado.Parando)
            { texto = motor.Migrando ? "Migrando..." : "Trabajando..."; color = Tema.Ambar; ico = icoAmbar; }
            else if (a == Estado.Apagado && m == Estado.Apagado) { texto = "Apagado"; color = Tema.Gris; ico = icoGris; }
            else { texto = "Encendido a medias"; color = Tema.Ambar; ico = icoAmbar; }

            lblGlobal.Text = texto;
            puntoGlobal.Color = color;
            miEstado.Text = "●  " + texto;
            miEstado.ForeColor = color;
            if (texto != ultimoEstadoBandeja)
            {
                bandeja.Icon = ico;
                bandeja.Text = "Brasa — " + texto;
                ultimoEstadoBandeja = texto;
            }

            bool libre = operaciones == 0 && !motor.Migrando;
            btnTodo.Enabled = libre && !(a == Estado.Encendido && m == Estado.Encendido);
            btnApagarTodo.Enabled = libre && !(a == Estado.Apagado && m == Estado.Apagado);
            btnReiniciar.Enabled = libre;
        }

        void PintarFila(FilaServicio f)
        {
            var s = f.S;
            string est; Color c;
            switch (s.Estado)
            {
                case Estado.Encendido: est = "Encendido"; c = Tema.Verde; break;
                case Estado.Arrancando: est = "Arrancando..."; c = Tema.Ambar; break;
                case Estado.Parando: est = "Apagando..."; c = Tema.Ambar; break;
                case Estado.Error: est = "Error"; c = Tema.Rojo; break;
                default: est = "Apagado"; c = Tema.Gris; break;
            }
            f.Punto.Color = c;

            var partes = est + "  ·  puerto " + s.Puerto;
            if (s.Estado == Estado.Encendido)
            {
                if (s.MemoriaMB > 0) partes += "  ·  " + s.MemoriaMB + " MB";
                if (s.Inicio.HasValue) partes += "  ·  " + Duracion(DateTime.Now - s.Inicio.Value);
            }
            f.Estado.Text = partes;
            f.Estado.ForeColor = s.Estado == Estado.Error ? Tema.Rojo : Tema.Texto;
            f.Detalle.Text = s.Estado == Estado.Error ? s.Detalle : "";

            bool on = s.Estado == Estado.Encendido || s.Estado == Estado.Arrancando;
            f.Accion.Text = on ? "Apagar" : "Encender";
            f.Accion.Estilo = on ? EstiloBoton.Normal : EstiloBoton.Principal;
            f.Accion.Enabled = operaciones == 0 && !s.Ocupado && !motor.Migrando;
        }

        static string Duracion(TimeSpan t)
        {
            if (t.TotalMinutes < 1) return "recién encendido";
            if (t.TotalHours < 1) return (int)t.TotalMinutes + " min";
            if (t.TotalDays < 1) return (int)t.TotalHours + " h " + t.Minutes + " min";
            return (int)t.TotalDays + " d " + t.Hours + " h";
        }

        void Escribir(string msg, bool error = false)
        {
            if (registro.IsDisposed) return;
            if (registro.Lines.Length > 400)
            {
                registro.Select(0, registro.GetFirstCharIndexFromLine(100));
                registro.SelectedText = "";
            }
            registro.SelectionStart = registro.TextLength;
            registro.SelectionColor = Tema.Gris;
            registro.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  ");
            registro.SelectionColor = error ? Tema.Rojo : Tema.Texto;
            registro.AppendText(msg + "\n");
            registro.SelectionStart = registro.TextLength;
            registro.ScrollToCaret();
        }
    }
}
