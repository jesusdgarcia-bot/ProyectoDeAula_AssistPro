using System;
using System.Security.Cryptography;
using OpenCvSharp;

namespace WindowsForms_Presentacion
{
    /// <summary>Clasifica un recorte facial con el modelo anti-spoof-mn3 de Open Model Zoo.</summary>
    internal sealed class DetectorAntiSuplantacionOnnx : IDisposable
    {
        private const int Tamano = 128;
        private static readonly Vec3f MediaRgb = new Vec3f(151.2405f, 119.5950f, 107.8395f);
        private static readonly Vec3f DesviacionRgb = new Vec3f(63.0105f, 56.4570f, 55.0035f);
        private readonly InferenciaFacialOnnx modelo;

        public DetectorAntiSuplantacionOnnx()
        {
            string archivo = InferenciaFacialOnnx.RutaModelo("anti-spoof-mn3.onnx",
                "6de4534964b723397b3e8c995cadcf43bc007cc2f9930b95ae25f76adccece5d1d4d058d0b15117b9e4a9f758424f92a",
                SHA384.Create);
            modelo = new InferenciaFacialOnnx(archivo, "actual_input_1", "output1", Tamano);
        }

        public float Evaluar(Mat imagen, Rect rostro)
        {
            using (var recorte = new Mat(imagen, rostro))
            using (var reducida = new Mat())
            {
                Cv2.Resize(recorte, reducida, new Size(Tamano, Tamano), 0, 0, InterpolationFlags.Linear);
                float[] probabilidades = modelo.Ejecutar(reducida, MediaRgb, DesviacionRgb);
                // El ONNX original devuelve probabilidades en el orden persona real, suplantación.
                if (probabilidades.Length != 2 || !EsProbabilidad(probabilidades[0]) ||
                    !EsProbabilidad(probabilidades[1]) || Math.Abs(probabilidades[0] + probabilidades[1] - 1f) > 0.001f)
                    throw new InvalidOperationException("El modelo anti-suplantación devolvió un resultado incompatible.");

                return probabilidades[0];
            }
        }

        private static bool EsProbabilidad(float valor)
        {
            return valor >= 0f && valor <= 1f;
        }

        public void Dispose()
        {
            modelo.Dispose();
        }
    }
}
