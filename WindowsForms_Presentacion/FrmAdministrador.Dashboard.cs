using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Logica;

namespace WindowsForms_Presentacion
{
    internal sealed partial class FrmAdministrador
    {
        private readonly FlowLayoutPanel filtrosDashboard = new FlowLayoutPanel();
        private readonly DateTimePicker desdeDashboard = new DateTimePicker();
        private readonly DateTimePicker hastaDashboard = new DateTimePicker();
        private readonly TableLayoutPanel contenidoDashboard = new TableLayoutPanel();
        private readonly DataGridView tablaDashboard = new DataGridView();
        private readonly Label[] valoresDashboard = new Label[4];
        private readonly AccesoButton[] botonesReporte = new AccesoButton[3];
        private ResumenDashboard resumenDashboard;
        private int reporteDashboard;

        private void ConfigurarDashboard()
        {
            modulos.Items.Add("Dashboard");
            vistas.Add(new Vista { EsDashboard = true });
            MostrarReporteDashboard(0);
        }

        private Control CrearFiltrosDashboard()
        {
            var hoy = DateTime.Today;
            filtrosDashboard.AutoSize = true;
            filtrosDashboard.WrapContents = false;
            filtrosDashboard.Visible = false;
            filtrosDashboard.Margin = Padding.Empty;
            AgregarFecha(filtrosDashboard, "Desde", desdeDashboard, hoy.AddDays(-29));
            AgregarFecha(filtrosDashboard, "Hasta", hastaDashboard, hoy);
            desdeDashboard.ValueChanged += (origen, evento) => CambiarPeriodoDashboard();
            hastaDashboard.ValueChanged += (origen, evento) => CambiarPeriodoDashboard();
            return filtrosDashboard;
        }

        private Control CrearContenidoDashboard()
        {
            contenidoDashboard.Dock = DockStyle.Fill;
            contenidoDashboard.Margin = Padding.Empty;
            contenidoDashboard.Visible = false;
            contenidoDashboard.ColumnCount = 1;
            contenidoDashboard.RowCount = 3;
            contenidoDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contenidoDashboard.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            contenidoDashboard.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            contenidoDashboard.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var indicadores = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, Margin = Padding.Empty, ColumnCount = 4, RowCount = 1,
                AccessibleName = "Indicadores de la empresa"
            };
            indicadores.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int indice = 0; indice < valoresDashboard.Length; indice++)
                indicadores.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            indicadores.Controls.Add(CrearIndicador(0, "Empleados\nactivos hoy", RolTema.AcentoMorado), 0, 0);
            indicadores.Controls.Add(CrearIndicador(1, "Empleados con\nmarcaciones", RolTema.AcentoAzul), 1, 0);
            indicadores.Controls.Add(CrearIndicador(2, "Marcaciones\ndel periodo", RolTema.AcentoVerde), 2, 0);
            indicadores.Controls.Add(CrearIndicador(3, "Novedades\ndel periodo", RolTema.AcentoNaranja), 3, 0);
            contenidoDashboard.Controls.Add(indicadores, 0, 0);

