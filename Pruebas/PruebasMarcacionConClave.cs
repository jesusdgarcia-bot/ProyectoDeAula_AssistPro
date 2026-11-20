using System;
using System.Threading.Tasks;
using Entidades;
using Logica;

internal static class PruebasMarcacionConClave
{
    public static async Task Comprobar()
    {
        const string clave = " prueba de acceso ";
        var verificador = new ClavesPbkdf2();
        var cuenta = new Usuario
        {
            IdUsuario = 6,
            IdEmpleado = 8,
            IdRol = 1,
            NombreUsuario = "personal.prueba",
            ContrasenaHash = verificador.CrearHash(clave),
            Estado = "A",
            Empleado = new Empleado { IdEmpleado = 8, Estado = "A" },
            Rol = new Rol { NombreRol = "Empleado" }
        };
        var repo = new Repositorio(cuenta);
        var servicio = new ServicioMarcacionConClave(new ServicioAutenticacion(repo, verificador, "A"), repo);
        var sesion = new SesionUsuario(cuenta);

        await ExigirRechazoAsync(() => servicio.RegistrarAsync(sesion, 41, "Entrada", clave.Trim()));
        cuenta.Estado = "I";
        await ExigirRechazoAsync(() => servicio.RegistrarAsync(sesion, 41, "Entrada", clave));
        cuenta.Estado = "A";
        cuenta.IdEmpleado = 9;
        await ExigirRechazoAsync(() => servicio.RegistrarAsync(sesion, 41, "Entrada", clave));
        cuenta.IdEmpleado = 8;
        if (repo.Escrituras != 0)
            throw new InvalidOperationException("Una contraseña incorrecta o una cuenta cambiada no puede registrar asistencia.");

        var registro = await servicio.RegistrarAsync(sesion, 41, "Entrada", clave);
        if (repo.Escrituras != 1 || registro.IdEmpleado != 8 || registro.IdDispositivo != 41 ||
            registro.TipoMarcacion != "Entrada" || registro.MetodoVerificacion != "Clave")
            throw new InvalidOperationException("La marcación debe usar la sesión verificada y el terminal indicado.");
    }

    private static async Task ExigirRechazoAsync(Func<Task> marcar)
    {
        try { await marcar(); }
        catch (UnauthorizedAccessException) { return; }
        throw new InvalidOperationException("La marcación debía rechazar la solicitud.");
    }

    private sealed class Repositorio : IRepositorioUsuarios, IRepositorioMarcacionConClave
    {
        private readonly Usuario cuenta;

        public Repositorio(Usuario cuenta) { this.cuenta = cuenta; }

        public int Escrituras { get; private set; }

        public Task<Usuario> BuscarAsync(string nombreUsuario)
        {
            return Task.FromResult(nombreUsuario == cuenta.NombreUsuario ? cuenta : null);
        }

        public Task<RegistroAsistencia> GuardarMarcacionConClaveAsync(SesionUsuario identidad, int idDispositivo, string tipoMarcacion)
        {
            Escrituras++;
            return Task.FromResult(new RegistroAsistencia
            {
                IdEmpleado = identidad.IdEmpleado,
                IdDispositivo = idDispositivo,
                TipoMarcacion = tipoMarcacion,
                MetodoVerificacion = "Clave"
            });
        }
    }
}
