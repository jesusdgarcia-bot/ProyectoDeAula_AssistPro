# AssistPRO Hospital

Control de asistencia laboral del personal hospitalario. .NET Framework 4.8 (WinForms) en capas
`Entidades` / `Persistencia` / `Logica` / `WindowsForms_Presentacion`, sobre **Oracle Database**.

## Iniciar la aplicación

Configura la conexión Oracle como se indica en **Base de datos**. Abre la solución en Visual Studio y ejecuta `WindowsForms_Presentacion` como proyecto de inicio en Windows x64.

Inicia sesión con una cuenta activa y elige el perfil correspondiente. En **Mi asistencia** el personal puede marcar, consultar sus registros y ver su turno en curso o el siguiente programado. Las cuentas con rol `Administrador` o `Admin` abren el panel de administración.

## Configuración y operación hospitalaria

1. Registra las **Sedes**, las **Unidades y servicios** y los **Cargos** antes de agregar el personal y crear sus cuentas. En **Dispositivos**, asocia cada terminal de asistencia con la sede donde opera.
2. Guarda el ID del terminal en `WindowsForms_Presentacion/AssistPRO.local.config` y recompila. En un equipo instalado, colócalo junto al ejecutable y reinicia AssistPRO. Las instalaciones anteriores pueden seguir usando `IdDispositivoFacial` como identificador del terminal.

   ```xml
   <appSettings>
       <add key="IdTerminalAsistencia" value="ID_DEL_TERMINAL" />
   </appSettings>
   ```

3. Define los **Turnos** y sus días de inicio, por ejemplo `Lun-Vie`, `Lun,Mié,Sáb` o `Todos`. Una salida anterior a la hora de entrada corresponde al día siguiente. Una guardia de 19:00 a 07:00 que empieza el último día de la asignación sigue vigente hasta las 07:00 del día posterior. Registra la vigencia en **Asignación de turnos**.
4. En **Mi asistencia**, pulsa **Marcar entrada** o **Marcar salida** y confirma con la contraseña de la cuenta abierta. Puedes conservar puesto tu equipo de protección. Oracle usa la hora de Colombia y guarda cada marcación en su fecha real. Si una guardia cruza medianoche, la entrada y la salida pueden aparecer en fechas distintas. Se pueden registrar guardias extraordinarias aunque no haya un turno programado.
5. En **Asistencia**, filtra por personal, sede del terminal y unidad o servicio actual del personal. El resumen por día agrupa por fecha calendario. Si alguien no pudo marcar, indica al administrador la fecha, la hora y el terminal para que use **Marcación manual** o corrija el registro cuando haya conexión. Las novedades son laborales y se describen sin incluir información de pacientes.

## Piloto académico de verificación facial

