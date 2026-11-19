using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsForms_Presentacion
{
    public partial class Form1 : Form
    {
        private static Color Surface => TemaAplicacion.Actual.Superficie;
        private readonly TextBox usuario = new TextBox();
        private readonly TextBox clave = new TextBox();
        private readonly SelectorDeslizante perfilAcceso = new SelectorDeslizante
        {
            OpcionIzquierda = "Empleado",
            OpcionDerecha = "Administrador",
            AccessibleName = "Perfil de acceso",
            Height = 40
        };
        private readonly Label errorUsuario = new Label();
        private readonly Label errorClave = new Label();
        private readonly Label estado = new Label();

        private readonly Logica.ServicioAutenticacion autenticacion;
        private readonly Logica.IConsultaAsistencia consultaAsistencia;
        private readonly Logica.IConsultaAdministracion consultaAdministracion;
        private readonly Logica.ServicioReconocimientoFacial reconocimientoFacial;
        private bool autenticando;
        public Logica.SesionUsuario SesionActual { get; private set; }
        private static string EstadoActivo => System.Configuration.ConfigurationManager.AppSettings["EstadoActivo"] ?? "A";

        public Form1() : this(new Logica.ServicioAutenticacion(
            new Persistencia.RepositorioUsuarios(), new Logica.ClavesPbkdf2(),
            EstadoActivo))
        {
        }

        public Form1(Logica.ServicioAutenticacion autenticacion)
            : this(autenticacion, new Persistencia.ConsultaAsistencia(), new Persistencia.ConsultaAdministracion(EstadoActivo))
        {
        }

        internal Form1(Logica.ServicioAutenticacion autenticacion, Logica.IConsultaAsistencia consultaAsistencia,
            Logica.IConsultaAdministracion consultaAdministracion)
        {
            this.autenticacion = autenticacion ?? throw new ArgumentNullException(nameof(autenticacion));
            this.consultaAsistencia = consultaAsistencia ?? throw new ArgumentNullException(nameof(consultaAsistencia));
            this.consultaAdministracion = consultaAdministracion ?? throw new ArgumentNullException(nameof(consultaAdministracion));
            reconocimientoFacial = new Logica.ServicioReconocimientoFacial(new Persistencia.RepositorioFacial(EstadoActivo), EstadoActivo);
            InitializeComponent();
            Text = "AssistPRO | Acceso";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 11);
            BackColor = TemaAplicacion.Actual.Fondo;
            ForeColor = TemaAplicacion.Actual.Texto;
            ClientSize = new Size(820, 660);
            MinimumSize = new Size(420, 540);
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            CrearAcceso();
            TemaAplicacion.Registrar(this);
        }

        private void CrearAcceso()
        {
            var raiz = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty
            };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.Controls.Add(CrearEncabezadoAcceso(), 0, 0);

            var scroll = new FondoAcceso { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
            var composicion = new TableLayoutPanel
            {
                ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, TabStop = false
            };
            composicion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            composicion.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 452));
            composicion.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var presentacion = CrearPresentacionAcceso();
            composicion.Controls.Add(presentacion, 0, 0);

            var layout = new TarjetaAcceso
            {
                AutoSize = false, Dock = DockStyle.Fill,
                ColumnCount = 1, RowCount = 0, Padding = new Padding(32, 28, 32, 28),
                BackColor = Surface, Margin = Padding.Empty, TabStop = false
            };
            TemaAplicacion.DefinirRol(layout, RolTema.Superficie);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Agregar(layout, Texto("Iniciar sesión", 25, RolTema.Normal, FontStyle.Bold), 0, 5);
            var instrucciones = Texto("Selecciona tu perfil e ingresa tus credenciales.", 10, RolTema.TextoSecundario);
            Agregar(layout, instrucciones, 0, 24);

            Agregar(layout, Texto("Perfil de acceso", 10, RolTema.TextoSecundario), 0, 5);
            Agregar(layout, perfilAcceso, 0, 20);

            Agregar(layout, Texto("Nombre de usuario", 10, RolTema.TextoSecundario), 0, 7);
            Agregar(layout, Campo(usuario, false), 0, 0);
            PrepararError(errorUsuario);
            Agregar(layout, errorUsuario, 4, 0);
            Agregar(layout, Texto("Contraseña", 10, RolTema.TextoSecundario), 16, 7);
            Agregar(layout, Campo(clave, true), 0, 0);
            PrepararError(errorClave);
            Agregar(layout, errorClave, 4, 0);

            var entrar = new AccesoButton
            {
                Text = "Iniciar sesión", Height = 48, Principal = true, Plano = true,
                Font = new Font(Font.FontFamily, 11F, FontStyle.Bold)
            };
            entrar.Click += ValidarAcceso;
            Agregar(layout, entrar, 20, 0);
            AcceptButton = entrar;
            estado.AutoSize = true;
            TemaAplicacion.DefinirRol(estado, RolTema.TextoSecundario);
            estado.AccessibleDescription = "Estado del acceso";
            estado.Visible = false;
            estado.Font = new Font(Font.FontFamily, 10);
            estado.TextChanged += (s, e) => estado.Visible = estado.Text.Length > 0;
            Agregar(layout, estado, 10, 0);
            Agregar(layout, Texto("Acceso biométrico", 9, RolTema.TextoSecundario), 24, 9);
            var biometria = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
            biometria.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            biometria.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var huella = new AccesoButton { Text = "Huella dactilar", Icono = "huella", Plano = true, Height = 44, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 5, 0), Font = new Font(Font.FontFamily, 10) };
            var rostro = new AccesoButton { Text = "Rostro", Icono = "rostro", Plano = true, Height = 44, Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0), Font = huella.Font };
            huella.Click += (s, e) => estado.Text = "El acceso por huella aún no está habilitado. Utiliza tu usuario y contraseña.";
            rostro.Click += ValidarAccesoFacial;
            biometria.Controls.Add(huella, 0, 0);
            biometria.Controls.Add(rostro, 1, 0);
            Agregar(layout, biometria, 0, 0);
            composicion.Controls.Add(layout, 1, 0);
            scroll.Controls.Add(composicion);
            raiz.Controls.Add(scroll, 0, 1);
            Controls.Add(raiz);

            bool ajustando = false;
            scroll.Layout += (s, e) =>
            {
                if (ajustando) return;
                ajustando = true;
                try
                {
                    float escala = DeviceDpi / 96F;
                    int margen = (int)(24 * escala);
                    bool dosColumnas = scroll.ClientSize.Width >= 1000 * escala;
                    int ancho = Math.Min((int)((dosColumnas ? 1080 : 452) * escala),
                        Math.Max(1, scroll.ClientSize.Width - 2 * margen));
                    int anchoTarjeta = dosColumnas ? (int)(452 * escala) : ancho;
                    composicion.Width = ancho;
                    composicion.ColumnStyles[1].Width = anchoTarjeta;
                    presentacion.Visible = dosColumnas;
                    presentacion.Margin = new Padding(0, 0, (int)(64 * escala), 0);
                    var anchoTexto = new Size(Math.Max(1, anchoTarjeta - layout.Padding.Horizontal), 0);
                    instrucciones.MaximumSize = anchoTexto;
                    estado.MaximumSize = anchoTexto;
                    errorUsuario.MaximumSize = anchoTexto;
                    errorClave.MaximumSize = anchoTexto;
                    composicion.Height = layout.GetPreferredSize(new Size(anchoTarjeta, 0)).Height;
                    scroll.AutoScrollMinSize = new Size(0, composicion.Height + 2 * margen);
                    int x = Math.Max(margen, (scroll.ClientSize.Width - ancho) / 2);
                    int y = Math.Max(margen, (scroll.ClientSize.Height - composicion.Height) / 2);
                    composicion.Location = new Point(x + scroll.AutoScrollPosition.X, y + scroll.AutoScrollPosition.Y);
                }
                finally
                {
                    ajustando = false;
                }
            };
            EventHandler ajustarAltura = (s, e) => scroll.PerformLayout();
            estado.TextChanged += ajustarAltura;
            errorUsuario.TextChanged += ajustarAltura;
            errorClave.TextChanged += ajustarAltura;
            usuario.TextChanged += (s, e) => errorUsuario.Text = "";
            clave.TextChanged += (s, e) => errorClave.Text = "";
            Shown += (s, e) => usuario.Focus();
        }

        private Control CrearEncabezadoAcceso()
        {
            var encabezado = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                Padding = new Padding(28, 16, 28, 16), Margin = Padding.Empty
            };
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            encabezado.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            encabezado.Controls.Add(new MarcaAcceso { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 8, 3) }, 0, 0);
            var nombre = Texto("AssistPRO", 18, RolTema.Normal, FontStyle.Bold);
            nombre.Dock = DockStyle.Fill;
            nombre.TextAlign = ContentAlignment.MiddleLeft;
            nombre.Margin = Padding.Empty;
            encabezado.Controls.Add(nombre, 1, 0);
            var tema = TemaAplicacion.CrearSelectorTema();
            encabezado.Controls.Add(tema, 2, 0);
            return encabezado;
        }

        private Control CrearPresentacionAcceso()
        {
            var presentacion = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            presentacion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            presentacion.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
            presentacion.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            presentacion.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            presentacion.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
            var titulo = Texto("Gestión de asistencia\npara tu empresa.", 32, RolTema.Normal, FontStyle.Bold);
            titulo.Dock = DockStyle.Fill;
            titulo.Margin = Padding.Empty;
            var descripcion = Texto("Consulta marcaciones, administra el personal y revisa los reportes de tu empresa.",
                12, RolTema.TextoSecundario);
            descripcion.Dock = DockStyle.Fill;
            descripcion.Margin = new Padding(0, 20, 0, 0);
            presentacion.Controls.Add(titulo, 0, 1);
            presentacion.Controls.Add(descripcion, 0, 2);
            return presentacion;
        }

        private static Label Texto(string texto, float tamano, RolTema rol, FontStyle estilo = FontStyle.Regular)
        {
            var etiqueta = new Label { Text = texto, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", tamano, estilo) };
            TemaAplicacion.DefinirRol(etiqueta, rol);
            return etiqueta;
        }

        private static void Agregar(TableLayoutPanel layout, Control control, int superior, int inferior)
        {
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, superior, 0, inferior);
            control.TabIndex = layout.Controls.Count;
            layout.Controls.Add(control, 0, layout.RowCount++);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        private static void PrepararError(Label label)
        {
            label.AutoSize = true;
            label.Visible = false;
            TemaAplicacion.DefinirRol(label, RolTema.Error);
            label.Font = new Font("Segoe UI", 9);
            label.TextChanged += (s, e) => label.Visible = label.Text.Length > 0;
        }

        private static Control Campo(TextBox campo, bool password)
        {
            var borde = new CampoAcceso { Height = 48, Padding = new Padding(46, 4, 14, 4), BackColor = Surface, Cursor = Cursors.IBeam, Icono = password ? "candado" : "usuario" };
            var linea = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = password ? 2 : 1, RowCount = 1, Margin = Padding.Empty
            };
            linea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            linea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            TemaAplicacion.DefinirRol(linea, RolTema.Entrada);
            var interior = new Panel
            {
                Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 7), Margin = Padding.Empty,
                BackColor = CampoAcceso.Superficie
            };
            TemaAplicacion.DefinirRol(interior, RolTema.Entrada);
            campo.Dock = DockStyle.Fill;
            campo.BorderStyle = BorderStyle.None;
            campo.Font = new Font("Segoe UI", 11.5F);
            campo.BackColor = interior.BackColor;
            campo.ForeColor = TemaAplicacion.Actual.Texto;
            campo.Cursor = Cursors.IBeam;
            campo.UseSystemPasswordChar = password;
            campo.AccessibleName = password ? "Contraseña" : "Nombre de usuario";
            campo.AccessibleDescription = password ? "Ingresa tu contraseña" : "Ej. juan.perez";
            interior.Controls.Add(campo);
            var ejemplo = new Label
            {
                Text = campo.AccessibleDescription, Font = campo.Font, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Cursor = Cursors.IBeam,
                TabStop = false, Visible = campo.TextLength == 0 && !campo.Focused, UseMnemonic = false
            };
            TemaAplicacion.DefinirRol(ejemplo, RolTema.TextoSecundario);
            ejemplo.Click += (s, e) => campo.Focus();
            EventHandler actualizarEjemplo = (s, e) => ejemplo.Visible = campo.TextLength == 0 && !campo.Focused;
            campo.TextChanged += actualizarEjemplo;
            campo.GotFocus += actualizarEjemplo;
            campo.LostFocus += actualizarEjemplo;
            MouseEventHandler enfocarCampo = (s, e) =>
            {
                if (e.Button == MouseButtons.Left) campo.Focus();
            };
            borde.MouseDown += enfocarCampo;
            linea.MouseDown += enfocarCampo;
            interior.MouseDown += enfocarCampo;
            interior.Controls.Add(ejemplo);
            ejemplo.BringToFront();
            linea.Controls.Add(interior, 0, 0);

            if (password)
            {
                linea.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
                var ojo = new OjoAcceso { Dock = DockStyle.Fill, Margin = Padding.Empty, TabIndex = 1 };
                TemaAplicacion.DefinirRol(ojo, RolTema.Entrada);
                ojo.CheckedChanged += (s, e) =>
                {
                    int inicio = campo.SelectionStart;
                    int longitud = campo.SelectionLength;
                    campo.UseSystemPasswordChar = !ojo.Checked;
                    campo.Select(inicio, longitud);
                };
                ojo.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left && ojo.ClientRectangle.Contains(e.Location)) campo.Focus();
                };
                campo.TextChanged += (s, e) =>
                {
                    if (campo.TextLength == 0) ojo.Checked = false;
                };
                campo.ReadOnlyChanged += (s, e) =>
                {
                    ojo.Enabled = !campo.ReadOnly;
                    if (campo.ReadOnly) ojo.Checked = false;
                };
                linea.Controls.Add(ojo, 1, 0);
            }

            borde.Controls.Add(linea);
            return borde;
        }

        private async void ValidarAcceso(object sender, EventArgs e)
        {
            if (autenticando || SesionActual != null) return;
            errorUsuario.Text = string.IsNullOrWhiteSpace(usuario.Text) ? "Escribe tu usuario o ID de colaborador." : "";
            errorClave.Text = string.IsNullOrEmpty(clave.Text) ? "Escribe tu contraseña." : "";
            if (errorUsuario.Text.Length > 0) { usuario.Focus(); return; }
            if (errorClave.Text.Length > 0) { clave.Focus(); return; }
            bool accesoAdministrador = perfilAcceso.Checked;
            autenticando = true;
            var boton = (Button)sender;
            boton.Enabled = false;
            perfilAcceso.Enabled = false;
            usuario.ReadOnly = true;
            clave.ReadOnly = true;
            estado.Text = "Verificando credenciales...";
            try
            {
                var sesion = await autenticacion.AutenticarAsync(usuario.Text, clave.Text);
                if (IsDisposed) return;
                clave.Clear();
                if (sesion == null || sesion.EsAdministrador != accesoAdministrador)
                {
                    estado.Text = "Acceso denegado. Revisa tus credenciales y el perfil elegido.";
                    return;
                }

                AbrirSesion(sesion);
            }
            catch (Exception)
            {
                if (!IsDisposed)
                    estado.Text = "No se pudo completar el acceso. Revisa la conexión y vuelve a intentarlo.";
            }
            finally
            {
                autenticando = false;
                if (!IsDisposed)
                {
                    boton.Enabled = true;
                    perfilAcceso.Enabled = true;
                    usuario.ReadOnly = false;
                    clave.ReadOnly = false;
                }
            }
        }

        private async void ValidarAccesoFacial(object sender, EventArgs e)
        {
            if (autenticando || SesionActual != null) return;
            autenticando = true;
            bool accesoAdministrador = perfilAcceso.Checked;
            var boton = (Button)sender;
            boton.Enabled = perfilAcceso.Enabled = false;
            usuario.ReadOnly = clave.ReadOnly = true;
            errorUsuario.Text = errorClave.Text = string.Empty;
            clave.Clear();
            try
            {
                using (var captura = new FrmCapturaFacial("AssistPRO | Acceso por rostro", registrar: false))
                {
                    if (captura.ShowDialog(this) != DialogResult.OK)
                    {
                        estado.Text = "Reconocimiento cancelado.";
                        return;
                    }
                    estado.Text = "Comparando con los rostros registrados…";
                    var sesion = await reconocimientoFacial.AutenticarAsync(captura.Caracteristicas);
                    if (IsDisposed) return;
                    if (sesion == null)
                    {
                        estado.Text = "No se pudo reconocer tu rostro con suficiente certeza. Repite la captura o usa tu contraseña.";
                        return;
                    }
                    if (sesion.EsAdministrador != accesoAdministrador)
                    {
                        estado.Text = "El rostro no corresponde al perfil elegido. Selecciona el perfil de tu cuenta y vuelve a intentarlo.";
                        return;
                    }
                    AbrirSesion(sesion);
                }
            }
            catch (InvalidOperationException error)
            {
                if (!IsDisposed) estado.Text = error.Message;
            }
            catch (Exception)
            {
                if (!IsDisposed) estado.Text = "No se pudo completar el acceso facial. Revisa la conexión con Oracle e inténtalo de nuevo.";
            }
            finally
            {
                autenticando = false;
                if (!IsDisposed)
                {
                    boton.Enabled = perfilAcceso.Enabled = true;
                    usuario.ReadOnly = clave.ReadOnly = false;
                }
            }
        }

        private void AbrirSesion(Logica.SesionUsuario sesion)
        {
            SesionActual = sesion;
            try
            {
                using (Form principal = sesion.EsAdministrador
                    ? (Form)new FrmAdministrador(sesion, consultaAdministracion)
                    : new FrmPrincipal(sesion, consultaAsistencia))
                {
                    Hide();
                    principal.ShowDialog();
                }
            }
            finally
            {
                SesionActual = null;
                if (!IsDisposed)
                {
                    usuario.Clear();
                    clave.Clear();
                    perfilAcceso.Checked = false;
                    estado.Text = string.Empty;
                    Show();
                    usuario.Focus();
                }
            }
        }
    }

    internal sealed class AccesoButton : Button
    {
        public bool Principal { get; set; }
        public bool Plano { get; set; }
        public string Icono { get; set; }
        public AccesoButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = TemaAplicacion.Actual.Texto;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var paleta = TemaAplicacion.Actual;
            e.Graphics.Clear(Parent?.BackColor ?? paleta.Fondo);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var path = new GraphicsPath())
            {
                const int diameter = 20;
                path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
                path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
                path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                bool hover = ClientRectangle.Contains(PointToClient(MousePosition));
                Color fill = hover && Enabled ? ControlPaint.Light(BackColor, .12f) : BackColor;
                if (Plano)
                {
                    using (var brush = new SolidBrush(Principal ? paleta.PrimarioFin : fill))
                        e.Graphics.FillPath(brush, path);
                }
                else
                {
                    using (var brush = new LinearGradientBrush(rect, Principal ? paleta.PrimarioInicio : ControlPaint.Light(fill, .08f), Principal ? paleta.PrimarioFin : fill, 90f))
                        e.Graphics.FillPath(brush, path);
                }
                using (var pen = new Pen(Focused ? paleta.BordeFoco : paleta.Borde, Focused ? 2 : 1))
                    e.Graphics.DrawPath(pen, path);
            }
            var texto = Rectangle.Inflate(ClientRectangle, -12, -4);
            if (!string.IsNullOrEmpty(Icono))
            {
                if (!Plano)
                {
                    using (var circulo = new SolidBrush(paleta.FondoIcono))
                        e.Graphics.FillEllipse(circulo, 9, Height / 2 - 16, 32, 32);
                }
                DibujoAcceso.Icono(e.Graphics, Icono, new Rectangle(14, Height / 2 - 11, 22, 22));
                texto.X = 46;
                texto.Width = Math.Max(1, Width - 54);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, texto,
                Enabled ? (Principal ? paleta.TextoPrimario : ForeColor) : paleta.TextoSecundario,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    }
}


