using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace WindowsForms_Presentacion
{
    internal enum RolTema
    {
        Normal,
        Lateral,
        Superficie,
        Entrada,
        TextoSecundario,
        Error,
        AcentoMorado,
        AcentoAzul,
        AcentoVerde,
        AcentoNaranja,
        SelectorTema
    }

    internal sealed class PaletaTema
    {
        internal Color Fondo { get; set; }
        internal Color Lateral { get; set; }
        internal Color Superficie { get; set; }
        internal Color Entrada { get; set; }
        internal Color Texto { get; set; }
        internal Color TextoSecundario { get; set; }
        internal Color Error { get; set; }
        internal Color Enlace { get; set; }
        internal Color Borde { get; set; }
        internal Color BordeFoco { get; set; }
        internal Color EncabezadoTabla { get; set; }
        internal Color TextoTabla { get; set; }
        internal Color AlternanciaTabla { get; set; }
        internal Color LineaTabla { get; set; }
        internal Color SeleccionTabla { get; set; }
        internal Color TextoSeleccionTabla { get; set; }
        internal Color PrimarioInicio { get; set; }
        internal Color PrimarioFin { get; set; }
        internal Color TextoPrimario { get; set; }
        internal Color Icono { get; set; }
        internal Color FondoIcono { get; set; }
        internal Color AcentoMorado { get; set; }
        internal Color AcentoAzul { get; set; }
        internal Color AcentoVerde { get; set; }
        internal Color AcentoNaranja { get; set; }
    }

    internal static class TemaAplicacion
    {
        private static readonly PaletaTema Oscuro = new PaletaTema
        {
            Fondo = Color.FromArgb(13, 10, 20),
            Lateral = Color.FromArgb(25, 16, 39),
            Superficie = Color.FromArgb(24, 18, 35),
            Entrada = Color.FromArgb(17, 12, 26),
            Texto = Color.FromArgb(244, 237, 249),
            TextoSecundario = Color.FromArgb(204, 185, 225),
            Error = Color.FromArgb(255, 178, 190),
            Enlace = Color.FromArgb(201, 165, 255),
            Borde = Color.FromArgb(112, 80, 149),
            BordeFoco = Color.FromArgb(224, 204, 255),
            EncabezadoTabla = Color.FromArgb(49, 30, 70),
            TextoTabla = Color.FromArgb(231, 220, 245),
            AlternanciaTabla = Color.FromArgb(30, 22, 43),
            LineaTabla = Color.FromArgb(55, 38, 73),
            SeleccionTabla = Color.FromArgb(85, 48, 124),
            TextoSeleccionTabla = Color.White,
            PrimarioInicio = Color.FromArgb(154, 62, 221),
            PrimarioFin = Color.FromArgb(107, 26, 188),
            TextoPrimario = Color.White,
            Icono = Color.FromArgb(221, 192, 255),
            FondoIcono = Color.FromArgb(83, 37, 127),
            AcentoMorado = Color.FromArgb(190, 148, 247),
            AcentoAzul = Color.FromArgb(143, 178, 255),
            AcentoVerde = Color.FromArgb(121, 220, 199),
            AcentoNaranja = Color.FromArgb(248, 185, 131)
        };

        private static readonly PaletaTema Claro = new PaletaTema
        {
            Fondo = Color.FromArgb(246, 243, 250),
            Lateral = Color.FromArgb(235, 228, 244),
            Superficie = Color.White,
            Entrada = Color.White,
            Texto = Color.FromArgb(36, 27, 50),
            TextoSecundario = Color.FromArgb(98, 83, 111),
            Error = Color.FromArgb(172, 41, 66),
            Enlace = Color.FromArgb(99, 49, 162),
            Borde = Color.FromArgb(143, 120, 166),
            BordeFoco = Color.FromArgb(116, 59, 176),
            EncabezadoTabla = Color.FromArgb(231, 221, 240),
            TextoTabla = Color.FromArgb(48, 35, 60),
            AlternanciaTabla = Color.FromArgb(248, 245, 252),
            LineaTabla = Color.FromArgb(221, 210, 232),
            SeleccionTabla = Color.FromArgb(222, 202, 244),
            TextoSeleccionTabla = Color.FromArgb(44, 20, 68),
            PrimarioInicio = Color.FromArgb(137, 58, 201),
            PrimarioFin = Color.FromArgb(99, 35, 166),
            TextoPrimario = Color.White,
            Icono = Color.FromArgb(105, 49, 154),
            FondoIcono = Color.FromArgb(233, 216, 249),
            AcentoMorado = Color.FromArgb(114, 51, 166),
            AcentoAzul = Color.FromArgb(41, 87, 164),
            AcentoVerde = Color.FromArgb(24, 112, 89),
            AcentoNaranja = Color.FromArgb(159, 77, 19)
        };

        private static readonly ConditionalWeakTable<Control, EstiloControl> estilos =
            new ConditionalWeakTable<Control, EstiloControl>();
        private static event EventHandler Cambiado;

        internal static bool EsOscuro { get; private set; } = true;
        internal static PaletaTema Actual => EsOscuro ? Oscuro : Claro;

        internal static void DefinirRol(Control control, RolTema rol)
        {
            estilos.GetOrCreateValue(control).Rol = rol;
        }

        internal static SelectorDeslizante CrearSelectorTema()
        {
            var selector = new SelectorDeslizante
            {
                OpcionIzquierda = "Modo oscuro",
                OpcionDerecha = "Modo claro",
                IconoIzquierdo = "luna",
                IconoDerecho = "sol",
                Checked = !EsOscuro,
                AccessibleName = "Tema de la aplicación",
                CausesValidation = false,
                Size = new Size(88, 32),
                Anchor = AnchorStyles.Right,
                Margin = Padding.Empty
            };
            DefinirRol(selector, RolTema.SelectorTema);
            selector.CheckedChanged += (origen, evento) =>
            {
                bool oscuroSeleccionado = !selector.Checked;
                if (EsOscuro == oscuroSeleccionado) return;
                EsOscuro = oscuroSeleccionado;
                Cambiado?.Invoke(null, EventArgs.Empty);
            };
            return selector;
        }

        internal static void Registrar(Form formulario)
        {
            EventHandler aplicar = (origen, evento) => Aplicar(formulario);
            Cambiado += aplicar;
            formulario.Disposed += (origen, evento) => Cambiado -= aplicar;
            Aplicar(formulario);
        }

        private static void Aplicar(Form formulario)
        {
            if (formulario.IsDisposed || formulario.Disposing) return;
            formulario.SuspendLayout();
            try
            {
                AplicarControl(formulario);
                formulario.Invalidate(true);
            }
            finally
            {
                formulario.ResumeLayout(true);
            }
        }

        private static void AplicarControl(Control control)
        {
            var paleta = Actual;
            control.ForeColor = paleta.Texto;
            control.BackColor = control.Parent?.BackColor ?? paleta.Fondo;

            if (control is DataGridView tabla)
                AplicarTabla(tabla);
            else if (control is TextBoxBase || control is ComboBox || control is ListBox || control is DateTimePicker)
                control.BackColor = paleta.Entrada;
            else if (control is AccesoButton || control is SelectorDeslizante)
                control.BackColor = paleta.Superficie;
            else if (control is Label || control is CheckBox || control is MarcaAcceso)
                control.BackColor = Color.Transparent;

            if (control is ComboBox lista)
                lista.FlatStyle = FlatStyle.Flat;

            if (control is RadioButton opcion)
            {
                opcion.UseVisualStyleBackColor = false;
                opcion.BackColor = paleta.Superficie;
                opcion.FlatAppearance.BorderColor = paleta.Borde;
                opcion.FlatAppearance.CheckedBackColor = paleta.SeleccionTabla;
                opcion.FlatAppearance.MouseOverBackColor = paleta.AlternanciaTabla;
                opcion.FlatAppearance.MouseDownBackColor = paleta.SeleccionTabla;
            }

            if (control is LinkLabel enlace)
            {
                enlace.LinkColor = paleta.Enlace;
                enlace.VisitedLinkColor = paleta.Enlace;
                enlace.ActiveLinkColor = paleta.Texto;
            }

            if (control is DateTimePicker fecha)
            {
                fecha.CalendarMonthBackground = paleta.Entrada;
                fecha.CalendarForeColor = paleta.Texto;
                fecha.CalendarTitleBackColor = paleta.EncabezadoTabla;
                fecha.CalendarTitleForeColor = paleta.Texto;
                fecha.CalendarTrailingForeColor = paleta.TextoSecundario;
            }

            if (estilos.TryGetValue(control, out var estilo))
                AplicarRol(control, estilo.Rol);

            foreach (Control hijo in control.Controls)
                AplicarControl(hijo);
        }

        private static void AplicarRol(Control control, RolTema rol)
        {
            var paleta = Actual;
            switch (rol)
            {
                case RolTema.Lateral:
                    control.BackColor = paleta.Lateral;
                    break;
                case RolTema.Superficie:
                    control.BackColor = paleta.Superficie;
                    break;
                case RolTema.Entrada:
                    control.BackColor = paleta.Entrada;
                    break;
                case RolTema.TextoSecundario:
                    control.ForeColor = paleta.TextoSecundario;
                    break;
                case RolTema.Error:
                    control.ForeColor = paleta.Error;
                    break;
                case RolTema.AcentoMorado:
                    AplicarAcento(control, paleta.AcentoMorado);
                    break;
                case RolTema.AcentoAzul:
                    AplicarAcento(control, paleta.AcentoAzul);
                    break;
                case RolTema.AcentoVerde:
                    AplicarAcento(control, paleta.AcentoVerde);
                    break;
                case RolTema.AcentoNaranja:
                    AplicarAcento(control, paleta.AcentoNaranja);
                    break;
                case RolTema.SelectorTema:
                    ((SelectorDeslizante)control).Checked = !EsOscuro;
                    control.AccessibleDescription = EsOscuro
                        ? "Modo oscuro seleccionado. Luna a la izquierda y sol a la derecha."
                        : "Modo claro seleccionado. Luna a la izquierda y sol a la derecha.";
                    break;
            }
        }

        private static void AplicarAcento(Control control, Color acento)
        {
            if (control is Panel) control.BackColor = acento;
            else control.ForeColor = acento;
        }

        private static void AplicarTabla(DataGridView tabla)
        {
            var paleta = Actual;
            tabla.BackgroundColor = paleta.Superficie;
            tabla.GridColor = paleta.LineaTabla;
            tabla.EnableHeadersVisualStyles = false;
            tabla.ColumnHeadersDefaultCellStyle.BackColor = paleta.EncabezadoTabla;
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = paleta.Texto;
            tabla.ColumnHeadersDefaultCellStyle.SelectionBackColor = paleta.EncabezadoTabla;
            tabla.ColumnHeadersDefaultCellStyle.SelectionForeColor = paleta.Texto;
            tabla.DefaultCellStyle.BackColor = paleta.Superficie;
            tabla.DefaultCellStyle.ForeColor = paleta.TextoTabla;
            tabla.DefaultCellStyle.SelectionBackColor = paleta.SeleccionTabla;
            tabla.DefaultCellStyle.SelectionForeColor = paleta.TextoSeleccionTabla;
            tabla.AlternatingRowsDefaultCellStyle.BackColor = paleta.AlternanciaTabla;
        }

        private sealed class EstiloControl
        {
            public EstiloControl() { }
            internal RolTema Rol { get; set; }
        }
    }
}
