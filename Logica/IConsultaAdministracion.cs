using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entidades;

namespace Logica
{
    public interface IConsultaAdministracion
    {
        Task<List<RegistroAsistencia>> ConsultarAsistenciaAsync(DateTime desde, DateTime hasta);
        Task<List<Empleado>> ListarEmpleadosAsync();
        Task<List<CuentaAdministrada>> ListarUsuariosAsync();
    }

    public sealed class CuentaAdministrada
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; }
        public string Empleado { get; set; }
        public string Rol { get; set; }
        public string Estado { get; set; }
    }
}
