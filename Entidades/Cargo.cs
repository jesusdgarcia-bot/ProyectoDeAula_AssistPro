using System.Collections.Generic;

namespace Entidades
{
    public class Cargo
    {
        public int IdCargo { get; set; }
        public string NombreCargo { get; set; }
        public string NivelJerarquico { get; set; }

        public virtual ICollection<Empleado> Empleados { get; set; } = new HashSet<Empleado>();
    }
}
