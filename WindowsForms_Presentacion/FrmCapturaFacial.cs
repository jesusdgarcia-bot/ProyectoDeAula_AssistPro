using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Logica;
using OpenCvSharp;
using Size = System.Drawing.Size;

namespace WindowsForms_Presentacion
{
    internal sealed class FrmCapturaFacial : Form
    {
        private readonly PictureBox video = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
        private readonly Label estado = new Label
        {
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 14, 0)
        };
        private readonly NumericUpDown camara = new NumericUpDown { Minimum = 0, Maximum = 10, Width = 55, AccessibleName = "Número de cámara" };
        private readonly AccesoButton encender = new AccesoButton { Text = "Encender cámara", Size = new Size(140, 38), Plano = true };
        private readonly AccesoButton capturar = new AccesoButton { Enabled = false, Size = new Size(140, 38), Principal = true, Plano = true };
        private readonly CancellationTokenSource cierre = new CancellationTokenSource();
        private readonly string accionCaptura;
        private CancellationTokenSource captura;
        private volatile bool solicitarCaptura;
        private bool enCurso;
        private bool cerrando;

        internal CapturaFacial Captura { get; private set; }

        internal FrmCapturaFacial(string titulo, bool registrar)
        {
            Text = titulo;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(720, 620);
            MinimumSize = new Size(540, 500);
            MaximizeBox = false;
            BackColor = TemaAplicacion.Actual.Fondo;
            ForeColor = TemaAplicacion.Actual.Texto;
            int indice;
            if (int.TryParse(ConfigurationManager.AppSettings["CamaraFacial"], out indice) && indice >= 0 && indice <= 10)
                camara.Value = indice;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(20) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var instrucciones = new Label
            {
                Text = "Mira de frente a la cámara con buena iluminación. Coloca todo el rostro dentro de la imagen.",
                Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 0, 0, 12)
            };
            TemaAplicacion.DefinirRol(instrucciones, RolTema.TextoSecundario);
            layout.Controls.Add(instrucciones, 0, 0);
            video.AccessibleName = "Vista previa de la cámara";
            TemaAplicacion.DefinirRol(video, RolTema.Video);
            layout.Controls.Add(video, 0, 1);
            estado.Text = "Preparando cámara…";
            estado.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
            estado.AccessibleName = "Estado de la captura facial";
            TemaAplicacion.DefinirRol(estado, RolTema.Superficie);
            layout.Controls.Add(estado, 0, 2);
            var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            acciones.Controls.Add(new Label { Text = "Cámara", AutoSize = true, Margin = new Padding(0, 7, 6, 0) });
            camara.Margin = new Padding(0, 8, 8, 0);
            acciones.Controls.Add(camara);
            acciones.Controls.Add(encender);
            accionCaptura = registrar ? "Registrar rostro" : "Verificar rostro";
            capturar.Text = accionCaptura;
            acciones.Controls.Add(capturar);
            var cancelar = new AccesoButton { Text = "Cancelar", Size = new Size(88, 38), Plano = true };
            cancelar.Click += (s, e) => Close();
            acciones.Controls.Add(cancelar);
            CancelButton = cancelar;
            layout.Controls.Add(acciones, 0, 3);
            var privacidad = new Label
            {
                Text = registrar
                    ? "El video se procesa en este equipo. Se guarda una plantilla facial en Oracle al finalizar."
                    : "El video se procesa en este equipo. Tu rostro debe estar registrado por un administrador.",
                Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 10, 0, 0)
            };
            TemaAplicacion.DefinirRol(privacidad, RolTema.TextoSecundario);
            layout.Controls.Add(privacidad, 0, 4);
            Controls.Add(layout);
            encender.Click += async (s, e) =>
            {
                if (enCurso) captura.Cancel();
                else await IniciarAsync();
            };
            capturar.Click += (s, e) =>
            {
                solicitarCaptura = true;
                capturar.Enabled = false;
                estado.Text = "Mantén el rostro visible y mira de frente.";
            };
            Shown += async (s, e) => await IniciarAsync();
            FormClosing += (s, e) =>
            {
                if (!enCurso) return;
                e.Cancel = true;
                cerrando = true;
                cierre.Cancel();
                captura.Cancel();
                estado.Text = "Cerrando cámara…";
            };
            FormClosed += (s, e) =>
            {
                video.Image?.Dispose();
                video.Image = null;
                cierre.Dispose();
            };
            TemaAplicacion.Registrar(this);
        }

        private async Task IniciarAsync()
        {
            if (enCurso || cerrando) return;
            enCurso = true;
            solicitarCaptura = false;
            camara.Enabled = capturar.Enabled = false;
            encender.Text = "Detener cámara";
            estado.Text = "Cargando los modelos faciales y abriendo la cámara…";
            captura = CancellationTokenSource.CreateLinkedTokenSource(cierre.Token);
            var token = captura.Token;
            int indice = (int)camara.Value;
            bool huboError = false;
            try
            {
                var resultado = await Task.Run(() =>
                {
                    if (token.IsCancellationRequested) return null;
                    try
                    {
                        return Capturar(indice, token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        return null;
                    }
                });
                if (resultado != null && !token.IsCancellationRequested)
                {
                    Captura = resultado;
                    // El hilo de captura ya liberó la cámara.
                    enCurso = false;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception error)
            {
                huboError = true;
                if (!cerrando && !IsDisposed)
                    estado.Text = MensajeError(error);
            }
            finally
            {
                enCurso = false;
                captura.Dispose();
                captura = null;
                if (!IsDisposed)
                {
                    capturar.Enabled = false;
                    camara.Enabled = true;
                    encender.Text = "Encender cámara";
                    video.Image?.Dispose();
                    video.Image = null;
                    if (cerrando) Close();
                    else if (!huboError)
                        estado.Text = "Cámara detenida. Puedes volver a encenderla.";
                }
            }
        }

        private CapturaFacial Capturar(int indice, CancellationToken token)
        {
            using (var motor = new MotorReconocimientoFacial())
            using (var fuente = AbrirCamara(indice, token))
            using (var frame = new Mat())
            {
                var muestras = new SesionCapturaFacial();
                int fallos = 0;
                while (!token.IsCancellationRequested)
                {
                    bool tomandoMuestras = solicitarCaptura;
                    if (!fuente.Read(frame) || frame.Empty())
                    {
                        muestras.Reiniciar();
                        if (++fallos >= 20)
                            throw new InvalidOperationException("La cámara no está entregando imágenes. Cierra otras aplicaciones que la usen o prueba otro número de cámara.");
                        token.WaitHandle.WaitOne(150);
                        continue;
                    }
                    fallos = 0;
                    var analisis = motor.Analizar(frame, extraerCaracteristicas: tomandoMuestras);
                    if (token.IsCancellationRequested) break;
                    string mensaje = analisis.Indicacion;
                    if (tomandoMuestras)
                    {
                        if (analisis.Caracteristicas == null) muestras.Reiniciar();
                        else
                        {
                            var resultado = muestras.AgregarMuestra(analisis.Caracteristicas);
                            if (resultado != null) return resultado;
                            mensaje = $"Mantén el rostro visible. Muestra {muestras.Muestras} de {SesionCapturaFacial.MuestrasRequeridas}…";
                        }
                    }
                    else if (analisis.RostroApto)
                    {
                        mensaje = "Pulsa «" + accionCaptura + "» y mantén la mirada al frente.";
                    }
                    if (analisis.Rostro.HasValue)
                        Cv2.Rectangle(frame, analisis.Rostro.Value, analisis.RostroApto ? Scalar.LimeGreen : Scalar.Orange, 2);
                    Cv2.Flip(frame, frame, FlipMode.Y);
                    Cv2.ImEncode(".bmp", frame, out byte[] imagen);
                    Bitmap vista;
                    using (var stream = new MemoryStream(imagen))
                    using (var original = new Bitmap(stream)) vista = new Bitmap(original);
                    // Invoke limita la cola a una imagen y mantiene el bitmap en el hilo UI.
                    try
                    {
                        Invoke(new Action(() =>
                        {
                            if (cerrando) { vista.Dispose(); return; }
                            var anterior = video.Image;
                            video.Image = vista;
                            anterior?.Dispose();
                            if (tomandoMuestras || !solicitarCaptura) estado.Text = mensaje;
                            capturar.Enabled = analisis.RostroApto && !solicitarCaptura && !token.IsCancellationRequested;
                        }));
                    }
                    catch { vista.Dispose(); throw; }
                    token.WaitHandle.WaitOne(160);
                }
                return null;
            }
        }

        private static VideoCapture AbrirCamara(int indice, CancellationToken token)
        {
            foreach (var backend in new[] { VideoCaptureAPIs.DSHOW, VideoCaptureAPIs.MSMF })
            {
                token.ThrowIfCancellationRequested();
                var fuente = new VideoCapture();
                try
                {
                    if (fuente.Open(indice, backend) && fuente.IsOpened())
                    {
                        fuente.Set(VideoCaptureProperties.FrameWidth, 640);
                        fuente.Set(VideoCaptureProperties.FrameHeight, 480);
                        return fuente;
                    }
                }
                catch (OpenCVException) { }
                fuente.Dispose();
            }
            throw new InvalidOperationException("No se pudo abrir la cámara. Revisa los permisos de cámara de Windows, cierra otras aplicaciones que la usen o prueba otro número de cámara.");
        }

        private static string MensajeError(Exception error)
        {
            if (error is FileNotFoundException || error is InvalidOperationException) return error.Message;
            if (error is DllNotFoundException || error is BadImageFormatException || error is TypeInitializationException)
                return "No se pudo cargar OpenCV u ONNX Runtime. Ejecuta la compilación x64 e instala Microsoft Visual C++ Redistributable 2015–2022 (x64).";
            return "No se pudo procesar la cámara. Comprueba los modelos faciales y vuelve a intentarlo.";
        }
    }
}
