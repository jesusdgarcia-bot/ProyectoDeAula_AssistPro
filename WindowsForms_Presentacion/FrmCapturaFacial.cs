using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using Size = System.Drawing.Size;

namespace WindowsForms_Presentacion
{
    internal sealed class FrmCapturaFacial : Form
    {
        private readonly PictureBox video = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
        private readonly Label estado = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        private readonly NumericUpDown camara = new NumericUpDown { Minimum = 0, Maximum = 10, Width = 55, AccessibleName = "Número de cámara" };
        private readonly Button encender = new Button { Text = "Encender cámara", AutoSize = true };
        private readonly Button capturar = new Button { AutoSize = true, Enabled = false };
        private readonly CancellationTokenSource cierre = new CancellationTokenSource();
        private CancellationTokenSource captura;
        private volatile bool solicitarCaptura;
        private bool enCurso;
        private bool cerrando;

        internal float[] Caracteristicas { get; private set; }

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
            if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings["CamaraFacial"], out indice) && indice >= 0 && indice <= 10)
                camara.Value = indice;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(18) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label
            {
                Text = "Mira de frente a la cámara, con buena iluminación y sin otras personas en la imagen.",
                Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 0, 0, 12)
            }, 0, 0);
            video.AccessibleName = "Vista previa de la cámara";
            layout.Controls.Add(video, 0, 1);
            estado.Text = "Preparando cámara…";
            estado.AccessibleName = "Estado del reconocimiento facial";
            layout.Controls.Add(estado, 0, 2);
            var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            acciones.Controls.Add(new Label { Text = "Cámara", AutoSize = true, Margin = new Padding(0, 7, 6, 0) });
            acciones.Controls.Add(camara);
            acciones.Controls.Add(encender);
            capturar.Text = registrar ? "Registrar rostro" : "Verificar rostro";
            acciones.Controls.Add(capturar);
            var cancelar = new Button { Text = "Cancelar", AutoSize = true };
            cancelar.Click += (s, e) => Close();
            acciones.Controls.Add(cancelar);
            CancelButton = cancelar;
            layout.Controls.Add(acciones, 0, 3);
            layout.Controls.Add(new Label
            {
                Text = registrar
                    ? "Se capturan tres muestras y se guarda una plantilla facial en Oracle. Las imágenes no se guardan."
                    : "El video se procesa en este equipo. Tu rostro debe estar registrado por un administrador.",
                Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 10, 0, 0)
            }, 0, 4);
            Controls.Add(layout);
            encender.Click += async (s, e) =>
            {
                if (enCurso) captura.Cancel();
                else await IniciarAsync();
            };
            capturar.Click += (s, e) => { solicitarCaptura = true; capturar.Enabled = false; };
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
            estado.Text = "Cargando reconocimiento y abriendo cámara…";
            captura = CancellationTokenSource.CreateLinkedTokenSource(cierre.Token);
            int indice = (int)camara.Value;
            bool huboError = false;
            try
            {
                var resultado = await Task.Run(() => Capturar(indice, captura.Token));
                if (resultado != null && !cerrando)
                {
                    Caracteristicas = resultado;
                    // El worker ya liberó la cámara antes de devolver las muestras.
                    enCurso = false;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (OperationCanceledException) { }
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

        private float[] Capturar(int indice, CancellationToken token)
        {
            using (var motor = new MotorReconocimientoFacial())
            using (var fuente = AbrirCamara(indice, token))
            using (var frame = new Mat())
            {
                var muestras = new List<float[]>();
                int fallos = 0;
                while (!token.IsCancellationRequested)
                {
                    if (!fuente.Read(frame) || frame.Empty())
                    {
                        if (++fallos >= 20)
                            throw new InvalidOperationException("La cámara no está entregando imágenes. Cierra otras aplicaciones que la usen o prueba otro número de cámara.");
                        token.WaitHandle.WaitOne(150);
                        continue;
                    }
                    fallos = 0;
                    var analisis = motor.Analizar(frame);
                    string mensaje = analisis.Indicacion;
                    if (solicitarCaptura)
                    {
                        if (analisis.Caracteristicas == null) muestras.Clear();
                        else
                        {
                            if (muestras.Count > 0 && Similitud(muestras[0], analisis.Caracteristicas) < 0.6)
                                muestras.Clear();
                            muestras.Add(analisis.Caracteristicas);
                            mensaje = $"Mantén el rostro visible. Muestra {muestras.Count} de 3…";
                            if (muestras.Count == 3) return Promediar(muestras);
                        }
                    }
                    if (analisis.Rostro.HasValue)
                        Cv2.Rectangle(frame, analisis.Rostro.Value, analisis.Caracteristicas == null ? Scalar.Orange : Scalar.LimeGreen, 2);
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
                            estado.Text = mensaje;
                            capturar.Enabled = analisis.Caracteristicas != null && !solicitarCaptura;
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

        private static double Similitud(float[] primera, float[] segunda)
        {
            double resultado = 0;
            for (int i = 0; i < primera.Length; i++) resultado += primera[i] * segunda[i];
            return resultado;
        }

        private static float[] Promediar(List<float[]> muestras)
        {
            var promedio = new float[Logica.PlantillaFacial.Dimensiones];
            foreach (var muestra in muestras)
                for (int i = 0; i < promedio.Length; i++) promedio[i] += muestra[i] / muestras.Count;
            double norma = 0;
            foreach (float valor in promedio) norma += valor * valor;
            norma = Math.Sqrt(norma);
            for (int i = 0; i < promedio.Length; i++) promedio[i] = (float)(promedio[i] / norma);
            return promedio;
        }

        private static string MensajeError(Exception error)
        {
            if (error is FileNotFoundException || error is InvalidOperationException) return error.Message;
            if (error is DllNotFoundException || error is BadImageFormatException || error is TypeInitializationException)
                return "No se pudo cargar OpenCV. Ejecuta la compilación x64 e instala Microsoft Visual C++ Redistributable 2015–2022 (x64).";
            return "No se pudo procesar la cámara. Comprueba los modelos faciales y vuelve a intentarlo.";
        }
    }
}
