using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

namespace Logica
{
    public sealed class CapturaFacialVerificada
    {
        private readonly long emitida = Stopwatch.GetTimestamp();
        private PlantillaFacial plantilla;

        internal CapturaFacialVerificada(PlantillaFacial plantilla)
        {
            this.plantilla = plantilla;
        }

        internal PlantillaFacial Consumir()
        {
            var resultado = Interlocked.Exchange(ref plantilla, null);
            return Stopwatch.GetTimestamp() - emitida < 30L * Stopwatch.Frequency ? resultado : null;
        }
    }

    public enum EtapaPruebaDeVida
    {
        AlinearRostro,
        CerrarOjos,
        AbrirOjos,
        GirarIzquierda,
        GirarDerecha,
        VolverAlFrente,
        Completada
    }

    public sealed class ResultadoPruebaDeVida
    {
        internal ResultadoPruebaDeVida(EtapaPruebaDeVida etapa, string indicacion, CapturaFacialVerificada captura)
        {
            Etapa = etapa;
            Indicacion = indicacion;
            Captura = captura;
        }

        public EtapaPruebaDeVida Etapa { get; }
        public string Indicacion { get; }
        public CapturaFacialVerificada Captura { get; }
    }

    // Una sesión procesa en orden los fotogramas de un intento de captura.
    public sealed class SesionPruebaDeVida
    {
        private const int FotogramasAccion = 2;
        private const int FotogramasFrontales = 3;
        private const double GiroFrontal = 12;
        private const double GiroObjetivo = 22;
        private const double OjosAbiertos = 0.35;
        private const double OjosCerrados = 0.62;
        private const double SimilitudDuranteCaptura = 0.60;
        private static readonly TimeSpan DuracionMaxima = TimeSpan.FromSeconds(25);
        private static readonly TimeSpan PausaMaxima = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan TiempoParaReencuadrar = TimeSpan.FromMilliseconds(600);

        private readonly double umbralAntisuplantacion;
        private EtapaPruebaDeVida etapa;
        private PlantillaFacial referencia;
        private float[] sumaMuestras;
        private TimeSpan? comienzo;
        private TimeSpan? ultimoFotograma;
        private TimeSpan? inicioReencuadre;
        private int consecutivos;
        private int fallosAntisuplantacion;
        private bool girarPrimero;
        private bool giroCompletado;
        private bool parpadeoCompletado;
        private EtapaPruebaDeVida direccionGiro;

        public SesionPruebaDeVida(double umbralAntisuplantacion = 0.80)
        {
            if (!EsProbabilidad(umbralAntisuplantacion) || umbralAntisuplantacion <= 0.5 || umbralAntisuplantacion >= 1)
                throw new ArgumentOutOfRangeException(nameof(umbralAntisuplantacion));
            this.umbralAntisuplantacion = umbralAntisuplantacion;
        }

        public void Reiniciar()
        {
            etapa = EtapaPruebaDeVida.AlinearRostro;
            referencia = null;
            sumaMuestras = null;
            comienzo = null;
            ultimoFotograma = null;
            inicioReencuadre = null;
            consecutivos = 0;
            fallosAntisuplantacion = 0;
            giroCompletado = false;
            parpadeoCompletado = false;
        }

