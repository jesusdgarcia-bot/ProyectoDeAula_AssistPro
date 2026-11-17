-- Datos ficticios para AssistPRO. Revisa el bloque antes de ejecutarlo.
-- Completa :hash_admin y :hash_empleado con los resultados de ClavesPbkdf2.CrearHash.
-- Cuentas: prueba.admin (Administrador) y prueba.empleado (Empleado).
-- Database Actions en modo stateless confirma automaticamente un bloque exitoso.
-- En un cliente con confirmacion manual, ejecuta COMMIT en la misma sesion para guardarlo.

DECLARE
    v_hash_admin VARCHAR2(32767) := :hash_admin;
    v_hash_empleado VARCHAR2(32767) := :hash_empleado;
    v_sede NUMBER;
    v_departamento NUMBER;
    v_cargo NUMBER;
    v_dispositivo NUMBER;
    v_admin NUMBER;
    v_empleado NUMBER;
    v_fecha_admin DATE;
    v_fecha_empleado DATE;
    v_hoy CONSTANT DATE := TRUNC(SYSDATE);

    FUNCTION obtener_rol(p_nombre VARCHAR2) RETURN NUMBER IS
        v_rol NUMBER;
    BEGIN
        SELECT MIN(ID_ROL) INTO v_rol
        FROM ASISTENCIA.ROL
        WHERE UPPER(TRIM(NOMBRE_ROL)) = UPPER(p_nombre);

        IF v_rol IS NULL THEN
            INSERT INTO ASISTENCIA.ROL (NOMBRE_ROL)
            VALUES (p_nombre)
            RETURNING ID_ROL INTO v_rol;
        END IF;
        RETURN v_rol;
    END;

    PROCEDURE preparar_empleado(
        p_documento VARCHAR2, p_nombre VARCHAR2, p_apellido VARCHAR2, p_usuario VARCHAR2,
        p_id OUT NUMBER, p_fecha_base OUT DATE
    ) IS
        v_coincide NUMBER;
        v_ingreso DATE;
        v_correo VARCHAR2(100) := p_usuario || '@example.test';
    BEGIN
        BEGIN
            SELECT e.ID_EMPLEADO, e.FECHA_INGRESO,
                   CASE WHEN e.TIPO_DOCUMENTO = 'CC' AND e.NOMBRES = p_nombre
                        AND e.APELLIDOS = p_apellido AND e.CORREO = v_correo
                        AND e.ID_DEPARTAMENTO = v_departamento AND e.ID_CARGO = v_cargo
                        AND e.ESTADO = 'A' THEN 1 ELSE 0 END
            INTO p_id, v_ingreso, v_coincide
            FROM ASISTENCIA.EMPLEADO e
            WHERE e.NUMERO_DOCUMENTO = p_documento
            FOR UPDATE NOWAIT;

            IF v_coincide = 0 THEN
                RAISE_APPLICATION_ERROR(-20002, 'El documento de prueba ya pertenece a otro empleado.');
            END IF;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN
                v_ingreso := v_hoy - 7;
                INSERT INTO ASISTENCIA.EMPLEADO (
                    TIPO_DOCUMENTO, NUMERO_DOCUMENTO, NOMBRES, APELLIDOS, CORREO,
                    FECHA_INGRESO, ESTADO, ID_DEPARTAMENTO, ID_CARGO
                ) VALUES (
                    'CC', p_documento, p_nombre, p_apellido, v_correo,
                    v_ingreso, 'A', v_departamento, v_cargo
                ) RETURNING ID_EMPLEADO INTO p_id;
        END;

        -- Ancla la fecha de las marcaciones al alta inicial del empleado de prueba.
        p_fecha_base := TRUNC(v_ingreso) + 7;
    END;

    PROCEDURE preparar_cuenta(p_id NUMBER, p_rol NUMBER, p_usuario VARCHAR2, p_hash VARCHAR2) IS
        v_colisiones NUMBER;
        v_coincide NUMBER;
        v_correo VARCHAR2(100) := p_usuario || '@example.test';
    BEGIN
        SELECT COUNT(*),
               COUNT(CASE WHEN NOMBRE_USUARIO = p_usuario AND ID_EMPLEADO = p_id
                          AND ID_ROL = p_rol AND CORREO_ACCESO = v_correo
                          AND ESTADO = 'A' THEN 1 END)
        INTO v_colisiones, v_coincide
        FROM ASISTENCIA.USUARIO
        WHERE NOMBRE_USUARIO = p_usuario OR ID_EMPLEADO = p_id OR CORREO_ACCESO = v_correo;

        IF v_colisiones = 0 THEN
            INSERT INTO ASISTENCIA.USUARIO (
                ID_EMPLEADO, ID_ROL, NOMBRE_USUARIO, CONTRASENA_HASH, CORREO_ACCESO, ESTADO
            ) VALUES (p_id, p_rol, p_usuario, p_hash, v_correo, 'A');
        ELSIF v_colisiones <> 1 OR v_coincide <> 1 THEN
            RAISE_APPLICATION_ERROR(-20003, 'Conflicto con una cuenta existente para ' || p_usuario || '.');
        END IF;
    END;

    PROCEDURE agregar_marcacion(p_empleado NUMBER, p_fecha DATE, p_hora NUMBER, p_tipo VARCHAR2) IS
        v_momento DATE := p_fecha + p_hora / 24;
    BEGIN
        INSERT INTO ASISTENCIA.REGISTRO_ASISTENCIA (
            ID_EMPLEADO, ID_DISPOSITIVO, FECHA, HORA, TIPO_MARCACION, METODO_VERIFICACION
        )
        SELECT p_empleado, v_dispositivo, p_fecha, v_momento, p_tipo, 'Manual'
        FROM DUAL
        WHERE NOT EXISTS (
            SELECT 1 FROM ASISTENCIA.REGISTRO_ASISTENCIA
            WHERE ID_EMPLEADO = p_empleado AND ID_DISPOSITIVO = v_dispositivo
              AND FECHA = p_fecha AND HORA = v_momento AND TIPO_MARCACION = p_tipo
              AND METODO_VERIFICACION = 'Manual'
        );
    END;

    PROCEDURE validar_hash(p_hash VARCHAR2, p_parametro VARCHAR2) IS
    BEGIN
        IF p_hash IS NULL OR NOT REGEXP_LIKE(p_hash,
            '^PBKDF2-SHA256\$600000\$[A-Za-z0-9+/]{22}==\$[A-Za-z0-9+/]{43}=$', 'c') THEN
            RAISE_APPLICATION_ERROR(-20001, p_parametro || ' debe contener el hash de ClavesPbkdf2.CrearHash.');
        END IF;
    END;
