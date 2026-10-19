# ProyectoDeAula_AssistPro

Sistema de asistencia con biometría. .NET Framework 4.8 (WinForms) en capas
`Entidades` / `Persistencia` / `Logica` / `WindowsForms_Presentacion`, sobre **Oracle XE 18c**.

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
