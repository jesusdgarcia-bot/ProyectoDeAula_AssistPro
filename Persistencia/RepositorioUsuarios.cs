using System.Data.Entity;
using System.Threading.Tasks;
using Entidades;
using Logica;

namespace Persistencia
{
    public sealed class RepositorioUsuarios : IRepositorioUsuarios
    {
        public async Task<Usuario> BuscarAsync(string nombreUsuario)
        {
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
