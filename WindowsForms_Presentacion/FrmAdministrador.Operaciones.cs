using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Entidades;

namespace WindowsForms_Presentacion
{
    internal sealed partial class FrmAdministrador
    {
        private void ConfigurarOperaciones()
        {
            AgregarVista("Asistencia", new[] { "ID", "Empleado", "Fecha", "Hora", "Marcación", "Dispositivo", "Verificación" },
                () => consulta.ConsultarAsistenciaAsync(desde.Value, hasta.Value, idEmpleadoFiltrado),
                registro => new object[]
                {
                    registro.IdRegistro, NombreEmpleado(registro.Empleado), registro.Fecha.ToString("dd/MM/yyyy"),
                    registro.Hora.ToString("HH:mm:ss"), registro.TipoMarcacion, registro.Dispositivo?.NombreDispositivo,
                    registro.MetodoVerificacion
                }, RegistrarMarcacionAsync, usaFechas: true);
            AgregarVista("Empleados", new[] { "ID", "Nombre", "Documento", "Departamento", "Cargo", "Estado" },
                consulta.ListarEmpleadosAsync, empleado => new object[]
                {
                    empleado.IdEmpleado, NombreEmpleado(empleado), empleado.NumeroDocumento,
                    empleado.Departamento?.NombreDepartamento, empleado.Cargo?.NombreCargo, empleado.Estado
                }, AgregarEmpleadoAsync);
            AgregarVista("Usuarios", new[] { "ID", "Usuario", "Empleado", "Rol", "Estado" },
                consulta.ListarUsuariosAsync, cuenta => new object[]
                {
                    cuenta.IdUsuario, cuenta.NombreUsuario, cuenta.Empleado, cuenta.Rol, cuenta.Estado
                }, AgregarUsuarioAsync);
            AgregarVista("Asignaciones", new[] { "Empleado", "Horario", "Desde", "Hasta" },
                consulta.ListarAsignacionesAsync, asignacion => new object[]
                {
                    NombreEmpleado(asignacion.Empleado), asignacion.Horario?.NombreHorario,
                    asignacion.FechaInicio.ToString("dd/MM/yyyy"), asignacion.FechaFin?.ToString("dd/MM/yyyy")
                }, AsignarHorarioAsync);
            AgregarVista("Novedades", new[] { "Empleado", "Tipo", "Desde", "Hasta", "Estado", "Motivo" },
                consulta.ListarNovedadesAsync, novedad => new object[]
                {
                    NombreEmpleado(novedad.Empleado), novedad.TipoNovedad?.NombreTipo,
                    novedad.FechaInicio.ToString("dd/MM/yyyy"), novedad.FechaFin?.ToString("dd/MM/yyyy"),
                    novedad.EstadoAprobacion, novedad.Motivo
                }, AgregarNovedadAsync);
            AgregarVista("Plantillas", new[] { "ID", "Empleado", "Biometría", "Fecha de registro" },
                consulta.ListarPlantillasAsync, plantilla => new object[]
                {
                    plantilla.IdPlantilla, plantilla.Empleado, plantilla.TipoBiometria,
                    plantilla.FechaRegistro.ToString("dd/MM/yyyy")
                }, RegistrarRostroAsync);
        }

        private async Task<bool> RegistrarRostroAsync()
        {
            var empleados = await consulta.ListarEmpleadosAsync();
            var cuentas = await consulta.ListarUsuariosAsync();
            if (IsDisposed || Disposing) return false;
            var activos = new HashSet<int>(cuentas.Where(c => c.Estado?.Trim() == estadoActivo).Select(c => c.IdEmpleado));
            var disponibles = empleados.Where(e => e.Estado?.Trim() == estadoActivo && activos.Contains(e.IdEmpleado)).ToList();
            if (disponibles.Count == 0)
                throw new InvalidOperationException("Registra primero un empleado activo con una cuenta activa en Usuarios.");
            var empleado = ElegirEmpleado(disponibles);
            int idSeleccionado;
            using (var editor = new EditorRegistro("Registrar o reemplazar rostro", empleado))
            {
                if (editor.ShowDialog(this) != DialogResult.OK) return false;
                idSeleccionado = empleado.ValorId;
            }
            var seleccionado = disponibles.Single(e => e.IdEmpleado == idSeleccionado);
            using (var captura = new FrmCapturaFacial("Registrar rostro | " + NombreEmpleado(seleccionado), registrar: true))
            {
                if (captura.ShowDialog(this) != DialogResult.OK) return false;
                estado.Text = "Guardando plantilla facial en Oracle…";
                var servicio = new Logica.ServicioReconocimientoFacial(new Persistencia.RepositorioFacial(estadoActivo), estadoActivo);
                await servicio.RegistrarAsync(sesion, seleccionado.IdEmpleado, captura.Caracteristicas);
                return true;
            }
        }

