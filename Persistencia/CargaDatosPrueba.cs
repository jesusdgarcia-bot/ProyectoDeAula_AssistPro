using System;
using System.Threading.Tasks;
using Logica;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Persistencia
{
    public sealed class CargaDatosPrueba
    {
        private readonly string estadoActivo;

        public CargaDatosPrueba(string estadoActivo)
        {
            this.estadoActivo = estadoActivo;
        }

        public async Task<int> CargarAsync(SesionUsuario sesion)
        {
            if (sesion == null || !sesion.EsAdministrador) throw new UnauthorizedAccessException();

            using (var contexto = new AssistProContext())
            {
                var conexion = (OracleConnection)contexto.Database.Connection;
                await conexion.OpenAsync().ConfigureAwait(false);
                using (var transaccion = conexion.BeginTransaction())
                {
                    int insertadas = await InsertarAsync(conexion, transaccion, sesion, estadoActivo).ConfigureAwait(false);
                    transaccion.Commit();
                    return insertadas;
                }
            }
        }

        internal static async Task<int> InsertarAsync(OracleConnection conexion, OracleTransaction transaccion,
            SesionUsuario sesion, string estadoActivo)
        {
            using (var comando = conexion.CreateCommand())
            {
                comando.Transaction = transaccion;
                comando.BindByName = true;
                comando.CommandText = BloqueCarga;
                comando.Parameters.Add("usuario", OracleDbType.Int32).Value = sesion.IdUsuario;
                comando.Parameters.Add("empleado", OracleDbType.Int32).Value = sesion.IdEmpleado;
                comando.Parameters.Add("activo", OracleDbType.Varchar2).Value = estadoActivo;
                comando.Parameters.Add("nombre_dispositivo", OracleDbType.Varchar2).Value =
                    "[PRUEBA] Panel " + sesion.IdEmpleado.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var resultado = comando.Parameters.Add("insertadas", OracleDbType.Decimal);
                resultado.Direction = System.Data.ParameterDirection.Output;
                await comando.ExecuteNonQueryAsync().ConfigureAwait(false);
                return ((OracleDecimal)resultado.Value).ToInt32();
            }
        }

        private const string BloqueCarga = @"
DECLARE
    v_empleado NUMBER;
    v_sede NUMBER;
    v_dispositivo NUMBER;
    v_existentes NUMBER;
    v_fecha DATE;
BEGIN
    SELECT e.ID_EMPLEADO, d.ID_SEDE INTO v_empleado, v_sede
    FROM ASISTENCIA.USUARIO u
    JOIN ASISTENCIA.ROL r ON r.ID_ROL = u.ID_ROL
    JOIN ASISTENCIA.EMPLEADO e ON e.ID_EMPLEADO = u.ID_EMPLEADO
    JOIN ASISTENCIA.DEPARTAMENTO d ON d.ID_DEPARTAMENTO = e.ID_DEPARTAMENTO
    WHERE u.ID_USUARIO = :usuario AND e.ID_EMPLEADO = :empleado
      AND TRIM(u.ESTADO) = :activo AND TRIM(e.ESTADO) = :activo
      AND UPPER(TRIM(r.NOMBRE_ROL)) IN ('ADMINISTRADOR', 'ADMIN')
    FOR UPDATE OF u.ID_USUARIO;

    SELECT MIN(ID_DISPOSITIVO) INTO v_dispositivo
    FROM ASISTENCIA.DISPOSITIVO_BIOMETRICO
    WHERE ID_SEDE = v_sede AND NOMBRE_DISPOSITIVO = :nombre_dispositivo AND TIPO_DISPOSITIVO = 'Manual';

    IF v_dispositivo IS NULL THEN
        INSERT INTO ASISTENCIA.DISPOSITIVO_BIOMETRICO
            (ID_SEDE, NOMBRE_DISPOSITIVO, TIPO_DISPOSITIVO, ESTADO)
        VALUES (v_sede, :nombre_dispositivo, 'Manual', :activo)
        RETURNING ID_DISPOSITIVO INTO v_dispositivo;
    END IF;

    SELECT COUNT(*) INTO v_existentes
    FROM ASISTENCIA.REGISTRO_ASISTENCIA
    WHERE ID_EMPLEADO = v_empleado AND ID_DISPOSITIVO = v_dispositivo;

    :insertadas := 0;
    IF v_existentes = 0 THEN
        FOR dia IN 1..3 LOOP
            v_fecha := TRUNC(SYSDATE) - dia;
            INSERT INTO ASISTENCIA.REGISTRO_ASISTENCIA
                (ID_EMPLEADO, ID_DISPOSITIVO, FECHA, HORA, TIPO_MARCACION, METODO_VERIFICACION)
            VALUES (v_empleado, v_dispositivo, v_fecha, v_fecha + 8 / 24, 'Entrada', 'Manual');
            INSERT INTO ASISTENCIA.REGISTRO_ASISTENCIA
                (ID_EMPLEADO, ID_DISPOSITIVO, FECHA, HORA, TIPO_MARCACION, METODO_VERIFICACION)
            VALUES (v_empleado, v_dispositivo, v_fecha, v_fecha + 17 / 24, 'Salida', 'Manual');
        END LOOP;
        :insertadas := 6;
    END IF;
END;";
    }
}
