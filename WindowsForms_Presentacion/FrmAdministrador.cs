using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Entidades;
using Logica;

namespace WindowsForms_Presentacion
{
    internal sealed partial class FrmAdministrador : Form
    {
        private static Color Fondo => TemaAplicacion.Actual.Fondo;
        private static Color FondoLateral => TemaAplicacion.Actual.Lateral;
        private static Color TextoSecundario => TemaAplicacion.Actual.TextoSecundario;
        private static Color FondoTabla => TemaAplicacion.Actual.Superficie;
        private static Color EncabezadoTabla => TemaAplicacion.Actual.EncabezadoTabla;
        private static Color SeleccionTabla => TemaAplicacion.Actual.SeleccionTabla;
        private readonly List<Vista> vistas = new List<Vista>();
        private readonly ListBox modulos = new ListBox();
        private readonly DataGridView tabla = new DataGridView();
        private readonly FlowLayoutPanel filtros = new FlowLayoutPanel();
        private readonly DateTimePicker desde = new DateTimePicker();
        private readonly DateTimePicker hasta = new DateTimePicker();
        private readonly AccesoButton actualizar = new AccesoButton
        {
            Text = "Actualizar vista", Principal = true, Size = new Size(150, 40)
        };
        private readonly AccesoButton agregar = new AccesoButton
        {
            Text = "Agregar", Principal = true, Size = new Size(174, 40)
        };
        private readonly AccesoButton filtrarEmpleado = new AccesoButton
        {
            Text = "Filtrar empleado", BackColor = SeleccionTabla, Size = new Size(160, 40)
        };
        private int? idEmpleadoFiltrado;
        private readonly SesionUsuario sesion;
        private readonly IConsultaAdministracion consulta;
        private readonly string estadoActivo = System.Configuration.ConfigurationManager.AppSettings["EstadoActivo"] ?? "A";
        private readonly Label titulo = new Label();
        private readonly Label estado = new Label();
        private bool cargando;

        internal FrmAdministrador(SesionUsuario sesion, IConsultaAdministracion consulta)
        {
            if (sesion == null) throw new ArgumentNullException(nameof(sesion));
            if (!sesion.EsAdministrador) throw new UnauthorizedAccessException();
            if (consulta == null) throw new ArgumentNullException(nameof(consulta));
            this.sesion = sesion;
            this.consulta = consulta;

            Text = "AssistPRO | Administración";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            ClientSize = new Size(Math.Min(1120, area.Width - 64), Math.Min(740, area.Height - 64));
            MinimumSize = new Size(Math.Min(800, area.Width), Math.Min(520, area.Height));
            WindowState = FormWindowState.Maximized;
            BackColor = Fondo;
            ForeColor = TemaAplicacion.Actual.Texto;
            Font = new Font("Segoe UI", 11F);

            ConstruirInterfaz(sesion);
            ConfigurarDashboard();
            ConfigurarOperaciones();
            ConfigurarCatalogos();

            modulos.SelectedIndexChanged += async (origen, evento) => await CargarVistaAsync();
            actualizar.Click += async (origen, evento) => await CargarVistaAsync();
            agregar.Click += async (origen, evento) => await AgregarRegistroAsync();
            filtrarEmpleado.Click += async (origen, evento) => await FiltrarEmpleadoAsync();
            Shown += (origen, evento) =>
            {
                AjustarAlturaModulos();
                modulos.SelectedIndex = 0;
            };
            TemaAplicacion.Registrar(this);
        }

        private void ConstruirInterfaz(SesionUsuario sesion)
        {
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            raiz.Controls.Add(CrearNavegacion(sesion), 0, 0);
            raiz.Controls.Add(CrearContenido(), 1, 0);
            Controls.Add(raiz);
        }