        private async Task FiltrarEmpleadoAsync()
        {
            if (cargando) return;
            bool seleccionado = false;
            var estadoAnterior = estado.Text;
            CambiarCarga(true);
            try
            {
                var empleados = await consulta.ListarEmpleadosAsync();
                if (IsDisposed || Disposing) return;
                var opciones = new[] { new { Id = 0, Nombre = "Todos los empleados" } }.Concat(
                    empleados.Select(e => new { Id = e.IdEmpleado, Nombre = NombreEmpleado(e) + " · " + e.NumeroDocumento }));
                var empleado = CampoRegistro.Elegir("Empleado", opciones, e => e.Nombre, e => e.Id);
                using (var editor = new EditorRegistro("Filtrar por empleado", empleado))
                {
                    if (editor.ShowDialog(this) != DialogResult.OK) return;
                    idEmpleadoFiltrado = empleado.ValorId == 0 ? (int?)null : empleado.ValorId;
                    seleccionado = true;
                }
            }
            catch (Exception)
            {
                estadoAnterior = "No se pudo cargar la lista de empleados.";
            }
            finally
            {
                CambiarCarga(false);
                if (!seleccionado && !IsDisposed && !Disposing) estado.Text = estadoAnterior;
            }
            if (seleccionado && !IsDisposed && !Disposing) await CargarVistaAsync();
        }

        private async Task<bool> RegistrarMarcacionAsync()
        {
            var empleado = ElegirEmpleado(await consulta.ListarEmpleadosAsync());
            var dispositivo = ElegirReferencia("Dispositivo", await consulta.ListarDispositivosAsync(),
                d => d.NombreDispositivo + " · " + d.Sede?.NombreSede, d => d.IdDispositivo, "Dispositivos");
            var fecha = CampoRegistro.Fecha("Fecha");
            var hora = CampoRegistro.Hora("Hora");
            var tipo = CampoRegistro.Opciones("Tipo de marcación", "Entrada", "Salida");
            DateTime dia = DateTime.Today;
            int idEmpleado = 0;
            bool guardado = await GuardarAltaAsync("Registrar marcación", () =>
            {
                dia = fecha.ValorFecha;
                idEmpleado = empleado.ValorId;
                return new RegistroAsistencia
                {
                    IdEmpleado = idEmpleado,
                    IdDispositivo = dispositivo.ValorId,
                    Fecha = dia,
                    Hora = dia.Add(hora.ValorHora),
                    TipoMarcacion = tipo.ValorTexto,
                    MetodoVerificacion = "Manual"
                };
            }, empleado, dispositivo, fecha, hora, tipo);
            if (guardado && !IsDisposed && !Disposing)
            {
                if (dia < desde.Value.Date || dia > hasta.Value.Date)
                    desde.Value = hasta.Value = dia;
                if (idEmpleadoFiltrado.HasValue) idEmpleadoFiltrado = idEmpleado;
            }
            return guardado;
        }

        private async Task<bool> AgregarEmpleadoAsync()
        {
            var departamento = ElegirReferencia("Departamento", await consulta.ListarDepartamentosAsync(),
                d => d.NombreDepartamento + " · " + d.Sede?.NombreSede, d => d.IdDepartamento, "Departamentos");
            var cargo = ElegirReferencia("Cargo", await consulta.ListarCargosAsync(),
                c => c.NombreCargo, c => c.IdCargo, "Cargos");
            var tipo = CampoRegistro.Texto("Tipo de documento", 2);
            var documento = CampoRegistro.Texto("Número de documento", 20);
            var nombres = CampoRegistro.Texto("Nombres", 60);
            var apellidos = CampoRegistro.Texto("Apellidos", 60);
            var correo = CampoRegistro.Texto("Correo", 100, requerido: false);
            var telefono = CampoRegistro.Texto("Teléfono", 20, requerido: false);
            var nacimiento = CampoRegistro.Fecha("Fecha de nacimiento", opcional: true);
            var ingreso = CampoRegistro.Fecha("Fecha de ingreso");
            return await GuardarAltaAsync("Agregar empleado", () =>
            {
                if (nacimiento.ValorFechaOpcional > ingreso.ValorFecha)
                    throw new ArgumentException("El nacimiento debe ser anterior o igual al ingreso.");
                return new Empleado
                {
                    TipoDocumento = tipo.ValorTexto.ToUpperInvariant(),
                    NumeroDocumento = documento.ValorTexto,
                    Nombres = nombres.ValorTexto,
                    Apellidos = apellidos.ValorTexto,
                    Correo = correo.ValorTexto,
                    Telefono = telefono.ValorTexto,
                    FechaNacimiento = nacimiento.ValorFechaOpcional,
                    FechaIngreso = ingreso.ValorFecha,
                    Estado = estadoActivo,
                    IdDepartamento = departamento.ValorId,
                    IdCargo = cargo.ValorId
                };
            }, tipo, documento, nombres, apellidos, correo, telefono, nacimiento, ingreso, departamento, cargo);
        }

