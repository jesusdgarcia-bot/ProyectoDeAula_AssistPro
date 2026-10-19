using System.Data.Entity.ModelConfiguration;
using Entidades;

namespace Persistencia.Configuraciones
{
    internal class HorarioConfiguracion : EntityTypeConfiguration<Horario>
    {
        public HorarioConfiguracion()
        {
            ToTable("HORARIO");
            HasKey(x => x.IdHorario);

            Property(x => x.IdHorario).Identidad("ID_HORARIO");
            Property(x => x.NombreHorario).Varchar("NOMBRE_HORARIO", 50).IsRequired();
            Property(x => x.HoraEntrada).Fecha("HORA_ENTRADA");
            Property(x => x.HoraSalida).Fecha("HORA_SALIDA");
            Property(x => x.DiasAplicables).Varchar("DIAS_APLICABLES", 30).IsRequired();
        }
    }
}
