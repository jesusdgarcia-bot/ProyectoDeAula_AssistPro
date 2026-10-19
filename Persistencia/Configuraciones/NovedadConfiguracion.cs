using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class NovedadConfiguracion : EntityTypeConfiguration<Novedad>
    {
        public NovedadConfiguracion()
        {
            ToTable("NOVEDAD");
            HasKey(x => x.IdNovedad);

            Property(x => x.IdNovedad).Identidad("ID_NOVEDAD");
            Property(x => x.IdEmpleado).HasColumnName("ID_EMPLEADO");
            Property(x => x.IdTipoNovedad).HasColumnName("ID_TIPO_NOVEDAD");
            Property(x => x.FechaInicio).Fecha("FECHA_INICIO");
            Property(x => x.FechaFin).Fecha("FECHA_FIN");
            Property(x => x.Motivo).Varchar("MOTIVO", 200);
            Property(x => x.EstadoAprobacion).Varchar("ESTADO_APROBACION", 15).IsRequired();
            Property(x => x.IdUsuarioAprueba).HasColumnName("ID_USUARIO_APRUEBA");

            HasRequired(x => x.Empleado).WithMany(e => e.Novedades).HasForeignKey(x => x.IdEmpleado);
            HasRequired(x => x.TipoNovedad).WithMany(t => t.Novedades).HasForeignKey(x => x.IdTipoNovedad);
            HasOptional(x => x.UsuarioAprueba).WithMany(u => u.NovedadesAprobadas).HasForeignKey(x => x.IdUsuarioAprueba);
        }
    }
}
