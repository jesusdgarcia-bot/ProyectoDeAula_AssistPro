using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace WindowsForms_Presentacion
{
    internal sealed class InferenciaFacialOnnx : IDisposable
    {
        private readonly InferenceSession sesion;
        private readonly string entrada;
        private readonly string[] salidas;
        private readonly long[] forma;

        public InferenciaFacialOnnx(string archivo, string entrada, string salida, int tamano)
        {
            this.entrada = entrada;
            salidas = new[] { salida };
            forma = new long[] { 1, 3, tamano, tamano };
            // SFace enumera sus pesos como entradas auxiliares y genera un aviso por cada peso.
            using (var opciones = new SessionOptions
            {
                IntraOpNumThreads = 2,
                LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR
            })
                sesion = new InferenceSession(archivo, opciones);
        }

        public float[] Ejecutar(Mat imagen, Vec3f media, Vec3f desviacion)
        {
            if (imagen.Type() != MatType.CV_8UC3 || imagen.Width != forma[3] || imagen.Height != forma[2] ||
                !imagen.GetArray(out Vec3b[] pixeles))
                throw new ArgumentException("La imagen no tiene el tamaño o los canales esperados por el modelo.", nameof(imagen));

            using (var tensor = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, forma))
            using (var opciones = new RunOptions())
            {
                var datos = tensor.GetTensorMutableDataAsSpan<float>();
                // Entrada RGB en formato NCHW a partir de los píxeles BGR de OpenCV.
                int plano = pixeles.Length;
                for (int i = 0; i < plano; i++)
                {
                    datos[i] = (pixeles[i].Item2 - media.Item0) / desviacion.Item0;
                    datos[plano + i] = (pixeles[i].Item1 - media.Item1) / desviacion.Item1;
                    datos[2 * plano + i] = (pixeles[i].Item0 - media.Item2) / desviacion.Item2;
                }

                var entradas = new Dictionary<string, OrtValue> { { entrada, tensor } };
                using (var resultados = sesion.Run(opciones, entradas, salidas))
                    return resultados.Single().GetTensorDataAsSpan<float>().ToArray();
            }
        }

        internal static string RutaModelo(string nombre, string hashEsperado, Func<HashAlgorithm> crearHash)
        {
            string archivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modelos", nombre);
            if (!File.Exists(archivo))
                throw new InvalidOperationException("Falta el modelo facial " + nombre +
                    ". Ejecuta Scripts/Preparar-ModelosFaciales.ps1 y vuelve a compilar.");

            using (var flujo = File.OpenRead(archivo))
            using (var algoritmo = crearHash())
            {
                string hash = BitConverter.ToString(algoritmo.ComputeHash(flujo)).Replace("-", string.Empty);
                if (!hash.Equals(hashEsperado, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("El modelo facial " + nombre +
                        " está dañado o es incompatible. Ejecuta Scripts/Preparar-ModelosFaciales.ps1 y vuelve a compilar.");
            }
            return archivo;
        }

        public void Dispose()
        {
            sesion.Dispose();
        }
    }
}
