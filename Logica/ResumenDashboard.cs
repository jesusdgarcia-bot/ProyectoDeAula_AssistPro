using System;
using System.Collections.Generic;

namespace Logica
{
    public sealed class ResumenDashboard
    {
        public int EmpleadosActivos { get; set; }
        public int EmpleadosConMarcaciones { get; set; }
        public int TotalMarcaciones { get; set; }
        public int TotalNovedades { get; set; }
        public IReadOnlyList<MarcacionesDiaDashboard> MarcacionesPorDia { get; set; } = Array.Empty<MarcacionesDiaDashboard>();
        public IReadOnlyList<MarcacionesDepartamentoDashboard> MarcacionesPorDepartamento { get; set; } = Array.Empty<MarcacionesDepartamentoDashboard>();
        public IReadOnlyList<NovedadesEstadoDashboard> NovedadesPorEstado { get; set; } = Array.Empty<NovedadesEstadoDashboard>();
    }

    public sealed class MarcacionesDiaDashboard
    {
        public DateTime Dia { get; set; }
        public int Total { get; set; }
        public int Entradas { get; set; }
        public int Salidas { get; set; }
    }

    public sealed class MarcacionesDepartamentoDashboard
    {
        public string Sede { get; set; }
        public string Departamento { get; set; }
        public int EmpleadosConMarcaciones { get; set; }
        public int TotalMarcaciones { get; set; }
    }

    public sealed class NovedadesEstadoDashboard
    {
        public string Estado { get; set; }
        public int Total { get; set; }
    }
}
