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
    internal sealed class FrmAdministrador : Form
    {
        private static readonly Color Fondo = Color.FromArgb(13, 10, 20);
        private static readonly Color FondoLateral = Color.FromArgb(25, 16, 39);
        private static readonly Color TextoSecundario = Color.FromArgb(204, 185, 225);
        private static readonly Color FondoTabla = Color.FromArgb(24, 18, 35);
        private static readonly Color EncabezadoTabla = Color.FromArgb(49, 30, 70);
        private static readonly Color SeleccionTabla = Color.FromArgb(85, 48, 124);
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
        private readonly Label titulo = new Label();
        private readonly Label estado = new Label();
        private bool cargando;

        internal FrmAdministrador(SesionUsuario sesion, IConsultaAdministracion consulta)
        {
            if (sesion == null) throw new ArgumentNullException(nameof(sesion));
            if (!sesion.EsAdministrador) throw new UnauthorizedAccessException();
            if (consulta == null) throw new ArgumentNullException(nameof(consulta));

            Text = "AssistPRO | Administración";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1120, 740);
            MinimumSize = new Size(800, 520);
            BackColor = Fondo;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 11F);

            ConstruirInterfaz(sesion);
            AgregarVista("Asistencia", new[] { "Empleado", "Fecha", "Hora", "Marcación", "Verificación" },
                () => consulta.ConsultarAsistenciaAsync(desde.Value, hasta.Value),
                registro => new object[]
                {
                    NombreEmpleado(registro.Empleado), registro.Fecha.ToString("dd/MM/yyyy"),
                    registro.Hora.ToString("HH:mm:ss"), registro.TipoMarcacion, registro.MetodoVerificacion
                }, usaFechas: true);
            AgregarVista("Empleados", new[] { "ID", "Nombre", "Documento", "Departamento", "Cargo", "Estado" },
                consulta.ListarEmpleadosAsync, empleado => new object[]
                {
                    empleado.IdEmpleado, NombreEmpleado(empleado), empleado.NumeroDocumento,
                    empleado.Departamento?.NombreDepartamento, empleado.Cargo?.NombreCargo, empleado.Estado
                });
            AgregarVista("Usuarios", new[] { "ID", "Usuario", "Empleado", "Rol", "Estado" },
                consulta.ListarUsuariosAsync, cuenta => new object[]
                {
                    cuenta.IdUsuario, cuenta.NombreUsuario, cuenta.Empleado, cuenta.Rol, cuenta.Estado
                });

            modulos.SelectedIndexChanged += async (origen, evento) => await CargarVistaAsync();
            actualizar.Click += async (origen, evento) => await CargarVistaAsync();
            Shown += (origen, evento) => modulos.SelectedIndex = 0;
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
            modulos.AccessibleName = "Módulos";
            modulos.IntegralHeight = false;
            modulos.DrawMode = DrawMode.OwnerDrawFixed;
            modulos.ItemHeight = Font.Height + 28;
            modulos.DrawItem += DibujarModulo;
            panel.Controls.Add(modulos, 0, 1);
            panel.Controls.Add(new Label
            {
                Text = sesion.Nombre, Dock = DockStyle.Fill, AutoEllipsis = true, UseMnemonic = false,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 2);
            panel.Controls.Add(new Label
            {
                Text = sesion.Rol, Dock = DockStyle.Fill, AutoEllipsis = true, UseMnemonic = false,
                ForeColor = TextoSecundario
            }, 0, 3);
            var salir = new AccesoButton
            {
                Text = "Cerrar sesión", Dock = DockStyle.Fill, BackColor = Color.FromArgb(48, 30, 68)
            };
            salir.Click += (origen, evento) => Close();
            panel.Controls.Add(salir, 0, 4);
            return panel;
        }

        private void DibujarModulo(object origen, DrawItemEventArgs evento)
        {
            if (evento.Index < 0) return;
            bool seleccionado = (evento.State & DrawItemState.Selected) != 0;
            using (var fondo = new SolidBrush(FondoLateral))
                evento.Graphics.FillRectangle(fondo, evento.Bounds);
            var rectangulo = Rectangle.Inflate(evento.Bounds, -1, -3);
            if (seleccionado)
            {
                var estadoGrafico = evento.Graphics.Save();
                evento.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var contorno = DibujoAcceso.Redondeado(rectangulo, 12))
                using (var fondo = new LinearGradientBrush(rectangulo, Color.FromArgb(142, 69, 201),
                    Color.FromArgb(99, 39, 158), 90F))
                    evento.Graphics.FillPath(fondo, contorno);
                evento.Graphics.Restore(estadoGrafico);
            }
            TextRenderer.DrawText(evento.Graphics, modulos.Items[evento.Index].ToString(), modulos.Font,
                Rectangle.Inflate(rectangulo, -12, 0), seleccionado ? Color.White : TextoSecundario,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if ((evento.State & DrawItemState.Focus) != 0)
                ControlPaint.DrawFocusRectangle(evento.Graphics, Rectangle.Inflate(rectangulo, -4, -4),
                    Color.White, FondoLateral);
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

            titulo.AutoSize = true;
            titulo.Font = new Font(Font.FontFamily, 26, FontStyle.Bold);
            titulo.Margin = new Padding(0, 0, 0, 16);
            panel.Controls.Add(titulo, 0, 0);

            var acciones = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
            filtros.AutoSize = true;
            filtros.WrapContents = false;
            filtros.Margin = Padding.Empty;
            AgregarFecha("Desde", desde, DateTime.Today.AddDays(-30));
            AgregarFecha("Hasta", hasta, DateTime.Today);
            acciones.Controls.Add(filtros);
            acciones.Controls.Add(actualizar);
            panel.Controls.Add(acciones, 0, 1);

            tabla.Dock = DockStyle.Fill;
            tabla.AccessibleName = "Registros del módulo";
            tabla.BackgroundColor = FondoTabla;
            tabla.BorderStyle = BorderStyle.None;
            tabla.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            tabla.GridColor = Color.FromArgb(55, 38, 73);
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
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            tabla.ColumnHeadersDefaultCellStyle.SelectionBackColor = EncabezadoTabla;
            tabla.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            tabla.ColumnHeadersDefaultCellStyle.Padding = new Padding(8);
            tabla.DefaultCellStyle.BackColor = FondoTabla;
            tabla.DefaultCellStyle.ForeColor = Color.FromArgb(231, 220, 245);
            tabla.DefaultCellStyle.Padding = new Padding(8, 0, 5, 0);
            tabla.DefaultCellStyle.SelectionBackColor = SeleccionTabla;
            tabla.DefaultCellStyle.SelectionForeColor = Color.White;
            tabla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(30, 22, 43);
            tabla.RowTemplate.Height = 42;
            panel.Controls.Add(tabla, 0, 2);

            estado.AutoSize = true;
            estado.AccessibleName = "Estado de la consulta";
            estado.ForeColor = TextoSecundario;
            estado.Margin = new Padding(0, 14, 0, 0);
            panel.Controls.Add(estado, 0, 3);
            return panel;
        }

        private void AgregarFecha(string nombre, DateTimePicker selector, DateTime fecha)
        {
            var campo = new Panel { Size = new Size(150, 62), Margin = new Padding(0, 0, 12, 0) };
            campo.Controls.Add(new Label { Text = nombre, AutoSize = true, ForeColor = TextoSecundario });
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
            Func<T, object[]> valores, bool usaFechas = false)
        {
            modulos.Items.Add(nombre);
            vistas.Add(new Vista
            {
                Columnas = columnas,
                UsaFechas = usaFechas,
                CargarAsync = async () => (await listar()).Select(valores).ToArray()
            });
        }

        private async Task CargarVistaAsync()
        {
            if (cargando || modulos.SelectedIndex < 0) return;

            var vista = vistas[modulos.SelectedIndex];
            titulo.Text = modulos.SelectedItem.ToString();
            filtros.Visible = vista.UsaFechas;
            actualizar.Margin = vista.UsaFechas ? new Padding(0, 22, 0, 0) : Padding.Empty;
            tabla.Columns.Clear();
            foreach (string columna in vista.Columnas)
            {
                int indice = tabla.Columns.Add(columna, columna);
                tabla.Columns[indice].MinimumWidth = 54;
                if (columna == "ID") tabla.Columns[indice].FillWeight = 40;
            }
            if (vista.UsaFechas && desde.Value.Date > hasta.Value.Date)
            {
                estado.Text = "La fecha inicial debe ser anterior o igual a la fecha final.";
                desde.Focus();
                return;
            }

            cargando = true;
            modulos.Enabled = actualizar.Enabled = filtros.Enabled = false;
            estado.Text = "Consultando registros…";
            try
            {
                var filas = await vista.CargarAsync();
                if (IsDisposed || Disposing) return;
                foreach (object[] fila in filas) tabla.Rows.Add(fila);
                estado.Text = filas.Length == 0 ? "No hay registros para mostrar."
                    : filas.Length == 1 ? "1 registro" : $"{filas.Length} registros";
                if (vista.UsaFechas && filas.Length == 500)
                    estado.Text = "Se muestran hasta 500 marcaciones. Reduce el rango para consultar otras.";
            }
            catch (Exception)
            {
                if (!IsDisposed) estado.Text = "No se pudieron cargar los registros. Revisa la conexión e inténtalo de nuevo.";
            }
            finally
            {
                cargando = false;
                if (!IsDisposed) modulos.Enabled = actualizar.Enabled = filtros.Enabled = true;
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
            internal Func<Task<object[][]>> CargarAsync { get; set; }
        }
    }
}
