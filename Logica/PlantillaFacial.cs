using System;
using System.IO;

namespace Logica
{
    // El formato identifica tanto la versión como el modelo: SFace (OpenCV, 2021dec).
    // Nunca contiene una fotografía; únicamente el vector normalizado del modelo.
    public sealed class PlantillaFacial
    {
        public const int Dimensiones = 128;
        public const string TipoBiometria = "Facial";
        private const int Firma = 0x46535041; // APSF, little endian
        private const int Version = 1;
        private const int LongitudSerializada = 12 + Dimensiones * sizeof(float);
        private readonly float[] caracteristicas;

        private PlantillaFacial(float[] caracteristicas)
        {
            this.caracteristicas = caracteristicas;
        }

        public static PlantillaFacial Crear(float[] valores)
        {
            if (valores == null || valores.Length != Dimensiones)
                throw new ArgumentException("La captura facial debe contener 128 características de SFace.", nameof(valores));

            double normaCuadrada = 0;
            foreach (float valor in valores)
            {
                if (float.IsNaN(valor) || float.IsInfinity(valor))
                    throw new ArgumentException("La captura facial contiene valores inválidos. Repite la captura.", nameof(valores));
                normaCuadrada += (double)valor * valor;
            }
            if (normaCuadrada <= 1e-20)
                throw new ArgumentException("No se obtuvo una característica facial válida. Repite la captura.", nameof(valores));

            double norma = Math.Sqrt(normaCuadrada);
            var normalizados = new float[Dimensiones];
            for (int i = 0; i < Dimensiones; i++)
                normalizados[i] = (float)(valores[i] / norma);
            return new PlantillaFacial(normalizados);
        }

        public byte[] Serializar()
        {
            using (var memoria = new MemoryStream(LongitudSerializada))
            using (var escritor = new BinaryWriter(memoria))
            {
                escritor.Write(Firma);
                escritor.Write(Version);
                escritor.Write(Dimensiones);
                foreach (float valor in caracteristicas) escritor.Write(valor);
                return memoria.ToArray();
            }
        }

        public static bool IntentarLeer(byte[] datos, out PlantillaFacial plantilla)
        {
            plantilla = null;
            if (datos == null || datos.Length != LongitudSerializada) return false;
            using (var memoria = new MemoryStream(datos, false))
            using (var lector = new BinaryReader(memoria))
            {
                if (lector.ReadInt32() != Firma || lector.ReadInt32() != Version || lector.ReadInt32() != Dimensiones)
                    return false;
                var valores = new float[Dimensiones];
                for (int i = 0; i < Dimensiones; i++) valores[i] = lector.ReadSingle();
                try
                {
                    plantilla = Crear(valores);
                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }

        public double Similitud(PlantillaFacial otra)
        {
            if (otra == null) throw new ArgumentNullException(nameof(otra));
            double producto = 0;
            for (int i = 0; i < Dimensiones; i++)
                producto += (double)caracteristicas[i] * otra.caracteristicas[i];
            return Math.Max(-1, Math.Min(1, producto));
        }

        internal void AcumularEn(float[] suma)
        {
            for (int i = 0; i < Dimensiones; i++) suma[i] += caracteristicas[i];
        }
    }
}
