using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entidades;

namespace Logica
{
    public interface IConsultaAsistencia
    {
        Task<List<RegistroAsistencia>> ConsultarAsync(int idEmpleado, DateTime desde, DateTime hasta);
        Task<List<TipoNovedad>> ConsultarTiposNovedadAsync();
        Task<List<Novedad>> ConsultarNovedadesAsync(int idEmpleado);
        Task EnviarNovedadAsync(int idEmpleado, int idTipoNovedad, DateTime fechaInicio,
            DateTime? fechaFin, string motivo);
    }
}
