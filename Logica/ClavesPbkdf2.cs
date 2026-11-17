using System;
using System.Globalization;
using System.Security.Cryptography;

namespace Logica
{
    public sealed class ClavesPbkdf2 : IVerificadorClave
    {
        private const int Iteraciones = 600000;
        private const string Algoritmo = "PBKDF2-SHA256";

        public string CrearHash(string clave)
        {
            if (string.IsNullOrEmpty(clave) || clave.Length > 1024)
                throw new ArgumentException("La contraseña debe contener entre 1 y 1024 caracteres.", nameof(clave));
            var sal = new byte[16];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(sal);
            var hash = Derivar(clave, sal, Iteraciones);
            return Algoritmo + "$" + Iteraciones.ToString(CultureInfo.InvariantCulture) + "$" +
                Convert.ToBase64String(sal) + "$" + Convert.ToBase64String(hash);
        }

        public bool Verificar(string clave, string almacenado)
        {
            var partes = (almacenado ?? "").Split('$');
            int iteraciones;
            if (partes.Length != 4 || partes[0] != Algoritmo ||
                !int.TryParse(partes[1], out iteraciones) || iteraciones < 100000 || iteraciones > 2000000)
                return Rechazar(clave);
            try
            {
                var sal = Convert.FromBase64String(partes[2]);
                var esperado = Convert.FromBase64String(partes[3]);
                if (sal.Length != 16 || esperado.Length != 32) return Rechazar(clave);
                var calculado = Derivar(clave, sal, iteraciones);
                int diferencia = 0;
                for (int i = 0; i < calculado.Length; i++) diferencia |= calculado[i] ^ esperado[i];
                return diferencia == 0;
            }
            catch (FormatException) { return Rechazar(clave); }
        }

        private static bool Rechazar(string clave)
        {
            Derivar(clave, new byte[16], Iteraciones);
            return false;
        }

        private static byte[] Derivar(string clave, byte[] sal, int iteraciones)
        {
            using (var algoritmo = new Rfc2898DeriveBytes(clave, sal, iteraciones, HashAlgorithmName.SHA256))
                return algoritmo.GetBytes(32);
        }
    }
}
