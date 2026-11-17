using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entidades;

namespace Logica
{
    public interface IConsultaAdministracion
    {
        Task<List<RegistroAsistencia>> ConsultarAsistenciaAsync(DateTime desde, DateTime hasta, int? idEmpleado = null);
        Task<List<Empleado>> ListarEmpleadosAsync();
        Task<List<CuentaAdministrada>> ListarUsuariosAsync();
        Task<List<Sede>> ListarSedesAsync();
        Task<List<Departamento>> ListarDepartamentosAsync();
        Task<List<Cargo>> ListarCargosAsync();
        Task<List<Rol>> ListarRolesAsync();
        Task<List<DispositivoBiometrico>> ListarDispositivosAsync();
        Task<List<Horario>> ListarHorariosAsync();
        Task<List<EmpleadoHorario>> ListarAsignacionesAsync();
        Task<List<TipoNovedad>> ListarTiposNovedadAsync();
        Task<List<Novedad>> ListarNovedadesAsync();
        Task<List<PlantillaAdministrada>> ListarPlantillasAsync();
        Task AgregarAsync<T>(SesionUsuario sesion, T registro) where T : class;
        Task CrearUsuarioAsync(SesionUsuario sesion, Usuario usuario, string clave);
    }

    public sealed class CuentaAdministrada
    {
        public int IdUsuario { get; set; }
        public int IdEmpleado { get; set; }
        public string NombreUsuario { get; set; }
        public string Empleado { get; set; }
        public string Rol { get; set; }
        public string Estado { get; set; }
    }

    public sealed class PlantillaAdministrada
    {
        public int IdPlantilla { get; set; }
        public string Empleado { get; set; }
        public string TipoBiometria { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
