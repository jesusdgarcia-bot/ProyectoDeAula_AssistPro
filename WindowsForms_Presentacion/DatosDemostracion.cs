using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entidades;
using Logica;

namespace WindowsForms_Presentacion
{
    internal sealed class DatosDemostracion : IRepositorioUsuarios, IConsultaAsistencia, IConsultaAdministracion
    {
        private const string EstadoActivo = "A";
        private readonly Usuario[] usuarios;
        private readonly RegistroAsistencia[] registros;

        private DatosDemostracion(ClavesPbkdf2 claves)
        {
            var empleado = CrearUsuario(1, "demo", claves.CrearHash("AssistProDemo26!"), "Empleado de demostración", new Empleado
            {
                IdEmpleado = 1,
                Nombres = "Laura",
                Apellidos = "Mendoza",
                NumeroDocumento = "1000",
                Estado = EstadoActivo,
                Departamento = new Departamento { NombreDepartamento = "Operaciones" },
                Cargo = new Cargo { NombreCargo = "Auxiliar" }
            });
            var administrador = CrearUsuario(2, "admin", claves.CrearHash("AssistProAdmin26!"), "Administrador", new Empleado
            {
                IdEmpleado = 2,
                Nombres = "Daniel",
                Apellidos = "Rojas",
                NumeroDocumento = "1001",
                Estado = EstadoActivo,
                Departamento = new Departamento { NombreDepartamento = "Administración" },
                Cargo = new Cargo { NombreCargo = "Administrador" }
            });
            usuarios = new[] { empleado, administrador };

            DateTime hoy = DateTime.Today;
            registros = new[]
            {
                CrearRegistro(8, administrador.Empleado, hoy.AddDays(-1).AddHours(17).AddMinutes(15), "Salida"),
                CrearRegistro(7, administrador.Empleado, hoy.AddDays(-1).AddHours(8).AddMinutes(10), "Entrada"),
                CrearRegistro(6, empleado.Empleado, hoy.AddDays(-1).AddHours(17).AddMinutes(5), "Salida"),
                CrearRegistro(5, empleado.Empleado, hoy.AddDays(-1).AddHours(8).AddMinutes(1), "Entrada"),
                CrearRegistro(4, empleado.Empleado, hoy.AddDays(-2).AddHours(17).AddMinutes(8), "Salida"),
                CrearRegistro(3, empleado.Empleado, hoy.AddDays(-2).AddHours(7).AddMinutes(56), "Entrada"),
                CrearRegistro(2, empleado.Empleado, hoy.AddDays(-3).AddHours(17).AddMinutes(2), "Salida"),
                CrearRegistro(1, empleado.Empleado, hoy.AddDays(-3).AddHours(8), "Entrada")
            };
        }

        internal static Form1 CrearFormulario()
        {
            var claves = new ClavesPbkdf2();
            var datos = new DatosDemostracion(claves);
            var autenticacion = new ServicioAutenticacion(datos, claves, EstadoActivo);
            return new Form1(autenticacion, datos, datos) { Text = "AssistPRO | Acceso de demostración" };
        }

        public Task<Usuario> BuscarAsync(string nombreUsuario)
        {
            return Task.FromResult(usuarios.SingleOrDefault(usuario =>
                string.Equals(usuario.NombreUsuario, nombreUsuario, StringComparison.Ordinal)));
        }

        public Task<List<RegistroAsistencia>> ConsultarAsync(int idEmpleado, DateTime desde, DateTime hasta)
        {
            return Task.FromResult(ConsultarRegistros(desde, hasta, idEmpleado));
        }

        public Task<List<RegistroAsistencia>> ConsultarAsistenciaAsync(DateTime desde, DateTime hasta)
        {
            return Task.FromResult(ConsultarRegistros(desde, hasta, null));
        }

        public Task<List<Empleado>> ListarEmpleadosAsync()
        {
            return Task.FromResult(usuarios.Select(usuario => usuario.Empleado).ToList());
        }

        public Task<List<CuentaAdministrada>> ListarUsuariosAsync()
        {
            return Task.FromResult(usuarios.OrderBy(usuario => usuario.NombreUsuario).Select(usuario => new CuentaAdministrada
            {
                IdUsuario = usuario.IdUsuario,
                NombreUsuario = usuario.NombreUsuario,
                Empleado = usuario.Empleado.Nombres + " " + usuario.Empleado.Apellidos,
                Rol = usuario.Rol.NombreRol,
                Estado = usuario.Estado
            }).ToList());
        }

        private List<RegistroAsistencia> ConsultarRegistros(DateTime desde, DateTime hasta, int? idEmpleado)
        {
            return registros.Where(registro => (!idEmpleado.HasValue || registro.IdEmpleado == idEmpleado.Value) &&
                registro.Fecha >= desde.Date && registro.Fecha <= hasta.Date)
                .OrderByDescending(registro => registro.Hora).ThenByDescending(registro => registro.IdRegistro).ToList();
        }

        private static Usuario CrearUsuario(int id, string nombreUsuario, string hash, string rol, Empleado empleado)
        {
            return new Usuario
            {
                IdUsuario = id,
                IdEmpleado = empleado.IdEmpleado,
                IdRol = id,
                NombreUsuario = nombreUsuario,
                ContrasenaHash = hash,
                Estado = EstadoActivo,
                Empleado = empleado,
                Rol = new Rol { IdRol = id, NombreRol = rol }
            };
        }

        private static RegistroAsistencia CrearRegistro(int idRegistro, Empleado empleado, DateTime momento, string tipo)
        {
            return new RegistroAsistencia
            {
                IdRegistro = idRegistro,
                IdEmpleado = empleado.IdEmpleado,
                Empleado = empleado,
                Fecha = momento.Date,
                Hora = momento,
                TipoMarcacion = tipo,
                MetodoVerificacion = "Manual"
            };
        }
    }
}
