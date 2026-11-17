using System;
using System.Threading.Tasks;
using Entidades;

namespace Logica
{
    public interface IRepositorioUsuarios
    {
        Task<Usuario> BuscarAsync(string nombreUsuario);
    }

    public interface IVerificadorClave
    {
        bool Verificar(string clave, string hash);
    }

    public sealed class SesionUsuario
    {
        public int IdUsuario { get; private set; }
        public int IdEmpleado { get; private set; }
        public int IdRol { get; private set; }
        public string Nombre { get; private set; }
        public string Rol { get; private set; }
        public bool EsAdministrador => EsRolAdministrador(Rol);

        public static bool EsRolAdministrador(string rol)
        {
            var nombre = rol?.Trim();
            return string.Equals(nombre, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(nombre, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        public SesionUsuario(Usuario usuario)
        {
            IdUsuario = usuario.IdUsuario;
            IdEmpleado = usuario.IdEmpleado;
            IdRol = usuario.IdRol;
            Nombre = usuario.Empleado.Nombres + " " + usuario.Empleado.Apellidos;
            Rol = usuario.Rol.NombreRol;
        }
    }

    public sealed class ServicioAutenticacion
    {
        private readonly IRepositorioUsuarios repositorio;
        private readonly IVerificadorClave verificador;
        private readonly string estadoActivo;

        public ServicioAutenticacion(IRepositorioUsuarios repositorio, IVerificadorClave verificador, string estadoActivo)
        {
            this.repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            this.verificador = verificador ?? throw new ArgumentNullException(nameof(verificador));
            if (string.IsNullOrWhiteSpace(estadoActivo) || estadoActivo.Length != 1)
                throw new ArgumentException("El estado activo debe tener un carácter.", nameof(estadoActivo));
            this.estadoActivo = estadoActivo;
        }

        public async Task<SesionUsuario> AutenticarAsync(string nombre, string clave)
        {
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 30 ||
                string.IsNullOrEmpty(clave) || clave.Length > 1024)
                return null;

            var usuario = await repositorio.BuscarAsync(nombre.Trim()).ConfigureAwait(false);
            // El verificador también realiza trabajo criptográfico para usuarios inexistentes.
            bool coincide = await Task.Run(() => verificador.Verificar(clave, usuario?.ContrasenaHash)).ConfigureAwait(false);
            if (!coincide || usuario == null || usuario.Estado?.Trim() != estadoActivo ||
                usuario.Empleado == null || usuario.Empleado.Estado?.Trim() != estadoActivo || usuario.Rol == null)
                return null;

            return new SesionUsuario(usuario);
        }
    }
}
