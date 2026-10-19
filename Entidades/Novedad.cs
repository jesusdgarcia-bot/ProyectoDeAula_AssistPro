using System;

namespace Entidades
{
    public class Novedad
    {
        public int IdNovedad { get; set; }
        public int IdEmpleado { get; set; }
        public int IdTipoNovedad { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Motivo { get; set; }
        public string EstadoAprobacion { get; set; }
        public int? IdUsuarioAprueba { get; set; }

        public virtual Empleado Empleado { get; set; }
        public virtual TipoNovedad TipoNovedad { get; set; }
        public virtual Usuario UsuarioAprueba { get; set; }
    }
}
