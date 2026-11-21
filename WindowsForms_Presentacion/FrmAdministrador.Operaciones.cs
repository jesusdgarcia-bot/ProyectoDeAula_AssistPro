using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Entidades;
using Logica;

namespace WindowsForms_Presentacion
{
    internal sealed partial class FrmAdministrador
    {
        private void ConfigurarOperaciones()
        {
            AgregarVista(ModuloMarcaciones, new[] { "ID", "Personal", "Fecha", "Hora", "Marcación", "Sede", "Unidad actual", "Terminal", "Verificación" },
                () => consulta is IConsultaAsistenciaHospitalaria hospitalaria
                    ? hospitalaria.ConsultarAsistenciaAsync(desde.Value, hasta.Value, idEmpleadoFiltrado,
                        idSedeFiltrada, idUnidadFiltrada, textoBuscado)
                    : consulta.ConsultarAsistenciaAsync(desde.Value, hasta.Value, idEmpleadoFiltrado, textoBuscado),
                registro => new object[]
                {
                    registro.IdRegistro, NombreEmpleado(registro.Empleado), registro.Fecha.ToString("dd/MM/yyyy"),
                    registro.Hora.ToString("HH:mm:ss"), registro.TipoMarcacion, registro.Dispositivo?.Sede?.NombreSede,
                    registro.Empleado?.Departamento?.NombreDepartamento, registro.Dispositivo?.NombreDispositivo,
                    string.Equals(registro.MetodoVerificacion?.Trim(), "Clave", StringComparison.OrdinalIgnoreCase)
                        ? "Contraseña" : registro.MetodoVerificacion
                }, () => RegistrarMarcacionAsync(), usaFechas: true, editar: RegistrarMarcacionAsync,
                eliminar: registro => consulta.EliminarAsync<RegistroAsistencia>(sesion, registro.IdRegistro),
                textoAgregar: "Marcación manual");
            AgregarVista(ModuloPersonal, new[] { "ID", "Nombre", "Documento", "Unidad o servicio", "Cargo", "Estado" },
                consulta.ListarEmpleadosAsync, empleado => new object[]
                {
                    empleado.IdEmpleado, NombreEmpleado(empleado), empleado.NumeroDocumento,
                    empleado.Departamento?.NombreDepartamento, empleado.Cargo?.NombreCargo, empleado.Estado
                }, () => AgregarEmpleadoAsync(), editar: AgregarEmpleadoAsync,
                eliminar: empleado => consulta.EliminarAsync<Empleado>(sesion, empleado.IdEmpleado),
                textoAgregar: "Registrar personal");
            AgregarVista(ModuloCuentas, new[] { "ID", "Usuario", "Empleado", "Rol", "Estado" },
                consulta.ListarUsuariosAsync, cuenta => new object[]
                {
                    cuenta.IdUsuario, cuenta.NombreUsuario, cuenta.Empleado, cuenta.Rol, cuenta.Estado
                }, () => AgregarUsuarioAsync(), editar: AgregarUsuarioAsync,
                eliminar: cuenta => consulta.EliminarAsync<Usuario>(sesion, cuenta.IdUsuario));
            AgregarVista(ModuloAsignaciones, new[] { "Personal", "Turno", "Horario", "Desde", "Hasta" },
                consulta.ListarAsignacionesAsync, asignacion => new object[]
                {
                    NombreEmpleado(asignacion.Empleado), asignacion.Horario?.NombreHorario,
                    HorasTurno(asignacion.Horario),
                    asignacion.FechaInicio.ToString("dd/MM/yyyy"), asignacion.FechaFin?.ToString("dd/MM/yyyy")
                }, () => AsignarHorarioAsync(), editar: AsignarHorarioAsync,
                eliminar: asignacion => consulta.EliminarAsync<EmpleadoHorario>(sesion, asignacion.IdEmpleadoHorario),
                textoAgregar: "Asignar turno");
            AgregarVista(ModuloNovedades, new[] { "Personal", "Tipo", "Desde", "Hasta", "Estado", "Motivo" },
                consulta.ListarNovedadesAsync, novedad => new object[]
                {
                    NombreEmpleado(novedad.Empleado), novedad.TipoNovedad?.NombreTipo,
                    novedad.FechaInicio.ToString("dd/MM/yyyy"), novedad.FechaFin?.ToString("dd/MM/yyyy"),
                    novedad.EstadoAprobacion, novedad.Motivo
                }, () => AgregarNovedadAsync(), editar: AgregarNovedadAsync,
                eliminar: novedad => consulta.EliminarAsync<Novedad>(sesion, novedad.IdNovedad));
            AgregarVista(ModuloBiometria, new[] { "ID", "Personal", "Biometría", "Fecha de registro" },
                consulta.ListarPlantillasAsync, plantilla => new object[]
                {
                    plantilla.IdPlantilla, plantilla.Empleado, plantilla.TipoBiometria,
                    plantilla.FechaRegistro.ToString("dd/MM/yyyy")
                }, reconocimientoHabilitado ? (Func<Task<bool>>)(() => RegistrarRostroAsync()) : null,
                editar: reconocimientoHabilitado ? (Func<PlantillaAdministrada, Task<bool>>)RegistrarRostroAsync : null,
                eliminar: plantilla => consulta.EliminarAsync<PlantillaBiometrica>(sesion, plantilla.IdPlantilla),
                textoAgregar: "Registrar rostro");
            AgregarVista(ModuloVisitantes, new[] { "Nombre", "Documento / ID", "Correo", "Fecha de registro" },
                ServicioVisitantes.ListarAsync, visitante => new object[]
                {
                    visitante.Nombre, visitante.Identificacion, visitante.Correo,
                    visitante.FechaRegistro.ToString("dd/MM/yyyy HH:mm")
                }, () => AgregarVisitanteAsync());
        }

