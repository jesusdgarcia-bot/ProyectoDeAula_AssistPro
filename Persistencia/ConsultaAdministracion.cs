using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using Entidades;
using Logica;
using Oracle.ManagedDataAccess.Client;

namespace Persistencia
{
    public sealed class ConsultaAdministracion : IConsultaAdministracion
    {
        private const int LongitudMinimaClave = 8;
        private readonly string estadoActivo;

        public ConsultaAdministracion() : this("A")
        {
        }

        public ConsultaAdministracion(string estadoActivo)
        {
            if (string.IsNullOrWhiteSpace(estadoActivo) || estadoActivo.Length != 1)
                throw new ArgumentException("El estado activo debe tener un carácter.", nameof(estadoActivo));
            this.estadoActivo = estadoActivo;
        }

        public Task<List<RegistroAsistencia>> ConsultarAsistenciaAsync(DateTime desde, DateTime hasta, int? idEmpleado = null)
        {
            return ConsultaAsistencia.ConsultarRegistrosAsync(desde, hasta, idEmpleado, incluirDetalle: true);
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
                        IdEmpleado = u.IdEmpleado,
                        NombreUsuario = u.NombreUsuario,
                        Empleado = u.Empleado.Nombres + " " + u.Empleado.Apellidos,
                        Rol = u.Rol.NombreRol,
                        Estado = u.Estado
                    }).ToListAsync().ConfigureAwait(false);
            }
        }

        public Task<List<Sede>> ListarSedesAsync() => ListarAsync(c => c.Sedes.OrderBy(s => s.NombreSede));
        public Task<List<Departamento>> ListarDepartamentosAsync() =>
            ListarAsync(c => c.Departamentos.Include(d => d.Sede).OrderBy(d => d.NombreDepartamento));
        public Task<List<Cargo>> ListarCargosAsync() => ListarAsync(c => c.Cargos.OrderBy(cargo => cargo.NombreCargo));
        public Task<List<Rol>> ListarRolesAsync() => ListarAsync(c => c.Roles.OrderBy(r => r.NombreRol));
        public Task<List<DispositivoBiometrico>> ListarDispositivosAsync() =>
            ListarAsync(c => c.Dispositivos.Include(d => d.Sede).OrderBy(d => d.NombreDispositivo));
        public Task<List<Horario>> ListarHorariosAsync() => ListarAsync(c => c.Horarios.OrderBy(h => h.NombreHorario));
        public Task<List<EmpleadoHorario>> ListarAsignacionesAsync() =>
            ListarAsync(c => c.EmpleadosHorarios.Include(a => a.Empleado).Include(a => a.Horario)
                .OrderByDescending(a => a.FechaInicio).ThenByDescending(a => a.IdEmpleadoHorario));
        public Task<List<TipoNovedad>> ListarTiposNovedadAsync() => ListarAsync(c => c.TiposNovedad.OrderBy(t => t.NombreTipo));
        public Task<List<Novedad>> ListarNovedadesAsync() =>
            ListarAsync(c => c.Novedades.Include(n => n.Empleado).Include(n => n.TipoNovedad)
                .OrderByDescending(n => n.FechaInicio).ThenByDescending(n => n.IdNovedad));
        public Task<List<PlantillaAdministrada>> ListarPlantillasAsync() =>
            ListarAsync(c => c.PlantillasBiometricas.OrderByDescending(p => p.FechaRegistro).Select(p => new PlantillaAdministrada
            {
                IdPlantilla = p.IdPlantilla,
                Empleado = p.Empleado.Nombres + " " + p.Empleado.Apellidos,
                TipoBiometria = p.TipoBiometria,
                FechaRegistro = p.FechaRegistro
            }));

        public Task AgregarAsync<T>(SesionUsuario sesion, T registro) where T : class
        {
            if (registro == null) throw new ArgumentNullException(nameof(registro));
            var tipo = typeof(T);
            if (tipo != typeof(Sede) && tipo != typeof(Departamento) && tipo != typeof(Cargo) &&
                tipo != typeof(Empleado) && tipo != typeof(Rol) && tipo != typeof(DispositivoBiometrico) &&
                tipo != typeof(Horario) && tipo != typeof(EmpleadoHorario) && tipo != typeof(TipoNovedad) &&
                tipo != typeof(Novedad) && tipo != typeof(RegistroAsistencia))
                throw new ArgumentException("El tipo de registro no admite el alta en el panel.", nameof(registro));
            return GuardarAsync(sesion, registro);
        }

        public async Task CrearUsuarioAsync(SesionUsuario sesion, Usuario usuario, string clave)
        {
            if (usuario == null) throw new ArgumentNullException(nameof(usuario));
            ExigirSesionAdministrativa(sesion);
            if (clave == null || clave.Length < LongitudMinimaClave)
                throw new ArgumentException($"La contraseña debe tener al menos {LongitudMinimaClave} caracteres.");
            usuario.ContrasenaHash = await Task.Run(() => new ClavesPbkdf2().CrearHash(clave)).ConfigureAwait(false);
            await GuardarAsync(sesion, usuario).ConfigureAwait(false);
        }

        private static async Task<List<T>> ListarAsync<T>(Func<AssistProContext, IQueryable<T>> consulta) where T : class
        {
            using (var contexto = new AssistProContext())
            {
                contexto.Configuration.LazyLoadingEnabled = false;
                contexto.Configuration.ProxyCreationEnabled = false;
                return await consulta(contexto).AsNoTracking().ToListAsync().ConfigureAwait(false);
            }
        }

        private async Task GuardarAsync<T>(SesionUsuario sesion, T registro) where T : class
        {
            ExigirSesionAdministrativa(sesion);
            using (var contexto = new AssistProContext())
            {
                var acceso = await contexto.Usuarios.AsNoTracking()
                    .Where(u => u.IdUsuario == sesion.IdUsuario && u.IdEmpleado == sesion.IdEmpleado)
                    .Select(u => new { u.Estado, EstadoEmpleado = u.Empleado.Estado, u.Rol.NombreRol })
                    .SingleOrDefaultAsync().ConfigureAwait(false);
                if (acceso == null || acceso.Estado?.Trim() != estadoActivo ||
                    acceso.EstadoEmpleado?.Trim() != estadoActivo || !SesionUsuario.EsRolAdministrador(acceso.NombreRol))
                    throw new UnauthorizedAccessException("La cuenta ya no tiene acceso administrativo.");

                contexto.Set<T>().Add(registro);
                if (contexto.ChangeTracker.Entries().Count() != 1)
                    throw new ArgumentException("Selecciona las referencias existentes antes de guardar.");
                try
                {
                    await contexto.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (DbEntityValidationException ex)
                {
                    var mensaje = ex.EntityValidationErrors.SelectMany(e => e.ValidationErrors)
                        .Select(e => e.ErrorMessage).FirstOrDefault();
                    throw new InvalidOperationException(mensaje ?? "Revisa los campos del registro.", ex);
                }
                catch (DbUpdateException ex) when (ex.GetBaseException() is OracleException)
                {
                    var error = (OracleException)ex.GetBaseException();
                    if (error.Number == 1)
                        throw new InvalidOperationException("Ya existe un registro con esos datos únicos.", ex);
                    if (error.Number == 2291)
                        throw new InvalidOperationException("Una de las referencias seleccionadas ya no existe. Abre el formulario de nuevo.", ex);
                    if (error.Number == 12899)
                        throw new InvalidOperationException("Uno de los campos supera el tamaño permitido por Oracle.", ex);
                    throw;
                }
            }
        }

        private static void ExigirSesionAdministrativa(SesionUsuario sesion)
        {
            if (sesion == null || !sesion.EsAdministrador)
                throw new UnauthorizedAccessException("Se requiere una cuenta de administrador.");
        }
    }
}
