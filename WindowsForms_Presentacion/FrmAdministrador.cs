using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Entidades;
using Logica;

namespace WindowsForms_Presentacion
{
    internal sealed partial class FrmAdministrador : Form
    {
        private const string ModuloDashboard = "Dashboard";
        private const string ModuloPersonal = "Personal";
        private const string ModuloMarcaciones = "Marcaciones";
        private const string ModuloTurnos = "Turnos";
        private const string ModuloAsignaciones = "Asignación de turnos";
        private const string ModuloNovedades = "Novedades laborales";
        private const string ModuloTiposNovedad = "Tipos de novedad";
        private const string ModuloSedes = "Sedes";
        private const string ModuloUnidades = "Unidades y servicios";
        private const string ModuloCargos = "Cargos";
        private const string ModuloTerminales = "Terminales de asistencia";
        private const string ModuloCuentas = "Cuentas de acceso";
        private const string ModuloRoles = "Roles de acceso";
        private const string ModuloBiometria = "Biometría";
        private const string ModuloVisitantes = "Visitantes";
        private static Color Fondo => TemaAplicacion.Actual.Fondo;
        private static Color FondoLateral => TemaAplicacion.Actual.Lateral;
        private static Color TextoSecundario => TemaAplicacion.Actual.TextoSecundario;
        private static Color FondoTabla => TemaAplicacion.Actual.Superficie;
        private static Color EncabezadoTabla => TemaAplicacion.Actual.EncabezadoTabla;
        private static Color SeleccionTabla => TemaAplicacion.Actual.SeleccionTabla;
        private readonly ListBox modulos = new ListBox();
        private Vista VistaActual => modulos.SelectedItem as Vista;
        private readonly DataGridView tabla = new DataGridView();
        private readonly FlowLayoutPanel filtros = new FlowLayoutPanel();
        private readonly DateTimePicker desde = new DateTimePicker();
        private readonly DateTimePicker hasta = new DateTimePicker();
        private readonly AccesoButton actualizar = new AccesoButton
        {
            Text = "Actualizar", AccessibleName = "Actualizar vista", Size = new Size(104, 34), Plano = true
        };
        private readonly AccesoButton agregar = new AccesoButton
        {
            Text = "Agregar", Principal = true, Size = new Size(96, 34), Plano = true
        };
        private readonly AccesoButton filtrarAsistencia = new AccesoButton
        {
            Text = "Filtrar asistencia", BackColor = SeleccionTabla, Size = new Size(154, 34), Plano = true
        };
        private readonly AccesoButton asistenciaFacial = new AccesoButton
        {
            Text = "Asistencia facial", AccessibleName = "Registrar asistencia con reconocimiento facial",
            Principal = true, Size = new Size(154, 34), Plano = true, Visible = false
        };
        private readonly AccesoButton reenviarQr = new AccesoButton
        {
            Text = "Reenviar QR", AccessibleName = "Reenviar código QR del visitante",
            Size = new Size(112, 34), Plano = true, Visible = false
        };
        private readonly AccesoButton correoVisitantes = new AccesoButton
        {
            Text = "Configurar correo", AccessibleName = "Configurar envío de invitaciones",
            Size = new Size(142, 34), Plano = true, Visible = false
        };
        private readonly AccesoButton editar = new AccesoButton
        {
            Text = "Editar", AccessibleName = "Actualizar registro seleccionado",
            Size = new Size(80, 34), Enabled = false, Plano = true
        };
        private readonly AccesoButton eliminar = new AccesoButton
        {
            Text = "Eliminar", Size = new Size(88, 34), Enabled = false, Plano = true
        };
        private readonly Panel herramientas = new Panel();
        private readonly FlowLayoutPanel acciones = new FlowLayoutPanel();
        private readonly RowStyle filaHerramientas = new RowStyle(SizeType.Absolute, 46);
        private readonly FlowLayoutPanel busqueda = new FlowLayoutPanel();
        private readonly CampoAcceso campoBusqueda = new CampoAcceso();
        private readonly TextBox buscarTexto = new TextBox
        {
            MaxLength = 200, AccessibleName = "Buscar en los registros", Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None, Margin = Padding.Empty
        };
        private readonly AccesoButton buscar = new AccesoButton { Text = "Buscar", Size = new Size(80, 34), Plano = true };
        private readonly AccesoButton limpiarBusqueda = new AccesoButton { Text = "Limpiar", Size = new Size(82, 34), Plano = true };
        private bool ajustandoHerramientas;
        private readonly int primerCatalogo;
        private int moduloBajoCursor = -1;
        private string textoBuscado = string.Empty;
        private int? idEmpleadoFiltrado;
        private int? idSedeFiltrada;
        private int? idUnidadFiltrada;
        private readonly SesionUsuario sesion;
        private readonly IConsultaAdministracion consulta;
        private readonly bool reconocimientoHabilitado;
        private readonly string estadoActivo = System.Configuration.ConfigurationManager.AppSettings["EstadoActivo"] ?? "A";
        private readonly Label titulo = new Label();
        private readonly Label estado = new Label();
        private bool cargando;

