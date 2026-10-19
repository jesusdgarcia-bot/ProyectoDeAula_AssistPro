using System;
using System.Collections.Generic;

namespace Entidades
{
    public class Empleado
    {
        public int IdEmpleado { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public DateTime FechaIngreso { get; set; }
        public string Estado { get; set; }
        public int IdDepartamento { get; set; }
        public int IdCargo { get; set; }

        public virtual Departamento Departamento { get; set; }
        public virtual Cargo Cargo { get; set; }
        public virtual ICollection<Novedad> Novedades { get; set; } = new HashSet<Novedad>();
        public virtual ICollection<RegistroAsistencia> Registros { get; set; } = new HashSet<RegistroAsistencia>();
        public virtual ICollection<EmpleadoHorario> Horarios { get; set; } = new HashSet<EmpleadoHorario>();
    }
}