        private Control CrearNavegacion(SesionUsuario sesion)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                Padding = new Padding(20, 28, 20, 20), BackColor = FondoLateral
            };
            TemaAplicacion.DefinirRol(panel, RolTema.Lateral);
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            panel.Controls.Add(new Label
            {
                Text = "AssistPRO", Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 23, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            modulos.Dock = DockStyle.Fill;
            modulos.BorderStyle = BorderStyle.None;
            modulos.BackColor = FondoLateral;
            modulos.ForeColor = TextoSecundario;
            TemaAplicacion.DefinirRol(modulos, RolTema.Lateral);
            modulos.AccessibleName = "Módulos";
            modulos.IntegralHeight = false;
            modulos.DrawMode = DrawMode.OwnerDrawFixed;
            modulos.ItemHeight = Font.Height + 14;
            modulos.DrawItem += DibujarModulo;
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
                Text = "Cerrar sesión", Dock = DockStyle.Fill
            };
            salir.Click += (origen, evento) => Close();
            panel.Controls.Add(salir, 0, 4);
            return panel;
        }

        // Con ventanas bajas la lista no cabe y aparecería una barra de desplazamiento clara: se compacta cada módulo.
        private void AjustarAlturaModulos()
        {
            if (modulos.Items.Count == 0 || modulos.Height <= 0) return;
            int ideal = Font.Height + 14;
            int altura = Math.Max(Font.Height + 4, Math.Min(ideal, modulos.Height / modulos.Items.Count));
            if (modulos.ItemHeight != altura) modulos.ItemHeight = altura;
        }

        private void DibujarModulo(object origen, DrawItemEventArgs evento)
        {
            if (evento.Index < 0) return;
            var tema = TemaAplicacion.Actual;
            bool seleccionado = (evento.State & DrawItemState.Selected) != 0;
            using (var fondo = new SolidBrush(FondoLateral))
                evento.Graphics.FillRectangle(fondo, evento.Bounds);
            var rectangulo = Rectangle.Inflate(evento.Bounds, -1, -3);
            if (seleccionado)
            {
                var estadoGrafico = evento.Graphics.Save();
                evento.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var contorno = DibujoAcceso.Redondeado(rectangulo, 12))
                using (var fondo = new LinearGradientBrush(rectangulo, tema.PrimarioInicio,
                    tema.PrimarioFin, 90F))
                    evento.Graphics.FillPath(fondo, contorno);
                evento.Graphics.Restore(estadoGrafico);
            }
            TextRenderer.DrawText(evento.Graphics, modulos.Items[evento.Index].ToString(), modulos.Font,
                Rectangle.Inflate(rectangulo, -12, 0), seleccionado ? tema.TextoPrimario : TextoSecundario,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if ((evento.State & DrawItemState.Focus) != 0)
                ControlPaint.DrawFocusRectangle(evento.Graphics, Rectangle.Inflate(rectangulo, -4, -4),
                    tema.BordeFoco, FondoLateral);
        }

