using System;
using System.Threading.Tasks;
using Entidades;

namespace Logica
{
    public interface IRepositorioMarcacionConClave
    {
        Task<RegistroAsistencia> GuardarMarcacionConClaveAsync(SesionUsuario identidad, int idDispositivo, string tipoMarcacion);
    }

    public sealed class ServicioMarcacionConClave
    {
        private readonly ServicioAutenticacion autenticacion;
        private readonly IRepositorioMarcacionConClave repositorio;

        public ServicioMarcacionConClave(ServicioAutenticacion autenticacion, IRepositorioMarcacionConClave repositorio)
        {
            this.autenticacion = autenticacion ?? throw new ArgumentNullException(nameof(autenticacion));
            this.repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        }

        public async Task<RegistroAsistencia> RegistrarAsync(SesionUsuario sesion, int idDispositivo, string tipoMarcacion, string clave)
        {
            ServicioMarcacionFacial.ValidarSolicitud(sesion, idDispositivo, tipoMarcacion);
            var identidad = await autenticacion.AutenticarAsync(sesion.NombreUsuario, clave).ConfigureAwait(false);
            if (!ServicioMarcacionFacial.CoincideConSesion(sesion, identidad))
                throw new UnauthorizedAccessException("No se pudo verificar la contraseña de la cuenta abierta.");

            return await repositorio.GuardarMarcacionConClaveAsync(identidad, idDispositivo, tipoMarcacion).ConfigureAwait(false);
        }
    }
}
