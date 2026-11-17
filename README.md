# ProyectoDeAula_AssistPro

Sistema de asistencia con biometría. .NET Framework 4.8 (WinForms) en capas
`Entidades` / `Persistencia` / `Logica` / `WindowsForms_Presentacion`, sobre **Oracle Database**.

## Iniciar la aplicación

Abre la solución en Visual Studio y ejecuta `WindowsForms_Presentacion` como proyecto de inicio. La aplicación usa la conexión Oracle configurada en `WindowsForms_Presentacion/App.config`.

Inicia sesión con una cuenta activa registrada en `ASISTENCIA.USUARIO` y elige el perfil correspondiente. El empleado consulta sus registros en **Mi asistencia**. Una cuenta con rol `Administrador` o `Admin` abre el panel de administración.

## Registrar datos desde el panel

En el panel de administración, selecciona el módulo y pulsa **Agregar** o **Registrar marcación**. Los nuevos registros se guardan en Oracle. Si un formulario requiere una referencia, crea primero el registro en su módulo. Por ejemplo, crea una sede, un departamento y un cargo antes de agregar un empleado. Para crear una cuenta de acceso, selecciona el empleado y su rol.

En **Asistencia** puedes consultar por rango de fechas y filtrar por empleado. Las marcaciones ingresadas desde el panel quedan identificadas como `Manual`. **Plantillas** muestra la información de las plantillas biométricas ya registradas.

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
