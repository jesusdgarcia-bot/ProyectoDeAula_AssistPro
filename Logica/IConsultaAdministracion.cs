using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entidades;

namespace Logica
{
    public interface IConsultaAdministracion
    {
        Task<ResumenDashboard> ObtenerDashboardAsync(SesionUsuario sesion, DateTime desde, DateTime hasta);
        Task<List<RegistroAsistencia>> ConsultarAsistenciaAsync(DateTime desde, DateTime hasta, int? idEmpleado = null,
            string busqueda = null);
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
        Task ActualizarAsync<T>(SesionUsuario sesion, T registro, T original) where T : class;
        Task ActualizarUsuarioAsync(SesionUsuario sesion, Usuario usuario, CuentaAdministrada original,
            string nuevaClave = null);
        Task EliminarAsync<T>(SesionUsuario sesion, int id) where T : class;
    }

    public sealed class CuentaAdministrada
    {
        public int IdUsuario { get; set; }
        public int IdEmpleado { get; set; }
        public int IdRol { get; set; }
        public string NombreUsuario { get; set; }
        public string CorreoAcceso { get; set; }
        public string Empleado { get; set; }
        public string Rol { get; set; }
        public string Estado { get; set; }
    }

    public sealed class PlantillaAdministrada
    {
        public int IdPlantilla { get; set; }
        public int IdEmpleado { get; set; }
        public string Empleado { get; set; }
        public string TipoBiometria { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
