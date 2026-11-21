using System;
using System.Drawing;
using System.Linq;
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
        private readonly Panel graficoDashboard = new Panel();
        private readonly Label tituloGraficoDashboard = new Label();
        private readonly Label subtituloGraficoDashboard = new Label();
        private readonly Label tituloDetalleDashboard = new Label();
        private readonly Label[] valoresDashboard = new Label[4];
        private readonly AccesoButton[] botonesReporte = new AccesoButton[3];
        private ResumenDashboard resumenDashboard;
        private int reporteDashboard;

        private void ConfigurarDashboard()
        {
            modulos.Items.Add("Panel de asistencia");
            vistas.Add(new Vista { EsDashboard = true });
            MostrarReporteDashboard(0);
        }

        private Control CrearFiltrosDashboard()
        {
            var hoy = PlanificacionTurnos.AhoraColombia.Date;
            filtrosDashboard.Size = new Size(272, 49);
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
            contenidoDashboard.RowCount = 5;
            contenidoDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contenidoDashboard.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
            contenidoDashboard.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            contenidoDashboard.RowStyles.Add(new RowStyle(SizeType.Absolute, 224));
            contenidoDashboard.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            contenidoDashboard.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var indicadores = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, Margin = Padding.Empty, ColumnCount = 4, RowCount = 1,
                AccessibleName = "Indicadores de asistencia del personal"
            };
            indicadores.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int indice = 0; indice < valoresDashboard.Length; indice++)
                indicadores.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            indicadores.Controls.Add(CrearIndicador(0, "Personal\nactivo hoy", RolTema.AcentoMorado), 0, 0);
            indicadores.Controls.Add(CrearIndicador(1, "Personal con\nmarcaciones", RolTema.AcentoAzul), 1, 0);
            indicadores.Controls.Add(CrearIndicador(2, "Marcaciones\ndel periodo", RolTema.AcentoVerde), 2, 0);
            indicadores.Controls.Add(CrearIndicador(3, "Novedades\ndel periodo", RolTema.AcentoNaranja), 3, 0);
            contenidoDashboard.Controls.Add(indicadores, 0, 0);
            contenidoDashboard.SizeChanged += (origen, evento) =>
            {
                float escala = DeviceDpi / 96F;
                bool dosFilas = contenidoDashboard.ClientSize.Width < 580 * escala && contenidoDashboard.ClientSize.Height >= 344 * escala;
                var columnas = dosFilas ? 2 : tarjetas.Length;
                if (indicadores.ColumnCount == columnas) return;

                contenidoDashboard.SuspendLayout();
                indicadores.SuspendLayout();
                indicadores.ColumnCount = columnas;
                indicadores.RowCount = tarjetas.Length / columnas;
                indicadores.ColumnStyles.Clear();
                indicadores.RowStyles.Clear();
                for (int columna = 0; columna < columnas; columna++)
                    indicadores.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columnas));
                for (int fila = 0; fila < indicadores.RowCount; fila++)
                    indicadores.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / indicadores.RowCount));
                for (int indice = 0; indice < tarjetas.Length; indice++)
                {
                    int columna = indice % columnas;
                    indicadores.SetCellPosition(tarjetas[indice], new TableLayoutPanelCellPosition(columna, indice / columnas));
                    tarjetas[indice].Margin = new Padding(0, 0, columna == columnas - 1 ? 0 : (int)(8 * escala), (int)(8 * escala));
                }
                filaIndicadores.Height = (dosFilas ? 186 : 103) * escala;
                indicadores.ResumeLayout(true);
                contenidoDashboard.ResumeLayout(true);
            };

            var reportes = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0, 2, 0, 6)
            };
            reportes.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            reportes.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
            for (int indice = 0; indice < botonesReporte.Length; indice++)
                reportes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / botonesReporte.Length));
            reportes.Controls.Add(new Label
            {
                Text = "ANÁLISIS", Dock = DockStyle.Fill, Margin = Padding.Empty,
                Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextoSecundario
            }, 0, 0);
            reportes.Controls.Add(CrearBotonReporte(0, "Por día", "Marcaciones por día"), 1, 0);
            reportes.Controls.Add(CrearBotonReporte(1, "Por unidad", "Marcaciones por unidad o servicio"), 2, 0);
            reportes.Controls.Add(CrearBotonReporte(2, "Novedades", "Novedades por estado"), 3, 0);
            contenidoDashboard.Controls.Add(reportes, 0, 1);

            var panelGrafico = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                Margin = new Padding(0, 0, 0, 8), Padding = new Padding(14, 9, 14, 8),
                AccessibleName = "Gráfico de análisis de asistencia"
            };
            TemaAplicacion.DefinirRol(panelGrafico, RolTema.Superficie);
            panelGrafico.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panelGrafico.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            panelGrafico.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var encabezadoGrafico = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty
            };
            encabezadoGrafico.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            encabezadoGrafico.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            encabezadoGrafico.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            tituloGraficoDashboard.Dock = DockStyle.Fill;
            tituloGraficoDashboard.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            tituloGraficoDashboard.Text = "Marcaciones por día";
            subtituloGraficoDashboard.Dock = DockStyle.Fill;
            subtituloGraficoDashboard.Font = new Font("Segoe UI", 8.5F);
            subtituloGraficoDashboard.Text = "Entradas y salidas en el periodo seleccionado";
            TemaAplicacion.DefinirRol(subtituloGraficoDashboard, RolTema.TextoSecundario);
            encabezadoGrafico.Controls.Add(tituloGraficoDashboard, 0, 0);
            encabezadoGrafico.Controls.Add(subtituloGraficoDashboard, 0, 1);
            panelGrafico.Controls.Add(encabezadoGrafico, 0, 0);
            graficoDashboard.Dock = DockStyle.Fill;
            graficoDashboard.Margin = Padding.Empty;
            graficoDashboard.BackColor = FondoTabla;
            graficoDashboard.Paint += DibujarGraficoDashboard;
            TemaAplicacion.DefinirRol(graficoDashboard, RolTema.Superficie);
            panelGrafico.Controls.Add(graficoDashboard, 0, 1);
            contenidoDashboard.Controls.Add(panelGrafico, 0, 2);

            var encabezadoDetalle = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                Margin = new Padding(0, 0, 0, 4)
            };
            encabezadoDetalle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            encabezadoDetalle.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            encabezadoDetalle.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tituloDetalleDashboard.Text = "Detalle del reporte";
            tituloDetalleDashboard.Dock = DockStyle.Fill;
            tituloDetalleDashboard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            encabezadoDetalle.Controls.Add(tituloDetalleDashboard, 0, 0);
            var etiquetaPeriodo = new Label
            {
                Text = "SEDE SAN MARTÍN", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), Margin = Padding.Empty
            };
            TemaAplicacion.DefinirRol(etiquetaPeriodo, RolTema.TextoSecundario);
            encabezadoDetalle.Controls.Add(etiquetaPeriodo, 1, 0);
            contenidoDashboard.Controls.Add(encabezadoDetalle, 0, 3);

            ConfigurarTabla(tablaDashboard);
            tablaDashboard.Margin = Padding.Empty;
            tablaDashboard.RowTemplate.Height = 34;
            contenidoDashboard.Controls.Add(tablaDashboard, 0, 4);
            return contenidoDashboard;
        }

        private void DibujarGraficoDashboard(object origen, PaintEventArgs evento)
        {
            var superficie = (Panel)origen;
            var grafico = evento.Graphics;
            grafico.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            grafico.Clear(TemaAplicacion.Actual.Superficie);
            if (resumenDashboard == null)
            {
                using (var texto = new SolidBrush(TextoSecundario))
                using (var fuente = new Font("Segoe UI", 9F))
                    grafico.DrawString("Selecciona un periodo y actualiza la vista para ver el análisis.",
                        fuente, texto, new PointF(8, Math.Max(8, superficie.Height / 2F - 10)));
                return;
            }

            if (reporteDashboard == 0) DibujarSerieDiaria(grafico, superficie.ClientRectangle);
            else DibujarBarrasDashboard(grafico, superficie.ClientRectangle);
        }

        private void DibujarSerieDiaria(Graphics grafico, Rectangle limites)
        {
            var dias = resumenDashboard.MarcacionesPorDia.OrderBy(dia => dia.Dia).ToArray();
            if (dias.Length == 0)
            {
                DibujarSinDatos(grafico, limites);
                return;
            }

            int izquierda = 42, derecha = 12, arriba = 8, abajo = 28;
            var area = new Rectangle(izquierda, arriba, Math.Max(1, limites.Width - izquierda - derecha),
                Math.Max(1, limites.Height - arriba - abajo));
            int maximo = Math.Max(1, dias.Max(dia => Math.Max(dia.Entradas, dia.Salidas)));
            int escala = (int)Math.Ceiling(maximo / 4.0) * 4;
            var tema = TemaAplicacion.Actual;
            using (var rejilla = new Pen(tema.LineaTabla))
            using (var etiqueta = new SolidBrush(tema.TextoSecundario))
            using (var fuente = new Font("Segoe UI", 8F))
            using (var entradas = new Pen(tema.AcentoAzul, 2.5F))
            using (var salidas = new Pen(tema.AcentoVerde, 2.5F))
            using (var puntoEntrada = new SolidBrush(tema.AcentoAzul))
            using (var puntoSalida = new SolidBrush(tema.AcentoVerde))
            {
                for (int nivel = 0; nivel <= 4; nivel++)
                {
                    int y = area.Bottom - (int)(area.Height * nivel / 4F);
                    grafico.DrawLine(rejilla, area.Left, y, area.Right, y);
                    string valor = (escala * nivel / 4).ToString("N0");
                    var medida = grafico.MeasureString(valor, fuente);
                    grafico.DrawString(valor, fuente, etiqueta, area.Left - medida.Width - 7, y - medida.Height / 2);
                }

                var puntosEntrada = new PointF[dias.Length];
                var puntosSalida = new PointF[dias.Length];
                for (int indice = 0; indice < dias.Length; indice++)
                {
                    float x = dias.Length == 1 ? area.Left + area.Width / 2F
                        : area.Left + area.Width * indice / (dias.Length - 1F);
                    puntosEntrada[indice] = new PointF(x, area.Bottom - area.Height * dias[indice].Entradas / escala);
                    puntosSalida[indice] = new PointF(x, area.Bottom - area.Height * dias[indice].Salidas / escala);
                }
                if (dias.Length > 1)
                {
                    grafico.DrawLines(entradas, puntosEntrada);
                    grafico.DrawLines(salidas, puntosSalida);
                }
                for (int indice = 0; indice < dias.Length; indice++)
                {
                    grafico.FillEllipse(puntoEntrada, puntosEntrada[indice].X - 3.5F, puntosEntrada[indice].Y - 3.5F, 7, 7);
                    grafico.DrawEllipse(entradas, puntosEntrada[indice].X - 3.5F, puntosEntrada[indice].Y - 3.5F, 7, 7);
                    grafico.FillEllipse(puntoSalida, puntosSalida[indice].X - 3.5F, puntosSalida[indice].Y - 3.5F, 7, 7);
                    grafico.DrawEllipse(salidas, puntosSalida[indice].X - 3.5F, puntosSalida[indice].Y - 3.5F, 7, 7);
                }

                int paso = Math.Max(1, (int)Math.Ceiling(dias.Length / Math.Max(1, area.Width / 90F)));
                for (int indice = 0; indice < dias.Length; indice += paso)
                    grafico.DrawString(dias[indice].Dia.ToString("dd/MM"), fuente, etiqueta,
                        Math.Max(area.Left, puntosEntrada[indice].X - 20), area.Bottom + 5);

                float leyendaX = Math.Max(area.Left + 8, area.Right - 150);
                grafico.DrawLine(entradas, leyendaX, 5, leyendaX + 16, 5);
                grafico.DrawString("Entradas", fuente, etiqueta, leyendaX + 21, -2);
                grafico.DrawLine(salidas, leyendaX + 88, 5, leyendaX + 104, 5);
                grafico.DrawString("Salidas", fuente, etiqueta, leyendaX + 109, -2);
            }
        }

        private void DibujarBarrasDashboard(Graphics grafico, Rectangle limites)
        {
            string[] categorias;
            int[] valores;
            Color color;
            if (reporteDashboard == 1)
            {
                var departamentos = resumenDashboard.MarcacionesPorDepartamento
                    .OrderByDescending(item => item.TotalMarcaciones).Take(7).ToArray();
                categorias = departamentos.Select(item => string.IsNullOrWhiteSpace(item.Departamento)
                    ? item.Sede : item.Departamento).ToArray();
                valores = departamentos.Select(item => item.TotalMarcaciones).ToArray();
                color = TemaAplicacion.Actual.AcentoMorado;
            }
            else
            {
                var novedades = resumenDashboard.NovedadesPorEstado.OrderByDescending(item => item.Total).Take(7).ToArray();
                categorias = novedades.Select(item => item.Estado).ToArray();
                valores = novedades.Select(item => item.Total).ToArray();
                color = TemaAplicacion.Actual.AcentoNaranja;
            }

            if (valores.Length == 0)
            {
                DibujarSinDatos(grafico, limites);
                return;
            }

            int altoFila = Math.Min(26, Math.Max(18, (limites.Height - 8) / valores.Length));
            int xEtiqueta = 4;
            int anchoEtiqueta = Math.Min(180, Math.Max(88, limites.Width / 3));
            int xBarra = anchoEtiqueta + 12;
            int anchoBarra = Math.Max(24, limites.Width - xBarra - 46);
            int maximo = Math.Max(1, valores.Max());
            var tema = TemaAplicacion.Actual;
            using (var fuente = new Font("Segoe UI", 8.5F))
            using (var numero = new SolidBrush(tema.TextoSecundario))
            using (var barra = new SolidBrush(color))
            using (var fondoBarra = new SolidBrush(tema.EncabezadoTabla))
            {
                for (int indice = 0; indice < valores.Length; indice++)
                {
                    int y = 5 + indice * altoFila;
                    var rectEtiqueta = new Rectangle(xEtiqueta, y, anchoEtiqueta, altoFila);
                    TextRenderer.DrawText(grafico, categorias[indice] ?? "Sin clasificar", fuente, rectEtiqueta,
                        tema.TextoTabla, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    int anchoValor = (int)(anchoBarra * valores[indice] / (float)maximo);
                    int altoBarra = Math.Min(12, altoFila - 6);
                    int yBarra = y + (altoFila - altoBarra) / 2;
                    grafico.FillRectangle(fondoBarra, xBarra, yBarra, anchoBarra, altoBarra);
                    grafico.FillRectangle(barra, xBarra, yBarra, Math.Max(2, anchoValor), altoBarra);
                    grafico.DrawString(valores[indice].ToString("N0"), fuente, numero,
                        xBarra + anchoBarra + 8, y + (altoFila - fuente.Height) / 2F);
                }
            }
        }

        private void DibujarSinDatos(Graphics grafico, Rectangle limites)
        {
            using (var texto = new SolidBrush(TextoSecundario))
            using (var fuente = new Font("Segoe UI", 9F))
                grafico.DrawString("No hay datos para este análisis en el periodo seleccionado.",
                    fuente, texto, new PointF(8, Math.Max(8, limites.Height / 2F - 10)));
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
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Absolute, 2));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
            var banda = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            TemaAplicacion.DefinirRol(banda, acento);
            tarjeta.Controls.Add(banda, 0, 0);
            valoresDashboard[indice] = new Label
            {
                Text = "—", Dock = DockStyle.Fill, Margin = new Padding(10, 0, 2, 0),
                Font = new Font(Font.FontFamily, 21F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft,
                AccessibleName = etiqueta.Replace('\n', ' '), AutoEllipsis = true
            };
            TemaAplicacion.DefinirRol(valoresDashboard[indice], acento);
            tarjeta.Controls.Add(valoresDashboard[indice], 0, 1);
            var nombre = new Label
            {
                Text = etiqueta, Dock = DockStyle.Fill, Margin = new Padding(11, 0, 2, 0),
                ForeColor = TextoSecundario, Font = new Font(Font.FontFamily, 9.5F), AutoEllipsis = true
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
                Font = new Font(Font.FontFamily, 10F), Plano = true,
                Margin = new Padding(0, 0, indice == botonesReporte.Length - 1 ? 0 : 8, 0)
            };
            boton.Click += (origen, evento) => MostrarReporteDashboard(indice);
            botonesReporte[indice] = boton;
            return boton;
        }

        private void CambiarPeriodoDashboard()
        {
            LimpiarDashboard();
            graficoDashboard.Invalidate();
            if (contenidoDashboard.Visible)
                estado.Text = "Pulsa Actualizar para consultar el periodo seleccionado.";
        }

        private void LimpiarDashboard()
        {
            resumenDashboard = null;
            foreach (var valor in valoresDashboard) valor.Text = "—";
            tablaDashboard.Rows.Clear();
            graficoDashboard.Invalidate();
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
            estado.Text = "Consultando estadísticas de asistencia…";
            try
            {
                var resumen = await consulta.ObtenerDashboardAsync(sesion, inicio, fin);
                if (IsDisposed || Disposing) return;
                resumenDashboard = resumen;
                valoresDashboard[0].Text = resumen.EmpleadosActivos.ToString("N0");
                valoresDashboard[1].Text = resumen.EmpleadosConMarcaciones.ToString("N0");
                valoresDashboard[2].Text = resumen.TotalMarcaciones.ToString("N0");
                valoresDashboard[3].Text = resumen.TotalNovedades.ToString("N0");
                graficoDashboard.Invalidate();
                MostrarReporteDashboard(reporteDashboard);
            }
            catch (Exception error)
            {
                if (IsDisposed || Disposing) return;
                LimpiarDashboard();
                estado.Text = error is UnauthorizedAccessException ? error.Message
                    : "No se pudo cargar el panel de asistencia. Pulsa Actualizar vista.";
            }
            finally
            {
                CambiarCarga(false);
            }
        }

        private void MostrarReporteDashboard(int indice)
        {
            reporteDashboard = indice;
            tituloGraficoDashboard.Text = indice == 0 ? "Marcaciones por día"
                : indice == 1 ? "Actividad por departamento" : "Novedades por estado";
            subtituloGraficoDashboard.Text = indice == 0 ? "Entradas y salidas en el periodo seleccionado"
                : indice == 1 ? "Volumen de marcaciones por área · principales resultados"
                : "Cantidad de novedades registradas según su estado";
            graficoDashboard.Invalidate();
            tituloDetalleDashboard.Text = indice == 0 ? "Detalle diario"
                : indice == 1 ? "Detalle por departamento" : "Detalle de novedades";
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
                    AgregarColumnaReporte("Unidad o servicio", 140);
                    AgregarColumnaReporte("Personal", 115, "N0");
                    AgregarColumnaReporte("Marcaciones", 124, "N0");
                    if (resumenDashboard == null) return;
                    foreach (var departamento in resumenDashboard.MarcacionesPorDepartamento)
                        tablaDashboard.Rows.Add(departamento.Sede, departamento.Departamento,
                            departamento.EmpleadosConMarcaciones, departamento.TotalMarcaciones);
                    estado.Text = tablaDashboard.Rows.Count == 0 ? "No hay marcaciones en el periodo seleccionado."
                        : "Agrupado por la unidad y sede que figuran en la ficha actual del empleado.";
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
                Name = nombre, HeaderText = nombre, FillWeight = peso,
                MinimumWidth = TextRenderer.MeasureText(nombre, tablaDashboard.ColumnHeadersDefaultCellStyle.Font).Width
                    + tablaDashboard.ColumnHeadersDefaultCellStyle.Padding.Horizontal + (int)(18 * DeviceDpi / 96F),
                SortMode = DataGridViewColumnSortMode.Automatic
            };
            if (formato != null) columna.DefaultCellStyle.Format = formato;
            if (formato == "N0") columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            tablaDashboard.Columns.Add(columna);
        }
    }
}
