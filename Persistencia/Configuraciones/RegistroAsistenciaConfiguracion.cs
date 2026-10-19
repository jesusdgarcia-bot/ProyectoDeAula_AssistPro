using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class RegistroAsistenciaConfiguracion : EntityTypeConfiguration<RegistroAsistencia>
    {
        public RegistroAsistenciaConfiguracion()
        {
            ToTable("REGISTRO_ASISTENCIA");
            HasKey(x => x.IdRegistro);

            Property(x => x.IdRegistro).Identidad("ID_REGISTRO");
            Property(x => x.IdEmpleado).HasColumnName("ID_EMPLEADO");
            Property(x => x.IdDispositivo).HasColumnName("ID_DISPOSITIVO");
            Property(x => x.Fecha).Fecha("FECHA");
            Property(x => x.Hora).Fecha("HORA");
            Property(x => x.TipoMarcacion).Varchar("TIPO_MARCACION", 10).IsRequired();
            Property(x => x.MetodoVerificacion).Varchar("METODO_VERIFICACION", 20).IsRequired();

            HasRequired(x => x.Empleado).WithMany(e => e.Registros).HasForeignKey(x => x.IdEmpleado);
            HasRequired(x => x.Dispositivo).WithMany(d => d.Registros).HasForeignKey(x => x.IdDispositivo);
        }
    }
}
