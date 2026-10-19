using System.Collections.Generic;

namespace Entidades
{
    public class DispositivoBiometrico
    {
        public int IdDispositivo { get; set; }
        public int IdSede { get; set; }
        public string NombreDispositivo { get; set; }
        public string TipoDispositivo { get; set; }
        public string DireccionIp { get; set; }
        public string Ubicacion { get; set; }
        public string Estado { get; set; }

        public virtual Sede Sede { get; set; }
        public virtual ICollection<RegistroAsistencia> Registros { get; set; } = new HashSet<RegistroAsistencia>();
    }
}
