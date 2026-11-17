-- Alta de la cuenta temporal admin.presentacion en AssistPRO.
-- El parametro :hash_admin recibe la salida de ClavesPbkdf2.CrearHash.
-- Oracle conserva la contrasena de una cuenta de presentacion existente.

DECLARE
    v_hash VARCHAR2(32767) := :hash_admin;
    v_usuario CONSTANT VARCHAR2(30) := 'admin.presentacion';
    v_correo CONSTANT VARCHAR2(100) := 'admin.presentacion@example.test';
    v_documento CONSTANT VARCHAR2(20) := 'AP-PRES-ADMIN-001';
    v_id_sede NUMBER;
    v_id_departamento NUMBER;
    v_id_cargo NUMBER;
    v_id_empleado NUMBER;
    v_id_rol NUMBER;
    v_encontrados NUMBER;
    v_coinciden NUMBER;
BEGIN
    SAVEPOINT alta_admin_assistpro;

    IF v_hash IS NULL OR NOT REGEXP_LIKE(
        v_hash, '^PBKDF2-SHA256\$600000\$[A-Za-z0-9+/]{22}==\$[A-Za-z0-9+/]{43}=$', 'c'
    ) THEN
        RAISE_APPLICATION_ERROR(-20001, 'hash_admin debe contener el hash de ClavesPbkdf2.CrearHash.');
    END IF;

    SELECT COUNT(*),
           COUNT(CASE WHEN e.NUMERO_DOCUMENTO = v_documento AND e.CORREO = v_correo
                      AND e.NOMBRES = 'Alex' AND e.APELLIDOS = 'Presentacion'
                      AND e.ESTADO = 'A' AND u.ESTADO = 'A'
                      AND u.NOMBRE_USUARIO = v_usuario AND u.CORREO_ACCESO = v_correo
                      AND UPPER(TRIM(r.NOMBRE_ROL)) IN ('ADMINISTRADOR', 'ADMIN') THEN 1 END)
    INTO v_encontrados, v_coinciden
    FROM ASISTENCIA.USUARIO u
    JOIN ASISTENCIA.EMPLEADO e ON e.ID_EMPLEADO = u.ID_EMPLEADO
    JOIN ASISTENCIA.ROL r ON r.ID_ROL = u.ID_ROL
    WHERE u.NOMBRE_USUARIO = v_usuario OR u.CORREO_ACCESO = v_correo;

    IF v_encontrados = 1 AND v_coinciden = 1 THEN
        RETURN;
    ELSIF v_encontrados > 0 THEN
        RAISE_APPLICATION_ERROR(-20002, 'El usuario o correo de presentacion ya pertenece a otra cuenta.');
    END IF;

    SELECT MIN(ID_SEDE) INTO v_id_sede FROM ASISTENCIA.SEDE
    WHERE NOMBRE_SEDE = '[PRUEBA] Sede AssistPRO';
    IF v_id_sede IS NULL THEN
        INSERT INTO ASISTENCIA.SEDE (NOMBRE_SEDE, CIUDAD, DIRECCION)
        VALUES ('[PRUEBA] Sede AssistPRO', 'Bogota', 'Direccion ficticia')
        RETURNING ID_SEDE INTO v_id_sede;
    END IF;

    SELECT MIN(ID_DEPARTAMENTO) INTO v_id_departamento FROM ASISTENCIA.DEPARTAMENTO
    WHERE ID_SEDE = v_id_sede AND NOMBRE_DEPARTAMENTO = '[PRUEBA] Operaciones';
    IF v_id_departamento IS NULL THEN
        INSERT INTO ASISTENCIA.DEPARTAMENTO (NOMBRE_DEPARTAMENTO, ID_SEDE)
        VALUES ('[PRUEBA] Operaciones', v_id_sede)
        RETURNING ID_DEPARTAMENTO INTO v_id_departamento;
    END IF;

    SELECT MIN(ID_CARGO) INTO v_id_cargo FROM ASISTENCIA.CARGO
    WHERE NOMBRE_CARGO = '[PRUEBA] Colaborador';
    IF v_id_cargo IS NULL THEN
        INSERT INTO ASISTENCIA.CARGO (NOMBRE_CARGO)
        VALUES ('[PRUEBA] Colaborador')
        RETURNING ID_CARGO INTO v_id_cargo;
    END IF;

    SELECT MIN(ID_ROL) INTO v_id_rol FROM ASISTENCIA.ROL
    WHERE UPPER(TRIM(NOMBRE_ROL)) IN ('ADMINISTRADOR', 'ADMIN');
    IF v_id_rol IS NULL THEN
        INSERT INTO ASISTENCIA.ROL (NOMBRE_ROL) VALUES ('Administrador')
        RETURNING ID_ROL INTO v_id_rol;
    END IF;

    BEGIN
        SELECT ID_EMPLEADO,
               CASE WHEN NUMERO_DOCUMENTO = v_documento AND TIPO_DOCUMENTO = 'CC'
                         AND CORREO = v_correo AND NOMBRES = 'Alex' AND APELLIDOS = 'Presentacion'
                         AND ID_DEPARTAMENTO = v_id_departamento AND ID_CARGO = v_id_cargo
                         AND ESTADO = 'A' THEN 1 ELSE 0 END
        INTO v_id_empleado, v_coinciden
        FROM ASISTENCIA.EMPLEADO
        WHERE NUMERO_DOCUMENTO = v_documento OR CORREO = v_correo
        FOR UPDATE NOWAIT;

        IF v_coinciden = 0 THEN
            RAISE_APPLICATION_ERROR(-20003, 'El documento o correo de presentacion ya pertenece a otro empleado.');
        END IF;
    EXCEPTION
        WHEN TOO_MANY_ROWS THEN
            RAISE_APPLICATION_ERROR(-20003, 'El documento o correo de presentacion ya pertenece a otro empleado.');
        WHEN NO_DATA_FOUND THEN
            INSERT INTO ASISTENCIA.EMPLEADO (
                TIPO_DOCUMENTO, NUMERO_DOCUMENTO, NOMBRES, APELLIDOS, CORREO,
                FECHA_INGRESO, ESTADO, ID_DEPARTAMENTO, ID_CARGO
            ) VALUES (
                'CC', v_documento, 'Alex', 'Presentacion', v_correo,
                TRUNC(SYSDATE) - 7, 'A', v_id_departamento, v_id_cargo
            ) RETURNING ID_EMPLEADO INTO v_id_empleado;
    END;

    SELECT COUNT(*) INTO v_encontrados
    FROM ASISTENCIA.USUARIO
    WHERE ID_EMPLEADO = v_id_empleado;
    IF v_encontrados > 0 THEN
        RAISE_APPLICATION_ERROR(-20004, 'El empleado de presentacion ya tiene otra cuenta.');
    END IF;

    INSERT INTO ASISTENCIA.USUARIO (
        ID_EMPLEADO, ID_ROL, NOMBRE_USUARIO, CONTRASENA_HASH, CORREO_ACCESO, ESTADO
    ) VALUES (v_id_empleado, v_id_rol, v_usuario, v_hash, v_correo, 'A');
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK TO alta_admin_assistpro;
        RAISE;
END;
