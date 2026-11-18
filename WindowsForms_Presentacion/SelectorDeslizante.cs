using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsForms_Presentacion
{
    internal sealed class SelectorDeslizante : CheckBox
    {
        private readonly Timer animacion = new Timer { Interval = 15 };
        private string opcionIzquierda = string.Empty;
        private string opcionDerecha = string.Empty;
        private string iconoIzquierdo = string.Empty;
        private string iconoDerecho = string.Empty;
        private float posicion;
        private float posicionInicial;
        private int inicioArrastre;
        private bool presionado;
        private bool arrastrando;
        private bool suprimirClick;

        internal SelectorDeslizante()
        {
            AutoCheck = false;
            AutoSize = false;
            Appearance = Appearance.Button;
            FlatStyle = FlatStyle.Flat;
            Cursor = Cursors.Hand;
            Size = new Size(132, 40);
            TabStop = true;
            AccessibleRole = AccessibleRole.CheckButton;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            animacion.Tick += AvanzarAnimacion;
        }

        public string OpcionIzquierda
        {
            get => opcionIzquierda;
            set { opcionIzquierda = value ?? string.Empty; ActualizarDescripcion(); Invalidate(); }
        }

        public string OpcionDerecha
        {
            get => opcionDerecha;
            set { opcionDerecha = value ?? string.Empty; ActualizarDescripcion(); Invalidate(); }
        }

        public string IconoIzquierdo
        {
            get => iconoIzquierdo;
            set { iconoIzquierdo = value ?? string.Empty; Invalidate(); }
        }

        public string IconoDerecho
        {
            get => iconoDerecho;
            set { iconoDerecho = value ?? string.Empty; Invalidate(); }
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            ActualizarDescripcion();
            AnimarSeleccion();
            base.OnCheckedChanged(e);
        }

        protected override void OnClick(EventArgs e)
        {
            if (!suprimirClick && Enabled) Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                Focus();
                presionado = true;
                arrastrando = false;
                inicioArrastre = e.X;
                posicionInicial = posicion;
                animacion.Stop();
                Capture = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (presionado)
            {
                int umbral = Math.Max(2, SystemInformation.DragSize.Width / 2);
                if (Math.Abs(e.X - inicioArrastre) >= umbral) arrastrando = true;
                if (arrastrando)
                {
                    float recorrido = Math.Max(1F, (Width - 3F) / 2F);
                    posicion = Math.Max(0F, Math.Min(1F,
                        posicionInicial + (e.X - inicioArrastre) / recorrido));
                    Invalidate();
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                base.OnMouseUp(e);
                return;
            }

            suprimirClick = true;
            try
            {
                if (presionado && Enabled)
                {
                    bool nuevaSeleccion = Checked;
                    if (arrastrando) nuevaSeleccion = posicion >= .5F;
                    else if (ClientRectangle.Contains(e.Location)) nuevaSeleccion = e.X >= Width / 2;
                    presionado = false;
                    arrastrando = false;
                    Checked = nuevaSeleccion;
                    AnimarSeleccion();
                }
                base.OnMouseUp(e);
            }
            finally
            {
                presionado = false;
                arrastrando = false;
                Capture = false;
                suprimirClick = false;
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            if (presionado && !Capture)
            {
                presionado = false;
                arrastrando = false;
                AnimarSeleccion();
            }
            base.OnMouseCaptureChanged(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys tecla = keyData & Keys.KeyCode;
            return tecla == Keys.Left || tecla == Keys.Right || tecla == Keys.Home || tecla == Keys.End
                || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Home ||
                e.KeyCode == Keys.Right || e.KeyCode == Keys.End)
            {
                Checked = e.KeyCode == Keys.Right || e.KeyCode == Keys.End;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            if (presionado) Capture = false;
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            if (!Enabled && presionado) Capture = false;
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) FijarSeleccion();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            FijarSeleccion();
            base.OnHandleDestroyed(e);
        }

        private void ActualizarDescripcion()
        {
            AccessibleDescription = Checked ? opcionDerecha : opcionIzquierda;
        }

        private void AnimarSeleccion()
        {
            if (!IsHandleCreated || !Visible)
            {
                FijarSeleccion();
                return;
            }
            animacion.Start();
            Invalidate();
        }

        private void FijarSeleccion()
        {
            animacion.Stop();
            posicion = Checked ? 1F : 0F;
            Invalidate();
        }

        private void AvanzarAnimacion(object origen, EventArgs evento)
        {
            float destino = Checked ? 1F : 0F;
            float distancia = destino - posicion;
            if (Math.Abs(distancia) < .01F)
            {
                posicion = destino;
                animacion.Stop();
            }
            else posicion += distancia * .35F;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var paleta = TemaAplicacion.Actual;
            e.Graphics.Clear(Parent?.BackColor ?? paleta.Fondo);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float escala = DeviceDpi / 96F;
            float margen = 3F * escala;
            var pista = new RectangleF(1F, 1F, Width - 3F, Height - 3F);
            if (pista.Width <= margen * 2F || pista.Height <= margen * 2F) return;

            using (var contorno = DibujoAcceso.Redondeado(pista, pista.Height / 2F))
            using (var fondo = new SolidBrush(paleta.Entrada))
            using (var borde = new Pen(Focused ? paleta.BordeFoco : paleta.Borde, Focused ? 2F : 1F))
            {
                e.Graphics.FillPath(fondo, contorno);
                e.Graphics.DrawPath(borde, contorno);
            }

            float mitad = pista.Width / 2F;
            var seleccion = new RectangleF(pista.Left + margen + posicion * mitad,
                pista.Top + margen, mitad - margen * 2F, pista.Height - margen * 2F);
            using (var indicador = DibujoAcceso.Redondeado(seleccion, seleccion.Height / 2F))
            using (var fondo = new LinearGradientBrush(seleccion,
                Enabled ? paleta.PrimarioInicio : paleta.Borde,
                Enabled ? paleta.PrimarioFin : paleta.Borde, 90F))
            {
                e.Graphics.FillPath(fondo, indicador);
                DibujarOpciones(e.Graphics, pista, paleta.TextoSecundario, escala);
                var estadoGrafico = e.Graphics.Save();
                e.Graphics.SetClip(indicador, CombineMode.Intersect);
                DibujarOpciones(e.Graphics, pista, paleta.TextoPrimario, escala);
                e.Graphics.Restore(estadoGrafico);
            }
        }

        private void DibujarOpciones(Graphics dibujo, RectangleF pista, Color color, float escala)
        {
            float mitad = pista.Width / 2F;
            DibujarOpcion(dibujo, new RectangleF(pista.Left, pista.Top, mitad, pista.Height),
                opcionIzquierda, iconoIzquierdo, color, escala);
            DibujarOpcion(dibujo, new RectangleF(pista.Left + mitad, pista.Top, mitad, pista.Height),
                opcionDerecha, iconoDerecho, color, escala);
        }

        private void DibujarOpcion(Graphics dibujo, RectangleF area, string nombre, string icono,
            Color color, float escala)
        {
            if (icono == "luna" || icono == "sol")
            {
                float lado = Math.Min(18F * escala, area.Height - 10F * escala);
                var limites = new RectangleF(area.Left + (area.Width - lado) / 2F,
                    area.Top + (area.Height - lado) / 2F, lado, lado);
                if (icono == "sol") DibujarSol(dibujo, limites, color, escala);
                else DibujarLuna(dibujo, limites, color);
                return;
            }
            var texto = Rectangle.Round(RectangleF.Inflate(area, -6F * escala, 0F));
            TextRenderer.DrawText(dibujo, nombre, Font, texto, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping);
        }

        private static void DibujarSol(Graphics dibujo, RectangleF area, Color color, float escala)
        {
            float centroX = area.Left + area.Width / 2F;
            float centroY = area.Top + area.Height / 2F;
            float radio = area.Width / 5F;
            using (var linea = new Pen(color, 1.6F * escala))
            {
                linea.StartCap = LineCap.Round;
                linea.EndCap = LineCap.Round;
                dibujo.DrawEllipse(linea, centroX - radio, centroY - radio, radio * 2F, radio * 2F);
                for (int indice = 0; indice < 8; indice++)
                {
                    double angulo = indice * Math.PI / 4D;
                    float coseno = (float)Math.Cos(angulo);
                    float seno = (float)Math.Sin(angulo);
                    dibujo.DrawLine(linea, centroX + coseno * area.Width * .36F,
                        centroY + seno * area.Height * .36F, centroX + coseno * area.Width * .48F,
                        centroY + seno * area.Height * .48F);
                }
            }
        }

        private static void DibujarLuna(Graphics dibujo, RectangleF area, Color color)
        {
            using (var luna = new GraphicsPath())
            using (var relleno = new SolidBrush(color))
            {
                luna.AddArc(area, -60F, -240F);
                luna.AddBezier(new PointF(area.Left + area.Width * .75F, area.Top + area.Height * .933F),
                    new PointF(area.Left + area.Width * .3F, area.Top + area.Height * .8F),
                    new PointF(area.Left + area.Width * .3F, area.Top + area.Height * .2F),
                    new PointF(area.Left + area.Width * .75F, area.Top + area.Height * .067F));
                luna.CloseFigure();
                dibujo.FillPath(relleno, luna);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) animacion.Dispose();
            base.Dispose(disposing);
        }
    }
}