La instalación hospitalaria se entrega con la cámara deshabilitada. El piloto usa un modelo con condiciones de investigación y educación. Antes de habilitarlo, revisa la autorización y el manejo de las plantillas con el responsable de protección de datos. La marcación con contraseña está disponible también en el piloto. [Orientación de la SIC sobre datos biométricos](https://sedeelectronica.sic.gov.co/sites/default/files/normatividad/Concepto_14-118247.pdf)

En Windows x64, instala [Python 3.12 x64](https://www.python.org/downloads/windows/) y [Microsoft Visual C++ Redistributable 2015–2022 x64](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist). Desde la raíz prepara el entorno y, en la consola de desarrollador de Visual Studio, compila incluyendo los modelos.

```powershell
py -3.12 -m venv .venv-mediapipe
.\.venv-mediapipe\Scripts\python.exe -m pip install mediapipe==0.10.35
msbuild .\ProyectoDeAula_AssistPro.slnx /p:PrepararBiometriaFacial=true
```

Añade `HabilitarBiometriaFacial=true` como clave y valor en `AssistPRO.local.config`. Las compilaciones Debug y Release apuntan a `.venv-mediapipe`. Para usar otro intérprete, define `PythonMediaPipe` con su ruta absoluta en el mismo archivo. `CamaraFacial` controla el índice inicial de la cámara.

En **Biometría > Registrar rostro**, un administrador selecciona a una persona con cuenta activa. La persona completa la prueba de vida y se guarda su plantilla facial en Oracle. En **Mi asistencia**, quien elija **Verificar con el rostro** realiza una nueva prueba al marcar. El rostro reconocido debe corresponder a la cuenta abierta.

La prueba combina las señales de ojos y orientación de MediaPipe con el resultado del modelo antisuplantación ONNX. La secuencia y el lado del giro se eligen al azar. El video se procesa en memoria en el equipo. Oracle conserva el vector SFace normalizado y versionado en `PLANTILLA_BIOMETRICA`, junto con las marcaciones autorizadas. Se utiliza el esquema existente. Si ya existe una plantilla de otro tipo para el empleado, se conserva y el registro facial se rechaza.

El umbral inicial de antisuplantación es `0.80`. El reconocimiento usa una similitud de coseno mínima de `0.45` y exige un margen de `0.08` entre los dos mejores candidatos. La eficacia depende de la cámara, la iluminación y las condiciones de uso. El modelo antisuplantación se incorpora para evaluación académica. Consulta las fuentes y cómo preparar los archivos en [Modelos faciales](WindowsForms_Presentacion/Modelos/README.md).

## Pruebas automatizadas

`Pruebas` es un ejecutable de .NET Framework 4.8 que se puede ejecutar sin cámara ni Oracle. Compílalo en Visual Studio y ejecuta `Pruebas/bin/Debug/Pruebas.exe`. Cubre turnos nocturnos, marcación con contraseña, permisos y reconocimiento facial. Devuelve código 1 ante un fallo.

`Scripts/Datos_Demo_Consultas.sql` carga un lote sintético de Urgencias con una guardia nocturna y marcaciones en dos días. Úsalo en una base de pruebas.

## Base de datos (Entity Framework 6 Code-First + Migraciones)

- Las entidades (POCO) están en `Entidades/`.
- El `DbContext` (`AssistProContext`), el mapeo Fluent API a las tablas/columnas del diagrama
  (`Persistencia/Configuraciones/`) y las migraciones (`Persistencia/Migrations/`) están en `Persistencia/`.
- El esquema de destino es `ASISTENCIA`. Se conecta **como el usuario `ASISTENCIA`**.

### 1. Crear el usuario (una sola vez)

Conectado a `XEPDB1` como `SYSTEM` en SQL Developer:

```sql
CREATE USER ASISTENCIA IDENTIFIED BY "TuPassword"
  DEFAULT TABLESPACE USERS QUOTA UNLIMITED ON USERS;
GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, CREATE VIEW TO ASISTENCIA;
```

### 2. Cadena de conexión

En `WindowsForms_Presentacion` crea `AssistPRO.oracle.config`. Visual Studio lo copia al directorio de salida. En una instalación, el archivo va junto al ejecutable. Completa los datos de conexión del entorno y protege el archivo con los permisos de Windows del equipo. El repositorio ignora este archivo.

```xml
<connectionStrings>
    <add name="AssistProContext" providerName="Oracle.ManagedDataAccess.Client"
         connectionString="User Id=ASISTENCIA;Password=CLAVE_LOCAL;Data Source=localhost:1521/XEPDB1;" />
</connectionStrings>
```

### 3. Migraciones

La migración inicial ya está generada (`Persistencia/Migrations/*_InitialCreate.cs`). Para crear las tablas, en Visual Studio:
*Herramientas > Administrador de paquetes NuGet > Consola del Administrador de paquetes*.
Los comandos llevan `-ProjectName Persistencia` (donde está el contexto) y
`-StartUpProjectName WindowsForms_Presentacion` (donde está la cadena de conexión).

```powershell
# Crea las 13 tablas (y __MigrationHistory) en Oracle
Update-Database -ProjectName Persistencia -StartUpProjectName WindowsForms_Presentacion -Verbose

# Solo ver el SQL que se ejecutaría (para revisarlo en SQL Developer)
Update-Database -Script -ProjectName Persistencia -StartUpProjectName WindowsForms_Presentacion
```

Cada vez que cambie una entidad o su mapeo:

```powershell
Add-Migration <NombreDelCambio> -ProjectName Persistencia -StartUpProjectName WindowsForms_Presentacion
Update-Database -ProjectName Persistencia -StartUpProjectName WindowsForms_Presentacion
```

Para volver atrás: `Update-Database -TargetMigration:<NombreMigracionAnterior> ...` (o `-TargetMigration:0` para quitar todo).
**Hacer commit de los archivos de `Migrations/`.**

### Notas del modelo

- Los `ID_*` son `NUMBER(10) GENERATED ALWAYS AS IDENTITY` (Oracle 12c+): al insertar por SQL no se puede indicar el id.
- Sin borrado en cascada en ninguna FK.
- `USUARIO.ID_EMPLEADO` y `PLANTILLA_BIOMETRICA.ID_EMPLEADO` son 1 a 1 mediante índice único.
- Diferencias con el diagrama: `NUMBER(10)` en vez de `NUMBER(11)`; `VARCHAR2(n CHAR)` en vez de `BYTE`; y el proveedor de
  Oracle pone sus propios nombres a índices y FK (`IX_<TABLA>_<COLUMNA>`, `FK_<TABLA>_<COLUMNA>`), ignorando los `UQ_*`/`FK_*` del diagrama.
  Las PK sí quedan como `PK_<TABLA>`.
