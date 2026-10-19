using System.Collections.Generic;

namespace Entidades
{
    public class Departamento
    {
        public int IdDepartamento { get; set; }
        public string NombreDepartamento { get; set; }
        public int IdSede { get; set; }

        public virtual Sede Sede { get; set; }
        public virtual ICollection<Empleado> Empleados { get; set; } = new HashSet<Empleado>();
    }
}
