using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Entidades;
using Logica;

namespace Persistencia
{
    public sealed class ConsultaAsistencia : IConsultaAsistencia, IConsultaTurnos
    {
        public Task<List<RegistroAsistencia>> ConsultarAsync(int idEmpleado, DateTime desde, DateTime hasta)
        {
            return ConsultarRegistrosAsync(desde, hasta, idEmpleado);
        }

        public async Task<List<TurnoProgramado>> ConsultarTurnosAsync(int idEmpleado, DateTime desde, DateTime hasta)
        {
            if (idEmpleado <= 0) throw new ArgumentOutOfRangeException(nameof(idEmpleado));
            if (desde >= hasta) throw new ArgumentException("El intervalo de turnos es inválido.");

            DateTime diaAnterior = desde.Date == DateTime.MinValue ? desde.Date : desde.Date.AddDays(-1);
            using (var contexto = new AssistProContext())
            {
                var asignaciones = await contexto.EmpleadosHorarios.AsNoTracking().Include(a => a.Horario)
                    .Where(a => a.IdEmpleado == idEmpleado && a.FechaInicio < hasta &&
                        (a.FechaFin == null || a.FechaFin >= diaAnterior))
                    .ToListAsync().ConfigureAwait(false);
                return PlanificacionTurnos.EnIntervalo(asignaciones, desde, hasta);
            }
        }

        internal static async Task<List<RegistroAsistencia>> ConsultarRegistrosAsync(DateTime desde, DateTime hasta,
            int? idEmpleado, bool incluirDetalle = false, string busqueda = null, int? idSede = null, int? idUnidad = null)
        {
            if (desde.Date > hasta.Date || hasta.Date == DateTime.MaxValue.Date)
                throw new ArgumentException("El rango de fechas es inválido.");
            DateTime inicio = desde.Date;
            DateTime fin = hasta.Date.AddDays(1);
            using (var contexto = new AssistProContext())
            {
                contexto.Configuration.LazyLoadingEnabled = false;
                contexto.Configuration.ProxyCreationEnabled = false;
                var registros = contexto.RegistrosAsistencia.AsNoTracking()
                    .Where(r => r.Fecha >= inicio && r.Fecha < fin);
                if (idEmpleado.HasValue) registros = registros.Where(r => r.IdEmpleado == idEmpleado.Value);
                if (idSede.HasValue) registros = registros.Where(r => r.Dispositivo.IdSede == idSede.Value);
                if (idUnidad.HasValue) registros = registros.Where(r => r.Empleado.IdDepartamento == idUnidad.Value);
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    string texto = busqueda.Trim().ToUpperInvariant();
                    int idRegistro;
                    if (!int.TryParse(texto, out idRegistro)) idRegistro = -1;
                    registros = registros.Where(r => r.IdRegistro == idRegistro ||
                        (r.Empleado.Nombres + " " + r.Empleado.Apellidos).ToUpper().Contains(texto) ||
                        r.Empleado.NumeroDocumento.ToUpper().Contains(texto) ||
                        r.TipoMarcacion.ToUpper().Contains(texto) ||
                        r.Dispositivo.NombreDispositivo.ToUpper().Contains(texto) ||
                        r.Dispositivo.Sede.NombreSede.ToUpper().Contains(texto) ||
                        r.Empleado.Departamento.NombreDepartamento.ToUpper().Contains(texto) ||
                        r.MetodoVerificacion.ToUpper().Contains(texto));
                }
                if (incluirDetalle)
                    registros = registros.Include(r => r.Empleado.Departamento).Include(r => r.Dispositivo.Sede);
                return await registros
                    .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.Hora).ThenByDescending(r => r.IdRegistro)
                    .Take(500).ToListAsync().ConfigureAwait(false);
            }
        }
    }
}
