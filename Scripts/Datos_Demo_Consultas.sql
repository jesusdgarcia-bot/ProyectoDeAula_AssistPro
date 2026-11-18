-- AssistPro. Datos sintéticos para las cuatro consultas de sustentación.
-- Ejecutar con F5 en SQL Developer, conectado al esquema ASISTENCIA.
-- Cada ejecución agrega un lote nuevo. Consultar en la misma sesión.

DECLARE
    v_hoy             CONSTANT DATE := TRUNC(SYSDATE);
    v_lote            CONSTANT VARCHAR2(13) := SUBSTR(RAWTOHEX(SYS_GUID()), 1, 13);
    v_id_sede         ASISTENCIA.SEDE.ID_SEDE%TYPE;
    v_id_departamento ASISTENCIA.DEPARTAMENTO.ID_DEPARTAMENTO%TYPE;
    v_id_cargo        ASISTENCIA.CARGO.ID_CARGO%TYPE;
    v_id_dispositivo  ASISTENCIA.DISPOSITIVO_BIOMETRICO.ID_DISPOSITIVO%TYPE;
    v_id_tipo_novedad ASISTENCIA.TIPO_NOVEDAD.ID_TIPO_NOVEDAD%TYPE;
    v_id_ana          ASISTENCIA.EMPLEADO.ID_EMPLEADO%TYPE;
    v_id_luis         ASISTENCIA.EMPLEADO.ID_EMPLEADO%TYPE;

    PROCEDURE agregar_marcacion (
        p_id_empleado IN NUMBER,
        p_fecha       IN DATE,
        p_hora        IN NUMBER,
        p_tipo        IN VARCHAR2
    ) IS
    BEGIN
        INSERT INTO ASISTENCIA.REGISTRO_ASISTENCIA (
            ID_EMPLEADO, ID_DISPOSITIVO, FECHA, HORA, TIPO_MARCACION, METODO_VERIFICACION
        ) VALUES (
            p_id_empleado, v_id_dispositivo, p_fecha, p_fecha + p_hora / 24, p_tipo, 'Manual'
        );
    END agregar_marcacion;
BEGIN
    SAVEPOINT carga_demo_assistpro;

    INSERT INTO ASISTENCIA.SEDE (NOMBRE_SEDE, CIUDAD, DIRECCION)
    VALUES ('Sede Demo AssistPro', 'Valledupar', 'Direccion de prueba')
    RETURNING ID_SEDE INTO v_id_sede;

    INSERT INTO ASISTENCIA.DEPARTAMENTO (NOMBRE_DEPARTAMENTO, ID_SEDE)
    VALUES ('Operaciones Demo', v_id_sede)
    RETURNING ID_DEPARTAMENTO INTO v_id_departamento;

    INSERT INTO ASISTENCIA.CARGO (NOMBRE_CARGO)
    VALUES ('Auxiliar Demo')
    RETURNING ID_CARGO INTO v_id_cargo;

    INSERT INTO ASISTENCIA.EMPLEADO (
        TIPO_DOCUMENTO, NUMERO_DOCUMENTO, NOMBRES, APELLIDOS, FECHA_INGRESO,
        ESTADO, ID_DEPARTAMENTO, ID_CARGO
    ) VALUES (
        'CC', 'DEMO-' || v_lote || '-1', 'Ana', 'Demo', v_hoy - 30,
        'A', v_id_departamento, v_id_cargo
    )
    RETURNING ID_EMPLEADO INTO v_id_ana;

    INSERT INTO ASISTENCIA.EMPLEADO (
        TIPO_DOCUMENTO, NUMERO_DOCUMENTO, NOMBRES, APELLIDOS, FECHA_INGRESO,
        ESTADO, ID_DEPARTAMENTO, ID_CARGO
    ) VALUES (
        'CC', 'DEMO-' || v_lote || '-2', 'Luis', 'Prueba', v_hoy - 30,
        'A', v_id_departamento, v_id_cargo
    )
    RETURNING ID_EMPLEADO INTO v_id_luis;

    INSERT INTO ASISTENCIA.DISPOSITIVO_BIOMETRICO (
        ID_SEDE, NOMBRE_DISPOSITIVO, TIPO_DISPOSITIVO, ESTADO
    ) VALUES (
        v_id_sede, 'Terminal Demo AssistPro', 'Huella', 'A'
    )
    RETURNING ID_DISPOSITIVO INTO v_id_dispositivo;

    agregar_marcacion(v_id_ana,  v_hoy, 8,  'ENTRADA');
    agregar_marcacion(v_id_luis, v_hoy, 9,  'ENTRADA');
    agregar_marcacion(v_id_ana,  v_hoy, 16, 'SALIDA');
    agregar_marcacion(v_id_luis, v_hoy, 17, 'SALIDA');
    agregar_marcacion(v_id_ana,  v_hoy - 1, 8,  'ENTRADA');
    agregar_marcacion(v_id_ana,  v_hoy - 1, 16, 'SALIDA');

    INSERT INTO ASISTENCIA.TIPO_NOVEDAD (NOMBRE_TIPO)
    VALUES ('Permiso Demo')
    RETURNING ID_TIPO_NOVEDAD INTO v_id_tipo_novedad;

    INSERT INTO ASISTENCIA.NOVEDAD (
        ID_EMPLEADO, ID_TIPO_NOVEDAD, FECHA_INICIO, FECHA_FIN, ESTADO_APROBACION, MOTIVO
    ) VALUES (
        v_id_ana, v_id_tipo_novedad, v_hoy + 1, v_hoy + 1, 'Pendiente', 'Permiso de prueba por un dia'
    );

    INSERT INTO ASISTENCIA.NOVEDAD (
        ID_EMPLEADO, ID_TIPO_NOVEDAD, FECHA_INICIO, FECHA_FIN, ESTADO_APROBACION, MOTIVO
    ) VALUES (
        v_id_luis, v_id_tipo_novedad, v_hoy + 2, v_hoy + 3, 'Pendiente', 'Permiso de prueba por dos dias'
    );
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK TO carga_demo_assistpro;
        RAISE;
END;
/

-- Ejecutar COMMIT para conservar el lote o ROLLBACK para descartarlo.