        private Control CrearContenido()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                Padding = new Padding(28)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var encabezado = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Height = 50,
                Margin = new Padding(0, 0, 0, 16)
            };
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            encabezado.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            titulo.Dock = DockStyle.Fill;
            titulo.AutoEllipsis = true;
            titulo.Font = new Font(Font.FontFamily, 26, FontStyle.Bold);
            titulo.Margin = Padding.Empty;
            titulo.TextAlign = ContentAlignment.MiddleLeft;
            encabezado.Controls.Add(titulo, 0, 0);
            var selectorTema = TemaAplicacion.CrearSelectorTema();
            selectorTema.Margin = new Padding(8, 0, 0, 0);
            encabezado.Controls.Add(selectorTema, 1, 0);
            panel.Controls.Add(encabezado, 0, 0);

            var acciones = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
            filtros.AutoSize = true;
            filtros.WrapContents = false;
            filtros.Margin = Padding.Empty;
            AgregarFecha(filtros, "Desde", desde, DateTime.Today.AddDays(-30));
            AgregarFecha(filtros, "Hasta", hasta, DateTime.Today);
            acciones.Controls.Add(CrearFiltrosDashboard());
            acciones.Controls.Add(filtros);
            acciones.Controls.Add(agregar);
            acciones.Controls.Add(actualizar);
            acciones.Controls.Add(filtrarEmpleado);
            panel.Controls.Add(acciones, 0, 1);

            tabla.AccessibleName = "Registros del módulo";
            ConfigurarTabla(tabla);
            var contenido = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            contenido.Controls.Add(tabla);
            contenido.Controls.Add(CrearContenidoDashboard());
            panel.Controls.Add(contenido, 0, 2);

            estado.AutoSize = true;
            estado.AccessibleDescription = "Estado de la consulta";
            estado.ForeColor = TextoSecundario;
            TemaAplicacion.DefinirRol(estado, RolTema.TextoSecundario);
            estado.Margin = new Padding(0, 14, 0, 0);
            panel.Controls.Add(estado, 0, 3);
            return panel;
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
            tabla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tabla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tabla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tabla.EnableHeadersVisualStyles = false;
            tabla.ColumnHeadersDefaultCellStyle.BackColor = EncabezadoTabla;
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = TemaAplicacion.Actual.Texto;
            tabla.ColumnHeadersDefaultCellStyle.SelectionBackColor = EncabezadoTabla;
            tabla.ColumnHeadersDefaultCellStyle.SelectionForeColor = TemaAplicacion.Actual.Texto;
            tabla.ColumnHeadersDefaultCellStyle.Padding = new Padding(8);
            tabla.DefaultCellStyle.BackColor = FondoTabla;
            tabla.DefaultCellStyle.ForeColor = TemaAplicacion.Actual.TextoTabla;
            tabla.DefaultCellStyle.Padding = new Padding(8, 0, 5, 0);
            tabla.DefaultCellStyle.SelectionBackColor = SeleccionTabla;
            tabla.DefaultCellStyle.SelectionForeColor = TemaAplicacion.Actual.TextoSeleccionTabla;
            tabla.AlternatingRowsDefaultCellStyle.BackColor = TemaAplicacion.Actual.AlternanciaTabla;
            tabla.RowTemplate.Height = 42;
        }

        private static void AgregarFecha(FlowLayoutPanel filtros, string nombre, DateTimePicker selector, DateTime fecha)
        {
            var campo = new Panel { Size = new Size(150, 62), Margin = new Padding(0, 0, 12, 0) };
            var etiqueta = new Label { Text = nombre, AutoSize = true, ForeColor = TextoSecundario };
            TemaAplicacion.DefinirRol(etiqueta, RolTema.TextoSecundario);
            campo.Controls.Add(etiqueta);
            selector.AccessibleName = nombre;
            selector.Location = new Point(0, 24);
            selector.Width = 150;
            selector.Format = DateTimePickerFormat.Short;
            selector.MaxDate = new DateTime(9998, 12, 30);
            selector.Value = fecha;
            campo.Controls.Add(selector);
            filtros.Controls.Add(campo);
        }

        private void AgregarVista<T>(string nombre, string[] columnas, Func<Task<List<T>>> listar,
            Func<T, object[]> valores, Func<Task<bool>> crear = null, bool usaFechas = false)
        {
            modulos.Items.Add(nombre);
            vistas.Add(new Vista
            {
                Columnas = columnas,
                UsaFechas = usaFechas,
                AgregarAsync = crear,
                CargarAsync = async () => (await listar()).Select(valores).ToArray()
            });
        }

        private void CambiarCarga(bool enCurso)
        {
            cargando = enCurso;
            if (IsDisposed || Disposing) return;
            modulos.Enabled = actualizar.Enabled = filtros.Enabled = agregar.Enabled = filtrarEmpleado.Enabled = !enCurso;
            filtrosDashboard.Enabled = contenidoDashboard.Enabled = !enCurso;
        }

        private async Task AgregarRegistroAsync()
        {
            if (cargando || modulos.SelectedIndex < 0) return;

            var vista = vistas[modulos.SelectedIndex];
            if (vista.AgregarAsync == null) return;
            var estadoAnterior = estado.Text;
            bool guardado;
            CambiarCarga(true);
            estado.Text = "Preparando el formulario…";
            try
            {
                guardado = await vista.AgregarAsync();
            }
            catch (Exception error)
            {
                if (!IsDisposed && !Disposing)
                {
                    estado.Text = error is InvalidOperationException || error is UnauthorizedAccessException
                        ? error.Message : "No se pudo completar el alta en Oracle.";
                }
                return;
            }
            finally
            {
                CambiarCarga(false);
            }

            if (IsDisposed || Disposing) return;
            if (guardado) await CargarVistaAsync("Registro guardado en Oracle.");
            else estado.Text = estadoAnterior;
        }

        private async Task CargarVistaAsync(string resultadoCarga = null)
        {
            if (cargando || modulos.SelectedIndex < 0) return;

            var vista = vistas[modulos.SelectedIndex];
            titulo.Text = modulos.SelectedItem.ToString();
            filtros.Visible = vista.UsaFechas;
            filtrosDashboard.Visible = vista.EsDashboard;
            contenidoDashboard.Visible = vista.EsDashboard;
            tabla.Visible = !vista.EsDashboard;
            filtrarEmpleado.Visible = vista.UsaFechas;
            filtrarEmpleado.Text = idEmpleadoFiltrado.HasValue ? "Cambiar empleado" : "Filtrar empleado";
            agregar.Visible = vista.AgregarAsync != null;
            int separacion = vista.UsaFechas || vista.EsDashboard ? 22 : 0;
            agregar.Margin = new Padding(0, separacion, 8, 0);
            actualizar.Margin = new Padding(0, separacion, 8, 0);
            filtrarEmpleado.Margin = new Padding(0, separacion, 0, 0);
            agregar.Text = vista.UsaFechas ? "Registrar marcación"
                : modulos.SelectedItem.ToString() == "Plantillas" ? "Registrar rostro" : "Agregar";
            if (vista.EsDashboard)
            {
                await CargarDashboardAsync();
                return;
            }

            tabla.Columns.Clear();
            foreach (string columna in vista.Columnas)
            {
                int indice = tabla.Columns.Add(columna, columna);
                tabla.Columns[indice].MinimumWidth = 54;
                if (columna == "ID") tabla.Columns[indice].FillWeight = 40;
                else if (columna == "Empleado" || columna == "Nombre" || columna == "Dispositivo")
                    tabla.Columns[indice].FillWeight = 160;
            }
            if (vista.UsaFechas && desde.Value.Date > hasta.Value.Date)
            {
                estado.Text = "La fecha inicial debe ser anterior o igual a la fecha final.";
                desde.Focus();
                return;
            }

            CambiarCarga(true);
            estado.Text = "Consultando registros…";
            try
            {
                var filas = await vista.CargarAsync();
                if (IsDisposed || Disposing) return;
                foreach (object[] fila in filas) tabla.Rows.Add(fila);
                estado.Text = resultadoCarga ?? (filas.Length == 0 ? "No hay registros para mostrar."
                    : filas.Length == 1 ? "1 registro" : $"{filas.Length} registros");
                if (resultadoCarga == null && vista.UsaFechas && filas.Length == 500)
                    estado.Text = "Se muestran hasta 500 marcaciones. Reduce el rango para consultar otras.";
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
            internal string[] Columnas { get; set; }
            internal bool UsaFechas { get; set; }
            internal bool EsDashboard { get; set; }
            internal Func<Task<bool>> AgregarAsync { get; set; }
            internal Func<Task<object[][]>> CargarAsync { get; set; }
        }
    }
}
