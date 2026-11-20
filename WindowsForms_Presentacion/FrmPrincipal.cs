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
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                Padding = new Padding(16, 24, 16, 16), Margin = Padding.Empty
            };
            TemaAplicacion.DefinirRol(lateral, RolTema.Lateral);
            lateral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            lateral.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            lateral.Controls.Add(TemaAplicacion.CrearMarca(), 0, 0);
            var identidad = Etiqueta(sesion.Nombre + Environment.NewLine + sesion.Rol, 10);
            identidad.AutoEllipsis = true;
            identidad.UseMnemonic = false;
            lateral.Controls.Add(identidad, 0, 2);
            var tema = TemaAplicacion.CrearSelectorTema();
            lateral.Controls.Add(tema, 0, 3);
            var salir = new AccesoButton { Text = "Cerrar sesión", Dock = DockStyle.Fill, Plano = true };
            salir.Click += (sender, args) => Close();
            lateral.Controls.Add(salir, 0, 4);

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
            raiz.Controls.Add(lateral, 0, 0);
            raiz.Controls.Add(contenido, 1, 0);
            Controls.Add(raiz);
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
