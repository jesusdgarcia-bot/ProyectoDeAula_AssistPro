using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Entidades;
using Logica;

namespace Persistencia
{
    public sealed class ConsultaAsistencia : IConsultaAsistencia
    {
        public Task<List<RegistroAsistencia>> ConsultarAsync(int idEmpleado, DateTime desde, DateTime hasta)
        {
            return ConsultarRegistrosAsync(desde, hasta, idEmpleado);
        }

        internal static async Task<List<RegistroAsistencia>> ConsultarRegistrosAsync(DateTime desde, DateTime hasta, int? idEmpleado)
        {
            if (desde.Date > hasta.Date) throw new ArgumentException("El rango de fechas es inválido.");
            DateTime inicio = desde.Date;
            DateTime fin = hasta.Date.AddDays(1);
            using (var contexto = new AssistProContext())
            {
                contexto.Configuration.LazyLoadingEnabled = false;
                contexto.Configuration.ProxyCreationEnabled = false;
                var registros = contexto.RegistrosAsistencia.AsNoTracking()
                    .Where(r => r.Fecha >= inicio && r.Fecha < fin);
                registros = idEmpleado.HasValue
                    ? registros.Where(r => r.IdEmpleado == idEmpleado.Value)
                    : registros.Include(r => r.Empleado);
                return await registros
                    .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.Hora).ThenByDescending(r => r.IdRegistro)
                    .Take(500).ToListAsync().ConfigureAwait(false);
            }
        }
    }
}
