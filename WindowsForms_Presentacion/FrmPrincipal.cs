using System;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Entidades;
using Logica;

namespace WindowsForms_Presentacion
{
    public sealed class FrmPrincipal : Form
    {
        private static readonly Font FuenteEstadoNovedad = new Font("Segoe UI", 9F, FontStyle.Bold);
        private readonly SesionUsuario sesion;
        private readonly IConsultaAsistencia consulta;
        private readonly ServicioMarcacionFacial marcacionFacial;
        private readonly ServicioMarcacionConClave marcacionConClave;
        private readonly IConsultaTurnos consultaTurnos;
        private readonly int idTerminal;
        private readonly DateTimePicker desde = new DateTimePicker();
        private readonly DateTimePicker hasta = new DateTimePicker();
        private readonly DataGridView registros = new DataGridView();
        private readonly Label estado = new Label();
        private readonly AccesoButton botonAsistencia = new AccesoButton { Text = "Mi asistencia", Principal = true, Plano = true, Dock = DockStyle.Fill };
        private readonly AccesoButton botonHorarios = new AccesoButton { Text = "Mis horarios", Plano = true, Dock = DockStyle.Fill };
        private readonly AccesoButton botonNovedades = new AccesoButton { Text = "Mis reportes", AccessibleName = "Quejas y novedades", Plano = true, Dock = DockStyle.Fill };
        private readonly DataGridView tablaHorarios = new DataGridView();
        private readonly Label estadoHorarios = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        private readonly AccesoButton actualizarHorarios = new AccesoButton { Text = "Actualizar", Plano = true, Height = 40 };
        private readonly ComboBox tipoNovedad = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        private readonly DateTimePicker fechaNovedad = new DateTimePicker { Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
        private readonly DateTimePicker fechaFinNovedad = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Dock = DockStyle.Fill };
        private readonly TextBox motivoNovedad = new TextBox { Multiline = true, MaxLength = 200, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
        private readonly DataGridView tablaNovedades = new DataGridView();
        private readonly Label estadoNovedades = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        private readonly Label contadorNovedad = new Label { Text = "0 / 200", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
        private readonly AccesoButton enviarNovedad = new AccesoButton { Text = "Enviar al administrador", Principal = true, Plano = true, Height = 40 };
        private readonly AccesoButton actualizarNovedades = new AccesoButton { Text = "Actualizar", Plano = true, Height = 40 };
        private Control paginaAsistencia;
        private Control paginaHorarios;
        private Control paginaNovedades;
        private readonly AccesoButton buscar = new AccesoButton();
        private readonly Label estadoMarcacion = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label estadoTurno = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        private readonly CheckBox usarCamara = new CheckBox { Text = "Verificar con el rostro", AutoSize = true };
        private readonly AccesoButton entrada = new AccesoButton { Text = "Marcar entrada", Principal = true, Plano = true };
        private readonly AccesoButton salida = new AccesoButton { Text = "Marcar salida", Plano = true };
        private bool cargando;
        private bool cargandoTurno;
        private bool marcando;

        public FrmPrincipal(SesionUsuario sesion, IConsultaAsistencia consulta)
            : this(sesion, consulta, null, null)
        {
        }

        internal FrmPrincipal(SesionUsuario sesion, IConsultaAsistencia consulta, ServicioMarcacionFacial marcacion,
            ServicioMarcacionConClave marcacionConClave = null)
        {
            this.sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
            this.consulta = consulta ?? throw new ArgumentNullException(nameof(consulta));
            marcacionFacial = marcacion;
            this.marcacionConClave = marcacionConClave;
            consultaTurnos = consulta as IConsultaTurnos;
            string terminal = ConfigurationManager.AppSettings["IdTerminalAsistencia"];
            if (string.IsNullOrWhiteSpace(terminal)) terminal = ConfigurationManager.AppSettings["IdDispositivoFacial"];
            int.TryParse(terminal, out idTerminal);
            Text = "AssistPRO Hospital | Mi asistencia";
            StartPosition = FormStartPosition.CenterScreen;
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            ClientSize = new Size(Math.Min(1120, area.Width - 64), Math.Min(740, area.Height - 64));
            MinimumSize = new Size(Math.Min(760, area.Width), Math.Min(580, area.Height));
            WindowState = FormWindowState.Maximized;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 11);
            fechaNovedad.Value = PlanificacionTurnos.AhoraColombia.Date;
            fechaFinNovedad.Value = PlanificacionTurnos.AhoraColombia.Date;
            CrearInterfaz();
            Shown += async (sender, args) => await CargarAsync();
            FormClosing += (sender, args) =>
            {
                if (marcando && args.CloseReason == CloseReason.UserClosing) args.Cancel = true;
            };
            TemaAplicacion.Registrar(this);
        }

        private void CrearInterfaz()
        {
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var lateral = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8,
                Padding = new Padding(16, 24, 16, 16), Margin = Padding.Empty
            };
            TemaAplicacion.DefinirRol(lateral, RolTema.Lateral);
            lateral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            lateral.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            lateral.Controls.Add(TemaAplicacion.CrearMarca(), 0, 0);
            lateral.Controls.Add(botonAsistencia, 0, 1);
            lateral.Controls.Add(botonHorarios, 0, 2);
            lateral.Controls.Add(botonNovedades, 0, 3);
            var identidad = Etiqueta(sesion.Nombre + Environment.NewLine + sesion.Rol, 10);
            identidad.AutoEllipsis = true;
            identidad.UseMnemonic = false;
            lateral.Controls.Add(identidad, 0, 5);
            var tema = TemaAplicacion.CrearSelectorTema();
            lateral.Controls.Add(tema, 0, 6);
            var salir = new AccesoButton { Text = "Cerrar sesión", Dock = DockStyle.Fill, Plano = true };
            salir.Click += (sender, args) => Close();
            lateral.Controls.Add(salir, 0, 7);

            var contenido = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(24), Margin = Padding.Empty
            };
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, marcacionFacial == null && marcacionConClave == null ? 0 : 108));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, consultaTurnos == null ? 0 : 66));
            contenido.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            contenido.Controls.Add(Etiqueta("Mi asistencia", 26, FontStyle.Bold), 0, 0);
            if (marcacionFacial != null || marcacionConClave != null) contenido.Controls.Add(CrearAccionesMarcacion(), 0, 1);
            if (consultaTurnos != null)
            {
                estadoTurno.Text = "Consultando tu turno…";
                estadoTurno.AccessibleName = "Turno asignado";
                estadoTurno.Padding = new Padding(12, 0, 12, 0);
                TemaAplicacion.DefinirRol(estadoTurno, RolTema.Superficie);
                contenido.Controls.Add(estadoTurno, 0, 2);
            }

            var filtros = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, WrapContents = true, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 8, 12, 0), Margin = new Padding(0, 8, 0, 0)
            };
            TemaAplicacion.DefinirRol(filtros, RolTema.Superficie);
            DateTime hoy = PlanificacionTurnos.AhoraColombia.Date;
            desde.Value = hoy.AddDays(-30);
            hasta.Value = hoy;
            filtros.Controls.Add(Filtro("Desde", desde));
            filtros.Controls.Add(Filtro("Hasta", hasta));
            buscar.Text = "Consultar";
            buscar.Plano = true;
            buscar.Size = new Size(102, 34);
            buscar.Margin = new Padding(0, 25, 0, 0);
            buscar.Click += async (sender, args) => await CargarAsync();
            filtros.Controls.Add(buscar);
            contenido.Controls.Add(filtros, 0, 3);

            registros.Dock = DockStyle.Fill;
            registros.Margin = Padding.Empty;
            registros.ReadOnly = true;
            registros.AllowUserToAddRows = false;
            registros.AllowUserToDeleteRows = false;
            registros.AllowUserToResizeRows = false;
            registros.AutoGenerateColumns = false;
            registros.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            registros.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            registros.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            registros.RowHeadersVisible = false;
            registros.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            registros.MultiSelect = false;
            registros.BorderStyle = BorderStyle.None;
            registros.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            registros.EnableHeadersVisualStyles = false;
            registros.ColumnHeadersDefaultCellStyle.Padding = new Padding(8);
            registros.DefaultCellStyle.Padding = new Padding(8);
            registros.RowTemplate.Height = 42;
            registros.AccessibleName = "Marcaciones del empleado";
            Columna("Fecha", "Fecha", "dd/MM/yyyy");
            Columna("Hora", "Hora", "HH:mm:ss");
            Columna("Marcación", "TipoMarcacion", "");
            Columna("Verificación", "MetodoVerificacion", "");
            registros.CellFormatting += (sender, args) =>
            {
                if (registros.Columns[args.ColumnIndex].DataPropertyName == nameof(RegistroAsistencia.MetodoVerificacion) &&
                    string.Equals(args.Value as string, "Clave", StringComparison.OrdinalIgnoreCase))
                {
                    args.Value = "Contraseña";
                    args.FormattingApplied = true;
                }
            };
            contenido.Controls.Add(registros, 0, 4);
            estado.Dock = DockStyle.Fill;
            estado.TextAlign = ContentAlignment.MiddleLeft;
            TemaAplicacion.DefinirRol(estado, RolTema.TextoSecundario);
            estado.AccessibleDescription = "Estado de la consulta";
            estado.AutoEllipsis = true;
            contenido.Controls.Add(estado, 0, 5);
            paginaAsistencia = contenido;
            paginaAsistencia.Dock = DockStyle.Fill;
            paginaHorarios = CrearPaginaHorarios();
            paginaHorarios.Dock = DockStyle.Fill;
            paginaNovedades = CrearPaginaNovedades();
            paginaNovedades.Dock = DockStyle.Fill;
            var paginas = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            paginas.Controls.Add(paginaNovedades);
            paginas.Controls.Add(paginaHorarios);
            paginas.Controls.Add(paginaAsistencia);
            paginaAsistencia.BringToFront();
            botonAsistencia.Click += (sender, args) => MostrarPaginaAsistencia();
            botonHorarios.Click += async (sender, args) =>
            {
                MostrarPaginaHorarios();
                await CargarHorariosAsync();
            };
            botonNovedades.Click += async (sender, args) =>
            {
                MostrarPaginaNovedades();
                await CargarNovedadesAsync();
            };
            enviarNovedad.Click += async (sender, args) => await EnviarNovedadAsync();
            actualizarNovedades.Click += async (sender, args) => await CargarNovedadesAsync();
            actualizarHorarios.Click += async (sender, args) => await CargarHorariosAsync();
            motivoNovedad.TextChanged += (sender, args) =>
            {
                contadorNovedad.Text = motivoNovedad.TextLength + " / 200";
                contadorNovedad.ForeColor = motivoNovedad.TextLength >= 190
                    ? TemaAplicacion.Actual.AcentoNaranja : TemaAplicacion.Actual.TextoSecundario;
            };
            raiz.Controls.Add(lateral, 0, 0);
            raiz.Controls.Add(paginas, 1, 0);
            Controls.Add(raiz);
        }

        private Control CrearPaginaHorarios()
        {
            var contenido = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                Padding = new Padding(24), Margin = Padding.Empty
            };
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            TemaAplicacion.DefinirRol(contenido, RolTema.Superficie);

            contenido.Controls.Add(Etiqueta("Mis horarios", 25, FontStyle.Bold), 0, 0);
            var descripcion = Etiqueta("Consulta los turnos que tienes asignados para los próximos 30 días.", 10);
            TemaAplicacion.DefinirRol(descripcion, RolTema.TextoSecundario);
            contenido.Controls.Add(descripcion, 0, 1);

            tablaHorarios.Dock = DockStyle.Fill;
            tablaHorarios.ReadOnly = true;
            tablaHorarios.AllowUserToAddRows = false;
            tablaHorarios.AllowUserToDeleteRows = false;
            tablaHorarios.AllowUserToResizeRows = false;
            tablaHorarios.AutoGenerateColumns = false;
            tablaHorarios.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaHorarios.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tablaHorarios.RowHeadersVisible = false;
            tablaHorarios.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaHorarios.MultiSelect = false;
            tablaHorarios.BorderStyle = BorderStyle.None;
            tablaHorarios.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            tablaHorarios.EnableHeadersVisualStyles = false;
            tablaHorarios.ColumnHeadersDefaultCellStyle.Padding = new Padding(8);
            tablaHorarios.DefaultCellStyle.Padding = new Padding(8);
            tablaHorarios.RowTemplate.Height = 40;
            tablaHorarios.AccessibleName = "Turnos asignados para el empleado";
            AgregarColumnaHorario("Día", nameof(TurnoProgramado.Inicio), "dddd, dd/MM/yyyy");
            AgregarColumnaHorario("Turno", nameof(TurnoProgramado.Nombre), "");
            AgregarColumnaHorario("Entrada", nameof(TurnoProgramado.Inicio), "HH:mm");
            AgregarColumnaHorario("Salida", nameof(TurnoProgramado.Fin), "HH:mm");
            tablaHorarios.Columns["Día"].FillWeight = 175;
            tablaHorarios.Columns["Turno"].FillWeight = 130;
            contenido.Controls.Add(tablaHorarios, 0, 2);

            var pie = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
            TemaAplicacion.DefinirRol(estadoHorarios, RolTema.TextoSecundario);
            pie.Controls.Add(estadoHorarios, 0, 0);
            actualizarHorarios.Dock = DockStyle.Fill;
            pie.Controls.Add(actualizarHorarios, 1, 0);
            contenido.Controls.Add(pie, 0, 3);
            return contenido;
        }

        private void AgregarColumnaHorario(string titulo, string propiedad, string formato)
        {
            tablaHorarios.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = titulo,
                DataPropertyName = propiedad,
                Name = titulo,
                DefaultCellStyle = new DataGridViewCellStyle { Format = formato }
            });
        }

        private Control CrearPaginaNovedades()
        {
            var contenido = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                Padding = new Padding(24), Margin = Padding.Empty
            };
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 214));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            TemaAplicacion.DefinirRol(contenido, RolTema.Superficie);

            contenido.Controls.Add(Etiqueta("Quejas y novedades", 25, FontStyle.Bold), 0, 0);
            var descripcion = Etiqueta("Envía una solicitud laboral al administrador y consulta su estado. No incluyas datos clínicos de pacientes.", 10);
            TemaAplicacion.DefinirRol(descripcion, RolTema.TextoSecundario);
            contenido.Controls.Add(descripcion, 0, 1);

            var formulario = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 4, Padding = new Padding(12) };
            formulario.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            formulario.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
            formulario.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
            formulario.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            formulario.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            formulario.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            formulario.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            TemaAplicacion.DefinirRol(formulario, RolTema.Superficie);
            AgregarCampoNovedad(formulario, "Tipo de reporte", tipoNovedad, 0);
            AgregarCampoNovedad(formulario, "Desde", fechaNovedad, 1);
            AgregarCampoNovedad(formulario, "Hasta (opcional)", fechaFinNovedad, 2);
            var encabezadoMotivo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            encabezadoMotivo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            encabezadoMotivo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
            var etiquetaMotivo = Etiqueta("Describe la queja o novedad", 9);
            TemaAplicacion.DefinirRol(etiquetaMotivo, RolTema.TextoSecundario);
            TemaAplicacion.DefinirRol(contadorNovedad, RolTema.TextoSecundario);
            encabezadoMotivo.Controls.Add(etiquetaMotivo, 0, 0);
            encabezadoMotivo.Controls.Add(contadorNovedad, 1, 0);
            formulario.Controls.Add(encabezadoMotivo, 0, 2);
            formulario.SetColumnSpan(encabezadoMotivo, 3);
            motivoNovedad.AccessibleName = "Descripción de la queja o novedad";
            motivoNovedad.MaxLength = 200;
            motivoNovedad.Margin = new Padding(0, 2, 8, 0);
            TemaAplicacion.DefinirRol(motivoNovedad, RolTema.Entrada);
            formulario.Controls.Add(motivoNovedad, 0, 3);
            formulario.SetColumnSpan(motivoNovedad, 3);
            contenido.Controls.Add(formulario, 0, 2);

            tablaNovedades.Dock = DockStyle.Fill;
            tablaNovedades.ReadOnly = true;
            tablaNovedades.AllowUserToAddRows = false;
            tablaNovedades.AllowUserToDeleteRows = false;
            tablaNovedades.AllowUserToResizeRows = false;
            tablaNovedades.AutoGenerateColumns = false;
            tablaNovedades.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaNovedades.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tablaNovedades.RowHeadersVisible = false;
            tablaNovedades.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaNovedades.MultiSelect = false;
            tablaNovedades.BorderStyle = BorderStyle.None;
            tablaNovedades.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            tablaNovedades.EnableHeadersVisualStyles = false;
            tablaNovedades.ColumnHeadersDefaultCellStyle.Padding = new Padding(8);
            tablaNovedades.DefaultCellStyle.Padding = new Padding(8);
            tablaNovedades.RowTemplate.Height = 38;
            tablaNovedades.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            tablaNovedades.AccessibleName = "Mis quejas y novedades";
            AgregarColumnaNovedad("Tipo", "", "");
            AgregarColumnaNovedad("Desde", nameof(Novedad.FechaInicio), "dd/MM/yyyy");
            AgregarColumnaNovedad("Hasta", nameof(Novedad.FechaFin), "dd/MM/yyyy");
            AgregarColumnaNovedad("Estado", nameof(Novedad.EstadoAprobacion), "");
            AgregarColumnaNovedad("Descripción", nameof(Novedad.Motivo), "");
            tablaNovedades.Columns["Tipo"].FillWeight = 90;
            tablaNovedades.Columns["Desde"].FillWeight = 70;
            tablaNovedades.Columns["Hasta"].FillWeight = 70;
            tablaNovedades.Columns["Estado"].FillWeight = 80;
            tablaNovedades.Columns["Descripción"].FillWeight = 190;
            tablaNovedades.Columns["Descripción"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            tablaNovedades.CellFormatting += (sender, args) =>
            {
                if (args.RowIndex < 0 || args.ColumnIndex < 0 || tablaNovedades.Columns[args.ColumnIndex].Name != "Estado") return;
                string valor = Convert.ToString(args.Value)?.Trim().ToUpperInvariant();
                if (valor == "APROBADO") args.CellStyle.ForeColor = TemaAplicacion.Actual.AcentoVerde;
                else if (valor == "RECHAZADO") args.CellStyle.ForeColor = TemaAplicacion.Actual.Error;
                else if (valor == "PENDIENTE") args.CellStyle.ForeColor = TemaAplicacion.Actual.AcentoNaranja;
                args.CellStyle.Font = FuenteEstadoNovedad;
            };
            contenido.Controls.Add(tablaNovedades, 0, 3);

            var pie = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            TemaAplicacion.DefinirRol(estadoNovedades, RolTema.TextoSecundario);
            pie.Controls.Add(estadoNovedades, 0, 0);
            actualizarNovedades.Dock = enviarNovedad.Dock = DockStyle.Fill;
            pie.Controls.Add(actualizarNovedades, 1, 0);
            pie.Controls.Add(enviarNovedad, 2, 0);
            contenido.Controls.Add(pie, 0, 4);
            return contenido;
        }

        private void AgregarCampoNovedad(TableLayoutPanel layout, string titulo, Control campo, int columna)
        {
            var etiqueta = Etiqueta(titulo, 9);
            TemaAplicacion.DefinirRol(etiqueta, RolTema.TextoSecundario);
            layout.Controls.Add(etiqueta, columna, 0);
            campo.Margin = new Padding(0, 2, 8, 0);
            campo.AccessibleName = titulo;
            TemaAplicacion.DefinirRol(campo, RolTema.Entrada);
            layout.Controls.Add(campo, columna, 1);
        }

        private void AgregarColumnaNovedad(string titulo, string propiedad, string formato)
        {
            tablaNovedades.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = titulo,
                DataPropertyName = propiedad,
                Name = titulo,
                DefaultCellStyle = new DataGridViewCellStyle { Format = formato }
            });
        }

        private void MostrarPaginaAsistencia()
        {
            paginaAsistencia.BringToFront();
            botonAsistencia.Principal = true;
            botonHorarios.Principal = false;
            botonNovedades.Principal = false;
            botonAsistencia.Invalidate();
            botonHorarios.Invalidate();
            botonNovedades.Invalidate();
        }

        private void MostrarPaginaHorarios()
        {
            paginaHorarios.BringToFront();
            botonAsistencia.Principal = false;
            botonHorarios.Principal = true;
            botonNovedades.Principal = false;
            botonAsistencia.Invalidate();
            botonHorarios.Invalidate();
            botonNovedades.Invalidate();
        }

        private void MostrarPaginaNovedades()
        {
            paginaNovedades.BringToFront();
            botonAsistencia.Principal = false;
            botonHorarios.Principal = false;
            botonNovedades.Principal = true;
            botonAsistencia.Invalidate();
            botonHorarios.Invalidate();
            botonNovedades.Invalidate();
        }

        private async Task CargarHorariosAsync()
        {
            if (consultaTurnos == null)
            {
                tablaHorarios.DataSource = null;
                estadoHorarios.Text = "La consulta de horarios no está disponible en esta versión.";
                return;
            }

            actualizarHorarios.Enabled = false;
            estadoHorarios.Text = "Consultando tus horarios...";
            tablaHorarios.DataSource = null;
            try
            {
                DateTime inicio = PlanificacionTurnos.AhoraColombia;
                DateTime fin = inicio.Date.AddDays(31);
                var turnos = await consultaTurnos.ConsultarTurnosAsync(sesion.IdEmpleado, inicio, fin);
                if (IsDisposed) return;
                tablaHorarios.DataSource = turnos;
                tablaHorarios.ClearSelection();
                estadoHorarios.Text = turnos.Count == 0
                    ? "No tienes turnos asignados en los próximos 30 días."
                    : turnos.Count + " turnos asignados en los próximos 30 días.";
            }
            catch (Exception)
            {
                if (!IsDisposed) estadoHorarios.Text = "No se pudieron consultar tus horarios. Comprueba la conexión e inténtalo de nuevo.";
            }
            finally
            {
                if (!IsDisposed) actualizarHorarios.Enabled = true;
            }
        }

        private async Task CargarNovedadesAsync()
        {
            actualizarNovedades.Enabled = enviarNovedad.Enabled = false;
            estadoNovedades.Text = "Cargando tus reportes...";
            try
            {
                int tipoAnterior = 0;
                if (tipoNovedad.SelectedValue != null)
                    int.TryParse(Convert.ToString(tipoNovedad.SelectedValue), out tipoAnterior);
                var tipos = await consulta.ConsultarTiposNovedadAsync();
                var novedades = await consulta.ConsultarNovedadesAsync(sesion.IdEmpleado);
                if (IsDisposed) return;
                tipoNovedad.DataSource = tipos;
                tipoNovedad.DisplayMember = nameof(TipoNovedad.NombreTipo);
                tipoNovedad.ValueMember = nameof(TipoNovedad.IdTipoNovedad);
                if (tipoAnterior > 0 && tipos.Any(tipo => tipo.IdTipoNovedad == tipoAnterior))
                    tipoNovedad.SelectedValue = tipoAnterior;
                tablaNovedades.DataSource = novedades;
                foreach (DataGridViewRow fila in tablaNovedades.Rows)
                    if (fila.DataBoundItem is Novedad novedad)
                        fila.Cells["Tipo"].Value = novedad.TipoNovedad?.NombreTipo ?? "Sin tipo";
                enviarNovedad.Enabled = tipos.Count > 0;
                estadoNovedades.Text = tipos.Count == 0
                    ? "No hay tipos configurados. Solicita al administrador que registre uno en Tipos de novedad."
                    : novedades.Count == 0 ? "Aún no has enviado reportes."
                    : "Total: " + novedades.Count + "  ·  Pendientes: " + novedades.Count(n => string.Equals(n.EstadoAprobacion, "Pendiente", StringComparison.OrdinalIgnoreCase))
                        + "  ·  Aprobados: " + novedades.Count(n => string.Equals(n.EstadoAprobacion, "Aprobado", StringComparison.OrdinalIgnoreCase))
                        + "  ·  Rechazados: " + novedades.Count(n => string.Equals(n.EstadoAprobacion, "Rechazado", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                if (!IsDisposed)
                    estadoNovedades.Text = "No se pudo cargar la información. Comprueba la conexión y pulsa Actualizar.";
            }
            finally
            {
                if (!IsDisposed) actualizarNovedades.Enabled = true;
            }
        }

        private async Task EnviarNovedadAsync()
        {
            var tipo = tipoNovedad.SelectedItem as TipoNovedad;
            if (tipo == null)
            {
                estadoNovedades.Text = "Selecciona un tipo de queja o novedad.";
                return;
            }
            if (string.IsNullOrWhiteSpace(motivoNovedad.Text))
            {
                estadoNovedades.Text = "Escribe una descripción antes de enviar.";
                motivoNovedad.Focus();
                return;
            }
            if (fechaFinNovedad.Checked && fechaFinNovedad.Value.Date < fechaNovedad.Value.Date)
            {
                estadoNovedades.Text = "La fecha Hasta no puede ser anterior a Desde.";
                fechaFinNovedad.Focus();
                return;
            }

            enviarNovedad.Enabled = actualizarNovedades.Enabled = false;
            estadoNovedades.Text = "Enviando al administrador...";
            try
            {
                await consulta.EnviarNovedadAsync(sesion.IdEmpleado, tipo.IdTipoNovedad,
                    fechaNovedad.Value.Date, fechaFinNovedad.Checked ? fechaFinNovedad.Value.Date : (DateTime?)null,
                    motivoNovedad.Text);
                if (IsDisposed) return;
                motivoNovedad.Clear();
                await CargarNovedadesAsync();
                if (!IsDisposed) estadoNovedades.Text = "Tu reporte se envió como Pendiente y ya está disponible para el administrador.";
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                if (!IsDisposed) estadoNovedades.Text = error.Message;
            }
            catch (Exception)
            {
                if (!IsDisposed) estadoNovedades.Text = "No se pudo enviar. Comprueba la conexión e inténtalo de nuevo.";
            }
            finally
            {
                if (!IsDisposed)
                {
                    actualizarNovedades.Enabled = true;
                    enviarNovedad.Enabled = tipoNovedad.Items.Count > 0;
                }
            }
        }

        private Control CrearAccionesMarcacion()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 4, 0, 4),
                Padding = new Padding(12, 8, 12, 4)
            };
            TemaAplicacion.DefinirRol(panel, RolTema.Superficie);
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
            entrada.Size = salida.Size = new Size(145, 38);
            entrada.Enabled = salida.Enabled = idTerminal > 0;
            entrada.Click += async (sender, args) => await MarcarAsync("Entrada");
            salida.Click += async (sender, args) => await MarcarAsync("Salida");
            acciones.Controls.Add(entrada);
            acciones.Controls.Add(salida);
            if (marcacionFacial != null && marcacionConClave != null)
            {
                usarCamara.Margin = new Padding(9, 11, 0, 0);
                usarCamara.Enabled = idTerminal > 0;
                acciones.Controls.Add(usarCamara);
            }
            panel.Controls.Add(acciones, 0, 0);
            estadoMarcacion.AccessibleName = "Estado de la marcación";
            estadoMarcacion.Text = idTerminal <= 0 ? "Solicita a administración que asigne un terminal activo a este equipo."
                : marcacionConClave != null ? "Confirma con tu contraseña. Puedes marcar con tu equipo de protección puesto."
                : "Selecciona una marcación para iniciar la verificación facial.";
            TemaAplicacion.DefinirRol(estadoMarcacion, RolTema.TextoSecundario);
            panel.Controls.Add(estadoMarcacion, 0, 1);
            return panel;
        }

        private async Task MarcarAsync(string tipo)
        {
            if (marcando || cargando || idTerminal <= 0) return;
            marcando = true;
            entrada.Enabled = salida.Enabled = buscar.Enabled = usarCamara.Enabled = false;
            try
            {
                string titulo = "Marcar " + tipo.ToLowerInvariant();
                RegistroAsistencia registro;
                if (marcacionFacial != null && (marcacionConClave == null || usarCamara.Checked))
                {
                    using (var captura = new FrmCapturaFacial(titulo, registrar: false))
                    {
                        if (captura.ShowDialog(this) != DialogResult.OK)
                        {
                            usarCamara.Checked = false;
                            estadoMarcacion.Text = marcacionConClave != null
                                ? "Puedes volver a marcar con tu contraseña." : "Marcación cancelada.";
                            return;
                        }
                        estadoMarcacion.Text = "Verificando el rostro y guardando la marcación…";
                        registro = await marcacionFacial.RegistrarAsync(sesion, idTerminal, tipo, captura.Captura);
                    }
                }
                else
                {
                    var campo = CampoRegistro.Clave("Contraseña de tu cuenta");
                    string clave;
                    using (var dialogo = new EditorRegistro(titulo, "Confirmar", campo))
                    {
                        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
                        clave = campo.ValorTexto;
                        campo.Entrada.ResetText();
                    }
                    estadoMarcacion.Text = "Verificando la contraseña y guardando la marcación…";
                    registro = await marcacionConClave.RegistrarAsync(sesion, idTerminal, tipo, clave);
                }
                if (IsDisposed) return;
                estadoMarcacion.Text = $"{tipo} registrada el {registro.Fecha:dd/MM/yyyy} a las {registro.Hora:HH:mm:ss}.";
                if (registro.Fecha.Date > hasta.Value.Date && hasta.Value.Date == PlanificacionTurnos.AhoraColombia.Date.AddDays(-1))
                    hasta.Value = registro.Fecha.Date;
                if (registro.Fecha.Date >= desde.Value.Date && registro.Fecha.Date <= hasta.Value.Date)
                    await CargarAsync(actualizarTurno: false);
            }
            catch (Exception error) when (error is InvalidOperationException || error is UnauthorizedAccessException)
            {
                if (!IsDisposed) estadoMarcacion.Text = error.Message;
            }
            catch (Exception)
            {
                if (!IsDisposed) estadoMarcacion.Text = "No se pudo confirmar la marcación. Consulta tus registros antes de volver a intentarlo.";
            }
            finally
            {
                marcando = false;
                if (!IsDisposed)
                {
                    usarCamara.Checked = false;
                    entrada.Enabled = salida.Enabled = usarCamara.Enabled = idTerminal > 0;
                    buscar.Enabled = true;
                }
            }
        }

        private static Label Etiqueta(string texto, float tamano, FontStyle estilo = FontStyle.Regular)
        {
            return new Label { Text = texto, Dock = DockStyle.Fill, Font = new Font("Segoe UI", tamano, estilo), TextAlign = ContentAlignment.MiddleLeft };
        }

        private static Control Filtro(string nombre, DateTimePicker selector)
        {
            var panel = new Panel { Size = new Size(155, 72), Margin = new Padding(0, 0, 12, 0) };
            panel.Controls.Add(new Label { Text = nombre, AutoSize = true });
            selector.Format = DateTimePickerFormat.Short;
            selector.AccessibleName = nombre;
            selector.Location = new Point(0, 27);
            selector.Width = 150;
            selector.MaxDate = new DateTime(9998, 12, 30);
            panel.Controls.Add(selector);
            return panel;
        }

        private void Columna(string titulo, string propiedad, string formato)
        {
            registros.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = titulo, DataPropertyName = propiedad,
                DefaultCellStyle = new DataGridViewCellStyle { Format = formato }
            });
        }

        private async Task CargarAsync(bool actualizarTurno = true)
        {
            if (cargando) return;
            if (desde.Value.Date > hasta.Value.Date)
            {
                estado.Text = "La fecha inicial debe ser anterior o igual a la fecha final.";
                desde.Focus();
                return;
            }
            cargando = true;
            buscar.Enabled = false;
            entrada.Enabled = salida.Enabled = false;
            usarCamara.Enabled = false;
            desde.Enabled = false;
            hasta.Enabled = false;
            registros.DataSource = null;
            estado.Text = "Consultando marcaciones...";
            var tareaTurno = actualizarTurno ? CargarTurnoAsync() : Task.CompletedTask;
            try
            {
                var datos = await consulta.ConsultarAsync(sesion.IdEmpleado, desde.Value, hasta.Value);
                if (IsDisposed) return;
                registros.DataSource = datos;
                registros.ClearSelection();
                estado.Text = datos.Count == 0 ? "No hay marcaciones en este período."
                    : datos.Count == 500 ? "Se muestran las 500 marcaciones más recientes. Reduce el período para consultar otras."
                    : datos.Count + " marcaciones encontradas.";
            }
            catch (Exception)
            {
                if (!IsDisposed) estado.Text = "No se pudieron consultar las marcaciones. Comprueba la conexión y vuelve a consultar.";
            }
            finally
            {
                cargando = false;
                if (!IsDisposed)
                {
                    buscar.Enabled = !marcando;
                    entrada.Enabled = salida.Enabled = usarCamara.Enabled = !marcando && idTerminal > 0;
                    desde.Enabled = hasta.Enabled = true;
                }
            }
            await tareaTurno;
        }

        private async Task CargarTurnoAsync()
        {
            if (consultaTurnos == null || cargandoTurno) return;
            DateTime ahora = PlanificacionTurnos.AhoraColombia;
            cargandoTurno = true;
            estadoTurno.Text = "Consultando tu turno…";
            try
            {
                var turnos = await consultaTurnos.ConsultarTurnosAsync(sesion.IdEmpleado, ahora, ahora.Date.AddDays(8));
                if (IsDisposed) return;
                ahora = PlanificacionTurnos.AhoraColombia;
                var enCurso = turnos.LastOrDefault(t => t.Inicio <= ahora && ahora < t.Fin);
                var turno = enCurso ?? turnos.FirstOrDefault(t => t.Inicio > ahora);
                estadoTurno.Text = turno == null ? "Sin turnos asignados en los próximos siete días."
                    : $"{(enCurso == null ? "Próximo turno" : "Turno en curso")} · {turno.Nombre}{Environment.NewLine}" +
                        $"{turno.Inicio:dd/MM HH:mm} – {turno.Fin:dd/MM HH:mm}";
            }
            catch (Exception)
            {
                if (!IsDisposed) estadoTurno.Text = "No se pudo consultar tu turno. Pulsa Consultar para volver a intentar.";
            }
            finally
            {
                cargandoTurno = false;
            }
        }
    }
}