        public ResultadoPruebaDeVida Evaluar(int cantidadRostros, double parpadeoIzquierdo, double parpadeoDerecho,
            double giroGrados, double probabilidadReal, float[] caracteristicas, TimeSpan tiempoTranscurrido)
        {
            if (etapa == EtapaPruebaDeVida.Completada) return ResultadoActual(null);
            if (cantidadRostros > 1)
                return Rechazar("Debe aparecer una sola persona. Mira de frente para comenzar de nuevo.");
            if (tiempoTranscurrido < TimeSpan.Zero ||
                ultimoFotograma.HasValue && (tiempoTranscurrido <= ultimoFotograma.Value ||
                    tiempoTranscurrido - ultimoFotograma.Value > PausaMaxima) ||
                comienzo.HasValue && tiempoTranscurrido - comienzo.Value > DuracionMaxima)
                return Rechazar("Se interrumpió la prueba. Mira de frente para comenzar de nuevo.");

            bool reencuadrando = cantidadRostros == 0 && etapa != EtapaPruebaDeVida.AlinearRostro;
            if (reencuadrando && !inicioReencuadre.HasValue)
                inicioReencuadre = ultimoFotograma ?? tiempoTranscurrido;
            if (inicioReencuadre.HasValue && tiempoTranscurrido - inicioReencuadre.Value > TiempoParaReencuadrar)
                return Rechazar("Perdimos de vista tu rostro. Mira de frente para comenzar de nuevo.");

            ultimoFotograma = tiempoTranscurrido;
            if (reencuadrando)
            {
                consecutivos = 0;
                sumaMuestras = null;
                return new ResultadoPruebaDeVida(etapa, "Vuelve a colocar tu rostro completo dentro de la imagen.", null);
            }
            inicioReencuadre = null;
            if (cantidadRostros != 1 || caracteristicas == null || !EsProbabilidad(parpadeoIzquierdo) ||
                !EsProbabilidad(parpadeoDerecho) || !EsProbabilidad(probabilidadReal) || !(Math.Abs(giroGrados) <= 180))
                return Rechazar("Mantén el rostro completo dentro de la imagen y mira de frente.");

            PlantillaFacial muestra;
            try { muestra = PlantillaFacial.Crear(caracteristicas); }
            catch (ArgumentException) { return Rechazar("No se obtuvo una muestra válida. Mira de frente para comenzar de nuevo."); }

            if (referencia != null && referencia.Similitud(muestra) < SimilitudDuranteCaptura)
                return Rechazar("El rostro cambió durante la prueba. Mira de frente para comenzar de nuevo.");

            if (probabilidadReal < umbralAntisuplantacion)
            {
                consecutivos = 0;
                sumaMuestras = null;
                if (++fallosAntisuplantacion >= 2)
                    return Rechazar("No se pudo confirmar la presencia. Mira de frente con buena iluminación para repetir la prueba.");
                return ResultadoActual(null);
            }
            fallosAntisuplantacion = 0;

            bool deFrente = Math.Abs(giroGrados) <= GiroFrontal;
            bool abiertos = Math.Max(parpadeoIzquierdo, parpadeoDerecho) <= OjosAbiertos;
            switch (etapa)
            {
                case EtapaPruebaDeVida.AlinearRostro:
                    if (deFrente && abiertos && referencia == null)
                    {
                        referencia = muestra;
                        comienzo = tiempoTranscurrido;
                        ElegirMovimientos();
                    }
                    if (Sostener(deFrente && abiertos, FotogramasFrontales)) Avanzar();
                    break;
                case EtapaPruebaDeVida.CerrarOjos:
                    if (Sostener(deFrente && Math.Min(parpadeoIzquierdo, parpadeoDerecho) >= OjosCerrados, FotogramasAccion))
                        etapa = EtapaPruebaDeVida.AbrirOjos;
                    break;
                case EtapaPruebaDeVida.AbrirOjos:
                    if (Sostener(deFrente && abiertos, FotogramasAccion))
                    {
                        parpadeoCompletado = true;
                        Avanzar();
                    }
                    break;
                case EtapaPruebaDeVida.GirarIzquierda:
                case EtapaPruebaDeVida.GirarDerecha:
                    bool giroCorrecto = etapa == EtapaPruebaDeVida.GirarIzquierda
                        ? giroGrados >= GiroObjetivo : giroGrados <= -GiroObjetivo;
                    if (Sostener(giroCorrecto, FotogramasAccion))
                    {
                        giroCompletado = true;
                        Avanzar();
                    }
                    break;
                case EtapaPruebaDeVida.VolverAlFrente:
                    if (!deFrente || !abiertos)
                    {
                        consecutivos = 0;
                        sumaMuestras = null;
                        break;
                    }
                    if (sumaMuestras == null) sumaMuestras = new float[PlantillaFacial.Dimensiones];
                    muestra.AcumularEn(sumaMuestras);
                    if (Sostener(true, FotogramasFrontales))
                    {
                        var captura = new CapturaFacialVerificada(PlantillaFacial.Crear(sumaMuestras));
                        referencia = null;
                        sumaMuestras = null;
                        etapa = EtapaPruebaDeVida.Completada;
                        return ResultadoActual(captura);
                    }
                    break;
            }
            return ResultadoActual(null);
        }

        private void ElegirMovimientos()
        {
            var azar = new byte[1];
            using (var generador = RandomNumberGenerator.Create()) generador.GetBytes(azar);
            girarPrimero = (azar[0] & 1) == 0;
            direccionGiro = (azar[0] & 2) == 0 ? EtapaPruebaDeVida.GirarIzquierda : EtapaPruebaDeVida.GirarDerecha;
        }

        private void Avanzar()
        {
            if (giroCompletado && parpadeoCompletado) etapa = EtapaPruebaDeVida.VolverAlFrente;
            else if (!giroCompletado && (girarPrimero || parpadeoCompletado)) etapa = direccionGiro;
            else etapa = EtapaPruebaDeVida.CerrarOjos;
        }

        private bool Sostener(bool cumple, int necesarios)
        {
            consecutivos = cumple ? consecutivos + 1 : 0;
            if (consecutivos < necesarios) return false;
            consecutivos = 0;
            return true;
        }

        private ResultadoPruebaDeVida Rechazar(string indicacion)
        {
            Reiniciar();
            return new ResultadoPruebaDeVida(etapa, indicacion, null);
        }

        private ResultadoPruebaDeVida ResultadoActual(CapturaFacialVerificada captura)
        {
            string indicacion;
            switch (etapa)
            {
                case EtapaPruebaDeVida.CerrarOjos:
                    indicacion = "Mira de frente. Cierra ambos ojos y mantenlos cerrados un instante.";
                    break;
                case EtapaPruebaDeVida.AbrirOjos:
                    indicacion = "Abre ambos ojos y sigue mirando de frente.";
                    break;
                case EtapaPruebaDeVida.GirarIzquierda:
                    indicacion = "Gira la cabeza hacia tu izquierda y mantén la posición un instante.";
                    break;
                case EtapaPruebaDeVida.GirarDerecha:
                    indicacion = "Gira la cabeza hacia tu derecha y mantén la posición un instante.";
                    break;
                case EtapaPruebaDeVida.VolverAlFrente:
                    indicacion = "Vuelve a mirar de frente y mantén abiertos los ojos.";
                    break;
                case EtapaPruebaDeVida.Completada:
                    indicacion = "Prueba de vida completada.";
                    break;
                default:
                    indicacion = "Mira de frente a la cámara con ambos ojos abiertos.";
                    break;
            }
            return new ResultadoPruebaDeVida(etapa, indicacion, captura);
        }

        private static bool EsProbabilidad(double valor)
        {
            return valor >= 0 && valor <= 1;
        }
    }
}