BEGIN
    SAVEPOINT cargar_pruebas_assistpro;
    validar_hash(v_hash_admin, 'hash_admin');
    validar_hash(v_hash_empleado, 'hash_empleado');

    SELECT MIN(ID_SEDE) INTO v_sede FROM ASISTENCIA.SEDE
    WHERE NOMBRE_SEDE = '[PRUEBA] Sede AssistPRO';
    IF v_sede IS NULL THEN
        INSERT INTO ASISTENCIA.SEDE (NOMBRE_SEDE, CIUDAD, DIRECCION)
        VALUES ('[PRUEBA] Sede AssistPRO', 'Bogota', 'Direccion ficticia')
        RETURNING ID_SEDE INTO v_sede;
    END IF;

    SELECT MIN(ID_DEPARTAMENTO) INTO v_departamento FROM ASISTENCIA.DEPARTAMENTO
    WHERE ID_SEDE = v_sede AND NOMBRE_DEPARTAMENTO = '[PRUEBA] Operaciones';
    IF v_departamento IS NULL THEN
        INSERT INTO ASISTENCIA.DEPARTAMENTO (NOMBRE_DEPARTAMENTO, ID_SEDE)
        VALUES ('[PRUEBA] Operaciones', v_sede)
        RETURNING ID_DEPARTAMENTO INTO v_departamento;
    END IF;

    SELECT MIN(ID_CARGO) INTO v_cargo FROM ASISTENCIA.CARGO
    WHERE NOMBRE_CARGO = '[PRUEBA] Colaborador';
    IF v_cargo IS NULL THEN
        INSERT INTO ASISTENCIA.CARGO (NOMBRE_CARGO)
        VALUES ('[PRUEBA] Colaborador')
        RETURNING ID_CARGO INTO v_cargo;
    END IF;

    SELECT MIN(ID_DISPOSITIVO) INTO v_dispositivo FROM ASISTENCIA.DISPOSITIVO_BIOMETRICO
    WHERE ID_SEDE = v_sede AND NOMBRE_DISPOSITIVO = '[PRUEBA] Registro manual';
    IF v_dispositivo IS NULL THEN
        INSERT INTO ASISTENCIA.DISPOSITIVO_BIOMETRICO (
            ID_SEDE, NOMBRE_DISPOSITIVO, TIPO_DISPOSITIVO, ESTADO
        ) VALUES (v_sede, '[PRUEBA] Registro manual', 'Manual', 'A')
        RETURNING ID_DISPOSITIVO INTO v_dispositivo;
    END IF;

    preparar_empleado('AP-TEST-ADMIN-001', 'Paula', 'Prueba', 'prueba.admin', v_admin, v_fecha_admin);
    preparar_empleado('AP-TEST-EMP-001', 'Diego', 'Prueba', 'prueba.empleado', v_empleado, v_fecha_empleado);
    preparar_cuenta(v_admin, obtener_rol('Administrador'), 'prueba.admin', v_hash_admin);
    preparar_cuenta(v_empleado, obtener_rol('Empleado'), 'prueba.empleado', v_hash_empleado);

    agregar_marcacion(v_admin, v_fecha_admin - 1, 8, 'Entrada');
    agregar_marcacion(v_admin, v_fecha_admin - 1, 17, 'Salida');
    agregar_marcacion(v_empleado, v_fecha_empleado - 2, 8, 'Entrada');
    agregar_marcacion(v_empleado, v_fecha_empleado - 2, 17, 'Salida');
    agregar_marcacion(v_empleado, v_fecha_empleado - 1, 8, 'Entrada');
    agregar_marcacion(v_empleado, v_fecha_empleado - 1, 17, 'Salida');
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK TO cargar_pruebas_assistpro;
        RAISE;
END;
