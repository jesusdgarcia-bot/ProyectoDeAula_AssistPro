using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entidades;

namespace Logica
{
    public interface IRepositorioFacial
    {
        Task<List<CandidatoFacial>> ListarCandidatosActivosAsync();
        Task<Usuario> BuscarUsuarioActivoAsync(int idUsuario);
        Task GuardarAsync(SesionUsuario administrador, int idEmpleado, byte[] datosPlantilla);
    }

    public sealed class CandidatoFacial
    {
        public Usuario Usuario { get; set; }
        public byte[] DatosPlantilla { get; set; }
    }

    public sealed class ServicioReconocimientoFacial
    {
        // Un margen adicional impide elegir una identidad cuando dos registros se parecen demasiado.
        public const double UmbralSimilitud = 0.45;
        public const double MargenMinimo = 0.08;
        private readonly IRepositorioFacial repositorio;
        private readonly string estadoActivo;

        public ServicioReconocimientoFacial(IRepositorioFacial repositorio, string estadoActivo = "A")
        {
            this.repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            if (string.IsNullOrWhiteSpace(estadoActivo) || estadoActivo.Length != 1)
                throw new ArgumentException("El estado activo debe tener un carácter.", nameof(estadoActivo));
            this.estadoActivo = estadoActivo;
        }

        public Task RegistrarAsync(SesionUsuario administrador, int idEmpleado, CapturaFacialVerificada captura)
        {
            if (administrador == null || !administrador.EsAdministrador)
                throw new UnauthorizedAccessException("Se requiere una cuenta de administrador para registrar rostros.");
            if (idEmpleado <= 0)
                throw new ArgumentException("Selecciona un empleado válido.", nameof(idEmpleado));
            var plantilla = ConsumirCaptura(captura);
            return repositorio.GuardarAsync(administrador, idEmpleado, plantilla.Serializar());
        }

        public async Task<SesionUsuario> AutenticarAsync(CapturaFacialVerificada captura)
        {
            var plantillaCapturada = ConsumirCaptura(captura);
            var candidatos = await repositorio.ListarCandidatosActivosAsync().ConfigureAwait(false);
            Usuario mejorUsuario = null;
            double mejorPuntaje = double.NegativeInfinity;
            double segundoPuntaje = double.NegativeInfinity;
            int compatibles = 0;
            foreach (var candidato in candidatos)
            {
                PlantillaFacial plantilla;
                if (candidato == null || !EsUsuarioActivo(candidato.Usuario) ||
                    !PlantillaFacial.IntentarLeer(candidato.DatosPlantilla, out plantilla)) continue;
                compatibles++;
                double puntaje = plantillaCapturada.Similitud(plantilla);
                if (puntaje > mejorPuntaje)
                {
                    segundoPuntaje = mejorPuntaje;
                    mejorPuntaje = puntaje;
                    mejorUsuario = candidato.Usuario;
                }
                else if (puntaje > segundoPuntaje)
                {
                    segundoPuntaje = puntaje;
                }
            }
            if (compatibles == 0)
                throw new InvalidOperationException("No hay rostros registrados para cuentas activas. Un administrador debe registrarlos en Plantillas.");
            if (mejorUsuario == null || mejorPuntaje < UmbralSimilitud || mejorPuntaje - segundoPuntaje < MargenMinimo)
                return null;

            // La cuenta puede haber sido desactivada mientras se comparaban las plantillas.
            var actual = await repositorio.BuscarUsuarioActivoAsync(mejorUsuario.IdUsuario).ConfigureAwait(false);
            if (!EsUsuarioActivo(actual) || actual.IdEmpleado != mejorUsuario.IdEmpleado) return null;
            return new SesionUsuario(actual);
        }

        private static PlantillaFacial ConsumirCaptura(CapturaFacialVerificada captura)
        {
            var plantilla = captura?.Consumir();
            if (plantilla == null)
                throw new InvalidOperationException("Completa una nueva prueba de vida para validar tu rostro.");
            return plantilla;
        }

        private bool EsUsuarioActivo(Usuario usuario)
        {
            return usuario != null && usuario.Estado?.Trim() == estadoActivo && usuario.Empleado != null &&
                usuario.Empleado.Estado?.Trim() == estadoActivo && usuario.Rol != null;
        }
    }
}