            var reportes = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0, 4, 0, 8)
            };
            reportes.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            reportes.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            for (int indice = 0; indice < botonesReporte.Length; indice++)
                reportes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / botonesReporte.Length));
            reportes.Controls.Add(new Label
            {
                Text = "Reportes", Dock = DockStyle.Fill, Margin = Padding.Empty,
                Font = new Font(Font.FontFamily, 14F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            reportes.Controls.Add(CrearBotonReporte(0, "Por día", "Marcaciones por día"), 1, 0);
            reportes.Controls.Add(CrearBotonReporte(1, "Departamento", "Marcaciones por departamento"), 2, 0);
            reportes.Controls.Add(CrearBotonReporte(2, "Novedades", "Novedades por estado"), 3, 0);
            contenidoDashboard.Controls.Add(reportes, 0, 1);

            ConfigurarTabla(tablaDashboard);
            tablaDashboard.Margin = Padding.Empty;
            tablaDashboard.RowTemplate.Height = 34;
            contenidoDashboard.Controls.Add(tablaDashboard, 0, 2);
            return contenidoDashboard;
        }

        private Control CrearIndicador(int indice, string etiqueta, RolTema acento)
        {
            var tarjeta = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = FondoTabla,
                Margin = new Padding(0, 0, indice == valoresDashboard.Length - 1 ? 0 : 8, 8),
                AccessibleName = etiqueta.Replace('\n', ' ')
            };
            TemaAplicacion.DefinirRol(tarjeta, RolTema.Superficie);
            tarjeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Absolute, 3));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            var banda = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            TemaAplicacion.DefinirRol(banda, acento);
            tarjeta.Controls.Add(banda, 0, 0);
            valoresDashboard[indice] = new Label
            {
                Text = "—", Dock = DockStyle.Fill, Margin = new Padding(10, 0, 2, 0),
                Font = new Font(Font.FontFamily, 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft,
                AccessibleName = etiqueta.Replace('\n', ' '), AutoEllipsis = true
            };
            TemaAplicacion.DefinirRol(valoresDashboard[indice], acento);
            tarjeta.Controls.Add(valoresDashboard[indice], 0, 1);
            var nombre = new Label
            {
                Text = etiqueta, Dock = DockStyle.Fill, Margin = new Padding(11, 0, 2, 0),
                ForeColor = TextoSecundario, Font = new Font(Font.FontFamily, 10F)
            };
            TemaAplicacion.DefinirRol(nombre, RolTema.TextoSecundario);
            tarjeta.Controls.Add(nombre, 0, 2);
            return tarjeta;
        }

        private Control CrearBotonReporte(int indice, string texto, string nombreAccesible)
        {
            var boton = new AccesoButton
            {
                Text = texto, AccessibleName = nombreAccesible, Dock = DockStyle.Fill, BackColor = EncabezadoTabla,
                Font = new Font(Font.FontFamily, 10F),
                Margin = new Padding(0, 0, indice == botonesReporte.Length - 1 ? 0 : 8, 0)
            };
            boton.Click += (origen, evento) => MostrarReporteDashboard(indice);
            botonesReporte[indice] = boton;
            return boton;
        }

        private void CambiarPeriodoDashboard()
        {
            LimpiarDashboard();
            if (contenidoDashboard.Visible)
                estado.Text = "Pulsa Actualizar vista para consultar el periodo seleccionado.";
        }

        private void LimpiarDashboard()
        {
            resumenDashboard = null;
            foreach (var valor in valoresDashboard) valor.Text = "—";
            tablaDashboard.Rows.Clear();
        }

        private async Task CargarDashboardAsync()
        {
            LimpiarDashboard();
            DateTime inicio = desdeDashboard.Value.Date;
            DateTime fin = hastaDashboard.Value.Date;
            if (inicio > fin)
            {
                estado.Text = "La fecha inicial debe ser anterior o igual a la fecha final.";
                desdeDashboard.Focus();
                return;
            }

            CambiarCarga(true);
            estado.Text = "Consultando estadísticas de la empresa…";
            try
            {
                var resumen = await consulta.ObtenerDashboardAsync(sesion, inicio, fin);
                if (IsDisposed || Disposing) return;
                resumenDashboard = resumen;
                valoresDashboard[0].Text = resumen.EmpleadosActivos.ToString("N0");
                valoresDashboard[1].Text = resumen.EmpleadosConMarcaciones.ToString("N0");
                valoresDashboard[2].Text = resumen.TotalMarcaciones.ToString("N0");
                valoresDashboard[3].Text = resumen.TotalNovedades.ToString("N0");
                MostrarReporteDashboard(reporteDashboard);
            }
            catch (Exception error)
            {
                if (IsDisposed || Disposing) return;
                LimpiarDashboard();
                estado.Text = error is UnauthorizedAccessException ? error.Message
                    : "No se pudo cargar el dashboard. Pulsa Actualizar vista.";
            }
            finally
            {
                CambiarCarga(false);
            }
        }

        private void MostrarReporteDashboard(int indice)
        {
            reporteDashboard = indice;
            for (int actual = 0; actual < botonesReporte.Length; actual++)
            {
                var boton = botonesReporte[actual];
                boton.Principal = actual == indice;
                boton.AccessibleDescription = boton.Principal ? "Reporte seleccionado" : string.Empty;
                boton.Invalidate();
            }

            tablaDashboard.AccessibleName = botonesReporte[indice].AccessibleName;
            tablaDashboard.Rows.Clear();
            tablaDashboard.Columns.Clear();
            switch (indice)
            {
                case 0:
                    AgregarColumnaReporte("Fecha", 120, "dd/MM/yyyy");
                    AgregarColumnaReporte("Entradas", 85, "N0");
                    AgregarColumnaReporte("Salidas", 85, "N0");
                    AgregarColumnaReporte("Marcaciones", 100, "N0");
                    if (resumenDashboard == null) return;
                    foreach (var dia in resumenDashboard.MarcacionesPorDia)
                        tablaDashboard.Rows.Add(dia.Dia, dia.Entradas, dia.Salidas, dia.Total);
                    int dias = tablaDashboard.Rows.Count;
                    estado.Text = dias == 0 ? "No hay marcaciones en el periodo seleccionado."
                        : dias == 1 ? "1 día con marcaciones." : $"{dias:N0} días con marcaciones.";
                    break;
                case 1:
                    AgregarColumnaReporte("Sede", 101);
                    AgregarColumnaReporte("Departamento", 140);
                    AgregarColumnaReporte("Empleados", 115, "N0");
                    AgregarColumnaReporte("Marcaciones", 124, "N0");
                    if (resumenDashboard == null) return;
                    foreach (var departamento in resumenDashboard.MarcacionesPorDepartamento)
                        tablaDashboard.Rows.Add(departamento.Sede, departamento.Departamento,
                            departamento.EmpleadosConMarcaciones, departamento.TotalMarcaciones);
                    estado.Text = tablaDashboard.Rows.Count == 0 ? "No hay marcaciones en el periodo seleccionado."
                        : "Sede y departamento según la asignación actual del empleado.";
                    break;
                case 2:
                    AgregarColumnaReporte("Estado de aprobación", 160);
                    AgregarColumnaReporte("Novedades", 100, "N0");
                    if (resumenDashboard == null) return;
                    foreach (var novedad in resumenDashboard.NovedadesPorEstado)
                        tablaDashboard.Rows.Add(novedad.Estado, novedad.Total);
                    estado.Text = resumenDashboard.TotalNovedades == 0 ? "No hay novedades en el periodo seleccionado."
                        : "Incluye novedades que coinciden con el periodo.";
                    break;
            }
            tablaDashboard.ClearSelection();
        }

        private void AgregarColumnaReporte(string nombre, int peso, string formato = null)
        {
            var columna = new DataGridViewTextBoxColumn
            {
                Name = nombre, HeaderText = nombre, FillWeight = peso, MinimumWidth = 64,
                SortMode = DataGridViewColumnSortMode.Automatic
            };
            if (formato != null) columna.DefaultCellStyle.Format = formato;
            if (formato == "N0") columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            tablaDashboard.Columns.Add(columna);
        }
    }
}
