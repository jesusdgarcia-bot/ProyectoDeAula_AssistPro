using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Entidades;
using Logica;
using Oracle.ManagedDataAccess.Client;

namespace Persistencia
{
    public sealed class RepositorioMarcacionFacial : IRepositorioMarcacionFacial, IRepositorioMarcacionConClave
    {
        private static readonly TimeSpan VentanaDeReintento = TimeSpan.FromMinutes(1);
        private readonly string estadoActivo;

        public RepositorioMarcacionFacial(string estadoActivo)
        {
            if (string.IsNullOrWhiteSpace(estadoActivo) || estadoActivo.Length != 1)
                throw new ArgumentException("El estado activo debe tener un carácter.", nameof(estadoActivo));
            this.estadoActivo = estadoActivo;
        }

        public Task<RegistroAsistencia> GuardarMarcacionAsync(SesionUsuario identidad, int idDispositivo, string tipoMarcacion)
        {
            return GuardarAsync(identidad, idDispositivo, tipoMarcacion, "Facial");
        }

        public Task<RegistroAsistencia> GuardarMarcacionConClaveAsync(SesionUsuario identidad, int idDispositivo, string tipoMarcacion)
        {
            return GuardarAsync(identidad, idDispositivo, tipoMarcacion, "Clave");
        }

        private async Task<RegistroAsistencia> GuardarAsync(SesionUsuario identidad, int idDispositivo,
            string tipoMarcacion, string metodoVerificacion)
        {
            if (identidad == null) throw new ArgumentNullException(nameof(identidad));

            using (var contexto = new AssistProContext())
            {
                await contexto.Database.Connection.OpenAsync().ConfigureAwait(false);
                using (var transaccion = contexto.Database.BeginTransaction())
                {
                    var registro = await GuardarEnTransaccionAsync(contexto, identidad, idDispositivo,
                        tipoMarcacion, metodoVerificacion).ConfigureAwait(false);
                    transaccion.Commit();
                    return registro;
                }
            }
        }

        private async Task<RegistroAsistencia> GuardarEnTransaccionAsync(AssistProContext contexto,
            SesionUsuario identidad, int idDispositivo, string tipoMarcacion, string metodoVerificacion)
        {
            // Mantiene vigentes la cuenta, el empleado y el dispositivo hasta guardar la marcación.
            var autorizacion = await contexto.Database.SqlQuery<decimal>(@"
                SELECT u.ID_USUARIO
                  FROM ASISTENCIA.USUARIO u
                  JOIN ASISTENCIA.EMPLEADO e ON e.ID_EMPLEADO = u.ID_EMPLEADO
                  JOIN ASISTENCIA.DISPOSITIVO_BIOMETRICO d ON d.ID_DISPOSITIVO = :dispositivo
                 WHERE u.ID_USUARIO = :usuario AND e.ID_EMPLEADO = :empleado AND u.ID_ROL = :rol
                   AND u.ESTADO = :estadoUsuario AND e.ESTADO = :estadoEmpleado AND d.ESTADO = :estadoDispositivo
                 FOR UPDATE OF u.ESTADO, e.ESTADO, d.ESTADO",
                new OracleParameter("dispositivo", OracleDbType.Int32) { Value = idDispositivo },
                new OracleParameter("usuario", OracleDbType.Int32) { Value = identidad.IdUsuario },
                new OracleParameter("empleado", OracleDbType.Int32) { Value = identidad.IdEmpleado },
                new OracleParameter("rol", OracleDbType.Int32) { Value = identidad.IdRol },
                new OracleParameter("estadoUsuario", OracleDbType.Varchar2) { Value = estadoActivo },
                new OracleParameter("estadoEmpleado", OracleDbType.Varchar2) { Value = estadoActivo },
                new OracleParameter("estadoDispositivo", OracleDbType.Varchar2) { Value = estadoActivo }
            ).ToListAsync().ConfigureAwait(false);
            if (autorizacion.Count != 1)
                throw new UnauthorizedAccessException("Comprueba que la cuenta, el empleado y el dispositivo asignado estén activos.");

            DateTime ahora = await contexto.Database.SqlQuery<DateTime>(
                "SELECT CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE) FROM DUAL")
                .SingleAsync().ConfigureAwait(false);
            DateTime inicioVentana = ahora - VentanaDeReintento;
            DateTime primerDia = inicioVentana.Date;
            DateTime diaSiguiente = ahora.Date.AddDays(1);
            var ultima = await contexto.RegistrosAsistencia.AsNoTracking()
                .Where(r => r.IdEmpleado == identidad.IdEmpleado && r.Fecha >= primerDia && r.Fecha < diaSiguiente)
                .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.Hora).ThenByDescending(r => r.IdRegistro)
                .FirstOrDefaultAsync().ConfigureAwait(false);
            if (ultima != null && ultima.IdDispositivo == idDispositivo &&
                string.Equals(ultima.TipoMarcacion?.Trim(), tipoMarcacion, StringComparison.OrdinalIgnoreCase))
            {
                DateTime horaAnterior = ultima.Fecha.Date.Add(ultima.Hora.TimeOfDay);
                if (horaAnterior >= inicioVentana && horaAnterior <= ahora)
                    return ultima;
            }

            var registro = new RegistroAsistencia
            {
                IdEmpleado = identidad.IdEmpleado,
                IdDispositivo = idDispositivo,
                Fecha = ahora.Date,
                Hora = ahora,
                TipoMarcacion = tipoMarcacion,
                MetodoVerificacion = metodoVerificacion
            };
            contexto.RegistrosAsistencia.Add(registro);
            await contexto.SaveChangesAsync().ConfigureAwait(false);
            return registro;
        }
    }
}
