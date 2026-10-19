using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class RolConfiguracion : EntityTypeConfiguration<Rol>
    {
        public RolConfiguracion()
        {
            ToTable("ROL");
            HasKey(x => x.IdRol);

            Property(x => x.IdRol).Identidad("ID_ROL");
            Property(x => x.NombreRol).Varchar("NOMBRE_ROL", 30).IsRequired();
            Property(x => x.Descripcion).Varchar("DESCRIPCION", 150);
        }
    }
}
