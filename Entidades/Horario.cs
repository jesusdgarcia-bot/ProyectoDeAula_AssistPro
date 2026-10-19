using System;
using System.Collections.Generic;

namespace Entidades
{
    public class Horario
    {
        public int IdHorario { get; set; }
        public string NombreHorario { get; set; }
        public DateTime HoraEntrada { get; set; }
        public DateTime HoraSalida { get; set; }
        public string DiasAplicables { get; set; }

        public virtual ICollection<EmpleadoHorario> Empleados { get; set; } = new HashSet<EmpleadoHorario>();
    }
}
