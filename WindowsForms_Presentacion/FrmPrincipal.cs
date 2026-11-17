using System;
using System.Drawing;
using System.Windows.Forms;
using Logica;

namespace WindowsForms_Presentacion
{
    public sealed class FrmPrincipal : Form
    {
        private readonly SesionUsuario sesion;
        private readonly IConsultaAsistencia consulta;
        private readonly DateTimePicker desde = new DateTimePicker();
        private readonly DateTimePicker hasta = new DateTimePicker();
        private readonly DataGridView registros = new DataGridView();
        private readonly Label estado = new Label();
        private readonly AccesoButton buscar = new AccesoButton();
        private bool cargando;

        public FrmPrincipal(SesionUsuario sesion, IConsultaAsistencia consulta)
        {
            this.sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
            this.consulta = consulta ?? throw new ArgumentNullException(nameof(consulta));
            Text = "AssistPRO | Mi asistencia";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1120, 740);
            MinimumSize = new Size(760, 580);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 11);
            BackColor = Color.FromArgb(13, 10, 20);
            ForeColor = Color.White;
            CrearInterfaz();
            Shown += async (sender, args) => await CargarAsync();
        }

        private void CrearInterfaz()
        {
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var lateral = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                Padding = new Padding(20, 28, 20, 20), BackColor = Color.FromArgb(25, 16, 39)
            };
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            lateral.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            lateral.Controls.Add(Etiqueta("AssistPRO", 23, FontStyle.Bold), 0, 0);
            lateral.Controls.Add(new AccesoButton { Text = "Mi asistencia", Principal = true, Dock = DockStyle.Fill, TabStop = false }, 0, 1);
            var identidad = Etiqueta(sesion.Nombre + Environment.NewLine + sesion.Rol, 10);
            lateral.Controls.Add(identidad, 0, 3);
            var salir = new AccesoButton { Text = "Cerrar sesión", Dock = DockStyle.Fill, BackColor = Color.FromArgb(48, 30, 68) };
            salir.Click += (sender, args) => Close();
            lateral.Controls.Add(salir, 0, 4);

            var contenido = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(28) };
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 114));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            contenido.Controls.Add(Etiqueta("Mi asistencia", 26, FontStyle.Bold), 0, 0);
            contenido.Controls.Add(Etiqueta("Consulta tus entradas y salidas registradas.", 11), 0, 1);

            var filtros = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            desde.Value = DateTime.Today.AddDays(-30);
            hasta.Value = DateTime.Today;
            filtros.Controls.Add(Filtro("Desde", desde));
            filtros.Controls.Add(Filtro("Hasta", hasta));
            buscar.Text = "Consultar";
            buscar.Principal = true;
            buscar.Size = new Size(132, 40);
            buscar.Margin = new Padding(0, 25, 0, 0);
            buscar.Click += async (sender, args) => await CargarAsync();
            filtros.Controls.Add(buscar);
            contenido.Controls.Add(filtros, 0, 2);

            registros.Dock = DockStyle.Fill;
            registros.ReadOnly = true;
            registros.AllowUserToAddRows = false;
            registros.AllowUserToDeleteRows = false;
            registros.AutoGenerateColumns = false;
            registros.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            registros.RowHeadersVisible = false;
            registros.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            registros.MultiSelect = false;
            registros.BackgroundColor = Color.FromArgb(24, 18, 35);
            registros.BorderStyle = BorderStyle.None;
            registros.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            registros.GridColor = Color.FromArgb(55, 38, 73);
            registros.EnableHeadersVisualStyles = false;
            registros.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(49, 30, 70), ForeColor = Color.White, Padding = new Padding(8) };
            registros.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(24, 18, 35), ForeColor = Color.FromArgb(231, 220, 245), SelectionBackColor = Color.FromArgb(85, 48, 124), SelectionForeColor = Color.White, Padding = new Padding(8) };
            registros.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(30, 22, 43);
            registros.RowTemplate.Height = 42;
            registros.AccessibleName = "Marcaciones del empleado";
            Columna("Fecha", "Fecha", "dd/MM/yyyy");
            Columna("Hora", "Hora", "HH:mm:ss");
            Columna("Marcación", "TipoMarcacion", "");
            Columna("Verificación", "MetodoVerificacion", "");
            contenido.Controls.Add(registros, 0, 3);
            estado.Dock = DockStyle.Fill;
            estado.TextAlign = ContentAlignment.MiddleLeft;
            estado.ForeColor = Color.FromArgb(204, 185, 225);
            estado.AccessibleName = "Estado de la consulta";
            contenido.Controls.Add(estado, 0, 4);
            raiz.Controls.Add(lateral, 0, 0);
            raiz.Controls.Add(contenido, 1, 0);
            Controls.Add(raiz);
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

        private async System.Threading.Tasks.Task CargarAsync()
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
            desde.Enabled = false;
            hasta.Enabled = false;
            registros.DataSource = null;
            estado.Text = "Consultando marcaciones...";
            try
            {
                var datos = await consulta.ConsultarAsync(sesion.IdEmpleado, desde.Value, hasta.Value);
                if (IsDisposed) return;
                registros.DataSource = datos;
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
                if (!IsDisposed) { buscar.Enabled = true; desde.Enabled = true; hasta.Enabled = true; }
            }
        }
    }
}
