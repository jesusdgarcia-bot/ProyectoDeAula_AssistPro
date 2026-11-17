using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Entidades;
using Logica;

namespace Persistencia
{
    public sealed class ConsultaAdministracion : IConsultaAdministracion
    {
        public Task<List<RegistroAsistencia>> ConsultarAsistenciaAsync(DateTime desde, DateTime hasta)
        {
            return ConsultaAsistencia.ConsultarRegistrosAsync(desde, hasta, null);
        }

        public async Task<List<Empleado>> ListarEmpleadosAsync()
        {
            using (var contexto = new AssistProContext())
            {
                contexto.Configuration.LazyLoadingEnabled = false;
                contexto.Configuration.ProxyCreationEnabled = false;
                return await contexto.Empleados.AsNoTracking().Include(e => e.Cargo).Include(e => e.Departamento)
                    .OrderBy(e => e.Apellidos).ThenBy(e => e.Nombres).ThenBy(e => e.IdEmpleado)
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        public async Task<List<CuentaAdministrada>> ListarUsuariosAsync()
        {
            using (var contexto = new AssistProContext())
            {
                return await contexto.Usuarios.AsNoTracking().OrderBy(u => u.NombreUsuario)
                    .Select(u => new CuentaAdministrada
                    {
                        IdUsuario = u.IdUsuario,
                        NombreUsuario = u.NombreUsuario,
                        Empleado = u.Empleado.Nombres + " " + u.Empleado.Apellidos,
                        Rol = u.Rol.NombreRol,
                        Estado = u.Estado
                    }).ToListAsync().ConfigureAwait(false);
            }
        }
    }
}
