using System.Collections.Generic;

namespace Entidades
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public int IdEmpleado { get; set; }
        public int IdRol { get; set; }
        public string NombreUsuario { get; set; }
        public string ContrasenaHash { get; set; }
        public string CorreoAcceso { get; set; }
        public string Estado { get; set; }

        public virtual Empleado Empleado { get; set; }
        public virtual Rol Rol { get; set; }
        public virtual ICollection<Novedad> NovedadesAprobadas { get; set; } = new HashSet<Novedad>();
    }
}
