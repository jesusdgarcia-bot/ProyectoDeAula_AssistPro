using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Logica;

namespace WindowsForms_Presentacion
{
    internal sealed class CampoRegistro
    {
        private readonly bool requerido;
        private readonly bool secreto;
        private string textoOriginal;
        private DateTime? fechaOriginal;

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
        internal bool EsSecreto => secreto;

        internal string ValorTexto
        {
            get
            {
                if (!secreto && textoOriginal != null && Entrada.Text == textoOriginal)
                    return textoOriginal.Length > 0 ? textoOriginal : null;
                string texto = secreto ? Entrada.Text : Entrada.Text.Trim();
                return texto.Length > 0 ? texto : null;
            }
        }

        internal int ValorId => ((OpcionRelacion)((ComboBox)Entrada).SelectedItem).Id;
        internal DateTime ValorFecha
        {
            get
            {
                DateTime fecha = ((DateTimePicker)Entrada).Value.Date;
                return fechaOriginal.HasValue && fechaOriginal.Value.Date == fecha ? fechaOriginal.Value : fecha;
            }
        }
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

        internal static CampoRegistro Clave(string etiqueta, bool requerido = true)
        {
            return new CampoRegistro(etiqueta, new TextBox
            {
                MaxLength = 1024,
                UseSystemPasswordChar = true
            }, requerido, secreto: true);
        }

        internal static CampoRegistro Fecha(string etiqueta, bool opcional = false)
        {
            var selector = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                MaxDate = new DateTime(9998, 12, 30),
                ShowCheckBox = opcional,
                Value = PlanificacionTurnos.AhoraColombia.Date,
                Checked = !opcional
            };
            return new CampoRegistro(opcional ? etiqueta + " (opcional)" : etiqueta, selector, !opcional);
        }

        internal static CampoRegistro Hora(string etiqueta)
        {
            DateTime ahora = PlanificacionTurnos.AhoraColombia;
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
            var lista = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 12 };
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

        internal CampoRegistro ConTexto(string texto)
        {
            textoOriginal = texto;
            Entrada.Text = texto ?? string.Empty;
            return this;
        }

        internal CampoRegistro ConFecha(DateTime? fecha)
        {
            fechaOriginal = fecha;
            var selector = (DateTimePicker)Entrada;
            if (!fecha.HasValue)
            {
                if (selector.ShowCheckBox) selector.Checked = false;
                return this;
            }
            if (fecha.Value.Date < selector.MinDate.Date || fecha.Value.Date > selector.MaxDate.Date)
                throw new InvalidOperationException($"La fecha guardada en «{Etiqueta}» está fuera del intervalo que admite el selector.");
            selector.Value = fecha.Value.Date;
            selector.Checked = true;
            return this;
        }

        internal CampoRegistro ConHora(DateTime? hora)
        {
            if (hora.HasValue) ((DateTimePicker)Entrada).Value = PlanificacionTurnos.AhoraColombia.Date.Add(hora.Value.TimeOfDay);
            return this;
        }

        internal CampoRegistro SeleccionarId(int? id)
        {
            if (!id.HasValue) return this;
            var lista = (ComboBox)Entrada;
            for (int indice = 0; indice < lista.Items.Count; indice++)
            {
                if (((OpcionRelacion)lista.Items[indice]).Id != id.Value) continue;
                lista.SelectedIndex = indice;
                return this;
            }
            throw new InvalidOperationException($"La referencia guardada en «{Etiqueta}» ya no está disponible. Actualiza la vista.");
        }