        internal FrmAdministrador(SesionUsuario sesion, IConsultaAdministracion consulta, bool reconocimientoHabilitado = false)
        {
            if (sesion == null) throw new ArgumentNullException(nameof(sesion));
            if (!sesion.EsAdministrador) throw new UnauthorizedAccessException();
            if (consulta == null) throw new ArgumentNullException(nameof(consulta));
            this.sesion = sesion;
            this.consulta = consulta;
            this.reconocimientoHabilitado = reconocimientoHabilitado;

            Text = "AssistPRO | Gestión de Talento Humano · San Martín";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            ClientSize = new Size(Math.Min(1120, area.Width - 64), Math.Min(740, area.Height - 64));
            MinimumSize = new Size(Math.Min(800, area.Width), Math.Min(520, area.Height));
            WindowState = FormWindowState.Maximized;
            BackColor = Fondo;
            ForeColor = TemaAplicacion.Actual.Texto;
            Font = new Font("Segoe UI", 10.5F);

            ConstruirInterfaz(sesion);
            ConfigurarDashboard();
            ConfigurarOperaciones();
            primerCatalogo = modulos.Items.Count;
            ConfigurarCatalogos();

            modulos.SelectedIndexChanged += async (origen, evento) =>
            {
                buscarTexto.Clear();
                await CargarVistaAsync();
            };
            actualizar.Click += async (origen, evento) => await CargarVistaAsync();
            agregar.Click += async (origen, evento) => await AgregarRegistroAsync();
            editar.Click += async (origen, evento) => await EditarSeleccionadoAsync();
            eliminar.Click += async (origen, evento) => await EliminarSeleccionadoAsync();
            buscar.Click += async (origen, evento) => await CargarVistaAsync();
            limpiarBusqueda.Click += async (origen, evento) =>
            {
                buscarTexto.Clear();
                await CargarVistaAsync();
            };
            buscarTexto.KeyDown += async (origen, evento) =>
            {
                if (evento.KeyCode != Keys.Enter) return;
                evento.Handled = evento.SuppressKeyPress = true;
                await CargarVistaAsync();
            };
            tabla.SelectionChanged += (origen, evento) => ActualizarAccionesSeleccion();
            tabla.CellDoubleClick += async (origen, evento) =>
            {
                if (evento.RowIndex >= 0) await EditarSeleccionadoAsync();
            };
            filtrarAsistencia.Click += async (origen, evento) => await FiltrarAsistenciaAsync();
            asistenciaFacial.Click += async (origen, evento) => await TomarAsistenciaFacialAsync();
            reenviarQr.Click += async (origen, evento) => await ReenviarQrVisitanteAsync();
            correoVisitantes.Click += (origen, evento) => AbrirConfiguracionCorreoVisitantes();
            Shown += (origen, evento) =>
            {
                AjustarAlturaModulos();
                modulos.SelectedIndex = 0;
            };
            TemaAplicacion.Registrar(this);
        }

