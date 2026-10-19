using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class TipoNovedadConfiguracion : EntityTypeConfiguration<TipoNovedad>
    {
        public TipoNovedadConfiguracion()
        {
            ToTable("TIPO_NOVEDAD");
            HasKey(x => x.IdTipoNovedad);

            Property(x => x.IdTipoNovedad).Identidad("ID_TIPO_NOVEDAD");
            Property(x => x.NombreTipo).Varchar("NOMBRE_TIPO", 40).IsRequired();
            Property(x => x.Descripcion).Varchar("DESCRIPCION", 150);
        }
    }
}
