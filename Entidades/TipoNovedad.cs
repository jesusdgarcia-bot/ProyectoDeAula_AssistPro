using System.Collections.Generic;

namespace Entidades
{
    public class TipoNovedad
    {
        public int IdTipoNovedad { get; set; }
        public string NombreTipo { get; set; }
        public string Descripcion { get; set; }

        public virtual ICollection<Novedad> Novedades { get; set; } = new HashSet<Novedad>();
    }
}
