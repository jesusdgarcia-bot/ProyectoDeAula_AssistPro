using System;

namespace Entidades
{
    public class RegistroAsistencia
    {
        public int IdRegistro { get; set; }
        public int IdEmpleado { get; set; }
        public int IdDispositivo { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime Hora { get; set; }
        public string TipoMarcacion { get; set; }
        public string MetodoVerificacion { get; set; }

        public virtual Empleado Empleado { get; set; }
        public virtual DispositivoBiometrico Dispositivo { get; set; }
    }
}
