using System.Collections.Generic;

namespace Entidades
{
    public class Sede
    {
        public int IdSede { get; set; }
        public string NombreSede { get; set; }
        public string Ciudad { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }

        public virtual ICollection<Departamento> Departamentos { get; set; } = new HashSet<Departamento>();
        public virtual ICollection<DispositivoBiometrico> Dispositivos { get; set; } = new HashSet<DispositivoBiometrico>();
    }
}
