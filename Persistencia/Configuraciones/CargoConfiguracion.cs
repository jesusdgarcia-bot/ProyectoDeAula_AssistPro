using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class CargoConfiguracion : EntityTypeConfiguration<Cargo>
    {
        public CargoConfiguracion()
        {
            ToTable("CARGO");
            HasKey(x => x.IdCargo);

            Property(x => x.IdCargo).Identidad("ID_CARGO");
            Property(x => x.NombreCargo).Varchar("NOMBRE_CARGO", 80).IsRequired();
            Property(x => x.NivelJerarquico).Varchar("NIVEL_JERARQUICO", 30);
        }
    }
}
