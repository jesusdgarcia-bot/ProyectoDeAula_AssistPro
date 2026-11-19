# ProyectoDeAula_AssistPro

Sistema de asistencia con biometría. .NET Framework 4.8 (WinForms) en capas
`Entidades` / `Persistencia` / `Logica` / `WindowsForms_Presentacion`, sobre **Oracle Database**.

## Iniciar la aplicación

Abre la solución en Visual Studio y ejecuta `WindowsForms_Presentacion` como proyecto de inicio. La aplicación usa la conexión Oracle configurada en `WindowsForms_Presentacion/App.config`.

Inicia sesión con una cuenta activa registrada en `ASISTENCIA.USUARIO` y elige el perfil correspondiente. El empleado consulta sus registros en **Mi asistencia**. Una cuenta con rol `Administrador` o `Admin` abre el panel de administración.

## Registrar datos desde el panel

En el panel de administración, selecciona el módulo y pulsa **Agregar** o **Registrar marcación**. Los nuevos registros se guardan en Oracle. Si un formulario requiere una referencia, crea primero el registro en su módulo. Por ejemplo, crea una sede, un departamento y un cargo antes de agregar un empleado. Para crear una cuenta de acceso, selecciona el empleado y su rol.

En **Asistencia** puedes consultar por rango de fechas y filtrar por empleado. Las marcaciones ingresadas desde el panel quedan identificadas como `Manual`. **Plantillas** permite consultar las plantillas biométricas y registrar un rostro.

## Acceso con reconocimiento facial

1. Compila `WindowsForms_Presentacion` con Visual Studio en Windows de 64 bits. NuGet restaura OpenCvSharp y su runtime; la primera compilación descarga los modelos oficiales YuNet y SFace (~39 MB), verifica su SHA256 y los copia a `bin/.../Modelos`. Las siguientes compilaciones pueden reutilizarlos sin descargarlos.
2. Inicia sesión con usuario y contraseña de un administrador. En **Plantillas > Registrar rostro**, selecciona un empleado activo que tenga una cuenta activa. Mira de frente a la cámara y pulsa **Registrar rostro** cuando esté listo. Se capturan tres muestras coherentes y se guarda una plantilla en Oracle. Si ese empleado ya tiene una plantilla facial, se reemplaza.
3. Cierra la sesión, elige el perfil de la cuenta (**Empleado** o **Administrador**), pulsa **Rostro** y después **Verificar rostro**. El usuario y la contraseña pueden quedar vacíos para este acceso. Una coincidencia clara abre el mismo panel que el acceso con contraseña.

La captura exige una sola persona, el rostro completo y suficientemente cerca. Si la cámara falla, revisa los permisos para aplicaciones de escritorio en Windows y cierra otras aplicaciones que la estén usando. Puedes detenerla, cambiar su número (0 suele ser la cámara principal) y encenderla de nuevo. La clave `CamaraFacial` en `appSettings` permite elegir otro número inicial. Si OpenCV no carga, instala [Microsoft Visual C++ Redistributable x64](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist).

Para preparar o reparar los modelos manualmente, desde la raíz del repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\Preparar-ModelosFaciales.ps1
```

El procesamiento ocurre localmente. No se guardan fotos ni video: `PLANTILLA_BIOMETRICA.PLANTILLA` contiene un vector SFace normalizado y versionado con `TIPO_BIOMETRIA = Facial`. Se usa el esquema existente, sin migraciones. Como admite una sola plantilla por empleado, una biometría de otro tipo se conserva y bloquea el registro facial.

La identificación rechaza similitudes de coseno inferiores a `0.45` y diferencias menores de `0.08` entre los dos mejores candidatos. Cuenta, empleado y rol se consultan de nuevo antes de abrir la sesión; los permisos del administrador también se validan en Oracle antes de guardar. Los umbrales necesitan validación con las cámaras e iluminación de uso. Este flujo no incluye detección de vida ni protección frente a fotografías o videos; si se requiere esa protección, el acceso facial necesita un mecanismo adicional de verificación. El algoritmo sigue el [flujo YuNet/SFace de OpenCV](https://docs.opencv.org/4.x/d0/dd4/tutorial_dnn_face.html).

## Pruebas de reconocimiento facial

`Pruebas` es un ejecutable de .NET Framework 4.8 sin cámara ni Oracle. Compílalo en Visual Studio y ejecuta `Pruebas/bin/Debug/Pruebas.exe`. Comprueba coincidencias, rechazos, datos inválidos, cuentas inactivas y revalidación de permisos. Devuelve código 1 ante un fallo.

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

Editar `WindowsForms_Presentacion/App.config` (`Password` y, si aplica, `Data Source`):

```
User Id=ASISTENCIA;Password=...;Data Source=localhost:1521/XEPDB1;
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
