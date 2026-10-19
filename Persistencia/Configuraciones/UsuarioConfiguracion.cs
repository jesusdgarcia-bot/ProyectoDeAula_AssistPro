using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class UsuarioConfiguracion : EntityTypeConfiguration<Usuario>
    {
        public UsuarioConfiguracion()
        {
            ToTable("USUARIO");
            HasKey(x => x.IdUsuario);

            Property(x => x.IdUsuario).Identidad("ID_USUARIO");
            Property(x => x.IdEmpleado).HasColumnName("ID_EMPLEADO").Unico("UQ_USUARIO_EMPLEADO");
            Property(x => x.IdRol).HasColumnName("ID_ROL");
            Property(x => x.NombreUsuario).Varchar("NOMBRE_USUARIO", 30).IsRequired().Unico("UQ_USUARIO_NOMBRE");
            Property(x => x.ContrasenaHash).Varchar("CONTRASENA_HASH", 255).IsRequired();
            Property(x => x.CorreoAcceso).Varchar("CORREO_ACCESO", 100).IsRequired().Unico("UQ_USUARIO_CORREO");
            Property(x => x.Estado).Char("ESTADO", 1).IsRequired();

            // 1 a 1 con EMPLEADO: la unicidad la garantiza UQ_USUARIO_EMPLEADO.
            HasRequired(x => x.Empleado).WithMany().HasForeignKey(x => x.IdEmpleado);
            HasRequired(x => x.Rol).WithMany(r => r.Usuarios).HasForeignKey(x => x.IdRol);
        }
    }
}
