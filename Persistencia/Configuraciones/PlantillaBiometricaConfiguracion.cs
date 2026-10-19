using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class PlantillaBiometricaConfiguracion : EntityTypeConfiguration<PlantillaBiometrica>
    {
        public PlantillaBiometricaConfiguracion()
        {
            ToTable("PLANTILLA_BIOMETRICA");
            HasKey(x => x.IdPlantilla);

            Property(x => x.IdPlantilla).Identidad("ID_PLANTILLA");
            Property(x => x.IdEmpleado).HasColumnName("ID_EMPLEADO").Unico("UQ_PLANTILLA_EMPLEADO");
            Property(x => x.TipoBiometria).Varchar("TIPO_BIOMETRIA", 20).IsRequired();
            Property(x => x.Plantilla).HasColumnName("PLANTILLA").HasColumnType("blob").IsRequired();
            Property(x => x.FechaRegistro).Fecha("FECHA_REGISTRO");

            // 1 a 1 con EMPLEADO: la unicidad la garantiza UQ_PLANTILLA_EMPLEADO.
            HasRequired(x => x.Empleado).WithMany().HasForeignKey(x => x.IdEmpleado);
        }
    }
}
