using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class DepartamentoConfiguracion : EntityTypeConfiguration<Departamento>
    {
        public DepartamentoConfiguracion()
        {
            ToTable("DEPARTAMENTO");
            HasKey(x => x.IdDepartamento);

            Property(x => x.IdDepartamento).Identidad("ID_DEPARTAMENTO");
            Property(x => x.NombreDepartamento).Varchar("NOMBRE_DEPARTAMENTO", 80).IsRequired();
            Property(x => x.IdSede).HasColumnName("ID_SEDE");

            HasRequired(x => x.Sede).WithMany(s => s.Departamentos).HasForeignKey(x => x.IdSede);
        }
    }
}