        private async Task<bool> AgregarUsuarioAsync()
        {
            var empleados = await consulta.ListarEmpleadosAsync();
            var cuentas = await consulta.ListarUsuariosAsync();
            var ocupados = new HashSet<int>(cuentas.Select(c => c.IdEmpleado));
            var disponibles = empleados.Where(e => e.Estado?.Trim() == estadoActivo && !ocupados.Contains(e.IdEmpleado)).ToList();
            if (disponibles.Count == 0)
                throw new InvalidOperationException("Registra un empleado activo que todavía no tenga cuenta.");
            var empleado = ElegirEmpleado(disponibles);
            var rol = ElegirReferencia("Rol", await consulta.ListarRolesAsync(), r => r.NombreRol, r => r.IdRol, "Roles");
            var usuario = CampoRegistro.Texto("Nombre de usuario", 30);
            var correo = CampoRegistro.Texto("Correo de acceso", 100);
            var clave = CampoRegistro.Clave("Contraseña");
            return await AbrirAltaAsync("Agregar usuario", () => consulta.CrearUsuarioAsync(sesion, new Usuario
            {
                IdEmpleado = empleado.ValorId,
                IdRol = rol.ValorId,
                NombreUsuario = usuario.ValorTexto,
                CorreoAcceso = correo.ValorTexto,
                Estado = estadoActivo
            }, clave.ValorTexto), empleado, rol, usuario, correo, clave);
        }

        private async Task<bool> AsignarHorarioAsync()
        {
            var empleado = ElegirEmpleado(await consulta.ListarEmpleadosAsync());
            var horario = ElegirReferencia("Horario", await consulta.ListarHorariosAsync(),
                h => h.NombreHorario, h => h.IdHorario, "Horarios");
            var inicio = CampoRegistro.Fecha("Desde");
            var fin = CampoRegistro.Fecha("Hasta", opcional: true);
            return await GuardarAltaAsync("Asignar horario", () =>
            {
                ValidarPeriodo(inicio.ValorFecha, fin.ValorFechaOpcional);
                return new EmpleadoHorario
                {
                    IdEmpleado = empleado.ValorId, IdHorario = horario.ValorId,
                    FechaInicio = inicio.ValorFecha, FechaFin = fin.ValorFechaOpcional
                };
            }, empleado, horario, inicio, fin);
        }

        private async Task<bool> AgregarNovedadAsync()
        {
            var empleado = ElegirEmpleado(await consulta.ListarEmpleadosAsync());
            var tipo = ElegirReferencia("Tipo de novedad", await consulta.ListarTiposNovedadAsync(),
                t => t.NombreTipo, t => t.IdTipoNovedad, "Tipos de novedad");
            var inicio = CampoRegistro.Fecha("Desde");
            var fin = CampoRegistro.Fecha("Hasta", opcional: true);
            var motivo = CampoRegistro.Texto("Motivo", 200, requerido: false);
            return await GuardarAltaAsync("Agregar novedad", () =>
            {
                ValidarPeriodo(inicio.ValorFecha, fin.ValorFechaOpcional);
                return new Novedad
                {
                    IdEmpleado = empleado.ValorId, IdTipoNovedad = tipo.ValorId,
                    FechaInicio = inicio.ValorFecha, FechaFin = fin.ValorFechaOpcional,
                    Motivo = motivo.ValorTexto, EstadoAprobacion = "Pendiente"
                };
            }, empleado, tipo, inicio, fin, motivo);
        }

        private static CampoRegistro ElegirEmpleado(List<Empleado> filas)
        {
            return ElegirReferencia("Empleado", filas, e => NombreEmpleado(e) + " · " + e.NumeroDocumento,
                e => e.IdEmpleado, "Empleados");
        }

        private static CampoRegistro ElegirReferencia<T>(string etiqueta, List<T> filas,
            Func<T, string> mostrar, Func<T, int> id, string modulo)
        {
            if (filas.Count == 0)
                throw new InvalidOperationException($"Agrega primero un registro en «{modulo}».");
            return CampoRegistro.Elegir(etiqueta, filas, mostrar, id);
        }

        private static void ValidarPeriodo(DateTime inicio, DateTime? fin)
        {
            if (fin < inicio)
                throw new ArgumentException("La fecha final debe ser igual o posterior a la fecha inicial.");
        }

        private Task<bool> GuardarAltaAsync<T>(string titulo, Func<T> crear, params CampoRegistro[] campos) where T : class
        {
            return AbrirAltaAsync(titulo, () => consulta.AgregarAsync(sesion, crear()), campos);
        }

        private async Task<bool> AbrirAltaAsync(string titulo, Func<Task> guardar, params CampoRegistro[] campos)
        {
            if (IsDisposed || Disposing) return false;
            using (var editor = new EditorRegistro(titulo, campos))
            {
                while (!IsDisposed && !Disposing && editor.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        estado.Text = "Guardando en Oracle…";
                        await guardar();
                        return true;
                    }
                    catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
                    {
                        if (!IsDisposed && !Disposing)
                            MessageBox.Show(this, error.Message, titulo, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                return false;
            }
        }
    }
}