namespace WindowsForms_Presentacion
{
    internal static class DibujoAcceso
    {
        public static GraphicsPath Redondeado(RectangleF r, float radio)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radio * 2, Math.Min(r.Width, r.Height));
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void Icono(Graphics g, string nombre, Rectangle r)
        {
            var state = g.Save();
            g.TranslateTransform(r.X, r.Y);
            g.ScaleTransform(r.Width / 32f, r.Height / 32f);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = new Pen(TemaAplicacion.Actual.Icono, 2))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                switch (nombre)
                {
                    case "buscar":
                        g.DrawEllipse(p, 4, 4, 16, 16);
                        g.DrawLine(p, 18, 18, 28, 28);
                        break;
                    case "usuario":
                        g.DrawEllipse(p, 11, 2, 10, 10);
                        g.DrawArc(p, 4, 18, 24, 22, 180, 180);
                        g.DrawLine(p, 4, 29, 28, 29);
                        break;
                    case "candado":
                        g.DrawArc(p, 9, 2, 14, 20, 180, 180);
                        g.DrawRectangle(p, 5, 13, 22, 17);
                        g.DrawLine(p, 16, 19, 16, 24);
                        break;
                    case "huella":
                        for (int i = 0; i < 4; i++)
                        {
                            int inset = 3 + i * 3;
                            g.DrawArc(p, inset, inset, 32 - 2 * inset, 34 - 2 * inset, 160, 270);
                        }
                        g.DrawBezier(p, 15, 16, 17, 25, 12, 26, 10, 31);
                        g.DrawBezier(p, 21, 17, 24, 26, 20, 29, 18, 31);
                        break;
                    case "rostro":
                        g.DrawLines(p, new Point[] { new Point(2, 10), new Point(2, 2), new Point(10, 2) });
                        g.DrawLines(p, new Point[] { new Point(22, 2), new Point(30, 2), new Point(30, 10) });
                        g.DrawLines(p, new Point[] { new Point(2, 22), new Point(2, 30), new Point(10, 30) });
                        g.DrawLines(p, new Point[] { new Point(22, 30), new Point(30, 30), new Point(30, 22) });
                        g.DrawLine(p, 10, 11, 10, 14);
                        g.DrawLine(p, 22, 11, 22, 14);
                        g.DrawArc(p, 10, 16, 12, 9, 0, 180);
                        break;
                }
            }
            g.Restore(state);
        }
    }

    internal sealed class CampoAcceso : Panel
    {
        internal static Color Superficie => TemaAplicacion.Actual.Entrada;
        public string Icono { get; set; }

        public CampoAcceso() { DoubleBuffered = true; ResizeRedraw = true; }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float grosor = (ContainsFocus ? 2.5F : 1F) * DeviceDpi / 96F;
            float margen = grosor / 2F + .5F;
            using (var path = DibujoAcceso.Redondeado(new RectangleF(margen, margen,
                Width - margen * 2F, Height - margen * 2F), 12))
            using (var relleno = new SolidBrush(Superficie))
            using (var pen = new Pen(ContainsFocus ? TemaAplicacion.Actual.BordeFoco : TemaAplicacion.Actual.LineaTabla, grosor))
            {
                e.Graphics.FillPath(relleno, path);
                e.Graphics.DrawPath(pen, path);
            }
            DibujoAcceso.Icono(e.Graphics, Icono, new Rectangle(11, (Height - 22) / 2, 22, 22));
        }
    }

    internal sealed class OjoAcceso : CheckBox
    {
        internal OjoAcceso()
        {
            AutoSize = false;
            Appearance = Appearance.Button;
            FlatStyle = FlatStyle.Flat;
            CausesValidation = false;
            Cursor = Cursors.Hand;
            AccessibleName = "Mostrar contraseña";
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            AccessibleName = Checked ? "Ocultar contraseña" : "Mostrar contraseña";
            base.OnCheckedChanged(e);
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var paleta = TemaAplicacion.Actual;
            e.Graphics.Clear(paleta.Entrada);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float escala = DeviceDpi / 96F;
            float ancho = 20 * escala;
            float x = (Width - ancho) / 2;
            float y = Height / 2F;
            Color color = Enabled ? paleta.Icono : paleta.TextoSecundario;
            using (var linea = new Pen(color, 1.6F * escala))
            using (var ojo = new GraphicsPath())
            {
                linea.StartCap = LineCap.Round;
                linea.EndCap = LineCap.Round;
                ojo.AddBezier(x, y, x + ancho * .25F, y - ancho * .45F,
                    x + ancho * .75F, y - ancho * .45F, x + ancho, y);
                ojo.AddBezier(x + ancho, y, x + ancho * .75F, y + ancho * .45F,
                    x + ancho * .25F, y + ancho * .45F, x, y);
                e.Graphics.DrawPath(linea, ojo);
                e.Graphics.DrawEllipse(linea, x + ancho * .36F, y - ancho * .14F, ancho * .28F, ancho * .28F);
                if (!Checked)
                    e.Graphics.DrawLine(linea, x + escala, y - ancho * .42F, x + ancho - escala, y + ancho * .42F);
            }

            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -3, -3),
                    paleta.BordeFoco, paleta.Entrada);
        }
    }

    internal sealed class MarcaAcceso : Control
    {
        public MarcaAcceso()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            ResizeRedraw = true;
            Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            AccessibleName = "AssistPRO";
            AccessibleRole = AccessibleRole.Graphic;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int lado = Math.Min(Width, Height) - 2;
            if (lado <= 0) return;
            var limites = new Rectangle((Width - lado) / 2, (Height - lado) / 2, lado, lado);
            var paleta = TemaAplicacion.Actual;
            using (var contorno = DibujoAcceso.Redondeado(limites, 8))
            using (var fondo = new SolidBrush(paleta.PrimarioFin))
                e.Graphics.FillPath(fondo, contorno);
            TextRenderer.DrawText(e.Graphics, "A", Font, limites, paleta.TextoPrimario,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    internal sealed class TarjetaAcceso : TableLayoutPanel
    {
        public TarjetaAcceso() { DoubleBuffered = true; ResizeRedraw = true; }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? TemaAplicacion.Actual.Fondo);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(1, 1, Width - 3, Height - 3);
            if (r.Width <= 0 || r.Height <= 0) return;
            using (var path = DibujoAcceso.Redondeado(r, 18))
            using (var fill = new SolidBrush(BackColor))
            using (var edge = new Pen(TemaAplicacion.Actual.LineaTabla, 1F))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(edge, path);
            }
        }
    }

    internal sealed class FondoAcceso : Panel
    {
        public FondoAcceso() { DoubleBuffered = true; ResizeRedraw = true; BackColor = TemaAplicacion.Actual.Fondo; }
    }
}
