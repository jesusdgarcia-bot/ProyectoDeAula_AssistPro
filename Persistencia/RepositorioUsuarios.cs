using System.Data.Entity;
using System.Threading.Tasks;
using Entidades;
using Logica;

namespace Persistencia
{
    public sealed class RepositorioUsuarios : IRepositorioUsuarios
    {
        // NOMBRE_USUARIO mide 30 en la base: un valor más largo no puede existir y ni se consulta.
        private const int LongitudMaximaUsuario = 30;

        public async Task<Usuario> BuscarAsync(string nombreUsuario)
        {
            if (string.IsNullOrEmpty(nombreUsuario) || nombreUsuario.Length > LongitudMaximaUsuario) return null;

            using (var contexto = new AssistProContext())
            {
                contexto.Configuration.LazyLoadingEnabled = false;
                contexto.Configuration.ProxyCreationEnabled = false;
                return await contexto.Usuarios.AsNoTracking()
                    .Include(u => u.Empleado).Include(u => u.Rol)
                    .SingleOrDefaultAsync(u => u.NombreUsuario == nombreUsuario)
                    .ConfigureAwait(false);
            }
        }
    }
}
