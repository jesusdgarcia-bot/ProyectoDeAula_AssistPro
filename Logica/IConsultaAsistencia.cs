using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entidades;

namespace Logica
{
    public interface IConsultaAsistencia
    {
        Task<List<RegistroAsistencia>> ConsultarAsync(int idEmpleado, DateTime desde, DateTime hasta);
    }
}
