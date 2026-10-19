using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Configuration;

namespace Persistencia.Configuraciones
{
    /// <summary>Atajos para mapear los tipos de Oracle usados en el diagrama.</summary>
    internal static class ExtensionesConfiguracion
    {
        public static PrimitivePropertyConfiguration Identidad(this PrimitivePropertyConfiguration p, string columna)
        {
            return p.HasColumnName(columna).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        }

        /// <summary>VARCHAR2(longitud)</summary>
        public static StringPropertyConfiguration Varchar(this StringPropertyConfiguration p, string columna, int longitud)
        {
            return p.HasColumnName(columna).HasMaxLength(longitud).IsUnicode(false);
        }

        /// <summary>CHAR(longitud)</summary>
        public static StringPropertyConfiguration Char(this StringPropertyConfiguration p, string columna, int longitud)
        {
            return p.Varchar(columna, longitud).IsFixedLength();
        }

        /// <summary>DATE (Oracle guarda fecha y hora, sin fracciones de segundo)</summary>
        public static DateTimePropertyConfiguration Fecha(this DateTimePropertyConfiguration p, string columna)
        {
            return p.HasColumnName(columna).HasColumnType("date");
        }

        /// <summary>
        /// Restriccion UNIQUE (se crea como indice unico). El proveedor de Oracle ignora el nombre y usa
        /// IX_&lt;TABLA&gt;_&lt;COLUMNA&gt;; el nombre solo sirve para declarar la intencion.
        /// </summary>
        public static PrimitivePropertyConfiguration Unico(this PrimitivePropertyConfiguration p, string nombre)
        {
            return p.HasColumnAnnotation(
                IndexAnnotation.AnnotationName,
                new IndexAnnotation(new IndexAttribute(nombre) { IsUnique = true }));
        }
    }
}
