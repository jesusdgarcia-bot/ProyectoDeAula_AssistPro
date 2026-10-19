using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class DispositivoBiometricoConfiguracion : EntityTypeConfiguration<DispositivoBiometrico>
    {
        public DispositivoBiometricoConfiguracion()
        {
            ToTable("DISPOSITIVO_BIOMETRICO");
            HasKey(x => x.IdDispositivo);

            Property(x => x.IdDispositivo).Identidad("ID_DISPOSITIVO");
            Property(x => x.IdSede).HasColumnName("ID_SEDE");
            Property(x => x.NombreDispositivo).Varchar("NOMBRE_DISPOSITIVO", 60).IsRequired();
            Property(x => x.TipoDispositivo).Varchar("TIPO_DISPOSITIVO", 20).IsRequired();
            Property(x => x.DireccionIp).Varchar("DIRECCION_IP", 15);
            Property(x => x.Ubicacion).Varchar("UBICACION", 80);
            Property(x => x.Estado).Char("ESTADO", 1).IsRequired();

            HasRequired(x => x.Sede).WithMany(s => s.Dispositivos).HasForeignKey(x => x.IdSede);
        }
    }
}
