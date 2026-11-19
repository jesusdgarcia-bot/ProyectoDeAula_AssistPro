using System;
using System.Collections.Generic;
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
                () => AgregarSedeAsync(), editar: AgregarSedeAsync,
                eliminar: sede => consulta.EliminarAsync<Sede>(sesion, sede.IdSede));
            AgregarVista("Departamentos", new[] { "ID", "Departamento", "Sede" },
                consulta.ListarDepartamentosAsync,
                departamento => new object[]
                {
                    departamento.IdDepartamento, departamento.NombreDepartamento, departamento.Sede?.NombreSede
                }, () => AgregarDepartamentoAsync(), editar: AgregarDepartamentoAsync,
                eliminar: departamento => consulta.EliminarAsync<Departamento>(sesion, departamento.IdDepartamento));
            AgregarVista("Cargos", new[] { "ID", "Cargo", "Nivel jerárquico" },
                consulta.ListarCargosAsync,
                cargo => new object[] { cargo.IdCargo, cargo.NombreCargo, cargo.NivelJerarquico },
                () => AgregarCargoAsync(), editar: AgregarCargoAsync,
                eliminar: cargo => consulta.EliminarAsync<Cargo>(sesion, cargo.IdCargo));
            AgregarVista("Roles", new[] { "ID", "Rol", "Descripción" },
                consulta.ListarRolesAsync,
                rol => new object[] { rol.IdRol, rol.NombreRol, rol.Descripcion },
                () => AgregarRolAsync(), editar: AgregarRolAsync,
                eliminar: rol => consulta.EliminarAsync<Rol>(sesion, rol.IdRol));
            AgregarVista("Dispositivos", new[] { "ID", "Dispositivo", "Tipo", "Sede", "Dirección IP", "Ubicación", "Estado" },
                consulta.ListarDispositivosAsync,
                dispositivo => new object[]
                {
                    dispositivo.IdDispositivo, dispositivo.NombreDispositivo, dispositivo.TipoDispositivo,
                    dispositivo.Sede?.NombreSede, dispositivo.DireccionIp, dispositivo.Ubicacion, dispositivo.Estado
                }, () => AgregarDispositivoAsync(), editar: AgregarDispositivoAsync,
                eliminar: dispositivo => consulta.EliminarAsync<DispositivoBiometrico>(sesion, dispositivo.IdDispositivo));
            AgregarVista("Horarios", new[] { "ID", "Horario", "Entrada", "Salida", "Días aplicables" },
                consulta.ListarHorariosAsync,
                horario => new object[]
                {
                    horario.IdHorario, horario.NombreHorario, horario.HoraEntrada.ToString("HH:mm"),
                    horario.HoraSalida.ToString("HH:mm"), horario.DiasAplicables
                }, () => AgregarHorarioAsync(), editar: AgregarHorarioAsync,
                eliminar: horario => consulta.EliminarAsync<Horario>(sesion, horario.IdHorario));
            AgregarVista("Tipos de novedad", new[] { "ID", "Tipo de novedad", "Descripción" },
                consulta.ListarTiposNovedadAsync,
                tipo => new object[] { tipo.IdTipoNovedad, tipo.NombreTipo, tipo.Descripcion },
                () => AgregarTipoNovedadAsync(), editar: AgregarTipoNovedadAsync,
                eliminar: tipo => consulta.EliminarAsync<TipoNovedad>(sesion, tipo.IdTipoNovedad));
        }

        private Task<bool> AgregarSedeAsync(Sede original = null)
        {
            var nombre = CampoRegistro.Texto("Nombre de la sede", 100).ConTexto(original?.NombreSede);
            var ciudad = CampoRegistro.Texto("Ciudad", 50).ConTexto(original?.Ciudad);
            var direccion = CampoRegistro.Texto("Dirección", 150).ConTexto(original?.Direccion);
            var telefono = CampoRegistro.Texto("Teléfono", 20, requerido: false).ConTexto(original?.Telefono);
            return GuardarRegistroAsync(original == null ? "Agregar sede" : "Actualizar sede", original, () => new Sede
            {
                IdSede = original?.IdSede ?? 0,
                NombreSede = nombre.ValorTexto,
                Ciudad = ciudad.ValorTexto,
                Direccion = direccion.ValorTexto,
                Telefono = telefono.ValorTexto
            }, nombre, ciudad, direccion, telefono);
        }

        private async Task<bool> AgregarDepartamentoAsync(Departamento original = null)
        {
            var sede = ElegirReferencia("Sede", await consulta.ListarSedesAsync(),
                valor => valor.NombreSede + " · " + valor.Ciudad, valor => valor.IdSede, "Sedes").SeleccionarId(original?.IdSede);
            var nombre = CampoRegistro.Texto("Nombre del departamento", 80).ConTexto(original?.NombreDepartamento);
            return await GuardarRegistroAsync(original == null ? "Agregar departamento" : "Actualizar departamento", original, () => new Departamento
            {
                IdDepartamento = original?.IdDepartamento ?? 0,
                NombreDepartamento = nombre.ValorTexto,
                IdSede = sede.ValorId
            }, nombre, sede);
        }

        private Task<bool> AgregarCargoAsync(Cargo original = null)
        {
            var nombre = CampoRegistro.Texto("Nombre del cargo", 80).ConTexto(original?.NombreCargo);
            var nivel = CampoRegistro.Texto("Nivel jerárquico", 30, requerido: false).ConTexto(original?.NivelJerarquico);
            return GuardarRegistroAsync(original == null ? "Agregar cargo" : "Actualizar cargo", original, () => new Cargo
            {
                IdCargo = original?.IdCargo ?? 0,
                NombreCargo = nombre.ValorTexto,
                NivelJerarquico = nivel.ValorTexto
            }, nombre, nivel);
        }

        private Task<bool> AgregarRolAsync(Rol original = null)
        {
            var nombre = CampoRegistro.Texto("Nombre del rol", 30).ConTexto(original?.NombreRol);
            var descripcion = CampoRegistro.Texto("Descripción", 150, requerido: false).ConTexto(original?.Descripcion);
            return GuardarRegistroAsync(original == null ? "Agregar rol" : "Actualizar rol", original, () => new Rol
            {
                IdRol = original?.IdRol ?? 0,
                NombreRol = nombre.ValorTexto,
                Descripcion = descripcion.ValorTexto
            }, nombre, descripcion);
        }

        private async Task<bool> AgregarDispositivoAsync(DispositivoBiometrico original = null)
        {
            var sede = ElegirReferencia("Sede", await consulta.ListarSedesAsync(),
                valor => valor.NombreSede + " · " + valor.Ciudad, valor => valor.IdSede, "Sedes").SeleccionarId(original?.IdSede);
            var nombre = CampoRegistro.Texto("Nombre del dispositivo", 60).ConTexto(original?.NombreDispositivo);
            var tipo = CampoRegistro.Texto("Tipo de dispositivo", 20).ConTexto(original?.TipoDispositivo);
            var ip = CampoRegistro.Texto("Dirección IP", 15, requerido: false).ConTexto(original?.DireccionIp);
            var ubicacion = CampoRegistro.Texto("Ubicación", 80, requerido: false).ConTexto(original?.Ubicacion);
            var estadoDispositivo = original == null ? null : CampoEstado(original.Estado);
            var campos = new List<CampoRegistro> { nombre, tipo, sede, ip, ubicacion };
            if (estadoDispositivo != null) campos.Add(estadoDispositivo);
            return await GuardarRegistroAsync(original == null ? "Agregar dispositivo" : "Actualizar dispositivo", original, () => new DispositivoBiometrico
            {
                IdDispositivo = original?.IdDispositivo ?? 0,
                IdSede = sede.ValorId,
                NombreDispositivo = nombre.ValorTexto,
                TipoDispositivo = tipo.ValorTexto,
                DireccionIp = ip.ValorTexto,
                Ubicacion = ubicacion.ValorTexto,
                Estado = estadoDispositivo?.ValorTexto ?? estadoActivo
            }, campos.ToArray());
        }

        private Task<bool> AgregarHorarioAsync(Horario original = null)
        {
            var nombre = CampoRegistro.Texto("Nombre del horario", 50).ConTexto(original?.NombreHorario);
            var entrada = CampoRegistro.Hora("Hora de entrada").ConHora(original?.HoraEntrada);
            var salida = CampoRegistro.Hora("Hora de salida").ConHora(original?.HoraSalida);
            var dias = CampoRegistro.Texto("Días aplicables", 30).ConTexto(original?.DiasAplicables);
            return GuardarRegistroAsync(original == null ? "Agregar horario" : "Actualizar horario", original, () => new Horario
            {
                IdHorario = original?.IdHorario ?? 0,
                NombreHorario = nombre.ValorTexto,
                HoraEntrada = (original?.HoraEntrada.Date ?? DateTime.Today).Add(entrada.ValorHora),
                HoraSalida = (original?.HoraSalida.Date ?? DateTime.Today).Add(salida.ValorHora),
                DiasAplicables = dias.ValorTexto
            }, nombre, entrada, salida, dias);
        }

        private Task<bool> AgregarTipoNovedadAsync(TipoNovedad original = null)
        {
            var nombre = CampoRegistro.Texto("Nombre del tipo de novedad", 40).ConTexto(original?.NombreTipo);
            var descripcion = CampoRegistro.Texto("Descripción", 150, requerido: false).ConTexto(original?.Descripcion);
            return GuardarRegistroAsync(original == null ? "Agregar tipo de novedad" : "Actualizar tipo de novedad", original, () => new TipoNovedad
            {
                IdTipoNovedad = original?.IdTipoNovedad ?? 0,
                NombreTipo = nombre.ValorTexto,
                Descripcion = descripcion.ValorTexto
            }, nombre, descripcion);
        }
    }
}
