using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entidades;
using Logica;
using Persistencia;

namespace Pruebas
{
    // Runner sin conexión a Oracle ni cámara. Un fallo devuelve código 1.
    internal static class Program
    {
        private static int ejecutadas;

        private static int Main()
        {
            try
            {
                EjecutarAsync().GetAwaiter().GetResult();
                Console.WriteLine("Correctas: " + ejecutadas + " pruebas.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("FALLO: " + error);
                return 1;
            }
        }

        private static async Task EjecutarAsync()
        {
            await Probar("Los turnos diurnos y nocturnos respetan los días y fechas asignados", PruebasTurnos.Comprobar);
            await Probar("Reconoce una plantilla recuperada del almacenamiento", async () =>
            {
                var repo = RepositorioCon(Candidato(1, Vector(0)));
                var sesion = await Servicio(repo).AutenticarAsync(CompletarCaptura(Vector(0)));
                Verificar(sesion != null && sesion.IdUsuario == 1 && sesion.IdEmpleado == 101,
                    "Debe abrir la sesión correspondiente al rostro registrado.");
            });
            await Probar("Rechaza un rostro desconocido", async () =>
            {
                var sesion = await Servicio(RepositorioCon(Candidato(1, Vector(0)))).AutenticarAsync(CompletarCaptura(Vector(1)));
                Verificar(sesion == null, "Un rostro diferente no debe iniciar sesión.");
            });
            await Probar("Una coincidencia débil no basta para iniciar sesión", async () =>
            {
                var captura = Vector(0); captura[0] = 0.4f; captura[1] = (float)Math.Sqrt(1 - 0.4 * 0.4);
                var sesion = await Servicio(RepositorioCon(Candidato(1, Vector(0)))).AutenticarAsync(CompletarCaptura(captura));
                Verificar(sesion == null, "Una similitud positiva pero insuficiente debe rechazarse.");
            });
            await Probar("La magnitud del vector no cambia la identidad reconocida", async () =>
            {
                var captura = Vector(0); captura[0] = 15;
                var almacenada = Vector(0); almacenada[0] = 0.005f;
                var sesion = await Servicio(RepositorioCon(Candidato(1, almacenada))).AutenticarAsync(CompletarCaptura(captura));
                Verificar(sesion != null && sesion.IdUsuario == 1,
                    "Deben compararse las características normalizadas del modelo.");
            });
            await Probar("Rechaza dos identidades con coincidencia ambigua", async () =>
            {
                var repo = RepositorioCon(Candidato(1, Vector(0)), Candidato(2, Vector(0)));
                Verificar(await Servicio(repo).AutenticarAsync(CompletarCaptura(Vector(0))) == null,
                    "No debe elegir arbitrariamente entre dos empleados con el mismo rostro.");
            });
            await Probar("Elige una coincidencia clara aunque no sea el primer candidato", async () =>
            {
                var repo = RepositorioCon(Candidato(1, Vector(1)), Candidato(2, Vector(0)));
                var sesion = await Servicio(repo).AutenticarAsync(CompletarCaptura(Vector(0)));
                Verificar(sesion != null && sesion.IdUsuario == 2, "Debe comparar todos los candidatos.");
            });
            await Probar("Una plantilla dañada no impide reconocer otra válida", async () =>
            {
                var danada = Candidato(1, Vector(1));
                danada.DatosPlantilla = new byte[] { 1, 2, 3 };
                var sesion = await Servicio(RepositorioCon(danada, Candidato(2, Vector(0))))
                    .AutenticarAsync(CompletarCaptura(Vector(0)));
                Verificar(sesion != null && sesion.IdUsuario == 2, "Debe omitir los datos biométricos incompatibles.");
            });
            await Probar("Rechaza una cuenta inactiva", async () =>
            {
                var inactivo = Candidato(1, Vector(0));
                inactivo.Usuario.Estado = "I";
                var repo = RepositorioCon(inactivo, Candidato(2, Vector(1)));
                Verificar(await Servicio(repo).AutenticarAsync(CompletarCaptura(Vector(0))) == null, "La cuenta inactiva no debe acceder.");
            });
            await Probar("Rechaza un empleado inactivo", async () =>
            {
                var inactivo = Candidato(1, Vector(0));
                inactivo.Usuario.Empleado.Estado = "I";
                var repo = RepositorioCon(inactivo, Candidato(2, Vector(1)));
                Verificar(await Servicio(repo).AutenticarAsync(CompletarCaptura(Vector(0))) == null, "El empleado inactivo no debe acceder.");
            });
            await Probar("Rechaza una cuenta sin rol", async () =>
            {
                var sinRol = Candidato(1, Vector(0));
                sinRol.Usuario.Rol = null;
                var repo = RepositorioCon(sinRol, Candidato(2, Vector(1)));
                Verificar(await Servicio(repo).AutenticarAsync(CompletarCaptura(Vector(0))) == null, "Una cuenta sin rol no debe acceder.");
            });
            await Probar("Revalida la cuenta antes de entregar la sesión", async () =>
            {
                var repo = RepositorioCon(Candidato(1, Vector(0)));
                repo.Revalidar = id => null;
                Verificar(await Servicio(repo).AutenticarAsync(CompletarCaptura(Vector(0))) == null,
                    "Una cuenta eliminada después de cargar las plantillas no debe acceder.");
                Verificar(repo.Revalidaciones == 1, "Debe consultar de nuevo la cuenta reconocida.");
            });
            await Probar("Rechaza una desactivación ocurrida durante el reconocimiento", async () =>
            {
                var repo = RepositorioCon(Candidato(1, Vector(0)));
                repo.Revalidar = id => { var usuario = UsuarioActivo(id); usuario.Estado = "I"; return usuario; };
                Verificar(await Servicio(repo).AutenticarAsync(CompletarCaptura(Vector(0))) == null,
                    "La sesión no debe usar el estado antiguo de la cuenta.");
            });
            await Probar("Rechaza una cuenta reasignada a otro empleado durante el reconocimiento", async () =>
            {
                var repo = RepositorioCon(Candidato(1, Vector(0)));
                repo.Revalidar = id => { var usuario = UsuarioActivo(id); usuario.IdEmpleado = 999; return usuario; };
                Verificar(await Servicio(repo).AutenticarAsync(CompletarCaptura(Vector(0))) == null,
                    "El rostro no debe autorizar al nuevo titular de una cuenta reasignada.");
            });
            await Probar("Usa el rol vigente al crear la sesión", async () =>
            {
                var antiguo = Candidato(1, Vector(0));
                antiguo.Usuario.Rol.NombreRol = "Administrador";
                var repo = RepositorioCon(antiguo);
                repo.Revalidar = UsuarioActivo;
                var sesion = await Servicio(repo).AutenticarAsync(CompletarCaptura(Vector(0)));
                Verificar(sesion != null && !sesion.EsAdministrador, "No debe conservar privilegios ya revocados.");
            });
            await Probar("Informa la ausencia de plantillas registradas", async () =>
            {
                await EsperarAsync<InvalidOperationException>(() => Servicio(RepositorioCon()).AutenticarAsync(CompletarCaptura(Vector(0))));
            });
            await Probar("Rechaza capturas numéricamente inválidas", () =>
            {
                Esperar<ArgumentException>(() => PlantillaFacial.Crear(new float[128]));
                Esperar<ArgumentException>(() => PlantillaFacial.Crear(new float[12]));
                var nan = Vector(0); nan[3] = float.NaN;
                Esperar<ArgumentException>(() => PlantillaFacial.Crear(nan));
                var infinito = Vector(0); infinito[3] = float.PositiveInfinity;
                Esperar<ArgumentException>(() => PlantillaFacial.Crear(infinito));
                return Task.CompletedTask;
            });
            await Probar("Una captura inválida no consulta las plantillas", async () =>
            {
                var repo = RepositorioCon(Candidato(1, Vector(0)));
                var invalida = new PruebaDeVidaSimulada(new float[PlantillaFacial.Dimensiones]).Fotograma();
                await EsperarAsync<InvalidOperationException>(() => Servicio(repo).AutenticarAsync(invalida.Captura));
                Verificar(repo.Lecturas == 0 && repo.Revalidaciones == 0,
                    "La captura debe validarse antes de consultar información biométrica.");
            });
            await Probar("Rechaza BLOB truncado o incompatible", () =>
            {
                PlantillaFacial plantilla;
                Verificar(!PlantillaFacial.IntentarLeer(null, out plantilla), "Un BLOB nulo no es una plantilla.");
                Verificar(!PlantillaFacial.IntentarLeer(new byte[] { 1, 2, 3 }, out plantilla), "Debe rechazar datos truncados.");
                var bytes = PlantillaFacial.Crear(Vector(0)).Serializar();
                Array.Resize(ref bytes, bytes.Length - 1);
                Verificar(!PlantillaFacial.IntentarLeer(bytes, out plantilla), "Debe rechazar una plantilla incompleta.");
                var incompatible = PlantillaFacial.Crear(Vector(0)).Serializar();
                incompatible[0] ^= 0xFF;
                Verificar(!PlantillaFacial.IntentarLeer(incompatible, out plantilla), "Debe reconocer su formato antes de leerlo.");
                return Task.CompletedTask;
            });
            await Probar("Permite al administrador registrar una captura persistible", async () =>
            {
                var repo = RepositorioCon();
                var admin = UsuarioActivo(9); admin.Rol.NombreRol = "Administrador";
                await Servicio(repo).RegistrarAsync(new SesionUsuario(admin), 101, CompletarCaptura(Vector(0)));
                PlantillaFacial plantilla;
                Verificar(repo.Guardados == 1 && repo.EmpleadoGuardado == 101 &&
                    PlantillaFacial.IntentarLeer(repo.DatosGuardados, out plantilla),
                    "Debe enviar al repositorio la plantilla y el empleado seleccionados.");
            });
            await Probar("Un empleado no puede registrar rostros", async () =>
            {
                var repo = RepositorioCon();
                await EsperarAsync<UnauthorizedAccessException>(() => Servicio(repo).RegistrarAsync(
                    new SesionUsuario(UsuarioActivo(1)), 101, CompletarCaptura(Vector(0))));
                Verificar(repo.Guardados == 0, "No debe escribir una plantilla sin permiso administrativo.");
            });

            await Probar("La prueba exige movimiento y señales pasivas consecutivas", () =>
            {
                var sinMovimiento = new PruebaDeVidaSimulada(Vector(0));
                for (int i = 0; i < 20; i++)
                    Verificar(sinMovimiento.Fotograma().Captura == null, "Una secuencia inmóvil no debe validar presencia.");

                var sinPresencia = new PruebaDeVidaSimulada(Vector(0));
                for (int i = 0; i < 20; i++)
                    Verificar(sinPresencia.Fotograma(probabilidad: 0.2).Etapa == EtapaPruebaDeVida.AlinearRostro,
                        "Las señales activas no se deben pedir con una puntuación pasiva insuficiente.");
                return Task.CompletedTask;
            });
            await Probar("La pérdida de continuidad obliga a comenzar la prueba de vida de nuevo", () =>
            {
                var prueba = new PruebaDeVidaSimulada(Vector(0));
                var interrupciones = new Func<ResultadoPruebaDeVida>[]
                {
                    () => prueba.Fotograma(rostros: 0, intervaloMs: 650),
                    () => prueba.Fotograma(rostros: 2),
                    () => prueba.Fotograma(vector: Vector(1)),
                    () => prueba.Fotograma(intervaloMs: 3000),
                    () => prueba.Fotograma(intervaloMs: 0)
                };
                foreach (var interrumpir in interrupciones)
                {
                    prueba.Fotograma();
                    prueba.Fotograma();
                    Verificar(prueba.Fotograma().Etapa != EtapaPruebaDeVida.AlinearRostro, "La prueba debe pedir una acción.");
                    var resultado = interrumpir();
                    Verificar(resultado.Captura == null && resultado.Etapa == EtapaPruebaDeVida.AlinearRostro,
                        "La prueba debe volver a alinear un solo rostro tras la interrupción.");
                }
                return Task.CompletedTask;
            });
            await Probar("La pérdida breve conserva el reto sin contar los fotogramas interrumpidos", () =>
            {
                var prueba = new PruebaDeVidaSimulada(Vector(0));
                prueba.Fotograma();
                prueba.Fotograma();
                var etapa = prueba.Fotograma().Etapa;
                double parpadeo = etapa == EtapaPruebaDeVida.CerrarOjos ? 0.9 : 0.1;
                double giro = etapa == EtapaPruebaDeVida.GirarIzquierda ? 25
                    : etapa == EtapaPruebaDeVida.GirarDerecha ? -25 : 0;

                Verificar(prueba.Fotograma(parpadeo, giro).Etapa == etapa, "La acción necesita dos fotogramas válidos.");
                Verificar(prueba.Fotograma(rostros: 0).Etapa == etapa, "La pérdida breve debe conservar el reto.");
                Verificar(prueba.Fotograma(parpadeo, giro).Etapa == etapa, "La acción debe volver a reunir evidencia consecutiva.");
                Verificar(prueba.Fotograma(parpadeo, giro).Etapa != etapa, "El mismo rostro debe poder continuar el reto.");
                prueba.Fotograma(rostros: 0);
                var resultado = prueba.Fotograma(intervaloMs: 600);
                Verificar(resultado.Etapa == EtapaPruebaDeVida.AlinearRostro && resultado.Captura == null,
                    "Tras agotar el tiempo de reencuadre, recuperar el rostro debe iniciar otro intento.");
                return Task.CompletedTask;
            });
            await Probar("Una prueba aprobada solo autoriza una operación", async () =>
            {
                var captura = CompletarCaptura(Vector(0));
                var repo = RepositorioCon(Candidato(1, Vector(0)));
                var servicio = Servicio(repo);
                Verificar(await servicio.AutenticarAsync(captura) != null, "La primera operación debe poder consumir la captura.");
                await EsperarAsync<InvalidOperationException>(() => servicio.RegistrarAsync(SesionAdministrativa(), 101, captura));
                Verificar(repo.Lecturas == 1 && repo.Guardados == 0, "Una captura consumida no debe emitir otro registro.");
            });
            await Probar("La marcación con contraseña revalida la cuenta activa antes de guardar", PruebasMarcacionConClave.Comprobar);
            await Probar("La marcación facial guarda la identidad reconocida que coincide con la sesión", async () =>
            {
                var repositorioMarcacion = new RepositorioMarcacionFalso();
                var servicio = new ServicioMarcacionFacial(Servicio(RepositorioCon(Candidato(1, Vector(0)))), repositorioMarcacion);
                await EsperarAsync<UnauthorizedAccessException>(() => servicio.RegistrarAsync(
                    new SesionUsuario(UsuarioActivo(2)), 7, "Entrada", CompletarCaptura(Vector(0))));
                await EsperarAsync<InvalidOperationException>(() => servicio.RegistrarAsync(
                    new SesionUsuario(UsuarioActivo(1)), 7, "Entrada", null));
                Verificar(repositorioMarcacion.Guardados == 0, "El almacenamiento no debe recibir marcaciones rechazadas.");

                var marca = await servicio.RegistrarAsync(new SesionUsuario(UsuarioActivo(1)), 7, "Salida", CompletarCaptura(Vector(0)));
                Verificar(repositorioMarcacion.Guardados == 1 && marca.IdEmpleado == 101 && marca.IdDispositivo == 7 &&
                    marca.TipoMarcacion == "Salida", "La marcación debe usar el empleado reconocido y la selección de la interfaz.");
            });

            await Probar("Las modificaciones administrativas requieren una sesión de administrador", async () =>
            {
                var consulta = new ConsultaAdministracion();
                var empleado = new SesionUsuario(UsuarioActivo(1));
                await EsperarAsync<UnauthorizedAccessException>(() => consulta.ActualizarAsync(empleado,
                    new Sede { IdSede = 1, NombreSede = "Actualizada" }, new Sede { IdSede = 1 }));
                await EsperarAsync<UnauthorizedAccessException>(() => consulta.EliminarAsync<Sede>(empleado, 1));
                await EsperarAsync<UnauthorizedAccessException>(() => consulta.ActualizarUsuarioAsync(empleado,
                    new Usuario { IdUsuario = 1 }, new CuentaAdministrada { IdUsuario = 1 }));
            });
            await Probar("Una edición conserva la identidad del registro original", async () =>
            {
                var consulta = new ConsultaAdministracion();
                var admin = SesionAdministrativa();
                await EsperarAsync<ArgumentException>(() => consulta.ActualizarAsync(admin,
                    new Sede { IdSede = 2 }, new Sede { IdSede = 1 }));
                await EsperarAsync<ArgumentException>(() => consulta.ActualizarUsuarioAsync(admin,
                    new Usuario { IdUsuario = 2 }, new CuentaAdministrada { IdUsuario = 1 }));
            });
            await Probar("Usuarios y biometría utilizan sus operaciones específicas de actualización", async () =>
            {
                var consulta = new ConsultaAdministracion();
                var admin = SesionAdministrativa();
                await EsperarAsync<ArgumentException>(() => consulta.ActualizarAsync(admin,
                    new Usuario { IdUsuario = 1 }, new Usuario { IdUsuario = 1 }));
                await EsperarAsync<ArgumentException>(() => consulta.ActualizarAsync(admin,
                    new PlantillaBiometrica { IdPlantilla = 1 }, new PlantillaBiometrica { IdPlantilla = 1 }));
                await EsperarAsync<ArgumentException>(() => consulta.EliminarAsync<CuentaAdministrada>(admin, 1));
            });
            await Probar("La nueva contraseña se valida antes de acceder al almacenamiento", async () =>
            {
                var consulta = new ConsultaAdministracion();
                var admin = SesionAdministrativa();
                var usuario = new Usuario { IdUsuario = 1 };
                var original = new CuentaAdministrada { IdUsuario = 1 };
                await EsperarAsync<ArgumentException>(() => consulta.ActualizarUsuarioAsync(admin,
                    usuario, original, "corta"));
                await EsperarAsync<ArgumentException>(() => consulta.ActualizarUsuarioAsync(admin,
                    usuario, original, new string('x', 1025)));
            });
            await Probar("Editar y eliminar requieren registros e identificadores válidos", async () =>
            {
                var consulta = new ConsultaAdministracion();
                var admin = SesionAdministrativa();
                await EsperarAsync<ArgumentNullException>(() => consulta.ActualizarAsync<Sede>(admin,
                    null, new Sede { IdSede = 1 }));
                await EsperarAsync<ArgumentNullException>(() => consulta.ActualizarAsync(admin,
                    new Sede { IdSede = 1 }, (Sede)null));
                await EsperarAsync<ArgumentException>(() => consulta.ActualizarAsync(admin,
                    new Sede(), new Sede()));
                await EsperarAsync<ArgumentException>(() => consulta.EliminarAsync<Sede>(admin, 0));
                await EsperarAsync<ArgumentException>(() => consulta.EliminarAsync<Sede>(admin, -1));
            });
        }

        private static SesionUsuario SesionAdministrativa()
        {
            var usuario = UsuarioActivo(9);
            usuario.Rol.NombreRol = "Administrador";
            return new SesionUsuario(usuario);
        }

        private static ServicioReconocimientoFacial Servicio(RepositorioFalso repositorio) =>
            new ServicioReconocimientoFacial(repositorio, "A");

        private static RepositorioFalso RepositorioCon(params CandidatoFacial[] candidatos) =>
            new RepositorioFalso { Candidatos = new List<CandidatoFacial>(candidatos) };

        private static CandidatoFacial Candidato(int id, float[] vector) => new CandidatoFacial
        {
            Usuario = UsuarioActivo(id), DatosPlantilla = PlantillaFacial.Crear(vector).Serializar()
        };

        private static Usuario UsuarioActivo(int id) => new Usuario
        {
            IdUsuario = id, IdEmpleado = 100 + id, IdRol = 1, Estado = "A",
            Empleado = new Empleado { IdEmpleado = 100 + id, Nombres = "Empleado", Apellidos = id.ToString(), Estado = "A" },
            Rol = new Rol { IdRol = 1, NombreRol = "Empleado" }
        };

        private static float[] Vector(int indice)
        {
            var vector = new float[128]; vector[indice] = 1;
            return vector;
        }

        private static CapturaFacialVerificada CompletarCaptura(float[] vector)
        {
            var prueba = new PruebaDeVidaSimulada(vector);
            var resultado = prueba.Fotograma();
            for (int i = 0; i < 18 && resultado.Captura == null; i++)
            {
                switch (resultado.Etapa)
                {
                    case EtapaPruebaDeVida.CerrarOjos:
                        resultado = prueba.Fotograma(parpadeo: 0.9);
                        break;
                    case EtapaPruebaDeVida.GirarIzquierda:
                        resultado = prueba.Fotograma(giro: 25);
                        break;
                    case EtapaPruebaDeVida.GirarDerecha:
                        resultado = prueba.Fotograma(giro: -25);
                        break;
                    default:
                        resultado = prueba.Fotograma();
                        break;
                }
            }
            Verificar(resultado.Captura != null, "La secuencia válida debe completar la prueba.");
            Verificar(prueba.Fotograma().Captura == null, "La prueba terminada solo debe emitir una captura.");
            return resultado.Captura;
        }

        private static async Task Probar(string nombre, Func<Task> prueba)
        {
            await prueba();
            ejecutadas++;
            Console.WriteLine("OK " + nombre);
        }

        private static void Verificar(bool condicion, string mensaje)
        {
            if (!condicion) throw new Exception(mensaje);
        }

        private static void Esperar<T>(Action accion) where T : Exception
        {
            try { accion(); }
            catch (T) { return; }
            throw new Exception("Se esperaba " + typeof(T).Name + ".");
        }

        private static async Task EsperarAsync<T>(Func<Task> accion) where T : Exception
        {
            try { await accion(); }
            catch (T) { return; }
            throw new Exception("Se esperaba " + typeof(T).Name + ".");
        }

        private sealed class PruebaDeVidaSimulada
        {
            private readonly SesionPruebaDeVida prueba = new SesionPruebaDeVida();
            private readonly float[] rostro;
            private int milisegundos;

            public PruebaDeVidaSimulada(float[] rostro)
            {
                this.rostro = rostro;
            }

            public ResultadoPruebaDeVida Fotograma(double parpadeo = 0.1, double giro = 0, double probabilidad = 0.95,
                int rostros = 1, int intervaloMs = 120, float[] vector = null)
            {
                milisegundos += intervaloMs;
                return prueba.Evaluar(rostros, parpadeo, parpadeo, giro, probabilidad, vector ?? rostro,
                    TimeSpan.FromMilliseconds(milisegundos));
            }
        }

        private sealed class RepositorioMarcacionFalso : IRepositorioMarcacionFacial
        {
            public int Guardados;

            public Task<RegistroAsistencia> GuardarMarcacionAsync(SesionUsuario identidad, int idDispositivo, string tipoMarcacion)
            {
                Guardados++;
                return Task.FromResult(new RegistroAsistencia
                {
                    IdEmpleado = identidad.IdEmpleado,
                    IdDispositivo = idDispositivo,
                    TipoMarcacion = tipoMarcacion
                });
            }
        }

        private sealed class RepositorioFalso : IRepositorioFacial
        {
            public List<CandidatoFacial> Candidatos;
            public Func<int, Usuario> Revalidar;
            public int Revalidaciones;
            public int Lecturas;
            public int Guardados;
            public int EmpleadoGuardado;
            public byte[] DatosGuardados;

            public Task<List<CandidatoFacial>> ListarCandidatosActivosAsync()
            {
                Lecturas++;
                return Task.FromResult(Candidatos);
            }

            public Task<Usuario> BuscarUsuarioActivoAsync(int idUsuario)
            {
                Revalidaciones++;
                return Task.FromResult(Revalidar != null ? Revalidar(idUsuario) :
                    Candidatos.Find(c => c.Usuario.IdUsuario == idUsuario)?.Usuario);
            }

            public Task GuardarAsync(SesionUsuario administrador, int idEmpleado, byte[] datos)
            {
                Guardados++; EmpleadoGuardado = idEmpleado; DatosGuardados = datos;
                return Task.CompletedTask;
            }
        }
    }
}
