using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsForms_Presentacion
{
    internal sealed class CampoRegistro
    {
        private readonly bool requerido;
        private readonly bool secreto;

        private CampoRegistro(string etiqueta, Control entrada, bool requerido = true, bool secreto = false)
        {
            Etiqueta = etiqueta;
            Entrada = entrada;
            Entrada.AccessibleName = etiqueta;
            this.requerido = requerido;
            this.secreto = secreto;
        }

        internal string Etiqueta { get; }
        internal Control Entrada { get; }

        internal string ValorTexto
        {
            get
            {
                string texto = secreto ? Entrada.Text : Entrada.Text.Trim();
                return texto.Length > 0 ? texto : null;
            }
        }

        internal int ValorId => ((OpcionRelacion)((ComboBox)Entrada).SelectedItem).Id;
        internal DateTime ValorFecha => ((DateTimePicker)Entrada).Value.Date;
        internal DateTime? ValorFechaOpcional => ((DateTimePicker)Entrada).Checked ? ValorFecha : (DateTime?)null;
        internal TimeSpan ValorHora => ((DateTimePicker)Entrada).Value.TimeOfDay;

        internal string Validar()
        {
            if (Entrada is ComboBox lista && lista.SelectedIndex < 0)
                return lista.Items.Count == 0
                    ? $"No hay opciones para «{Etiqueta}». Registra primero ese dato en su módulo."
                    : $"Selecciona una opción para «{Etiqueta}».";
            if (requerido && Entrada is TextBox && ValorTexto == null)
                return $"Completa «{Etiqueta}».";
            return null;
        }

        internal static CampoRegistro Texto(string etiqueta, int maximo, bool requerido = true)
        {
            return new CampoRegistro(etiqueta, new TextBox { MaxLength = maximo }, requerido);
        }

        internal static CampoRegistro Clave(string etiqueta)
        {
            return new CampoRegistro(etiqueta, new TextBox
            {
                MaxLength = 1024,
                UseSystemPasswordChar = true
            }, secreto: true);
        }

        internal static CampoRegistro Fecha(string etiqueta, bool opcional = false)
        {
            var selector = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                MaxDate = new DateTime(9998, 12, 30),
                ShowCheckBox = opcional,
                Value = DateTime.Today,
                Checked = !opcional
            };
            return new CampoRegistro(opcional ? etiqueta + " (opcional)" : etiqueta, selector, !opcional);
        }

        internal static CampoRegistro Hora(string etiqueta)
        {
            DateTime ahora = DateTime.Now;
            return new CampoRegistro(etiqueta, new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm",
                ShowUpDown = true,
                Value = ahora.Date.AddHours(ahora.Hour).AddMinutes(ahora.Minute)
            });
        }

        internal static CampoRegistro Elegir<T>(string etiqueta, IEnumerable<T> opciones,
            Func<T, string> mostrar, Func<T, int> id)
        {
            var lista = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (T opcion in opciones)
                lista.Items.Add(new OpcionRelacion(id(opcion), mostrar(opcion)));
            return new CampoRegistro(etiqueta, lista);
        }

        internal static CampoRegistro Opciones(string etiqueta, params string[] opciones)
        {
            var lista = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            lista.Items.AddRange(opciones);
            return new CampoRegistro(etiqueta, lista);
        }

        private sealed class OpcionRelacion
        {
            private readonly string texto;

            internal OpcionRelacion(int id, string texto)
            {
                Id = id;
                this.texto = texto;
            }

            internal int Id { get; }
            public override string ToString() => texto;
        }
    }

    internal sealed class EditorRegistro : Form
    {
        private static readonly Color Fondo = Color.FromArgb(25, 16, 39);
        private static readonly Color FondoEntrada = Color.FromArgb(17, 12, 26);
        private readonly Label error = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(255, 193, 207),
            AccessibleName = "Validación del registro",
            TextAlign = ContentAlignment.MiddleLeft
        };

        internal EditorRegistro(string titulo, params CampoRegistro[] campos)
        {
            Text = titulo;
            Font = new Font("Segoe UI", 11);
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Fondo;
            ForeColor = Color.White;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            MinimumSize = new Size(360, 300);
            int altoDisponible = Screen.FromPoint(Cursor.Position).WorkingArea.Height - 64;
            ClientSize = new Size(494, Math.Min(140 + campos.Length * 60, Math.Max(260, Math.Min(620, altoDisponible))));

            var cuerpo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(22, 10, 22, 14)
            };
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
            cuerpo.Controls.Add(new Label
            {
                Text = titulo, Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 19, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false
            }, 0, 0);

            var desplazamiento = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var formulario = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1, RowCount = campos.Length * 2, Margin = Padding.Empty
            };
            formulario.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int indice = 0; indice < campos.Length; indice++)
            {
                CampoRegistro campo = campos[indice];
                formulario.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                formulario.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                formulario.Controls.Add(new Label
                {
                    Text = campo.Etiqueta, AutoSize = true, UseMnemonic = false,
                    ForeColor = Color.FromArgb(204, 185, 225), Margin = new Padding(0, 3, 0, 4)
                }, 0, indice * 2);
                Control entrada = campo.Entrada;
                entrada.Dock = DockStyle.Fill;
                entrada.Margin = new Padding(0, 0, 0, 10);
                entrada.TabIndex = indice;
                entrada.BackColor = FondoEntrada;
                entrada.ForeColor = Color.White;
                formulario.Controls.Add(entrada, 0, indice * 2 + 1);
            }
            desplazamiento.Controls.Add(formulario);
            cuerpo.Controls.Add(desplazamiento, 0, 1);

            var pie = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pie.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            pie.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            pie.Controls.Add(error, 0, 0);
            var botones = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Margin = Padding.Empty
            };
            var guardar = new AccesoButton
            {
                Text = "Guardar", Principal = true, Size = new Size(114, 40),
                BackColor = Color.FromArgb(139, 80, 224), Margin = Padding.Empty
            };
            guardar.Click += (origen, evento) =>
            {
                foreach (CampoRegistro campo in campos)
                {
                    string mensaje = campo.Validar();
                    if (mensaje == null) continue;
                    error.Text = mensaje;
                    desplazamiento.ScrollControlIntoView(campo.Entrada);
                    campo.Entrada.Focus();
                    return;
                }
                DialogResult = DialogResult.OK;
            };
            var cancelar = new AccesoButton
            {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                CausesValidation = false, Size = new Size(114, 40),
                BackColor = Color.FromArgb(48, 30, 68), Margin = new Padding(0, 0, 8, 0)
            };
            botones.Controls.Add(guardar);
            botones.Controls.Add(cancelar);
            pie.Controls.Add(botones, 0, 1);
            cuerpo.Controls.Add(pie, 0, 2);
            Controls.Add(cuerpo);

            AcceptButton = guardar;
            CancelButton = cancelar;
            if (campos.Length > 0)
                Shown += (origen, evento) => campos[0].Entrada.Focus();
        }
    }
}
