using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace WindowsForms_Presentacion
{
    internal sealed class SenalesMediaPipe
    {
        internal SenalesMediaPipe(double parpadeoIzquierdo, double parpadeoDerecho, double giroHorizontalGrados, Rect rostro)
        {
            ParpadeoIzquierdo = parpadeoIzquierdo;
            ParpadeoDerecho = parpadeoDerecho;
            GiroHorizontalGrados = giroHorizontalGrados;
            Rostro = rostro;
        }

        internal double ParpadeoIzquierdo { get; }
        internal double ParpadeoDerecho { get; }
        // En una imagen sin reflejar, el giro positivo corresponde a la izquierda de la persona.
        internal double GiroHorizontalGrados { get; }
        internal Rect Rostro { get; }
    }

    internal sealed class AnalizadorMediaPipe : IDisposable
    {
        private const int LadoMaximo = 640;
        private const int TiempoArranqueMs = 15000;
        private const int TiempoFotogramaMs = 3000;
        private const string HashModelo = "64184e229b263107bc2b804c6625db1341ff2bb731874b0bcc2fe6544e0bc9ff";
        private readonly Process proceso;
        private readonly byte[] fotograma = new byte[2 * sizeof(int) + LadoMaximo * LadoMaximo * 3];
        private Stream entrada;
        private StreamReader salida;
        private bool eliminado;

        internal AnalizadorMediaPipe(string ejecutablePython, string carpetaModelos, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var script = Path.GetFullPath(Path.Combine(carpetaModelos, "face_landmarker_worker.py"));
            var modelo = Path.GetFullPath(Path.Combine(carpetaModelos, "face_landmarker.task"));
            if (!File.Exists(script))
                throw new InvalidOperationException("Falta el analizador de MediaPipe junto a la aplicación. Vuelve a compilar AssistPRO.");
            ValidarModelo(modelo);

            string python = string.IsNullOrWhiteSpace(ejecutablePython) ? "python" : ejecutablePython.Trim();
            if (Path.IsPathRooted(python) || python.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0)
                python = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, python));

            proceso = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = python,
                    Arguments = "-I -u \"" + script + "\" \"" + modelo + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            try
            {
                token.ThrowIfCancellationRequested();
                if (!proceso.Start())
                    throw new InvalidOperationException("No se pudo iniciar el intérprete configurado en PythonMediaPipe.");
                entrada = proceso.StandardInput.BaseStream;
                salida = proceso.StandardOutput;
                // Drena los diagnósticos nativos para que no bloqueen la tubería.
                proceso.BeginErrorReadLine();
                string respuesta = EsperarRespuesta(salida.ReadLineAsync(), TiempoArranqueMs, token);
                if (respuesta == "dependencias")
                    throw new InvalidOperationException("Instala mediapipe==0.10.35 en Python 3.12 x64 y configura PythonMediaPipe con ese intérprete.");
                if (respuesta == "modelo")
                    throw new InvalidOperationException("MediaPipe no pudo cargar face_landmarker.task. Ejecuta Scripts/Preparar-ModelosFaciales.ps1 y vuelve a compilar.");
                if (respuesta != "listo")
                    throw new InvalidOperationException("El analizador de MediaPipe no pudo iniciar. Revisa la configuración de PythonMediaPipe.");
            }
            catch (Win32Exception error)
            {
                Dispose();
                throw new InvalidOperationException("No se pudo abrir Python. Configura PythonMediaPipe con la ruta a Python 3.12 x64 que tenga MediaPipe instalado.", error);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal SenalesMediaPipe Analizar(Mat frame, CancellationToken token, out bool variosRostros)
        {
            if (eliminado) throw new ObjectDisposedException(nameof(AnalizadorMediaPipe));
            token.ThrowIfCancellationRequested();
            try
            {
                int longitud = CopiarFotograma(frame);
                string respuesta = EsperarRespuesta(EnviarAsync(longitud), TiempoFotogramaMs, token);
                variosRostros = respuesta == "varios_rostros";
                return variosRostros ? null : Interpretar(respuesta, frame.Cols, frame.Rows);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private int CopiarFotograma(Mat frame)
        {
            double escala = Math.Min(1.0, (double)LadoMaximo / Math.Max(frame.Cols, frame.Rows));
            int ancho = Math.Max(1, (int)Math.Round(frame.Cols * escala));
            int alto = Math.Max(1, (int)Math.Round(frame.Rows * escala));
            using (var reducida = new Mat())
            using (var rgb = new Mat())
            {
                Mat fuente = frame;
                if (escala < 1.0)
                {
                    Cv2.Resize(frame, reducida, new Size(ancho, alto), interpolation: InterpolationFlags.Area);
                    fuente = reducida;
                }
                Cv2.CvtColor(fuente, rgb, ColorConversionCodes.BGR2RGB);
                // Dos enteros little-endian seguidos por ancho × alto × 3 bytes RGB.
                Buffer.BlockCopy(BitConverter.GetBytes(ancho), 0, fotograma, 0, sizeof(int));
                Buffer.BlockCopy(BitConverter.GetBytes(alto), 0, fotograma, sizeof(int), sizeof(int));
                int tamano = ancho * alto * 3;
                Marshal.Copy(rgb.Data, fotograma, 2 * sizeof(int), tamano);
                return 2 * sizeof(int) + tamano;
            }
        }

        private async Task<string> EnviarAsync(int longitud)
        {
            await entrada.WriteAsync(fotograma, 0, longitud).ConfigureAwait(false);
            await entrada.FlushAsync().ConfigureAwait(false);
            return await salida.ReadLineAsync().ConfigureAwait(false);
        }

        private static string EsperarRespuesta(Task<string> tarea, int tiempoMs, CancellationToken token)
        {
            try
            {
                if (Task.WaitAny(new Task[] { tarea }, tiempoMs, token) < 0)
                    throw new InvalidOperationException("MediaPipe tardó demasiado en responder. Vuelve a encender la cámara.");
                return tarea.GetAwaiter().GetResult()
                    ?? throw new InvalidOperationException("MediaPipe se cerró. Revisa PythonMediaPipe y la instalación de MediaPipe.");
            }
            catch (IOException error)
            {
                throw new InvalidOperationException("Se perdió la comunicación con MediaPipe. Vuelve a encender la cámara.", error);
            }
            finally
            {
                if (!tarea.IsCompleted || tarea.IsFaulted)
                    _ = tarea.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        private static SenalesMediaPipe Interpretar(string respuesta, int ancho, int alto)
        {
            if (respuesta == "sin_rostro") return null;

            string[] campos = respuesta.Split('\t');
            if (campos.Length != 8 || campos[0] != "ok")
                throw new InvalidOperationException("MediaPipe no pudo analizar el fotograma. Vuelve a encender la cámara.");

            var valores = new double[7];
            for (int i = 0; i < valores.Length; i++)
            {
                if (!double.TryParse(campos[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out valores[i])
                    || double.IsNaN(valores[i]) || double.IsInfinity(valores[i]))
                    throw new InvalidOperationException("MediaPipe devolvió señales faciales inválidas.");
            }
            if (valores[0] < 0 || valores[0] > 1 || valores[1] < 0 || valores[1] > 1 || Math.Abs(valores[2]) > 180
                || valores[3] < 0 || valores[4] < 0 || valores[5] > 1 || valores[6] > 1
                || valores[5] <= valores[3] || valores[6] <= valores[4])
                throw new InvalidOperationException("MediaPipe devolvió señales faciales fuera del intervalo esperado.");

            int x = (int)Math.Floor(valores[3] * ancho);
            int y = (int)Math.Floor(valores[4] * alto);
            int derecha = (int)Math.Ceiling(valores[5] * ancho);
            int abajo = (int)Math.Ceiling(valores[6] * alto);
            return new SenalesMediaPipe(valores[0], valores[1], valores[2], new Rect(x, y, derecha - x, abajo - y));
        }

        private static void ValidarModelo(string archivo)
        {
            const string mensaje = "Falta el modelo de MediaPipe o su SHA-256 no coincide. Ejecuta Scripts/Preparar-ModelosFaciales.ps1 y vuelve a compilar.";
            if (!File.Exists(archivo)) throw new InvalidOperationException(mensaje);
            using (var flujo = File.OpenRead(archivo))
            using (var sha = SHA256.Create())
            {
                string hash = BitConverter.ToString(sha.ComputeHash(flujo)).Replace("-", string.Empty);
                if (!hash.Equals(HashModelo, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(mensaje);
            }
        }

        public void Dispose()
        {
            if (eliminado) return;
            eliminado = true;
            try
            {
                if (!proceso.HasExited)
                {
                    proceso.Kill();
                    proceso.WaitForExit(500);
                }
            }
            catch (InvalidOperationException) { }
            catch (Win32Exception) { }
            finally
            {
                entrada?.Dispose();
                salida?.Dispose();
                proceso.Dispose();
            }
        }
    }
}
