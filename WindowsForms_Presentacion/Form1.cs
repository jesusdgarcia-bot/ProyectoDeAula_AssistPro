using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsForms_Presentacion
{
    public partial class Form1 : Form
    {
        private static readonly Color Surface = Color.FromArgb(25, 19, 37);
        private static readonly Color Muted = Color.FromArgb(192, 181, 207);
        private static readonly Color Accent = Color.FromArgb(139, 80, 224);
        private readonly TextBox usuario = new TextBox();
        private readonly TextBox clave = new TextBox();
        private readonly RadioButton perfilEmpleado = new RadioButton();
        private readonly RadioButton perfilAdministrador = new RadioButton();
        private readonly Label errorUsuario = new Label();
        private readonly Label errorClave = new Label();
        private readonly Label estado = new Label();

        private readonly Logica.ServicioAutenticacion autenticacion;
        private readonly Logica.IConsultaAsistencia consultaAsistencia;
        private readonly Logica.IConsultaAdministracion consultaAdministracion;
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
            InitializeComponent();
            Text = "AssistPRO | Acceso";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 11);
            BackColor = Color.FromArgb(13, 10, 20);
            ForeColor = Color.White;
            ClientSize = new Size(820, 660);
            MinimumSize = new Size(420, 540);
            DoubleBuffered = true;
            CrearAcceso();
        }

        private void CrearAcceso()
        {
            const int anchoMaximo = 464;
            const int margen = 20;
            var scroll = new FondoAcceso { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(margen) };
            var layout = new TarjetaAcceso
            {
                AutoSize = false,
                ColumnCount = 1, RowCount = 0, Padding = new Padding(26, 20, 26, 20),
                BackColor = Surface, Width = anchoMaximo, TabStop = false
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Agregar(layout, new AvatarAcceso { Height = 62 }, 0, 5);
            Agregar(layout, Texto("AssistPRO", 25, Color.White, FontStyle.Bold), 0, 2);
            Agregar(layout, Texto("Control de asistencia laboral", 10, Muted), 0, 15);

            Agregar(layout, Texto("Perfil de acceso", 10, Muted), 0, 5);
            var perfiles = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, AutoSize = true };
            perfiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            perfiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            PrepararPerfil(perfilEmpleado, "Empleado", new Padding(0, 0, 4, 0));
            PrepararPerfil(perfilAdministrador, "Administrador", new Padding(4, 0, 0, 0));
            perfiles.Controls.Add(perfilEmpleado, 0, 0);
            perfiles.Controls.Add(perfilAdministrador, 1, 0);
            perfilEmpleado.Checked = true;
            Agregar(layout, perfiles, 0, 11);

            Agregar(layout, Texto("Nombre de usuario", 10, Muted), 0, 4);
            Agregar(layout, Campo(usuario, false), 0, 0);
            PrepararError(errorUsuario);
            Agregar(layout, errorUsuario, 4, 0);
            Agregar(layout, Texto("Contraseña", 10, Muted), 10, 4);
            Agregar(layout, Campo(clave, true), 0, 0);
            PrepararError(errorClave);
            Agregar(layout, errorClave, 4, 0);

            var opciones = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, WrapContents = true,
                Font = new Font(Font.FontFamily, 10)
            };
            var mostrar = new CheckBox { Text = "Mostrar contraseña", AutoSize = true, ForeColor = Muted, Margin = new Padding(0, 0, 14, 3) };
            mostrar.CheckedChanged += (s, e) => clave.UseSystemPasswordChar = !mostrar.Checked;
            var recuperar = new LinkLabel
            {
                Text = "Recuperar acceso", AutoSize = true, LinkColor = Color.FromArgb(201, 165, 255),
                ActiveLinkColor = Color.White, VisitedLinkColor = Color.FromArgb(201, 165, 255),
                Margin = new Padding(0, 2, 0, 3)
            };
            recuperar.LinkClicked += (s, e) => estado.Text = "Contacta al administrador de tu empresa para recuperar el acceso.";
            opciones.Controls.Add(mostrar);
            opciones.Controls.Add(recuperar);
            Agregar(layout, opciones, 9, 10);

            var entrar = new AccesoButton { Text = "Iniciar sesión", BackColor = Accent, Height = 44, Principal = true };
            entrar.Click += ValidarAcceso;
            Agregar(layout, entrar, 0, 6);
            AcceptButton = entrar;
            estado.AutoSize = true;
            estado.ForeColor = Muted;
            estado.AccessibleName = "Estado del acceso";
            estado.Text = "Ingresa tus credenciales para continuar.";
            estado.Font = new Font(Font.FontFamily, 10);
            Agregar(layout, estado, 0, 13);
            Agregar(layout, Texto("Acceso biométrico", 10, Muted), 0, 5);
            var biometria = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
            biometria.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            biometria.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var huella = new AccesoButton { Text = "Huella dactilar", Icono = "huella", Height = 48, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 4, 0), BackColor = Color.FromArgb(43, 28, 62), Font = opciones.Font };
            var rostro = new AccesoButton { Text = "Rostro", Icono = "rostro", Height = 48, Dock = DockStyle.Fill, Margin = new Padding(4, 0, 0, 0), BackColor = huella.BackColor, Font = opciones.Font };
            huella.Click += (s, e) => estado.Text = "El acceso por huella aún no está habilitado. Utiliza tu usuario y contraseña.";
            rostro.Click += (s, e) => estado.Text = "El reconocimiento facial aún no está habilitado. Utiliza tu usuario y contraseña.";
            biometria.Controls.Add(huella, 0, 0);
            biometria.Controls.Add(rostro, 1, 0);
            Agregar(layout, biometria, 0, 0);
            scroll.Controls.Add(layout);
            Controls.Add(scroll);

            // El ancho depende del espacio disponible. AutoSize conserva todo el texto.
            scroll.Layout += (s, e) =>
            {
                int ancho = Math.Min(anchoMaximo, Math.Max(280, scroll.ClientSize.Width - 2 * margen));
                layout.Width = ancho;
                var anchoTexto = new Size(ancho - layout.Padding.Horizontal, 0);
                estado.MaximumSize = anchoTexto;
                errorUsuario.MaximumSize = anchoTexto;
                errorClave.MaximumSize = anchoTexto;
                layout.Height = layout.GetPreferredSize(new Size(ancho, 0)).Height;
                int x = Math.Max(margen, (scroll.ClientSize.Width - ancho) / 2);
                int y = Math.Max(margen, (scroll.ClientSize.Height - layout.Height) / 2);
                layout.Location = new Point(x + scroll.AutoScrollPosition.X, y + scroll.AutoScrollPosition.Y);
            };
            EventHandler ajustarAltura = (s, e) => scroll.PerformLayout();
            estado.TextChanged += ajustarAltura;
            errorUsuario.TextChanged += ajustarAltura;
            errorClave.TextChanged += ajustarAltura;
            usuario.TextChanged += (s, e) => errorUsuario.Text = "";
            clave.TextChanged += (s, e) => errorClave.Text = "";
            Shown += (s, e) => usuario.Focus();
        }

        private static void PrepararPerfil(RadioButton boton, string texto, Padding margen)
        {
            boton.Text = texto;
            boton.Appearance = Appearance.Button;
            boton.AutoSize = false;
            boton.Dock = DockStyle.Fill;
            boton.Height = 34;
            boton.Margin = margen;
            boton.TextAlign = ContentAlignment.MiddleCenter;
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderColor = Color.FromArgb(119, 76, 155);
            boton.FlatAppearance.CheckedBackColor = Color.FromArgb(102, 49, 171);
            boton.BackColor = Surface;
            boton.ForeColor = Color.White;
            boton.UseVisualStyleBackColor = false;
        }

        private static Label Texto(string texto, float tamano, Color color, FontStyle estilo = FontStyle.Regular)
        {
            return new Label { Text = texto, AutoSize = true, TextAlign = texto == "AssistPRO" || texto == "Control de asistencia laboral" ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft, ForeColor = color, Font = new Font("Segoe UI", tamano, estilo) };
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
            label.ForeColor = Color.FromArgb(255, 178, 190);
            label.Font = new Font("Segoe UI", 9);
            label.TextChanged += (s, e) => label.Visible = label.Text.Length > 0;
        }

        private static Control Campo(TextBox campo, bool password)
        {
            var borde = new CampoAcceso { Height = 42, Padding = new Padding(42, 3, 13, 3), BackColor = Surface, Icono = password ? "candado" : "usuario" };
            var interior = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 7, 0, 6), BackColor = CampoAcceso.Superficie };
            campo.Dock = DockStyle.Fill;
            campo.BorderStyle = BorderStyle.None;
            campo.Font = new Font("Segoe UI", 12);
            campo.BackColor = interior.BackColor;
            campo.ForeColor = Color.White;
            campo.UseSystemPasswordChar = password;
            campo.AccessibleName = password ? "Contraseña" : "Nombre de usuario";
            campo.Enter += (s, e) => { borde.Activo = true; borde.Invalidate(); };
            campo.Leave += (s, e) => { borde.Activo = false; borde.Invalidate(); };
            interior.Controls.Add(campo);
            borde.Controls.Add(interior);
            return borde;
        }

        private async void ValidarAcceso(object sender, EventArgs e)
        {
            if (autenticando || SesionActual != null) return;
            errorUsuario.Text = string.IsNullOrWhiteSpace(usuario.Text) ? "Escribe tu usuario o ID de colaborador." : "";
            errorClave.Text = string.IsNullOrEmpty(clave.Text) ? "Escribe tu contraseña." : "";
            if (errorUsuario.Text.Length > 0) { usuario.Focus(); return; }
            if (errorClave.Text.Length > 0) { clave.Focus(); return; }
            bool accesoAdministrador = perfilAdministrador.Checked;
            autenticando = true;
            var boton = (Button)sender;
            boton.Enabled = false;
            perfilEmpleado.Enabled = false;
            perfilAdministrador.Enabled = false;
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
                        perfilEmpleado.Checked = true;
                        estado.Text = "Ingresa tus credenciales para continuar.";
                        Show();
                        usuario.Focus();
                    }
                }
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
                    perfilEmpleado.Enabled = true;
                    perfilAdministrador.Enabled = true;
                    usuario.ReadOnly = false;
                    clave.ReadOnly = false;
                }
            }
        }
    }

    internal sealed class AccesoButton : Button
    {
        public bool Principal { get; set; }
        public string Icono { get; set; }
        public AccesoButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Color.White;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent.BackColor);
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
                using (var brush = new LinearGradientBrush(rect, Principal ? Color.FromArgb(175, 83, 245) : ControlPaint.Light(fill, .08f), Principal ? Color.FromArgb(107, 26, 188) : fill, 90f)) e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(Focused ? Color.FromArgb(224, 204, 255) : Color.FromArgb(112, 80, 149), Focused ? 2 : 1))
                    e.Graphics.DrawPath(pen, path);
            }
            var texto = Rectangle.Inflate(ClientRectangle, -12, -4);
            if (!string.IsNullOrEmpty(Icono))
            {
                using (var circulo = new SolidBrush(Color.FromArgb(83, 37, 127)))
                    e.Graphics.FillEllipse(circulo, 9, Height / 2 - 16, 32, 32);
                DibujoAcceso.Icono(e.Graphics, Icono, new Rectangle(14, Height / 2 - 11, 22, 22));
                texto.X = 46;
                texto.Width = Math.Max(1, Width - 54);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, texto,
                Enabled ? ForeColor : Color.Gray, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
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
            using (var p = new Pen(Color.FromArgb(221, 192, 255), 2))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                switch (nombre)
                {
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
        internal static readonly Color Superficie = Color.FromArgb(17, 12, 26);
        public string Icono { get; set; }
        public bool Activo { get; set; }

        public CampoAcceso() { DoubleBuffered = true; ResizeRedraw = true; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = DibujoAcceso.Redondeado(new RectangleF(1, 1, Width - 3, Height - 3), 12))
            using (var relleno = new SolidBrush(Superficie))
            using (var pen = new Pen(Activo ? Color.FromArgb(221, 181, 255) : Color.FromArgb(119, 76, 155), Activo ? 2 : 1))
            {
                e.Graphics.FillPath(relleno, path);
                e.Graphics.DrawPath(pen, path);
            }
            DibujoAcceso.Icono(e.Graphics, Icono, new Rectangle(11, (Height - 22) / 2, 22, 22));
        }
    }

    internal sealed class AvatarAcceso : Control
    {
        public AvatarAcceso()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int diametro = Math.Min(56, Math.Min(Width, Height) - 4);
            if (diametro <= 0) return;

            var borde = new Rectangle((Width - diametro) / 2, (Height - diametro) / 2, diametro, diametro);
            using (var fondo = new LinearGradientBrush(borde, Color.FromArgb(108, 38, 157), Color.FromArgb(48, 23, 76), 90))
            using (var linea = new Pen(Color.FromArgb(185, 114, 233), 1.5f))
            {
                e.Graphics.FillEllipse(fondo, borde);
                e.Graphics.DrawEllipse(linea, borde);
            }
            int icono = diametro * 4 / 7;
            DibujoAcceso.Icono(e.Graphics, "usuario", new Rectangle((Width - icono) / 2, (Height - icono) / 2, icono, icono));
        }
    }

    internal sealed class TarjetaAcceso : TableLayoutPanel
    {
        public TarjetaAcceso() { DoubleBuffered = true; ResizeRedraw = true; }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Color.FromArgb(13, 10, 20) : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(1, 1, Width - 3, Height - 3);
            if (r.Width <= 0 || r.Height <= 0) return;
            using (var path = DibujoAcceso.Redondeado(r, 26))
            using (var fill = new SolidBrush(BackColor))
            using (var edge = new Pen(Color.FromArgb(146, 88, 185), 1.5f))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(edge, path);
            }
            using (var glow = new LinearGradientBrush(new Rectangle(Width / 4, 1, Width / 2, 3), Color.Transparent, Color.FromArgb(220, 162, 255), 0f))
            {
                glow.SetBlendTriangularShape(.5f);
                e.Graphics.FillRectangle(glow, Width / 4, 1, Width / 2, 2);
            }
        }
    }

    internal sealed class FondoAcceso : Panel
    {
        public FondoAcceso() { DoubleBuffered = true; ResizeRedraw = true; BackColor = Color.FromArgb(13, 10, 20); }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            using (var grid = new Pen(Color.FromArgb(25, 17, 36)))
            {
                for (int x = 0; x < Width; x += 40) e.Graphics.DrawLine(grid, x, 0, x, Height);
                for (int y = 0; y < Height; y += 40) e.Graphics.DrawLine(grid, 0, y, Width, y);
            }
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(-Width / 3, -Height / 3, Width, Height);
                using (var glow = new PathGradientBrush(path))
                {
                    glow.CenterColor = Color.FromArgb(95, 99, 25, 156);
                    glow.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(glow, path);
                }
            }
            using (var ribbon = new Pen(Color.FromArgb(65, 147, 65, 211), 2))
            {
                e.Graphics.DrawBezier(ribbon, 0, 160, 100, 150, 200, 10, 360, 0);
                e.Graphics.DrawBezier(ribbon, Width - 300, Height, Width - 150, Height - 70, Width - 80, Height - 90, Width, Height - 220);
            }
        }
    }
}
