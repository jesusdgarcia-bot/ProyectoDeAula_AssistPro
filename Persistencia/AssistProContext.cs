using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using Entidades;
using Persistencia.Configuraciones;

namespace Persistencia
{
    [DbConfigurationType(typeof(OracleDbConfiguration))]
    public class AssistProContext : DbContext
    {
        public const string Esquema = "ASISTENCIA";

        public AssistProContext() : base("name=AssistProContext")
        {
        }

        public DbSet<Sede> Sedes { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<TipoNovedad> TiposNovedad { get; set; }
        public DbSet<Novedad> Novedades { get; set; }
        public DbSet<DispositivoBiometrico> Dispositivos { get; set; }
        public DbSet<RegistroAsistencia> RegistrosAsistencia { get; set; }
        public DbSet<PlantillaBiometrica> PlantillasBiometricas { get; set; }
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<EmpleadoHorario> EmpleadosHorarios { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Esquema);

            // Sin borrado en cascada: los datos de asistencia no deben desaparecer
            // por borrar un empleado, sede, etc.
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            modelBuilder.Configurations.Add(new SedeConfiguracion());
            modelBuilder.Configurations.Add(new DepartamentoConfiguracion());
            modelBuilder.Configurations.Add(new CargoConfiguracion());
            modelBuilder.Configurations.Add(new EmpleadoConfiguracion());
            modelBuilder.Configurations.Add(new RolConfiguracion());
            modelBuilder.Configurations.Add(new UsuarioConfiguracion());
            modelBuilder.Configurations.Add(new TipoNovedadConfiguracion());
            modelBuilder.Configurations.Add(new NovedadConfiguracion());
            modelBuilder.Configurations.Add(new DispositivoBiometricoConfiguracion());
            modelBuilder.Configurations.Add(new RegistroAsistenciaConfiguracion());
            modelBuilder.Configurations.Add(new PlantillaBiometricaConfiguracion());
            modelBuilder.Configurations.Add(new HorarioConfiguracion());
            modelBuilder.Configurations.Add(new EmpleadoHorarioConfiguracion());
        }
    }
}