        private async Task<bool> AgregarVisitanteAsync()
        {
            var configuracion = ServicioVisitantes.LeerConfiguracionCorreo();
            if (configuracion == null)
            {
                if (!ConfigurarCorreoVisitantes()) return false;
                configuracion = ServicioVisitantes.LeerConfiguracionCorreo();
                if (configuracion == null) return false;
            }

            var nombre = CampoRegistro.Texto("Nombre completo", 120);
            var identificacion = CampoRegistro.Texto("Documento / ID", 30);
            var correo = CampoRegistro.Texto("Correo electrónico", 160);
            using (var editor = new EditorRegistro("Registrar visitante y enviar QR", nombre, identificacion, correo))
            {
                while (!IsDisposed && !Disposing && editor.ShowDialog(this) == DialogResult.OK)
                {
                    VisitanteLocal visitante;
                    byte[] imagenQr;
                    try
                    {
                        string direccion = correo.ValorTexto;
                        new System.Net.Mail.MailAddress(direccion);
                        visitante = new VisitanteLocal
                        {
                            Nombre = nombre.ValorTexto,
                            Identificacion = identificacion.ValorTexto,
                            Correo = direccion,
                            CodigoQr = Guid.NewGuid().ToString("N"),
                            FechaRegistro = DateTime.Now
                        };
                        imagenQr = ServicioVisitantes.GenerarQr(visitante);
                        ServicioVisitantes.Guardar(visitante);
                    }
                    catch (Exception error) when (error is ArgumentException || error is InvalidOperationException ||
                        error is FormatException)
                    {
                        MessageBox.Show(this, error.Message, "Datos del visitante", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    estado.Text = "Visitante guardado en este equipo. Enviando el código QR…";
                    try
                    {
                        await ServicioVisitantes.EnviarQrAsync(visitante, configuracion, imagenQr);
                        if (!IsDisposed && !Disposing) estado.Text = "Código QR enviado a " + visitante.Correo + ".";
                    }
                    catch (InvalidOperationException error)
                    {
                        if (!IsDisposed && !Disposing) estado.Text = error.Message;
                    }
                    return true;
                }
            }
            return false;
        }

        private void AbrirConfiguracionCorreoVisitantes()
        {
            try
            {
                ConfigurarCorreoVisitantes();
            }
            catch (Exception error)
            {
                InformarErrorOperacion(error);
            }
        }

        private bool ConfigurarCorreoVisitantes()
        {
            var actual = ServicioVisitantes.LeerConfiguracionCorreo();
            var servidor = CampoRegistro.Texto("Servidor SMTP", 160).ConTexto(actual?.Servidor ?? "smtp.gmail.com");
            var puerto = CampoRegistro.Texto("Puerto SMTP con TLS", 5).ConTexto((actual?.Puerto ?? 587).ToString());
            var remitente = CampoRegistro.Texto("Correo remitente", 160).ConTexto(actual?.Remitente);
            var usuario = CampoRegistro.Texto("Usuario SMTP", 160).ConTexto(actual?.Usuario);
            var clave = CampoRegistro.Clave("Contraseña o clave de aplicación (vacía para conservar)", requerido: false);

            using (var editor = new EditorRegistro("Configurar envío de correo", servidor, puerto, remitente, usuario, clave))
            {
                while (!IsDisposed && !Disposing && editor.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        int numeroPuerto;
                        if (!int.TryParse(puerto.ValorTexto, out numeroPuerto) || numeroPuerto < 1 || numeroPuerto > 65535)
                            throw new ArgumentException("El puerto SMTP debe ser un número entre 1 y 65535.");
                        string contrasena = clave.ValorTexto ?? actual?.Clave;
                        if (string.IsNullOrWhiteSpace(contrasena))
                            throw new ArgumentException("Escribe la contraseña SMTP o la clave de aplicación del correo.");
                        new System.Net.Mail.MailAddress(remitente.ValorTexto);
                        ServicioVisitantes.GuardarConfiguracionCorreo(new ConfiguracionCorreoLocal
                        {
                            Servidor = servidor.ValorTexto,
                            Puerto = numeroPuerto,
                            Remitente = remitente.ValorTexto,
                            Usuario = usuario.ValorTexto,
                            Clave = contrasena
                        });
                        MessageBox.Show(this, "La configuración de correo quedó protegida en este equipo.",
                            "Correo de visitantes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                    catch (Exception error) when (error is ArgumentException || error is InvalidOperationException ||
                        error is FormatException)
                    {
                        MessageBox.Show(this, error.Message, "Configuración de correo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            return false;
        }

        private async Task ReenviarQrVisitanteAsync()
        {
            if (cargando || tabla.SelectedRows.Count != 1 || !(tabla.SelectedRows[0].Tag is VisitanteLocal visitante)) return;
            try
            {
                var configuracion = ServicioVisitantes.LeerConfiguracionCorreo();
                if (configuracion == null)
                {
                    if (!ConfigurarCorreoVisitantes()) return;
                    configuracion = ServicioVisitantes.LeerConfiguracionCorreo();
                    if (configuracion == null) return;
                }
                CambiarCarga(true);
                estado.Text = "Reenviando el código QR a " + visitante.Correo + "…";
                await ServicioVisitantes.EnviarQrAsync(visitante, configuracion);
                estado.Text = "Código QR reenviado a " + visitante.Correo + ".";
            }
            catch (Exception error)
            {
                if (!IsDisposed && !Disposing)
                    estado.Text = error is InvalidOperationException ? error.Message
                        : "No se pudo reenviar el QR. Verifica la conexión y la configuración SMTP.";
            }
            finally
            {
                CambiarCarga(false);
            }
        }

        private async Task<bool> RegistrarRostroAsync(PlantillaAdministrada original = null)
        {
            if (original != null && !string.Equals(original.TipoBiometria?.Trim(), PlantillaFacial.TipoBiometria,
                StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La captura de cámara permite actualizar las plantillas faciales. Selecciona una plantilla de rostro.");
            var empleados = await consulta.ListarEmpleadosAsync();
            var cuentas = await consulta.ListarUsuariosAsync();
            if (IsDisposed || Disposing) return false;
            var activos = new HashSet<int>(cuentas.Where(c => c.Estado?.Trim() == estadoActivo).Select(c => c.IdEmpleado));
            var disponibles = empleados.Where(e => e.Estado?.Trim() == estadoActivo && activos.Contains(e.IdEmpleado)).ToList();
            if (disponibles.Count == 0)
                throw new InvalidOperationException($"El personal debe tener una cuenta activa en «{ModuloCuentas}».");
            int idSeleccionado = original?.IdEmpleado ?? 0;
            if (original == null)
            {
                var empleado = ElegirEmpleado(disponibles);
                using (var editor = new EditorRegistro("Registrar o reemplazar rostro", empleado))
                {
                    if (editor.ShowDialog(this) != DialogResult.OK) return false;
                    idSeleccionado = empleado.ValorId;
                }
            }
            var seleccionado = disponibles.SingleOrDefault(e => e.IdEmpleado == idSeleccionado);
            if (seleccionado == null)
                throw new InvalidOperationException("El empleado de la plantilla debe tener una cuenta activa y estar activo para actualizar su rostro.");
            using (var captura = new FrmCapturaFacial("Registrar rostro | " + NombreEmpleado(seleccionado), registrar: true))
            {
                if (captura.ShowDialog(this) != DialogResult.OK) return false;
                estado.Text = "Guardando plantilla facial en Oracle…";
                var servicio = new Logica.ServicioReconocimientoFacial(new Persistencia.RepositorioFacial(estadoActivo), estadoActivo);
                await servicio.RegistrarAsync(sesion, seleccionado.IdEmpleado, captura.Captura);
                return true;
            }
        }

        private async Task FiltrarAsistenciaAsync()
        {
            if (cargando) return;
            bool seleccionado = false;
            var estadoAnterior = estado.Text;
            CambiarCarga(true);
            try
            {
                var cargarEmpleados = consulta.ListarEmpleadosAsync();
                CampoRegistro sede = null, unidad = null;
                if (consulta is IConsultaAsistenciaHospitalaria)
                {
                    var cargarSedes = consulta.ListarSedesAsync();
                    var cargarUnidades = consulta.ListarDepartamentosAsync();
                    await Task.WhenAll(cargarEmpleados, cargarSedes, cargarUnidades);
                    if (IsDisposed || Disposing) return;
                    sede = CampoFiltro("Sede del terminal", "Todas las sedes", await cargarSedes,
                        s => s.NombreSede, s => s.IdSede, idSedeFiltrada);
                    unidad = CampoFiltro("Unidad actual", "Todas las unidades", await cargarUnidades,
                        u => u.NombreDepartamento + " · " + u.Sede?.NombreSede, u => u.IdDepartamento, idUnidadFiltrada);
                }
                var empleados = await cargarEmpleados;
                if (IsDisposed || Disposing) return;
                var empleado = CampoFiltro("Empleado", "Todo el personal", empleados,
                    DescripcionEmpleado, e => e.IdEmpleado, idEmpleadoFiltrado);
                var campos = sede == null ? new[] { empleado } : new[] { sede, unidad, empleado };
                using (var editor = new EditorRegistro("Filtrar asistencia", campos))
                {
                    if (editor.ShowDialog(this) != DialogResult.OK) return;
                    idEmpleadoFiltrado = empleado.ValorId == 0 ? (int?)null : empleado.ValorId;
                    idSedeFiltrada = sede?.ValorId > 0 ? (int?)sede.ValorId : null;
                    idUnidadFiltrada = unidad?.ValorId > 0 ? (int?)unidad.ValorId : null;
                    seleccionado = true;
                }
            }
            catch (Exception)
            {
                estadoAnterior = "No se pudieron cargar los filtros de asistencia.";
            }
            finally
            {
                CambiarCarga(false);
                if (!seleccionado && !IsDisposed && !Disposing) estado.Text = estadoAnterior;
            }
            if (seleccionado && !IsDisposed && !Disposing) await CargarVistaAsync();
        }

        private static CampoRegistro CampoFiltro<T>(string etiqueta, string opcionGeneral, IEnumerable<T> filas,
            Func<T, string> mostrar, Func<T, int> id, int? seleccionado)
        {
            var opciones = new[] { new { Id = 0, Nombre = opcionGeneral } }
                .Concat(filas.Select(fila => new { Id = id(fila), Nombre = mostrar(fila) })).ToArray();
            int actual = seleccionado.HasValue && opciones.Any(o => o.Id == seleccionado.Value) ? seleccionado.Value : 0;
            return CampoRegistro.Elegir(etiqueta, opciones, o => o.Nombre, o => o.Id).SeleccionarId(actual);
        }

        private async Task<bool> RegistrarMarcacionAsync(RegistroAsistencia original = null)
        {
            var empleados = await consulta.ListarEmpleadosAsync();
            var dispositivos = await consulta.ListarDispositivosAsync();
            var empleado = ElegirEmpleado(empleados).SeleccionarId(original?.IdEmpleado);
            var dispositivo = ElegirReferencia("Terminal y sede", dispositivos,
                d => d.NombreDispositivo + " · " + d.Sede?.NombreSede, d => d.IdDispositivo, ModuloTerminales)
                .SeleccionarId(original?.IdDispositivo);
            var fecha = CampoRegistro.Fecha("Fecha de la marcación").ConFecha(original?.Fecha);
            var hora = CampoRegistro.Hora("Hora real de entrada o salida").ConHora(original?.Hora);
            var tipo = CampoRegistro.Opciones("Tipo de marcación", "Entrada", "Salida").SeleccionarTexto(original?.TipoMarcacion);
            DateTime dia = PlanificacionTurnos.AhoraColombia.Date;
            int idEmpleado = 0;
            bool guardado = await GuardarRegistroAsync(original == null ? "Marcación manual" : "Corregir marcación", original, () =>
            {
                dia = fecha.ValorFecha.Date;
                idEmpleado = empleado.ValorId;
                DateTime horaRegistro = original != null && original.Fecha.Date == dia && original.Hora.TimeOfDay == hora.ValorHora
                    ? original.Hora : dia.Add(hora.ValorHora);
                string metodo = original?.MetodoVerificacion ?? "Manual";
                if (original != null &&
                    (original.IdEmpleado != idEmpleado || original.IdDispositivo != dispositivo.ValorId ||
                        original.Fecha.Date != dia || original.Hora != horaRegistro || original.TipoMarcacion != tipo.ValorTexto))
                    metodo = "Manual";
                return new RegistroAsistencia
                {
                    IdRegistro = original?.IdRegistro ?? 0,
                    IdEmpleado = idEmpleado,
                    IdDispositivo = dispositivo.ValorId,
                    Fecha = fecha.ValorFecha,
                    Hora = horaRegistro,
                    TipoMarcacion = tipo.ValorTexto,
                    MetodoVerificacion = metodo
                };
            }, empleado, dispositivo, fecha, hora, tipo);
            if (guardado && !IsDisposed && !Disposing)
            {
                if (dia < desde.Value.Date || dia > hasta.Value.Date)
                    desde.Value = hasta.Value = dia;
                if (idEmpleadoFiltrado.HasValue) idEmpleadoFiltrado = idEmpleado;
                if (idSedeFiltrada.HasValue) idSedeFiltrada = dispositivos.First(d => d.IdDispositivo == dispositivo.ValorId).IdSede;
                if (idUnidadFiltrada.HasValue) idUnidadFiltrada = empleados.First(e => e.IdEmpleado == idEmpleado).IdDepartamento;
            }
            return guardado;
        }

        private async Task TomarAsistenciaFacialAsync()
        {
            if (cargando || !reconocimientoHabilitado) return;
            string resultadoCarga = null;
            CambiarCarga(true);
            try
            {
                estado.Text = "Preparando el registro de asistencia…";
                var dispositivos = (await consulta.ListarDispositivosAsync())
                    .Where(dispositivo => dispositivo.Estado?.Trim() == estadoActivo).ToList();
                if (IsDisposed || Disposing) return;
                if (dispositivos.Count == 0)
                    throw new InvalidOperationException($"Activa un terminal en «{ModuloTerminales}» antes de registrar marcaciones.");

                estado.Text = "Abriendo la cámara para identificar al empleado…";
                using (var captura = new FrmCapturaFacial("Tomar asistencia · reconocimiento facial", registrar: false))
                {
                    if (captura.ShowDialog(this) != DialogResult.OK)
                    {
                        estado.Text = "Registro facial cancelado; no se guardó ninguna marcación.";
                        return;
                    }

                    estado.Text = "Verificando la identidad…";
                    var servicio = new ServicioReconocimientoFacial(
                        new Persistencia.RepositorioFacial(estadoActivo), estadoActivo);
                    var identidad = await servicio.AutenticarAsync(captura.Captura);
                    if (IsDisposed || Disposing) return;
                    if (identidad == null)
                    {
                        estado.Text = "No se pudo confirmar la identidad. No se registró asistencia.";
                        return;
                    }

                    DateTime hoy = PlanificacionTurnos.AhoraColombia.Date;
                    var marcaciones = await consulta.ConsultarAsistenciaAsync(hoy, hoy, identidad.IdEmpleado);
                    if (IsDisposed || Disposing) return;
                    string ultimaMarcacion = marcaciones.FirstOrDefault()?.TipoMarcacion?.Trim();
                    string sugerencia = string.Equals(ultimaMarcacion, "Entrada", StringComparison.OrdinalIgnoreCase)
                        ? "Salida" : "Entrada";
                    var tipo = CampoRegistro.Opciones("Tipo de marcación", "Entrada", "Salida")
                        .SeleccionarTexto(sugerencia);

                    var dispositivoSugerido = dispositivos.FirstOrDefault(elemento =>
                        elemento.Sede?.NombreSede?.IndexOf("San Mart", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        ?? (dispositivos.Count == 1 ? dispositivos[0] : null);
                    var dispositivo = ElegirReferencia("Terminal y sede", dispositivos,
                        item => item.NombreDispositivo + " · " + item.Sede?.NombreSede,
                        item => item.IdDispositivo, ModuloTerminales)
                        .SeleccionarId(dispositivoSugerido?.IdDispositivo);

                    estado.Text = "Identidad confirmada: " + identidad.Nombre + ". Revisa y confirma la marcación.";
                    bool guardado = await GuardarRegistroAsync("Confirmar asistencia facial · " + identidad.Nombre,
                        (RegistroAsistencia)null, () =>
                        {
                            DateTime ahora = PlanificacionTurnos.AhoraColombia;
                            return new RegistroAsistencia
                            {
                                IdEmpleado = identidad.IdEmpleado,
                                IdDispositivo = dispositivo.ValorId,
                                Fecha = ahora.Date,
                                Hora = ahora,
                                TipoMarcacion = tipo.ValorTexto,
                                MetodoVerificacion = "Facial"
                            };
                        }, tipo, dispositivo);

                    if (IsDisposed || Disposing) return;
                    if (guardado)
                        resultadoCarga = "Asistencia registrada para " + identidad.Nombre + " mediante reconocimiento facial.";
                    else
                        estado.Text = "Registro cancelado; no se guardó ninguna marcación.";
                }
            }
            catch (Exception error)
            {
                if (IsDisposed || Disposing) return;
                if (error is UnauthorizedAccessException)
                {
                    InformarErrorOperacion(error);
                    return;
                }
                estado.Text = error is ArgumentException || error is InvalidOperationException
                    ? error.Message : "No se pudo completar el registro facial. Revisa la cámara y la conexión e inténtalo de nuevo.";
            }
            finally
            {
                CambiarCarga(false);
            }
            if (resultadoCarga != null && !IsDisposed && !Disposing)
                await CargarVistaAsync(resultadoCarga);
        }

        private async Task<bool> AgregarEmpleadoAsync(Empleado original = null)
        {
            var departamento = ElegirReferencia("Unidad o servicio", await consulta.ListarDepartamentosAsync(),
                d => d.NombreDepartamento + " · " + d.Sede?.NombreSede, d => d.IdDepartamento, ModuloUnidades)
                .SeleccionarId(original?.IdDepartamento);
            var cargo = ElegirReferencia("Cargo", await consulta.ListarCargosAsync(),
                c => c.NombreCargo, c => c.IdCargo, ModuloCargos).SeleccionarId(original?.IdCargo);
            var tipo = CampoRegistro.Texto("Tipo de documento", 2).ConTexto(original?.TipoDocumento);
            var documento = CampoRegistro.Texto("Número de documento", 20).ConTexto(original?.NumeroDocumento);
            var nombres = CampoRegistro.Texto("Nombres", 60).ConTexto(original?.Nombres);
            var apellidos = CampoRegistro.Texto("Apellidos", 60).ConTexto(original?.Apellidos);
            var correo = CampoRegistro.Texto("Correo", 100, requerido: false).ConTexto(original?.Correo);
            var telefono = CampoRegistro.Texto("Teléfono", 20, requerido: false).ConTexto(original?.Telefono);
            var nacimiento = CampoRegistro.Fecha("Fecha de nacimiento", opcional: true).ConFecha(original?.FechaNacimiento);
            var ingreso = CampoRegistro.Fecha("Fecha de ingreso").ConFecha(original?.FechaIngreso);
            var estadoEmpleado = original == null ? null : CampoEstado(original.Estado);
            var campos = new List<CampoRegistro> { tipo, documento, nombres, apellidos, correo, telefono, nacimiento, ingreso, departamento, cargo };
            if (estadoEmpleado != null) campos.Add(estadoEmpleado);
            return await GuardarRegistroAsync(original == null ? "Registrar personal" : "Actualizar datos del personal", original, () =>
            {
                if (nacimiento.ValorFechaOpcional?.Date > ingreso.ValorFecha.Date)
                    throw new ArgumentException("El nacimiento debe ser anterior o igual al ingreso.");
                return new Empleado
                {
                    IdEmpleado = original?.IdEmpleado ?? 0,
                    TipoDocumento = original != null && tipo.ValorTexto == original.TipoDocumento
                        ? original.TipoDocumento : tipo.ValorTexto.ToUpperInvariant(),
                    NumeroDocumento = documento.ValorTexto,
                    Nombres = nombres.ValorTexto,
                    Apellidos = apellidos.ValorTexto,
                    Correo = correo.ValorTexto,
                    Telefono = telefono.ValorTexto,
                    FechaNacimiento = nacimiento.ValorFechaOpcional,
                    FechaIngreso = ingreso.ValorFecha,
                    Estado = estadoEmpleado?.ValorTexto ?? estadoActivo,
                    IdDepartamento = departamento.ValorId,
                    IdCargo = cargo.ValorId
                };
            }, campos.ToArray());
        }

        private async Task<bool> AgregarUsuarioAsync(CuentaAdministrada original = null)
        {
            var empleados = await consulta.ListarEmpleadosAsync();
            var cuentas = await consulta.ListarUsuariosAsync();
            var ocupados = new HashSet<int>(cuentas.Where(c => c.IdUsuario != original?.IdUsuario).Select(c => c.IdEmpleado));
            var disponibles = empleados.Where(e => (e.Estado?.Trim() == estadoActivo || e.IdEmpleado == original?.IdEmpleado)
                && !ocupados.Contains(e.IdEmpleado)).ToList();
            if (disponibles.Count == 0)
                throw new InvalidOperationException("Registra un empleado activo que todavía no tenga cuenta.");
            var empleado = ElegirEmpleado(disponibles).SeleccionarId(original?.IdEmpleado);
            var rol = ElegirReferencia("Rol", await consulta.ListarRolesAsync(), r => r.NombreRol, r => r.IdRol, ModuloRoles)
                .SeleccionarId(original?.IdRol);
            var usuario = CampoRegistro.Texto("Nombre de usuario", 30).ConTexto(original?.NombreUsuario);
            var correo = CampoRegistro.Texto("Correo de acceso", 100).ConTexto(original?.CorreoAcceso);
            var clave = CampoRegistro.Clave(original == null ? "Contraseña" : "Nueva contraseña (opcional)", requerido: original == null);
            var estadoUsuario = original == null ? null : CampoEstado(original.Estado);
            var campos = new List<CampoRegistro> { empleado, rol, usuario, correo, clave };
            if (estadoUsuario != null) campos.Add(estadoUsuario);
            return await AbrirRegistroAsync(original == null ? "Agregar usuario" : "Actualizar usuario", () =>
            {
                var registro = new Usuario
                {
                    IdUsuario = original?.IdUsuario ?? 0,
                    IdEmpleado = empleado.ValorId,
                    IdRol = rol.ValorId,
                    NombreUsuario = usuario.ValorTexto,
                    CorreoAcceso = correo.ValorTexto,
                    Estado = estadoUsuario?.ValorTexto ?? estadoActivo
                };
                return original == null ? consulta.CrearUsuarioAsync(sesion, registro, clave.ValorTexto)
                    : consulta.ActualizarUsuarioAsync(sesion, registro, original, clave.ValorTexto);
            }, campos.ToArray());
        }

        private async Task<bool> AsignarHorarioAsync(EmpleadoHorario original = null)
        {
            var empleado = ElegirEmpleado(await consulta.ListarEmpleadosAsync()).SeleccionarId(original?.IdEmpleado);
            var horario = ElegirReferencia("Turno", await consulta.ListarHorariosAsync(),
                h => h.NombreHorario + " · " + HorasTurno(h) + " · " + h.DiasAplicables,
                h => h.IdHorario, ModuloTurnos).SeleccionarId(original?.IdHorario);
            var inicio = CampoRegistro.Fecha("Desde").ConFecha(original?.FechaInicio);
            var fin = CampoRegistro.Fecha("Hasta", opcional: true).ConFecha(original?.FechaFin);
            return await GuardarRegistroAsync(original == null ? "Asignar turno" : "Actualizar asignación de turno", original, () =>
            {
                ValidarPeriodo(inicio.ValorFecha, fin.ValorFechaOpcional);
                return new EmpleadoHorario
                {
                    IdEmpleadoHorario = original?.IdEmpleadoHorario ?? 0,
                    IdEmpleado = empleado.ValorId, IdHorario = horario.ValorId,
                    FechaInicio = inicio.ValorFecha, FechaFin = fin.ValorFechaOpcional
                };
            }, empleado, horario, inicio, fin);
        }

        private async Task<bool> AgregarNovedadAsync(Novedad original = null)
        {
            var empleado = ElegirEmpleado(await consulta.ListarEmpleadosAsync()).SeleccionarId(original?.IdEmpleado);
            var tipo = ElegirReferencia("Tipo de novedad", await consulta.ListarTiposNovedadAsync(),
                t => t.NombreTipo, t => t.IdTipoNovedad, ModuloTiposNovedad).SeleccionarId(original?.IdTipoNovedad);
            var inicio = CampoRegistro.Fecha("Desde").ConFecha(original?.FechaInicio);
            var fin = CampoRegistro.Fecha("Hasta", opcional: true).ConFecha(original?.FechaFin);
            var motivo = CampoRegistro.Texto("Motivo laboral (sin datos clínicos)", 200, requerido: false).ConTexto(original?.Motivo);
            var aprobacion = original == null ? null : CampoRegistro.Opciones("Estado de aprobación", "Pendiente", "Aprobado", "Rechazado")
                .SeleccionarTexto(original.EstadoAprobacion);
            var campos = new List<CampoRegistro> { empleado, tipo, inicio, fin, motivo };
            if (aprobacion != null) campos.Add(aprobacion);
            return await GuardarRegistroAsync(original == null ? "Agregar novedad" : "Actualizar novedad", original, () =>
            {
                ValidarPeriodo(inicio.ValorFecha, fin.ValorFechaOpcional);
                string estadoAprobacion = aprobacion?.ValorTexto ?? "Pendiente";
                int? idUsuarioAprueba = original?.IdUsuarioAprueba;
                if (!string.Equals(estadoAprobacion, original?.EstadoAprobacion, StringComparison.Ordinal))
                    idUsuarioAprueba = string.Equals(estadoAprobacion, "Pendiente", StringComparison.OrdinalIgnoreCase)
                        ? (int?)null : sesion.IdUsuario;
                return new Novedad
                {
                    IdNovedad = original?.IdNovedad ?? 0,
                    IdEmpleado = empleado.ValorId, IdTipoNovedad = tipo.ValorId,
                    FechaInicio = inicio.ValorFecha, FechaFin = fin.ValorFechaOpcional,
                    Motivo = motivo.ValorTexto, EstadoAprobacion = estadoAprobacion,
                    IdUsuarioAprueba = idUsuarioAprueba
                };
            }, campos.ToArray());
        }

        private static CampoRegistro ElegirEmpleado(List<Empleado> filas)
        {
            return ElegirReferencia("Empleado", filas, DescripcionEmpleado, e => e.IdEmpleado, ModuloPersonal);
        }

        private static string DescripcionEmpleado(Empleado empleado)
        {
            string descripcion = NombreEmpleado(empleado) + " · " + empleado.NumeroDocumento;
            return empleado.Departamento == null ? descripcion : descripcion + " · " + empleado.Departamento.NombreDepartamento;
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
            if (fin?.Date < inicio.Date)
                throw new ArgumentException("La fecha final debe ser igual o posterior a la fecha inicial.");
        }

        private CampoRegistro CampoEstado(string actual)
        {
            string estadoInactivo = estadoActivo == "I" ? "A" : "I";
            return CampoRegistro.Opciones("Estado", estadoActivo, estadoInactivo).SeleccionarTexto(actual);
        }

        private Task<bool> GuardarRegistroAsync<T>(string titulo, T original, Func<T> crear,
            params CampoRegistro[] campos) where T : class
        {
            return AbrirRegistroAsync(titulo, () => original == null ? consulta.AgregarAsync(sesion, crear())
                : consulta.ActualizarAsync(sesion, crear(), original), campos);
        }

        private async Task<bool> AbrirRegistroAsync(string titulo, Func<Task> guardar, params CampoRegistro[] campos)
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
