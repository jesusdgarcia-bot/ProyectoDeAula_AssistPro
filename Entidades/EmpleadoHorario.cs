using System;

namespace Entidades
{
    public class EmpleadoHorario
    {
        public int IdEmpleadoHorario { get; set; }
        public int IdEmpleado { get; set; }
        public int IdHorario { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public virtual Empleado Empleado { get; set; }
        public virtual Horario Horario { get; set; }
    }
}