        internal CampoRegistro SeleccionarTexto(string texto)
        {
            if (texto == null) return this;
            textoOriginal = texto;
            var lista = (ComboBox)Entrada;
            int indice = lista.Items.IndexOf(texto);
            if (indice < 0) indice = lista.Items.Add(texto);
            lista.SelectedIndex = indice;
            return this;
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
        private readonly Label error = new Label
        {
            AutoSize = true, Visible = false,
            Margin = new Padding(0, 4, 0, 8),
            AccessibleName = "Validación del registro",
            TextAlign = ContentAlignment.MiddleLeft
        };

        internal EditorRegistro(string titulo, params CampoRegistro[] campos)
            : this(titulo, "Guardar", campos)
        {
        }

        internal EditorRegistro(string titulo, string textoAccion, params CampoRegistro[] campos)
        {
            Text = titulo;
            Font = new Font("Segoe UI", 11);
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = TemaAplicacion.Actual.Superficie;
            ForeColor = TemaAplicacion.Actual.Texto;
            TemaAplicacion.DefinirRol(this, RolTema.Superficie);
            TemaAplicacion.DefinirRol(error, RolTema.Error);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            MinimumSize = new Size(374, 300);
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            float escalaPantalla = DeviceDpi / 96F;
            bool formularioGrande = campos.Length >= 6;
            int anchoDisponible = Math.Max(354, (int)(area.Width / escalaPantalla) - 48);
            int altoDisponible = Math.Max(264, (int)(area.Height / escalaPantalla) - 64);
            int ancho = Math.Min(formularioGrande ? 674 : 540, anchoDisponible);
            int filas = formularioGrande && ancho >= 610 ? (campos.Length + 1) / 2 : campos.Length;
            ClientSize = new Size(ancho, Math.Min(altoDisponible, Math.Max(264, Math.Min(640, 126 + filas * 68))));

            var cuerpo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(22, 10, 22, 14)
            };
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cuerpo.Controls.Add(new Label
            {
                Text = titulo, Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 17, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false, Margin = Padding.Empty
            }, 0, 0);

            var desplazamiento = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
            var formulario = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty
            };
            var grupos = new Control[campos.Length];
            CampoRegistro campoConError = null;
            for (int indice = 0; indice < campos.Length; indice++)
            {
                var campo = campos[indice];
                grupos[indice] = CrearGrupoCampo(campo, indice);
                campo.Entrada.TextChanged += (origen, evento) =>
                {
                    if (campoConError != campo || campo.Validar() != null) return;
                    campoConError = null;
                    error.Visible = false;
                };
                formulario.Controls.Add(grupos[indice]);
            }
            desplazamiento.SizeChanged += (origen, evento) => AjustarCampos();
            void AjustarCampos()
            {
                float escala = DeviceDpi / 96F;
                int columnas = formularioGrande && desplazamiento.ClientSize.Width >= 566 * escala ? 2 : 1;
                if (formulario.ColumnCount == columnas) return;
                formulario.SuspendLayout();
                formulario.ColumnCount = columnas;
                formulario.RowCount = (grupos.Length + columnas - 1) / columnas;
                formulario.ColumnStyles.Clear();
                formulario.RowStyles.Clear();
                for (int columna = 0; columna < columnas; columna++)
                    formulario.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columnas));
                for (int fila = 0; fila < formulario.RowCount; fila++) formulario.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                for (int indice = 0; indice < grupos.Length; indice++)
                {
                    int columna = indice % columnas;
                    formulario.SetCellPosition(grupos[indice], new TableLayoutPanelCellPosition(columna, indice / columnas));
                    grupos[indice].Margin = new Padding(0, 0, columna == columnas - 1 ? 0 : (int)(14 * escala), 0);
                }
                formulario.ResumeLayout(true);
            }
            AjustarCampos();
            desplazamiento.Controls.Add(formulario);
            cuerpo.Controls.Add(desplazamiento, 0, 1);

            var pie = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 8, 0, 0)
            };
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pie.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pie.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            pie.SizeChanged += (origen, evento) => error.MaximumSize = new Size(Math.Max(1, pie.Width), 0);
            pie.Controls.Add(error, 0, 0);
            var botones = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Margin = Padding.Empty
            };
            var guardar = new AccesoButton
            {
                Text = textoAccion, Principal = true, Plano = true, Size = new Size(114, 40),
                Margin = Padding.Empty
            };
            guardar.Click += (origen, evento) =>
            {
                for (int indice = 0; indice < campos.Length; indice++)
                {
                    var campo = campos[indice];
                    string mensaje = campo.Validar();
                    if (mensaje == null) continue;
                    campoConError = campo;
                    error.Text = mensaje;
                    error.Visible = true;
                    desplazamiento.ScrollControlIntoView(grupos[indice]);
                    campo.Entrada.Focus();
                    return;
                }
                DialogResult = DialogResult.OK;
            };
            var cancelar = new AccesoButton
            {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                CausesValidation = false, Plano = true, Size = new Size(114, 40),
                Margin = new Padding(0, 0, 8, 0)
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
            TemaAplicacion.Registrar(this);
        }

        private static Control CrearGrupoCampo(CampoRegistro campo, int indice)
        {
            var grupo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 2,
                TabIndex = indice, Padding = new Padding(0, 0, 0, 11)
            };
            grupo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grupo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grupo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var etiqueta = new Label
            {
                Text = campo.Etiqueta, AutoSize = true, UseMnemonic = false,
                Margin = new Padding(0, 2, 0, 4), Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            TemaAplicacion.DefinirRol(etiqueta, RolTema.TextoSecundario);
            grupo.Controls.Add(etiqueta, 0, 0);
            Control entrada = campo.Entrada;
            entrada.Dock = DockStyle.Fill;
            entrada.Margin = Padding.Empty;
            TemaAplicacion.DefinirRol(entrada, RolTema.Entrada);
            if (entrada is TextBox texto)
            {
                texto.BorderStyle = BorderStyle.None;
                var borde = new CampoAcceso
                {
                    Dock = DockStyle.Top, Height = 36, Padding = new Padding(11, 7, campo.EsSecreto ? 6 : 11, 5),
                    Margin = Padding.Empty
                };
                borde.Controls.Add(texto);
                if (campo.EsSecreto)
                {
                    var mostrar = new OjoAcceso { Dock = DockStyle.Right, Width = 33 };
                    mostrar.CheckedChanged += (origen, evento) => texto.UseSystemPasswordChar = !mostrar.Checked;
                    borde.Controls.Add(mostrar);
                }
                entrada = borde;
            }
            grupo.Controls.Add(entrada, 0, 1);
            return grupo;
        }
    }
}
