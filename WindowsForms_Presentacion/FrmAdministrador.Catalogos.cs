using System;
using System.Threading.Tasks;
using Entidades;

namespace WindowsForms_Presentacion
{
    internal sealed partial class FrmAdministrador
    {
        private void ConfigurarCatalogos()
        {
            AgregarVista("Sedes", new[] { "ID", "Sede", "Ciudad", "Dirección", "Teléfono" },
                consulta.ListarSedesAsync,
                sede => new object[] { sede.IdSede, sede.NombreSede, sede.Ciudad, sede.Direccion, sede.Telefono },
                AgregarSedeAsync);
            AgregarVista("Departamentos", new[] { "ID", "Departamento", "Sede" },
                consulta.ListarDepartamentosAsync,
                departamento => new object[]
                {
                    departamento.IdDepartamento, departamento.NombreDepartamento, departamento.Sede?.NombreSede
                }, AgregarDepartamentoAsync);
            AgregarVista("Cargos", new[] { "ID", "Cargo", "Nivel jerárquico" },
                consulta.ListarCargosAsync,
                cargo => new object[] { cargo.IdCargo, cargo.NombreCargo, cargo.NivelJerarquico },
                AgregarCargoAsync);
            AgregarVista("Roles", new[] { "ID", "Rol", "Descripción" },
                consulta.ListarRolesAsync,
                rol => new object[] { rol.IdRol, rol.NombreRol, rol.Descripcion },
                AgregarRolAsync);
            AgregarVista("Dispositivos", new[] { "ID", "Dispositivo", "Tipo", "Sede", "Dirección IP", "Ubicación", "Estado" },
                consulta.ListarDispositivosAsync,
                dispositivo => new object[]
                {
                    dispositivo.IdDispositivo, dispositivo.NombreDispositivo, dispositivo.TipoDispositivo,
                    dispositivo.Sede?.NombreSede, dispositivo.DireccionIp, dispositivo.Ubicacion, dispositivo.Estado
                }, AgregarDispositivoAsync);
            AgregarVista("Horarios", new[] { "ID", "Horario", "Entrada", "Salida", "Días aplicables" },
                consulta.ListarHorariosAsync,
                horario => new object[]
                {
                    horario.IdHorario, horario.NombreHorario, horario.HoraEntrada.ToString("HH:mm"),
                    horario.HoraSalida.ToString("HH:mm"), horario.DiasAplicables
                }, AgregarHorarioAsync);
            AgregarVista("Tipos de novedad", new[] { "ID", "Tipo de novedad", "Descripción" },
                consulta.ListarTiposNovedadAsync,
                tipo => new object[] { tipo.IdTipoNovedad, tipo.NombreTipo, tipo.Descripcion },
                AgregarTipoNovedadAsync);
        }

        private Task<bool> AgregarSedeAsync()
        {
            var nombre = CampoRegistro.Texto("Nombre de la sede", 100);
            var ciudad = CampoRegistro.Texto("Ciudad", 50);
            var direccion = CampoRegistro.Texto("Dirección", 150);
            var telefono = CampoRegistro.Texto("Teléfono", 20, requerido: false);
            return GuardarAltaAsync("Agregar sede", () => new Sede
            {
                NombreSede = nombre.ValorTexto,
                Ciudad = ciudad.ValorTexto,
                Direccion = direccion.ValorTexto,
                Telefono = telefono.ValorTexto
            }, nombre, ciudad, direccion, telefono);
        }

        private async Task<bool> AgregarDepartamentoAsync()
        {
            var sede = ElegirReferencia("Sede", await consulta.ListarSedesAsync(),
                valor => valor.NombreSede + " · " + valor.Ciudad, valor => valor.IdSede, "Sedes");
            var nombre = CampoRegistro.Texto("Nombre del departamento", 80);
            return await GuardarAltaAsync("Agregar departamento", () => new Departamento
            {
                NombreDepartamento = nombre.ValorTexto,
                IdSede = sede.ValorId
            }, nombre, sede);
        }

        private Task<bool> AgregarCargoAsync()
        {
            var nombre = CampoRegistro.Texto("Nombre del cargo", 80);
            var nivel = CampoRegistro.Texto("Nivel jerárquico", 30, requerido: false);
            return GuardarAltaAsync("Agregar cargo", () => new Cargo
            {
                NombreCargo = nombre.ValorTexto,
                NivelJerarquico = nivel.ValorTexto
            }, nombre, nivel);
        }

        private Task<bool> AgregarRolAsync()
        {
            var nombre = CampoRegistro.Texto("Nombre del rol", 30);
            var descripcion = CampoRegistro.Texto("Descripción", 150, requerido: false);
            return GuardarAltaAsync("Agregar rol", () => new Rol
            {
                NombreRol = nombre.ValorTexto,
                Descripcion = descripcion.ValorTexto
            }, nombre, descripcion);
        }

        private async Task<bool> AgregarDispositivoAsync()
        {
            var sede = ElegirReferencia("Sede", await consulta.ListarSedesAsync(),
                valor => valor.NombreSede + " · " + valor.Ciudad, valor => valor.IdSede, "Sedes");
            var nombre = CampoRegistro.Texto("Nombre del dispositivo", 60);
            var tipo = CampoRegistro.Texto("Tipo de dispositivo", 20);
            var ip = CampoRegistro.Texto("Dirección IP", 15, requerido: false);
            var ubicacion = CampoRegistro.Texto("Ubicación", 80, requerido: false);
            return await GuardarAltaAsync("Agregar dispositivo", () => new DispositivoBiometrico
            {
                IdSede = sede.ValorId,
                NombreDispositivo = nombre.ValorTexto,
                TipoDispositivo = tipo.ValorTexto,
                DireccionIp = ip.ValorTexto,
                Ubicacion = ubicacion.ValorTexto,
                Estado = estadoActivo
            }, nombre, tipo, sede, ip, ubicacion);
        }

        private Task<bool> AgregarHorarioAsync()
        {
            var nombre = CampoRegistro.Texto("Nombre del horario", 50);
            var entrada = CampoRegistro.Hora("Hora de entrada");
            var salida = CampoRegistro.Hora("Hora de salida");
            var dias = CampoRegistro.Texto("Días aplicables", 30);
            return GuardarAltaAsync("Agregar horario", () => new Horario
            {
                NombreHorario = nombre.ValorTexto,
                HoraEntrada = DateTime.Today.Add(entrada.ValorHora),
                HoraSalida = DateTime.Today.Add(salida.ValorHora),
                DiasAplicables = dias.ValorTexto
            }, nombre, entrada, salida, dias);
        }

        private Task<bool> AgregarTipoNovedadAsync()
        {
            var nombre = CampoRegistro.Texto("Nombre del tipo de novedad", 40);
            var descripcion = CampoRegistro.Texto("Descripción", 150, requerido: false);
            return GuardarAltaAsync("Agregar tipo de novedad", () => new TipoNovedad
            {
                NombreTipo = nombre.ValorTexto,
                Descripcion = descripcion.ValorTexto
            }, nombre, descripcion);
        }
    }
}
