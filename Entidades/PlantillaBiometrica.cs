using System;

namespace Entidades
{
    public class PlantillaBiometrica
    {
        public int IdPlantilla { get; set; }
        public int IdEmpleado { get; set; }
        public string TipoBiometria { get; set; }
        public byte[] Plantilla { get; set; }
        public DateTime FechaRegistro { get; set; }

        public virtual Empleado Empleado { get; set; }
    }
}
