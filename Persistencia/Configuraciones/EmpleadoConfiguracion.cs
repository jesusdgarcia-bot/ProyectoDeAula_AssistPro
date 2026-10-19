using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class EmpleadoConfiguracion : EntityTypeConfiguration<Empleado>
    {
        public EmpleadoConfiguracion()
        {
            ToTable("EMPLEADO");
            HasKey(x => x.IdEmpleado);

            Property(x => x.IdEmpleado).Identidad("ID_EMPLEADO");
            Property(x => x.TipoDocumento).Char("TIPO_DOCUMENTO", 2).IsRequired();
            Property(x => x.NumeroDocumento).Varchar("NUMERO_DOCUMENTO", 20).IsRequired().Unico("UQ_EMPLEADO_NUMDOC");
            Property(x => x.Nombres).Varchar("NOMBRES", 60).IsRequired();
            Property(x => x.Apellidos).Varchar("APELLIDOS", 60).IsRequired();
            Property(x => x.FechaNacimiento).Fecha("FECHA_NACIMIENTO");
            Property(x => x.Correo).Varchar("CORREO", 100).Unico("UQ_EMPLEADO_CORREO");
            Property(x => x.Telefono).Varchar("TELEFONO", 20);
            Property(x => x.FechaIngreso).Fecha("FECHA_INGRESO");
            Property(x => x.Estado).Char("ESTADO", 1).IsRequired();
            Property(x => x.IdDepartamento).HasColumnName("ID_DEPARTAMENTO");
            Property(x => x.IdCargo).HasColumnName("ID_CARGO");

            HasRequired(x => x.Departamento).WithMany(d => d.Empleados).HasForeignKey(x => x.IdDepartamento);
            HasRequired(x => x.Cargo).WithMany(c => c.Empleados).HasForeignKey(x => x.IdCargo);
        }
    }
}
