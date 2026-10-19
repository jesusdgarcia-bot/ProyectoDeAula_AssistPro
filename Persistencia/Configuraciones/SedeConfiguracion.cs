using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class SedeConfiguracion : EntityTypeConfiguration<Sede>
    {
        public SedeConfiguracion()
        {
            ToTable("SEDE");
            HasKey(x => x.IdSede);

            Property(x => x.IdSede).Identidad("ID_SEDE");
            Property(x => x.NombreSede).Varchar("NOMBRE_SEDE", 100).IsRequired();
            Property(x => x.Ciudad).Varchar("CIUDAD", 50).IsRequired();
            Property(x => x.Direccion).Varchar("DIRECCION", 150).IsRequired();
            Property(x => x.Telefono).Varchar("TELEFONO", 20);
        }
    }
}
