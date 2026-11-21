using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using QRCoder;

namespace WindowsForms_Presentacion
{
    public sealed class VisitanteLocal
    {
        public string Nombre { get; set; }
        public string Identificacion { get; set; }
        public string Correo { get; set; }
        public string CodigoQr { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public sealed class ConfiguracionCorreoLocal
    {
        public string Servidor { get; set; }
        public int Puerto { get; set; }
        public string Remitente { get; set; }
        public string Usuario { get; set; }
        public string Clave { get; set; }
    }

    internal static class ServicioVisitantes
    {
        private static readonly object sincronizacion = new object();
        private static readonly string carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AssistPro");
        private static string ArchivoVisitantes => Path.Combine(carpeta, "visitantes.dat");
        private static string ArchivoCorreo => Path.Combine(carpeta, "correo.dat");

        internal static Task<List<VisitanteLocal>> ListarAsync()
        {
            return Task.FromResult(LeerProtegido<List<VisitanteLocal>>(ArchivoVisitantes) ?? new List<VisitanteLocal>());
        }

        internal static void Guardar(VisitanteLocal visitante)
        {
            if (visitante == null) throw new ArgumentNullException(nameof(visitante));
            lock (sincronizacion)
            {
                var visitantes = LeerProtegido<List<VisitanteLocal>>(ArchivoVisitantes) ?? new List<VisitanteLocal>();
                if (visitantes.Exists(item => string.Equals(item.Identificacion, visitante.Identificacion,
                    StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Ya existe un visitante con ese documento o identificador.");
                visitantes.Add(visitante);
                GuardarProtegido(ArchivoVisitantes, visitantes);
            }
        }

        internal static ConfiguracionCorreoLocal LeerConfiguracionCorreo()
        {
            return LeerProtegido<ConfiguracionCorreoLocal>(ArchivoCorreo);
        }

        internal static void GuardarConfiguracionCorreo(ConfiguracionCorreoLocal configuracion)
        {
            if (configuracion == null) throw new ArgumentNullException(nameof(configuracion));
            GuardarProtegido(ArchivoCorreo, configuracion);
        }

        internal static byte[] GenerarQr(VisitanteLocal visitante)
        {
            if (visitante == null || string.IsNullOrWhiteSpace(visitante.CodigoQr))
                throw new ArgumentException("El visitante no tiene un código QR válido.", nameof(visitante));
            string contenido = "ASSISTPRO-VISITANTE:" + visitante.CodigoQr;
            using (var generador = new QRCodeGenerator())
            using (var datos = generador.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q))
            using (var qr = new QRCode(datos))
            using (var imagen = qr.GetGraphic(10))
            using (var salida = new MemoryStream())
            {
                imagen.Save(salida, System.Drawing.Imaging.ImageFormat.Png);
                return salida.ToArray();
            }
        }

        internal static async Task EnviarQrAsync(VisitanteLocal visitante, ConfiguracionCorreoLocal configuracion)
        {
            await EnviarQrAsync(visitante, configuracion, GenerarQr(visitante)).ConfigureAwait(false);
        }

        internal static async Task EnviarQrAsync(VisitanteLocal visitante, ConfiguracionCorreoLocal configuracion,
            byte[] imagenQr)
        {
            if (visitante == null) throw new ArgumentNullException(nameof(visitante));
            if (configuracion == null) throw new InvalidOperationException("Configura el correo antes de enviar invitaciones.");
            if (imagenQr == null || imagenQr.Length == 0) throw new ArgumentException("La imagen QR no es válida.", nameof(imagenQr));

            using (var mensaje = new MailMessage
            {
                From = new MailAddress(configuracion.Remitente, "AssistPRO · Hospital Eduardo Arredondo Daza"),
                Subject = "Código QR para su visita · Sede San Martín",
                Body = "Hola " + visitante.Nombre + ",\r\n\r\n" +
                    "Adjuntamos el código QR asociado a su registro de visitante para la sede San Martín " +
                    "de la E.S.E. Hospital Eduardo Arredondo Daza. Preséntelo en recepción al llegar.\r\n\r\n" +
                    "Si no reconoce este registro, comuníquese con la persona que lo invitó.\r\n\r\n" +
                    "AssistPRO · Gestión de visitantes",
                IsBodyHtml = false,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            })
            using (var cliente = new SmtpClient(configuracion.Servidor, configuracion.Puerto))
            {
                mensaje.To.Add(new MailAddress(visitante.Correo));
                mensaje.Attachments.Add(new Attachment(new MemoryStream(imagenQr), "Codigo-QR-Visitante.png", "image/png"));
                cliente.EnableSsl = true;
                cliente.UseDefaultCredentials = false;
                cliente.Credentials = new NetworkCredential(configuracion.Usuario, configuracion.Clave);
                cliente.DeliveryMethod = SmtpDeliveryMethod.Network;
                cliente.Timeout = 20000;
                try
                {
                    // El límite de SmtpClient se aplica al envío síncrono.
                    await Task.Run(() => cliente.Send(mensaje)).ConfigureAwait(false);
                }
                catch (SmtpException error)
                {
                    throw new InvalidOperationException(
                        "El registro local está guardado, pero no se pudo enviar el correo. Verifica la configuración SMTP y usa «Reenviar QR».", error);
                }
            }
        }

        private static T LeerProtegido<T>(string archivo) where T : class
        {
            lock (sincronizacion)
            {
                if (!File.Exists(archivo)) return null;
                byte[] protegido = File.ReadAllBytes(archivo);
                byte[] contenido = ProtectedData.Unprotect(protegido, null, DataProtectionScope.CurrentUser);
                using (var entrada = new MemoryStream(contenido))
                    return (T)new XmlSerializer(typeof(T)).Deserialize(entrada);
            }
        }

        private static void GuardarProtegido<T>(string archivo, T valor)
        {
            lock (sincronizacion)
            {
                Directory.CreateDirectory(carpeta);
                byte[] contenido;
                using (var salida = new MemoryStream())
                {
                    new XmlSerializer(typeof(T)).Serialize(salida, valor);
                    contenido = salida.ToArray();
                }
                byte[] protegido = ProtectedData.Protect(contenido, null, DataProtectionScope.CurrentUser);
                string temporal = archivo + ".tmp";
                File.WriteAllBytes(temporal, protegido);
                if (File.Exists(archivo)) File.Replace(temporal, archivo, null);
                else File.Move(temporal, archivo);
            }
        }
    }
}
