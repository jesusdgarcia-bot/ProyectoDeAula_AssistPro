using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class EmpleadoHorarioConfiguracion : EntityTypeConfiguration<EmpleadoHorario>
    {
        public EmpleadoHorarioConfiguracion()
        {
            ToTable("EMPLEADO_HORARIO");
            HasKey(x => x.IdEmpleadoHorario);

            Property(x => x.IdEmpleadoHorario).Identidad("ID_EMPLEADO_HORARIO");
            Property(x => x.IdEmpleado).HasColumnName("ID_EMPLEADO");
            Property(x => x.IdHorario).HasColumnName("ID_HORARIO");
            Property(x => x.FechaInicio).Fecha("FECHA_INICIO");
            Property(x => x.FechaFin).Fecha("FECHA_FIN");

            HasRequired(x => x.Empleado).WithMany(e => e.Horarios).HasForeignKey(x => x.IdEmpleado);
            HasRequired(x => x.Horario).WithMany(h => h.Empleados).HasForeignKey(x => x.IdHorario);
        }
    }
}
