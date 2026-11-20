using System;
using System.Threading.Tasks;
using Entidades;

namespace Logica
{
    public interface IRepositorioMarcacionFacial
    {
        Task<RegistroAsistencia> GuardarMarcacionAsync(SesionUsuario identidad, int idDispositivo, string tipoMarcacion);
    }

    public sealed class ServicioMarcacionFacial
    {
        private readonly ServicioReconocimientoFacial reconocimiento;
        private readonly IRepositorioMarcacionFacial repositorio;

        public ServicioMarcacionFacial(ServicioReconocimientoFacial reconocimiento, IRepositorioMarcacionFacial repositorio)
        {
            this.reconocimiento = reconocimiento ?? throw new ArgumentNullException(nameof(reconocimiento));
            this.repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        }

        public async Task<RegistroAsistencia> RegistrarAsync(SesionUsuario sesion, int idDispositivo, string tipoMarcacion,
            CapturaFacialVerificada captura)
        {
            ValidarSolicitud(sesion, idDispositivo, tipoMarcacion);

            var identidad = await reconocimiento.AutenticarAsync(captura).ConfigureAwait(false);
            if (!CoincideConSesion(sesion, identidad))
                throw new UnauthorizedAccessException("No se pudo confirmar que el rostro corresponde a la cuenta abierta.");

            return await repositorio.GuardarMarcacionAsync(identidad, idDispositivo, tipoMarcacion).ConfigureAwait(false);
        }

        internal static void ValidarSolicitud(SesionUsuario sesion, int idDispositivo, string tipoMarcacion)
        {
            if (sesion == null || sesion.IdUsuario <= 0 || sesion.IdEmpleado <= 0)
                throw new UnauthorizedAccessException("Inicia sesión para registrar la asistencia.");
            if (idDispositivo <= 0)
                throw new ArgumentException("Configura un terminal activo para registrar la asistencia.", nameof(idDispositivo));
            if (tipoMarcacion != "Entrada" && tipoMarcacion != "Salida")
                throw new ArgumentException("Selecciona Entrada o Salida para registrar la asistencia.", nameof(tipoMarcacion));
        }

        internal static bool CoincideConSesion(SesionUsuario sesion, SesionUsuario identidad)
        {
            return identidad != null && identidad.IdUsuario == sesion.IdUsuario && identidad.IdEmpleado == sesion.IdEmpleado;
        }
    }
}
