using System;
using System.IO;
using System.Security.Cryptography;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace WindowsForms_Presentacion
{
    internal sealed class ResultadoFacial
    {
        public float[] Caracteristicas { get; set; }
        public string Indicacion { get; set; }
        // Coordenadas de la imagen original, incluso cuando se reduce para inferencia.
        public Rect? Rostro { get; set; }
    }

    /// <summary>YuNet + SFace en CPU; una instancia se usa exclusivamente desde un hilo.</summary>
    internal sealed class MotorReconocimientoFacial : IDisposable
    {
        private const int TamanoDetector = 640;
        private const int Dimensiones = 128;
        private readonly FaceDetectorYN detector;
        private readonly Net reconocedor;
        private bool eliminado;

        public MotorReconocimientoFacial()
        {
            string carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modelos");
            string yunet = Path.Combine(carpeta, "face_detection_yunet_2023mar.onnx");
            string sface = Path.Combine(carpeta, "face_recognition_sface_2021dec.onnx");
            ValidarModelo(yunet, "8f2383e4dd3cfbb4553ea8718107fc0423210dc964f9f4280604804ed2552fa4");
            ValidarModelo(sface, "0ba9fbfa01b5270c96627c4ef784da859931e02f04419c829e83484087c34e79");
            try
            {
                detector = FaceDetectorYN.Create(yunet, string.Empty,
                    new Size(TamanoDetector, TamanoDetector), 0.9f, 0.3f, 5000, Backend.OPENCV, Target.CPU);
                reconocedor = CvDnn.ReadNetFromOnnx(sface);
                reconocedor.SetPreferableBackend(Backend.OPENCV);
                reconocedor.SetPreferableTarget(Target.CPU);
            }
            catch
            {
                if (detector != null) detector.Dispose();
                if (reconocedor != null) reconocedor.Dispose();
                throw;
            }
        }

        public ResultadoFacial Analizar(Mat imagen)
        {
            if (eliminado) throw new ObjectDisposedException(nameof(MotorReconocimientoFacial));
            if (imagen == null || imagen.Empty())
                return new ResultadoFacial { Indicacion = "Esperando una imagen de la cámara…" };
            if (imagen.Type() != MatType.CV_8UC3)
                throw new ArgumentException("La cámara debe entregar imágenes BGR de tres canales.", nameof(imagen));

            double escala = Math.Min(1.0, TamanoDetector / (double)Math.Max(imagen.Width, imagen.Height));
            int ancho = Math.Max(1, (int)Math.Round(imagen.Width * escala));
            int alto = Math.Max(1, (int)Math.Round(imagen.Height * escala));
            using (var reducida = new Mat())
            using (var entrada = new Mat())
            using (var caras = new Mat())
            {
                Cv2.Resize(imagen, reducida, new Size(ancho, alto), 0, 0, InterpolationFlags.Area);
                Cv2.CopyMakeBorder(reducida, entrada, 0, TamanoDetector - alto, 0,
                    TamanoDetector - ancho, BorderTypes.Constant, Scalar.All(0));
                detector.Detect(entrada, caras);
                if (caras.Empty() || caras.Rows == 0)
                    return new ResultadoFacial { Indicacion = "Mira a la cámara con buena iluminación." };
                if (caras.Rows != 1)
                    return new ResultadoFacial { Indicacion = "Debe aparecer una sola persona frente a la cámara." };

                float x = caras.At<float>(0, 0), y = caras.At<float>(0, 1);
                float w = caras.At<float>(0, 2), h = caras.At<float>(0, 3);
                int izquierda = Math.Max(0, (int)(x / escala));
                int arriba = Math.Max(0, (int)(y / escala));
                int derecha = Math.Min(imagen.Width, (int)Math.Ceiling((x + w) / escala));
                int abajo = Math.Min(imagen.Height, (int)Math.Ceiling((y + h) / escala));
                var rostro = new Rect(izquierda, arriba, Math.Max(0, derecha - izquierda), Math.Max(0, abajo - arriba));
                if (w < 100 || h < 100)
                    return new ResultadoFacial { Rostro = rostro, Indicacion = "Acércate un poco más a la cámara." };
                if (x < 0 || y < 0 || x + w > ancho || y + h > alto)
                    return new ResultadoFacial { Rostro = rostro, Indicacion = "Coloca todo el rostro dentro de la imagen." };

                using (var transformacion = CrearAlineacion(caras))
                using (var alineada = new Mat())
                {
                    Cv2.WarpAffine(entrada, alineada, transformacion, new Size(112, 112), InterpolationFlags.Linear);
                    // Equivale a FaceRecognizerSF::feature de OpenCV: valores 0..255 y orden RGB.
                    using (var blob = CvDnn.BlobFromImage(alineada, 1, new Size(112, 112), Scalar.All(0), true, false))
                    {
                        reconocedor.SetInput(blob);
                        using (var caracteristicas = reconocedor.Forward())
                        using (var vector = caracteristicas.Reshape(1, 1))
                        {
                            if (vector.Cols != Dimensiones || vector.Type() != MatType.CV_32FC1)
                                throw new InvalidOperationException("El modelo SFace no devolvió una plantilla compatible.");
                            var datos = new float[Dimensiones];
                            double norma = 0;
                            for (int i = 0; i < datos.Length; i++)
                            {
                                datos[i] = vector.At<float>(0, i);
                                if (float.IsNaN(datos[i]) || float.IsInfinity(datos[i]))
                                    throw new InvalidOperationException("No se pudo extraer una plantilla facial válida.");
                                norma += (double)datos[i] * datos[i];
                            }
                            if (norma < 1e-12) throw new InvalidOperationException("La plantilla facial está vacía.");
                            norma = Math.Sqrt(norma);
                            for (int i = 0; i < datos.Length; i++) datos[i] = (float)(datos[i] / norma);
                            return new ResultadoFacial
                            {
                                Caracteristicas = datos, Rostro = rostro,
                                Indicacion = "Rostro listo para capturar. Mantén la mirada al frente."
                            };
                        }
                    }
                }
            }
        }

        private static Mat CrearAlineacion(Mat cara)
        {
            // Landmarks y transformación de similitud usados por OpenCV SFace.
            // Referencia: opencv/modules/objdetect/src/face_recognize.cpp (Apache-2.0).
            double[,] destino = { { 38.2946, 51.6963 }, { 73.5318, 51.5014 }, { 56.0252, 71.7366 },
                { 41.5493, 92.3655 }, { 70.7299, 92.2041 } };
            double sx = 0, sy = 0, dx = 0, dy = 0;
            for (int i = 0; i < 5; i++)
            {
                sx += cara.At<float>(0, 4 + 2 * i); sy += cara.At<float>(0, 5 + 2 * i);
                dx += destino[i, 0]; dy += destino[i, 1];
            }
            sx /= 5; sy /= 5; dx /= 5; dy /= 5;
            double varianza = 0, producto = 0, cruzado = 0;
            for (int i = 0; i < 5; i++)
            {
                double px = cara.At<float>(0, 4 + 2 * i) - sx;
                double py = cara.At<float>(0, 5 + 2 * i) - sy;
                double qx = destino[i, 0] - dx, qy = destino[i, 1] - dy;
                varianza += px * px + py * py;
                producto += px * qx + py * qy;
                cruzado += px * qy - py * qx;
            }
            if (varianza < 1e-8) throw new InvalidOperationException("No se pudieron alinear los puntos del rostro.");
            double a = producto / varianza, b = cruzado / varianza;
            var matriz = new Mat(2, 3, MatType.CV_64FC1);
            matriz.Set(0, 0, a); matriz.Set(0, 1, -b); matriz.Set(0, 2, dx - a * sx + b * sy);
            matriz.Set(1, 0, b); matriz.Set(1, 1, a); matriz.Set(1, 2, dy - b * sx - a * sy);
            return matriz;
        }

        private static void ValidarModelo(string archivo, string esperado)
        {
            if (!File.Exists(archivo))
                throw new InvalidOperationException("Faltan los modelos faciales. Ejecuta Scripts/Preparar-ModelosFaciales.ps1 y vuelve a compilar.");
            using (var flujo = File.OpenRead(archivo))
            using (var sha = SHA256.Create())
            {
                string hash = BitConverter.ToString(sha.ComputeHash(flujo)).Replace("-", string.Empty);
                if (!hash.Equals(esperado, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("El modelo facial " + Path.GetFileName(archivo) +
                        " está dañado o es incompatible. Ejecuta Scripts/Preparar-ModelosFaciales.ps1 y vuelve a compilar.");
            }
        }

        public void Dispose()
        {
            if (eliminado) return;
            eliminado = true;
            reconocedor.Dispose();
            detector.Dispose();
        }
    }
}
