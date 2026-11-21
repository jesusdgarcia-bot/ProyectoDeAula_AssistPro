using System;
using System.Linq;
using System.Threading.Tasks;
using Entidades;
using Logica;

namespace Pruebas
{
    internal static class PruebasTurnos
    {
        internal static Task Comprobar()
        {
            var viernes = new DateTime(2026, 11, 20);
            var sabado = viernes.AddDays(1);
            var guardia = Asignar("Guardia nocturna", 19, 7, "Lunes a Viernes", viernes.AddDays(-4), viernes);

            var enCurso = PlanificacionTurnos.EnIntervalo(new[] { guardia }, sabado.AddHours(4), sabado.AddHours(5)).Single();
            Verificar(enCurso.Inicio == viernes.AddHours(19) && enCurso.Fin == sabado.AddHours(7),
                "La guardia debe conservar el día real de entrada y finalizar el día siguiente, aun si terminó su vigencia.");
            Verificar(!PlanificacionTurnos.EnIntervalo(new[] { guardia }, sabado.AddHours(7), sabado.AddDays(1)).Any(),
                "El turno finalizado debe quedar fuera de la programación del sábado.");
            Verificar(PlanificacionTurnos.EsNocturno(guardia.Horario), "El catálogo debe indicar el fin al día siguiente.");

            var finDeSemana = Asignar("Turno diurno", 7, 19, "Vie-Lun y Mié", viernes, null);
            var semana = PlanificacionTurnos.EnIntervalo(new[] { finDeSemana }, viernes, viernes.AddDays(7));
            Verificar(semana.Select(t => t.Inicio.DayOfWeek).SequenceEqual(new[]
            {
                DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Wednesday
            }), "Se deben reconocer rangos que cruzan de semana y nombres de días con tilde.");
            Verificar(semana.All(t => t.Fin.Date == t.Inicio.Date), "Los turnos diurnos deben finalizar el mismo día.");

            var asignacionConHora = Asignar("Turno diurno", 7, 19, "Vie", viernes.AddHours(22), viernes.AddHours(22));
            Verificar(PlanificacionTurnos.EnIntervalo(new[] { asignacionConHora }, viernes.AddHours(7), viernes.AddHours(8))
                .Single().Inicio == viernes.AddHours(7), "La vigencia de una asignación se aplica por fecha, incluso si el dato guarda una hora.");

            var desconocido = Asignar("Sin días definidos", 7, 19, "Pendiente", viernes, null);
            var indefinido = Asignar("Sin duración definida", 7, 7, "Todos", viernes, null);
            Verificar(!PlanificacionTurnos.EnIntervalo(new[] { desconocido, indefinido }, viernes, sabado).Any(),
                "Los datos ambiguos no deben generar falsas programaciones.");
            Verificar(!PlanificacionTurnos.DiasValidos("L-M-V") &&
                PlanificacionTurnos.DiasValidos("L-V") && PlanificacionTurnos.DiasValidos("Diario"),
                "La validación debe distinguir los formatos incorrectos de los formatos compatibles.");

            var diario = Asignar("Turno diario", 7, 19, " Todos  los\tdías ", viernes, null);
            var soloFinDeSemana = Asignar("Turno de fin de semana", 7, 19, "Fin de  semana", viernes, null);
            Verificar(PlanificacionTurnos.EnIntervalo(new[] { diario }, viernes, viernes.AddDays(7)).Count == 7,
                "Los espacios repetidos o tabulaciones no deben perder una programación diaria.");
            Verificar(PlanificacionTurnos.EnIntervalo(new[] { soloFinDeSemana }, viernes, viernes.AddDays(7))
                .Select(t => t.Inicio.DayOfWeek).SequenceEqual(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }),
                "Los espacios repetidos no deben perder los turnos de fin de semana.");

            return Task.CompletedTask;
        }

        private static EmpleadoHorario Asignar(string nombre, int entrada, int salida, string dias, DateTime desde, DateTime? hasta)
        {
            return new EmpleadoHorario
            {
                FechaInicio = desde,
                FechaFin = hasta,
                Horario = new Horario
                {
                    NombreHorario = nombre,
                    HoraEntrada = new DateTime(2000, 1, 1, entrada, 0, 0),
                    HoraSalida = new DateTime(2000, 1, 1, salida, 0, 0),
                    DiasAplicables = dias
                }
            };
        }

        private static void Verificar(bool cumple, string mensaje)
        {
            if (!cumple) throw new InvalidOperationException(mensaje);
        }
    }
}
