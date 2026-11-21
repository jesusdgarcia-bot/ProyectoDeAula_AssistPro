using System;
using System.Diagnostics;
using System.Threading;

namespace Logica
{
    public sealed class CapturaFacial
    {
        private readonly long emitida = Stopwatch.GetTimestamp();
        private PlantillaFacial plantilla;

        internal CapturaFacial(PlantillaFacial plantilla)
        {
            this.plantilla = plantilla;
        }

        internal PlantillaFacial Consumir()
        {
            var resultado = Interlocked.Exchange(ref plantilla, null);
            return Stopwatch.GetTimestamp() - emitida < 30L * Stopwatch.Frequency ? resultado : null;
        }
    }

    public sealed class SesionCapturaFacial
    {
        public const int MuestrasRequeridas = 3;
        private const double SimilitudMinima = 0.60;
        private PlantillaFacial referencia;
        private float[] sumaMuestras;

        public int Muestras { get; private set; }

        public void Reiniciar()
        {
            Muestras = 0;
            referencia = null;
            sumaMuestras = null;
        }

        public CapturaFacial AgregarMuestra(float[] valores)
        {
            if (Muestras == MuestrasRequeridas) return null;

            PlantillaFacial muestra;
            try
            {
                muestra = PlantillaFacial.Crear(valores);
            }
            catch (ArgumentException)
            {
                Reiniciar();
                return null;
            }

            if (referencia != null && referencia.Similitud(muestra) < SimilitudMinima)
                Reiniciar();
            if (referencia == null)
            {
                referencia = muestra;
                sumaMuestras = new float[PlantillaFacial.Dimensiones];
            }

            muestra.AcumularEn(sumaMuestras);
            Muestras++;
            if (Muestras < MuestrasRequeridas) return null;

            var captura = new CapturaFacial(PlantillaFacial.Crear(sumaMuestras));
            referencia = null;
            sumaMuestras = null;
            return captura;
        }
    }
}
