using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Entidades;

namespace Logica
{
    public interface IConsultaTurnos
    {
        Task<List<TurnoProgramado>> ConsultarTurnosAsync(int idEmpleado, DateTime desde, DateTime hasta);
    }

    public sealed class TurnoProgramado
    {
        internal TurnoProgramado(string nombre, DateTime inicio, DateTime fin)
        {
            Nombre = nombre;
            Inicio = inicio;
            Fin = fin;
        }

        public string Nombre { get; }
        public DateTime Inicio { get; }
        public DateTime Fin { get; }
    }

    public static class PlanificacionTurnos
    {
        public const string FormatoDias = "Usa días como Lun-Vie, Lun,Mié,Sáb o Todos.";
        private static readonly Regex SeparadoresRango = new Regex(@"\s+(?:A|AL)\s+|\s*[-–—]\s*");
        private static readonly Regex SeparadoresDias = new Regex(@"[,;/\s]+");
        private static readonly Lazy<TimeZoneInfo> ZonaHorariaColombia = new Lazy<TimeZoneInfo>(
            () => TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"));

        public static DateTime AhoraColombia => DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaHorariaColombia.Value), DateTimeKind.Unspecified);

        public static bool EsNocturno(Horario horario)
        {
            return horario != null && horario.HoraSalida.TimeOfDay < horario.HoraEntrada.TimeOfDay;
        }

        public static bool DiasValidos(string texto)
        {
            return InterpretarDias(texto) != null;
        }

        public static List<TurnoProgramado> EnIntervalo(IEnumerable<EmpleadoHorario> asignaciones, DateTime desde, DateTime hasta)
        {
            if (asignaciones == null) throw new ArgumentNullException(nameof(asignaciones));
            if (hasta <= desde) throw new ArgumentException("La consulta de turnos requiere un intervalo válido.");

            DateTime primerDia = desde.Date == DateTime.MinValue ? desde.Date : desde.Date.AddDays(-1);
            DateTime ultimoDia = hasta.AddTicks(-1).Date;
            var turnos = new List<TurnoProgramado>();
            foreach (var asignacion in asignaciones)
            {
                var horario = asignacion?.Horario;
                if (horario == null) continue;
                var dias = InterpretarDias(horario.DiasAplicables);
                if (dias == null) continue;

                var entrada = horario.HoraEntrada.TimeOfDay;
                var salida = horario.HoraSalida.TimeOfDay;
                if (entrada == salida) continue;
                var duracion = salida - entrada;
                if (duracion < TimeSpan.Zero) duracion += TimeSpan.FromDays(1);

                DateTime comienzo = asignacion.FechaInicio.Date > primerDia ? asignacion.FechaInicio.Date : primerDia;
                DateTime finVigencia = asignacion.FechaFin?.Date ?? ultimoDia;
                DateTime limite = finVigencia < ultimoDia ? finVigencia : ultimoDia;
                for (DateTime dia = comienzo; dia <= limite; dia = dia.AddDays(1))
                {
                    // La vigencia y los días aplican al inicio, incluso si la salida ocurre al día siguiente.
                    DateTime inicioTurno = dia.Add(entrada);
                    if (dias.Contains(dia.DayOfWeek) && inicioTurno < hasta && DateTime.MaxValue - inicioTurno >= duracion)
                    {
                        DateTime finTurno = inicioTurno.Add(duracion);
                        if (finTurno > desde) turnos.Add(new TurnoProgramado(horario.NombreHorario, inicioTurno, finTurno));
                    }
                    if (dia == limite) break;
                }
            }
            return turnos.OrderBy(turno => turno.Inicio).ThenBy(turno => turno.Fin).ToList();
        }

        private static HashSet<DayOfWeek> InterpretarDias(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            var normalizado = new StringBuilder();
            foreach (char caracter in texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
                    normalizado.Append(caracter);

            string dias = normalizado.ToString();
            switch (dias)
            {
                case "*":
                case "TODOS":
                case "TODOS LOS DIAS":
                case "DIARIO":
                case "24/7":
                    return new HashSet<DayOfWeek>(Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>());
                case "FIN DE SEMANA":
                case "FINES DE SEMANA":
                    return new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };
            }

            var resultado = new HashSet<DayOfWeek>();
            string[] partes = SeparadoresDias.Split(SeparadoresRango.Replace(dias, "-"));
            foreach (string parte in partes)
            {
                if (parte.Length == 0 || parte == "Y") continue;
                string[] extremos = parte.Split('-');
                if (extremos.Length > 2 || !BuscarDia(extremos[0], out var inicio) ||
                    !BuscarDia(extremos[extremos.Length - 1], out var fin)) return null;

                for (var dia = inicio; dia != fin; dia = (DayOfWeek)(((int)dia + 1) % 7))
                    resultado.Add(dia);
                resultado.Add(fin);
            }
            return resultado.Count > 0 ? resultado : null;
        }

        private static bool BuscarDia(string texto, out DayOfWeek dia)
        {
            switch (texto.TrimEnd('.'))
            {
                case "L": case "LU": case "LUN": case "LUNES": dia = DayOfWeek.Monday; return true;
                case "M": case "MA": case "MAR": case "MARTES": dia = DayOfWeek.Tuesday; return true;
                case "X": case "MI": case "MIE": case "MIER": case "MIERCOLES": dia = DayOfWeek.Wednesday; return true;
                case "J": case "JU": case "JUE": case "JUEVES": dia = DayOfWeek.Thursday; return true;
                case "V": case "VI": case "VIE": case "VIERNES": dia = DayOfWeek.Friday; return true;
                case "S": case "SA": case "SAB": case "SABADO": dia = DayOfWeek.Saturday; return true;
                case "D": case "DO": case "DOM": case "DOMINGO": dia = DayOfWeek.Sunday; return true;
                default: dia = default; return false;
            }
        }
    }
}