        private void ConstruirInterfaz(SesionUsuario sesion)
        {
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 216));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.Controls.Add(CrearNavegacion(sesion), 0, 0);
            raiz.Controls.Add(CrearContenido(), 1, 0);
            Controls.Add(raiz);
        }

        private Control CrearNavegacion(SesionUsuario sesion)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                Padding = new Padding(14, 14, 14, 14), BackColor = FondoLateral, Margin = Padding.Empty
            };
            TemaAplicacion.DefinirRol(panel, RolTema.Lateral);
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            var marca = TemaAplicacion.CrearMarca();
            marca.Margin = new Padding(0, 0, 0, 8);
            panel.Controls.Add(marca, 0, 0);
            modulos.Dock = DockStyle.Fill;
            modulos.BorderStyle = BorderStyle.None;
            modulos.BackColor = FondoLateral;
            modulos.ForeColor = TextoSecundario;
            TemaAplicacion.DefinirRol(modulos, RolTema.Lateral);
            modulos.AccessibleName = "Secciones del panel de administración";
            modulos.Margin = Padding.Empty;
            modulos.DisplayMember = nameof(Vista.Nombre);
            modulos.IntegralHeight = false;
            modulos.DrawMode = DrawMode.OwnerDrawFixed;
            modulos.ItemHeight = Font.Height + 14;
            modulos.DrawItem += DibujarModulo;
            modulos.MouseMove += (origen, evento) => MarcarModuloBajoCursor(modulos.IndexFromPoint(evento.Location));
            modulos.MouseLeave += (origen, evento) => MarcarModuloBajoCursor(-1);
            modulos.Resize += (origen, evento) => AjustarAlturaModulos();
            panel.Controls.Add(modulos, 0, 1);
            panel.Controls.Add(new Label
            {
                Text = sesion.Nombre, Dock = DockStyle.Fill, AutoEllipsis = true, UseMnemonic = false,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 2);
            var rolUsuario = new Label
            {
                Text = sesion.Rol, Dock = DockStyle.Fill, AutoEllipsis = true, UseMnemonic = false,
                ForeColor = TextoSecundario
            };
            TemaAplicacion.DefinirRol(rolUsuario, RolTema.TextoSecundario);
            panel.Controls.Add(rolUsuario, 0, 3);
            var salir = new AccesoButton
            {
                Text = "Cerrar sesión", Dock = DockStyle.Fill, Plano = true
            };
            salir.Click += (origen, evento) => Close();
            panel.Controls.Add(salir, 0, 4);
            return panel;
        }

        private void AjustarAlturaModulos()
        {
            if (modulos.Items.Count == 0 || modulos.Height <= 0) return;
            int ideal = Font.Height + (int)(16 * DeviceDpi / 96F);
            int altura = Math.Max(Font.Height + 4, Math.Min(ideal, modulos.Height / modulos.Items.Count));
            if (modulos.ItemHeight != altura) modulos.ItemHeight = altura;
        }

        private void MarcarModuloBajoCursor(int indice)
        {
            if (moduloBajoCursor == indice) return;
            int anterior = moduloBajoCursor;
            moduloBajoCursor = indice;
            if (anterior >= 0) modulos.Invalidate(modulos.GetItemRectangle(anterior));
            if (indice >= 0) modulos.Invalidate(modulos.GetItemRectangle(indice));
        }

        private void DibujarModulo(object origen, DrawItemEventArgs evento)
        {
            if (evento.Index < 0) return;
            var tema = TemaAplicacion.Actual;
            bool seleccionado = (evento.State & DrawItemState.Selected) != 0;
            using (var fondo = new SolidBrush(FondoLateral))
                evento.Graphics.FillRectangle(fondo, evento.Bounds);
            float escala = DeviceDpi / 96F;
            int margenVertical = Math.Max(1, (int)(2 * escala));
            var rectangulo = Rectangle.Inflate(evento.Bounds, -1, -margenVertical);
            if (evento.Index == primerCatalogo)
            {
                using (var linea = new Pen(tema.LineaTabla))
                    evento.Graphics.DrawLine(linea, rectangulo.Left + 5, evento.Bounds.Top,
                        rectangulo.Right - 5, evento.Bounds.Top);
            }
            if (seleccionado || modulos.Enabled && evento.Index == moduloBajoCursor)
            {
                var estadoGrafico = evento.Graphics.Save();
                evento.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var contorno = DibujoAcceso.Redondeado(rectangulo, 7 * escala))
                using (var fondo = new SolidBrush(seleccionado ? tema.SeleccionTabla : tema.AlternanciaTabla))
                    evento.Graphics.FillPath(fondo, contorno);
                if (seleccionado)
                {
                    int altura = Math.Max(1, rectangulo.Height - (int)(12 * escala));
                    using (var acento = new SolidBrush(tema.AcentoMorado))
                        evento.Graphics.FillRectangle(acento, rectangulo.Left + (int)(4 * escala),
                            rectangulo.Top + (rectangulo.Height - altura) / 2, Math.Max(2, (int)(3 * escala)), altura);
                }
                evento.Graphics.Restore(estadoGrafico);
            }
            TextRenderer.DrawText(evento.Graphics, modulos.GetItemText(modulos.Items[evento.Index]), modulos.Font,
                Rectangle.Inflate(rectangulo, -(int)(13 * escala), 0), seleccionado ? tema.TextoSeleccionTabla : TextoSecundario,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if ((evento.State & DrawItemState.Focus) != 0)
                ControlPaint.DrawFocusRectangle(evento.Graphics, Rectangle.Inflate(rectangulo, -4, -4),
                    tema.BordeFoco, FondoLateral);
        }

        private Control CrearContenido()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                Padding = new Padding(22, 18, 22, 16), Margin = Padding.Empty
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(filaHerramientas);
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var encabezado = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                Margin = new Padding(0, 0, 0, 12)
            };
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            encabezado.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            titulo.Dock = DockStyle.Fill;
            titulo.AutoEllipsis = true;
            titulo.Font = new Font(Font.FontFamily, 24, FontStyle.Bold);
            titulo.Margin = Padding.Empty;
            titulo.TextAlign = ContentAlignment.MiddleLeft;
            encabezado.Controls.Add(titulo, 0, 0);
            var selectorTema = TemaAplicacion.CrearSelectorTema();
            selectorTema.Margin = new Padding(8, 0, 0, 0);
            encabezado.Controls.Add(selectorTema, 1, 0);
            panel.Controls.Add(encabezado, 0, 0);

            herramientas.Dock = DockStyle.Fill;
            herramientas.Margin = Padding.Empty;
            filtros.Size = new Size(272, 49);
            filtros.WrapContents = false;
            filtros.Margin = Padding.Empty;
            DateTime hoy = PlanificacionTurnos.AhoraColombia.Date;
            AgregarFecha(filtros, "Desde", desde, hoy.AddDays(-30));
            AgregarFecha(filtros, "Hasta", hasta, hoy);
            herramientas.Controls.Add(CrearFiltrosDashboard());
            herramientas.Controls.Add(filtros);
            herramientas.Controls.Add(filtrarAsistencia);

            acciones.Margin = Padding.Empty;
            agregar.Margin = editar.Margin = eliminar.Margin = new Padding(0, 0, 6, 0);
            actualizar.Margin = Padding.Empty;
            foreach (var boton in new[] { agregar, editar, eliminar, actualizar, filtrarAsistencia, asistenciaFacial,
                reenviarQr, correoVisitantes, buscar, limpiarBusqueda })
                boton.Font = new Font(Font.FontFamily, 10F);
            TemaAplicacion.DefinirRol(eliminar, RolTema.Error);
            acciones.Controls.Add(agregar);
            acciones.Controls.Add(asistenciaFacial);
            acciones.Controls.Add(reenviarQr);
            acciones.Controls.Add(correoVisitantes);
            acciones.Controls.Add(editar);
            acciones.Controls.Add(eliminar);
            acciones.Controls.Add(actualizar);
            herramientas.Controls.Add(acciones);

            busqueda.WrapContents = false;
            busqueda.Margin = Padding.Empty;
            campoBusqueda.Size = new Size(188, 34);
            campoBusqueda.Padding = new Padding(38, 6, 10, 5);
            campoBusqueda.Margin = new Padding(0, 0, 6, 0);
            campoBusqueda.Icono = "buscar";
            campoBusqueda.Cursor = Cursors.IBeam;
            campoBusqueda.MouseDown += (origen, evento) => buscarTexto.Focus();
            campoBusqueda.Controls.Add(buscarTexto);
            buscarTexto.Font = new Font(Font.FontFamily, 10F);
            buscar.Margin = new Padding(0, 0, 6, 0);
            limpiarBusqueda.Margin = Padding.Empty;
            busqueda.Controls.Add(campoBusqueda);
            busqueda.Controls.Add(buscar);
            busqueda.Controls.Add(limpiarBusqueda);
            herramientas.Controls.Add(busqueda);
            herramientas.SizeChanged += (origen, evento) => AjustarHerramientas();
            panel.Controls.Add(herramientas, 0, 1);

            tabla.AccessibleName = "Registros del módulo";
            ConfigurarTabla(tabla);
            tabla.SortCompare += CompararFechas;
            var contenido = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            contenido.Controls.Add(tabla);
            contenido.Controls.Add(CrearContenidoDashboard());
            panel.Controls.Add(contenido, 0, 2);

            estado.AutoSize = true;
            estado.AccessibleDescription = "Estado de la consulta";
            estado.ForeColor = TextoSecundario;
            TemaAplicacion.DefinirRol(estado, RolTema.TextoSecundario);
            estado.Font = new Font(Font.FontFamily, 9.5F);
            estado.Margin = new Padding(0, 10, 0, 0);
            panel.Controls.Add(estado, 0, 3);
            panel.SizeChanged += (origen, evento) =>
                estado.MaximumSize = new Size(Math.Max(1, panel.ClientSize.Width - panel.Padding.Horizontal), 0);
            return panel;
        }

        private void AjustarHerramientas()
        {
            if (ajustandoHerramientas || herramientas.Width <= 0) return;
            ajustandoHerramientas = true;
            try
            {
                float escala = DeviceDpi / 96F;
                int separacion = (int)(10 * escala);
                int ancho = herramientas.ClientSize.Width;
                int anchoAcciones = acciones.Controls.Cast<Control>().Where(control => control.Visible)
                    .Sum(control => control.Width + control.Margin.Horizontal);
                int alto = actualizar.Height;

                if (filtrosDashboard.Visible)
                {
                    filtrosDashboard.Location = Point.Empty;
                    bool caben = ancho >= filtrosDashboard.Width + separacion + anchoAcciones;
                    int y = caben ? filtrosDashboard.Height - alto : filtrosDashboard.Height + separacion;
                    acciones.SetBounds(caben ? ancho - anchoAcciones : 0, y, anchoAcciones, alto);
                    filaHerramientas.Height = y + alto + separacion;
                    return;
                }

                int anchoBotonesBusqueda = buscar.Width + buscar.Margin.Horizontal + limpiarBusqueda.Width + limpiarBusqueda.Margin.Horizontal;
                int anchoMinimoCampo = (int)(170 * escala);
                int disponibleEnFila = ancho - anchoAcciones - separacion - anchoBotonesBusqueda - campoBusqueda.Margin.Horizontal;
                int disponibleCampo = disponibleEnFila >= anchoMinimoCampo ? disponibleEnFila
                    : ancho - anchoBotonesBusqueda - campoBusqueda.Margin.Horizontal;
                campoBusqueda.Width = Math.Max(1, Math.Min((int)(266 * escala), disponibleCampo));
                int anchoBusqueda = campoBusqueda.Width + campoBusqueda.Margin.Horizontal + anchoBotonesBusqueda;
                bool mismaFila = busqueda.Visible && anchoAcciones + anchoBusqueda + separacion <= ancho;
                int anchoBarra = mismaFila ? anchoAcciones : ancho;
                int altoAcciones = acciones.GetPreferredSize(new Size(Math.Max(1, anchoBarra), 0)).Height;
                acciones.SetBounds(0, 0, anchoBarra, altoAcciones);
                int final = altoAcciones;
                if (busqueda.Visible)
                {
                    int y = mismaFila ? 0 : altoAcciones + separacion;
                    busqueda.SetBounds(mismaFila ? ancho - anchoBusqueda : 0, y, anchoBusqueda, alto);
                    final = Math.Max(final, y + alto);
                }
                if (filtros.Visible)
                {
                    filtros.Location = new Point(0, final + separacion);
                    final += separacion + filtros.Height;
                    int xFiltro = filtros.Width + separacion;
                    int yFiltro = filtros.Top + filtros.Height - filtrarAsistencia.Height;
                    if (xFiltro + filtrarAsistencia.Width > ancho)
                    {
                        xFiltro = 0;
                        yFiltro = final + separacion;
                        final = yFiltro + filtrarAsistencia.Height;
                    }
                    filtrarAsistencia.Location = new Point(xFiltro, yFiltro);
                }
                filaHerramientas.Height = final + separacion;
            }
            finally
            {
                ajustandoHerramientas = false;
            }
        }

        private static void ConfigurarTabla(DataGridView tabla)
        {
            tabla.Dock = DockStyle.Fill;
            tabla.BackgroundColor = FondoTabla;
            tabla.BorderStyle = BorderStyle.None;
            tabla.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            tabla.GridColor = TemaAplicacion.Actual.LineaTabla;
            tabla.ReadOnly = true;
            tabla.AllowUserToAddRows = false;
            tabla.AllowUserToDeleteRows = false;
            tabla.AllowUserToResizeRows = false;
            tabla.RowHeadersVisible = false;
            tabla.MultiSelect = false;
            tabla.AllowUserToResizeColumns = true;
            tabla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tabla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tabla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tabla.EnableHeadersVisualStyles = false;
            tabla.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            tabla.ColumnHeadersDefaultCellStyle.BackColor = EncabezadoTabla;
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = TemaAplicacion.Actual.Texto;
            tabla.ColumnHeadersDefaultCellStyle.SelectionBackColor = EncabezadoTabla;
            tabla.ColumnHeadersDefaultCellStyle.SelectionForeColor = TemaAplicacion.Actual.Texto;
            tabla.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 6, 10, 6);
            tabla.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            tabla.DefaultCellStyle.BackColor = FondoTabla;
            tabla.DefaultCellStyle.ForeColor = TemaAplicacion.Actual.TextoTabla;
            tabla.DefaultCellStyle.Padding = new Padding(8, 0, 5, 0);
            tabla.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            tabla.DefaultCellStyle.SelectionBackColor = SeleccionTabla;
            tabla.DefaultCellStyle.SelectionForeColor = TemaAplicacion.Actual.TextoSeleccionTabla;
            tabla.AlternatingRowsDefaultCellStyle.BackColor = TemaAplicacion.Actual.AlternanciaTabla;
            tabla.RowTemplate.Height = 35;
        }

        private static void AgregarFecha(FlowLayoutPanel filtros, string nombre, DateTimePicker selector, DateTime fecha)
        {
            var campo = new Panel { Size = new Size(128, 49), Margin = new Padding(0, 0, 8, 0) };
            var etiqueta = new Label { Text = nombre, AutoSize = true, ForeColor = TextoSecundario, Font = new Font("Segoe UI", 9F) };
            TemaAplicacion.DefinirRol(etiqueta, RolTema.TextoSecundario);
            campo.Controls.Add(etiqueta);
            selector.AccessibleName = nombre;
            selector.Location = new Point(0, 20);
            selector.Width = 128;
            selector.Format = DateTimePickerFormat.Short;
            selector.MaxDate = new DateTime(9998, 12, 30);
            selector.Value = fecha;
            campo.Controls.Add(selector);
            filtros.Controls.Add(campo);
        }

        private static void CompararFechas(object origen, DataGridViewSortCompareEventArgs evento)
        {
            switch (evento.Column.Name)
            {
                case "Fecha":
                case "Desde":
                case "Hasta":
                case "Fecha de registro":
                    string formato = evento.Column.DefaultCellStyle.Format;
                    bool tienePrimera = DateTime.TryParseExact(evento.CellValue1 as string, formato,
                        CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime primera);
                    bool tieneSegunda = DateTime.TryParseExact(evento.CellValue2 as string, formato,
                        CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime segunda);
                    evento.SortResult = tienePrimera && tieneSegunda ? primera.CompareTo(segunda) : tienePrimera.CompareTo(tieneSegunda);
                    evento.Handled = true;
                    break;
            }
        }

        private void AgregarVista<T>(string nombre, string[] columnas, Func<Task<List<T>>> listar,
            Func<T, object[]> valores, Func<Task<bool>> crear = null, bool usaFechas = false,
            Func<T, Task<bool>> editar = null, Func<T, Task> eliminar = null, string textoAgregar = "Agregar")
        {
            modulos.Items.Add(new Vista
            {
                Nombre = nombre,
                Columnas = columnas,
                UsaFechas = usaFechas,
                TextoAgregar = textoAgregar,
                AgregarAsync = crear,
                EditarAsync = editar == null ? (Func<object, Task<bool>>)null : registro => editar((T)registro),
                EliminarAsync = eliminar == null ? (Func<object, Task>)null : registro => eliminar((T)registro),
                CargarAsync = async () => (await listar()).Select(registro => new FilaVista
                {
                    Registro = registro, Valores = valores(registro)
                }).ToArray()
            });
        }

        private void CambiarCarga(bool enCurso)
        {
            cargando = enCurso;
            if (IsDisposed || Disposing) return;
            modulos.Enabled = actualizar.Enabled = filtros.Enabled = agregar.Enabled = filtrarAsistencia.Enabled = asistenciaFacial.Enabled = !enCurso;
            correoVisitantes.Enabled = !enCurso;
            filtrosDashboard.Enabled = contenidoDashboard.Enabled = !enCurso;
            busqueda.Enabled = !enCurso;
            ActualizarAccionesSeleccion();
        }

        private void ActualizarAccionesSeleccion()
        {
            if (IsDisposed || Disposing) return;
            var vista = VistaActual;
            bool seleccionado = !cargando && tabla.SelectedRows.Count == 1;
            editar.Enabled = seleccionado && vista?.EditarAsync != null;
            eliminar.Enabled = seleccionado && vista?.EliminarAsync != null;
            bool esVisitantes = vista?.Nombre == ModuloVisitantes;
            reenviarQr.Enabled = esVisitantes && seleccionado && tabla.SelectedRows[0].Tag is VisitanteLocal;
        }

        private async Task AgregarRegistroAsync()
        {
            var vista = VistaActual;
            if (cargando || vista?.AgregarAsync == null) return;
            string mensaje = vista.Nombre == ModuloVisitantes ? null : "Registro guardado.";
            await EjecutarCambioAsync(vista.AgregarAsync, mensaje);
        }

        private async Task EditarSeleccionadoAsync()
        {
            var vista = VistaActual;
            if (cargando || vista?.EditarAsync == null || tabla.SelectedRows.Count != 1) return;
            object registro = tabla.SelectedRows[0].Tag;
            await EjecutarCambioAsync(() => vista.EditarAsync(registro), "Registro actualizado.", registro);
        }

        private async Task EjecutarCambioAsync(Func<Task<bool>> guardar, string mensaje, object registro = null)
        {
            var estadoAnterior = estado.Text;
            bool guardado;
            CambiarCarga(true);
            estado.Text = "Preparando el formulario…";
            try
            {
                guardado = await guardar();
            }
            catch (Exception error)
            {
                if (!IsDisposed && !Disposing)
                {
                    InformarErrorOperacion(error);
                }
                return;
            }
            finally
            {
                CambiarCarga(false);
            }

            if (IsDisposed || Disposing) return;
            if (guardado && AfectaSesion(registro)) CerrarSesionModificada();
            else if (guardado) await CargarVistaAsync(mensaje ?? estado.Text);
            else estado.Text = estadoAnterior;
        }

        private async Task EliminarSeleccionadoAsync()
        {
            var vista = VistaActual;
            if (cargando || vista?.EliminarAsync == null || tabla.SelectedRows.Count != 1) return;
            var fila = tabla.SelectedRows[0];
            object registro = fila.Tag;
            string descripcion = string.Join(" · ", fila.Cells.Cast<DataGridViewCell>()
                .Select(celda => Convert.ToString(celda.Value)).Where(valor => !string.IsNullOrWhiteSpace(valor)).Take(3));
            if (MessageBox.Show(this, "¿Eliminar este registro de «" + vista.Nombre + "»?\n\n" + descripcion,
                "Eliminar registro", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            bool eliminado = false;
            CambiarCarga(true);
            estado.Text = "Eliminando registro…";
            try
            {
                await vista.EliminarAsync(registro);
                eliminado = true;
            }
            catch (Exception error)
            {
                if (!IsDisposed && !Disposing) InformarErrorOperacion(error);
            }
            finally
            {
                CambiarCarga(false);
            }

            if (IsDisposed || Disposing || !eliminado) return;
            if (AfectaSesion(registro)) CerrarSesionModificada();
            else await CargarVistaAsync("Registro eliminado.");
        }

        private void InformarErrorOperacion(Exception error)
        {
            if (error is UnauthorizedAccessException)
            {
                MessageBox.Show(this, error.Message, "Acceso administrativo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                return;
            }
            estado.Text = error is ArgumentException || error is InvalidOperationException
                ? error.Message : "No se pudo completar la operación. Revisa la conexión y vuelve a intentarlo.";
        }

        private bool AfectaSesion(object registro)
        {
            return registro is CuentaAdministrada usuario && usuario.IdUsuario == sesion.IdUsuario
                || registro is Empleado empleado && empleado.IdEmpleado == sesion.IdEmpleado
                || registro is Rol rol && rol.IdRol == sesion.IdRol;
        }

        private void CerrarSesionModificada()
        {
            MessageBox.Show(this, "Tus datos de acceso han cambiado. Inicia sesión de nuevo para continuar.",
                "Sesión actualizada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private async Task CargarVistaAsync(string resultadoCarga = null)
        {
            var vista = VistaActual;
            if (cargando || vista == null) return;

            titulo.Text = vista.Nombre;
            filtros.Visible = vista.UsaFechas;
            filtrosDashboard.Visible = vista.EsDashboard;
            contenidoDashboard.Visible = vista.EsDashboard;
            tabla.Visible = !vista.EsDashboard;
            filtrarAsistencia.Visible = vista.UsaFechas;
            asistenciaFacial.Visible = reconocimientoHabilitado && vista.Nombre == ModuloMarcaciones;
            asistenciaFacial.Width = (int)(154 * DeviceDpi / 96F);
            correoVisitantes.Visible = reenviarQr.Visible = vista.Nombre == ModuloVisitantes;
            int totalFiltros = (idEmpleadoFiltrado.HasValue ? 1 : 0) + (idSedeFiltrada.HasValue ? 1 : 0)
                + (idUnidadFiltrada.HasValue ? 1 : 0);
            filtrarAsistencia.Text = totalFiltros == 0 ? "Filtrar asistencia" : $"Filtros activos ({totalFiltros})";
            agregar.Visible = vista.AgregarAsync != null;
            editar.Visible = vista.EditarAsync != null;
            eliminar.Visible = vista.EliminarAsync != null;
            busqueda.Visible = !vista.EsDashboard;
            agregar.Text = vista.TextoAgregar;
            agregar.Width = Math.Max((int)(96 * DeviceDpi / 96F),
                TextRenderer.MeasureText(agregar.Text, agregar.Font).Width + (int)(28 * DeviceDpi / 96F));
            AjustarHerramientas();
            if (vista.EsDashboard)
            {
                await CargarDashboardAsync();
                return;
            }

            textoBuscado = buscarTexto.Text.Trim();
            tabla.Columns.Clear();
            foreach (string columna in vista.Columnas)
            {
                int indice = tabla.Columns.Add(columna, columna);
                var columnaTabla = tabla.Columns[indice];
                int anchoEncabezado = TextRenderer.MeasureText(columna, tabla.ColumnHeadersDefaultCellStyle.Font).Width
                    + tabla.ColumnHeadersDefaultCellStyle.Padding.Horizontal + (int)(18 * DeviceDpi / 96F);
                columnaTabla.MinimumWidth = Math.Max((int)(64 * DeviceDpi / 96F), anchoEncabezado);
                if (columna == "ID")
                {
                    columnaTabla.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                    columnaTabla.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (columna == "Fecha" || columna == "Desde" || columna == "Hasta" || columna == "Fecha de registro")
                {
                    bool incluyeHora = columna == "Fecha de registro" && vista.Nombre == ModuloVisitantes;
                    columnaTabla.DefaultCellStyle.Format = incluyeHora ? "dd/MM/yyyy HH:mm" : "dd/MM/yyyy";
                    columnaTabla.MinimumWidth = Math.Max(columnaTabla.MinimumWidth,
                        TextRenderer.MeasureText(incluyeHora ? "00/00/0000 00:00" : "00/00/0000", tabla.Font).Width
                            + tabla.DefaultCellStyle.Padding.Horizontal);
                }
                else if (columna == "Hora")
                    columnaTabla.MinimumWidth = Math.Max(columnaTabla.MinimumWidth,
                        TextRenderer.MeasureText("00:00:00", tabla.Font).Width + tabla.DefaultCellStyle.Padding.Horizontal);
                else if (columna == "Personal" || columna == "Empleado" || columna == "Nombre" || columna == "Terminal"
                    || columna == "Motivo" || columna == "Descripción")
                    columnaTabla.FillWeight = 160;
            }
            if (vista.UsaFechas && desde.Value.Date > hasta.Value.Date)
            {
                const string aviso = "La fecha inicial debe ser anterior o igual a la fecha final.";
                estado.Text = resultadoCarga == null ? aviso : resultadoCarga + " " + aviso;
                desde.Focus();
                return;
            }

            CambiarCarga(true);
            estado.Text = "Consultando registros…";
            try
            {
                var filas = await vista.CargarAsync();
                if (IsDisposed || Disposing) return;
                if (!vista.UsaFechas && textoBuscado.Length > 0)
                    filas = filas.Where(fila => fila.Valores.Any(valor => valor != null &&
                        valor.ToString().IndexOf(textoBuscado, StringComparison.CurrentCultureIgnoreCase) >= 0)).ToArray();
                foreach (var fila in filas)
                {
                    int indice = tabla.Rows.Add(fila.Valores);
                    tabla.Rows[indice].Tag = fila.Registro;
                }
                tabla.ClearSelection();
                tabla.CurrentCell = null;
                string resumen = filas.Length == 0 ? "No hay registros para mostrar."
                    : filas.Length == 1 ? "1 registro" : $"{filas.Length:N0} registros";
                if (textoBuscado.Length > 0) resumen += " Búsqueda «" + textoBuscado + "».";
                if (vista.UsaFechas && filas.Length == 500)
                    resumen = "Se muestran hasta 500 marcaciones coincidentes. Reduce el rango para consultar otras.";
                if (vista.UsaFechas)
                    resumen += " Usa «Marcación manual» para registrar una entrada o salida que el personal no pudo marcar.";
                estado.Text = resultadoCarga == null ? resumen : resultadoCarga + " " + resumen;
            }
            catch (Exception)
            {
                if (!IsDisposed && !Disposing)
                    estado.Text = resultadoCarga == null
                        ? "No se pudieron cargar los registros. Revisa la conexión e inténtalo de nuevo."
                        : resultadoCarga + " Pulsa Actualizar vista para ver los registros.";
            }
            finally
            {
                CambiarCarga(false);
            }
        }

        private static string NombreEmpleado(Empleado empleado)
        {
            return empleado == null ? string.Empty : empleado.Nombres + " " + empleado.Apellidos;
        }

        private sealed class Vista
        {
            public string Nombre { get; set; }
            internal string[] Columnas { get; set; }
            internal bool UsaFechas { get; set; }
            internal bool EsDashboard { get; set; }
            internal string TextoAgregar { get; set; } = "Agregar";
            internal Func<Task<bool>> AgregarAsync { get; set; }
            internal Func<object, Task<bool>> EditarAsync { get; set; }
            internal Func<object, Task> EliminarAsync { get; set; }
            internal Func<Task<FilaVista[]>> CargarAsync { get; set; }
        }

        private sealed class FilaVista
        {
            internal object Registro { get; set; }
            internal object[] Valores { get; set; }
        }
    }
}
