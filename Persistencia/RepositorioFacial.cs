using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using Entidades;
using Logica;
using Oracle.ManagedDataAccess.Client;

namespace Persistencia
{
    public sealed class RepositorioFacial : IRepositorioFacial
    {
        private readonly string estadoActivo;

        public RepositorioFacial(string estadoActivo = "A")
        {
            if (string.IsNullOrWhiteSpace(estadoActivo) || estadoActivo.Length != 1)
                throw new ArgumentException("El estado activo debe tener un carácter.", nameof(estadoActivo));
            this.estadoActivo = estadoActivo;
        }

        public async Task<List<CandidatoFacial>> ListarCandidatosActivosAsync()
        {
            using (var contexto = CrearContexto())
            {
                // La proyección evita cargar hashes de contraseña o imágenes.
                var datos = await (from plantilla in contexto.PlantillasBiometricas.AsNoTracking()
                                   join usuario in contexto.Usuarios.AsNoTracking() on plantilla.IdEmpleado equals usuario.IdEmpleado
                                   where plantilla.TipoBiometria == PlantillaFacial.TipoBiometria &&
                                       usuario.Estado == estadoActivo && usuario.Empleado.Estado == estadoActivo
                                   select new
                                   {
                                       usuario.IdUsuario, usuario.IdEmpleado, usuario.IdRol, usuario.Estado,
                                       EstadoEmpleado = usuario.Empleado.Estado,
                                       usuario.Empleado.Nombres, usuario.Empleado.Apellidos,
                                       usuario.Rol.NombreRol, Datos = plantilla.Plantilla
                                   }).ToListAsync().ConfigureAwait(false);
                return datos.Select(d => new CandidatoFacial
                {
                    Usuario = new Usuario
                    {
                        IdUsuario = d.IdUsuario, IdEmpleado = d.IdEmpleado, IdRol = d.IdRol, Estado = d.Estado,
                        Empleado = new Empleado
                        {
                            IdEmpleado = d.IdEmpleado, Nombres = d.Nombres, Apellidos = d.Apellidos, Estado = d.EstadoEmpleado
                        },
                        Rol = new Rol { IdRol = d.IdRol, NombreRol = d.NombreRol }
                    },
                    DatosPlantilla = d.Datos
                }).ToList();
            }
        }

        public async Task<Usuario> BuscarUsuarioActivoAsync(int idUsuario)
        {
            using (var contexto = CrearContexto())
            {
                return await contexto.Usuarios.AsNoTracking().Include(u => u.Empleado).Include(u => u.Rol)
                    .SingleOrDefaultAsync(u => u.IdUsuario == idUsuario && u.Estado == estadoActivo &&
                        u.Empleado.Estado == estadoActivo).ConfigureAwait(false);
            }
        }

        public async Task GuardarAsync(SesionUsuario administrador, int idEmpleado, byte[] datosPlantilla)
        {
            if (administrador == null || !administrador.EsAdministrador)
                throw new UnauthorizedAccessException("Se requiere una cuenta de administrador para registrar rostros.");
            PlantillaFacial plantillaValidada;
            if (idEmpleado <= 0 || !PlantillaFacial.IntentarLeer(datosPlantilla, out plantillaValidada))
                throw new ArgumentException("Los datos de la plantilla facial no son válidos.");

            using (var contexto = CrearContexto())
            {
                await contexto.Database.Connection.OpenAsync().ConfigureAwait(false);
                using (var transaccion = contexto.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    var acceso = await contexto.Usuarios.AsNoTracking()
                        .Where(u => u.IdUsuario == administrador.IdUsuario && u.IdEmpleado == administrador.IdEmpleado)
                        .Select(u => new { u.Estado, EstadoEmpleado = u.Empleado.Estado, u.Rol.NombreRol })
                        .SingleOrDefaultAsync().ConfigureAwait(false);
                    if (acceso == null || acceso.Estado?.Trim() != estadoActivo || acceso.EstadoEmpleado?.Trim() != estadoActivo ||
                        !SesionUsuario.EsRolAdministrador(acceso.NombreRol))
                        throw new UnauthorizedAccessException("La cuenta ya no tiene acceso administrativo.");

                    bool empleadoActivo = await contexto.Usuarios.AnyAsync(u => u.IdEmpleado == idEmpleado &&
                        u.Estado == estadoActivo && u.Empleado.Estado == estadoActivo).ConfigureAwait(false);
                    if (!empleadoActivo)
                        throw new InvalidOperationException("El empleado debe tener una cuenta activa y estar activo para registrar su rostro.");

                    var existente = await contexto.PlantillasBiometricas.SingleOrDefaultAsync(p => p.IdEmpleado == idEmpleado)
                        .ConfigureAwait(false);
                    if (existente != null && !string.Equals(existente.TipoBiometria?.Trim(), PlantillaFacial.TipoBiometria, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Este empleado ya tiene otra biometría registrada. La base admite una sola plantilla por empleado; su registro no se modificó.");
                    if (existente == null)
                    {
                        existente = new PlantillaBiometrica { IdEmpleado = idEmpleado };
                        contexto.PlantillasBiometricas.Add(existente);
                    }
                    existente.TipoBiometria = PlantillaFacial.TipoBiometria;
                    existente.Plantilla = plantillaValidada.Serializar();
                    existente.FechaRegistro = DateTime.Now;
                    try
                    {
                        await contexto.SaveChangesAsync().ConfigureAwait(false);
                        transaccion.Commit();
                    }
                    catch (DbUpdateException ex) when (ex.GetBaseException() is OracleException)
                    {
                        var error = (OracleException)ex.GetBaseException();
                        if (error.Number == 1 || error.Number == 8177)
                            throw new InvalidOperationException("Otro proceso modificó las plantillas. Actualiza la lista y vuelve a registrar el rostro.", ex);
                        throw;
                    }
                }
            }
        }

        private static AssistProContext CrearContexto()
        {
            var contexto = new AssistProContext();
            contexto.Configuration.LazyLoadingEnabled = false;
            contexto.Configuration.ProxyCreationEnabled = false;
            return contexto;
        }
    }
}
