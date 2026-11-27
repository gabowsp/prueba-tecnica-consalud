-- Agrega paginación y conserva el package actual. Ejecutar después del script 01.
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET DEFINE OFF

CREATE OR REPLACE PACKAGE        PKG_DATOS AS

    -- Código de error de aplicación cuando el ID solicitado no existe.
    C_ERR_NO_EXISTE CONSTANT PLS_INTEGER := -20001;

    /**
     * Inserta un dato. El ID se toma de SEQ_DATOS y se devuelve en p_id.
     * Si CODIGO + SECUENCIA ya existen, Oracle lanza ORA-00001.
     */
    PROCEDURE SP_INSERTAR (
        p_codigo          IN  DATOS.CODIGO%TYPE,
        p_secuencia       IN  DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN  DATOS.FECHARECEPCION%TYPE,
        p_peso            IN  DATOS.PESO%TYPE,
        p_descripcion     IN  DATOS.DESCRIPCION%TYPE DEFAULT NULL,
        p_id              OUT DATOS.ID%TYPE
    );

    /**
     * Devuelve el dato con el ID indicado.
     * Si no existe, el cursor no trae filas.
     */
    PROCEDURE SP_OBTENER (
        p_id     IN  DATOS.ID%TYPE,
        p_cursor OUT SYS_REFCURSOR
    );

    /**
     * Lista los datos, del más reciente al más antiguo, junto con la cantidad
     * de registros que tiene cada código (TOTAL_CODIGO).
     * p_codigo es un filtro opcional (NULL = todos).
     */
    PROCEDURE SP_LISTAR (
        p_codigo IN  DATOS.CODIGO%TYPE DEFAULT NULL,
        p_cursor OUT SYS_REFCURSOR
    );

    /**
     * Actualiza el dato con el ID indicado.
     * Lanza C_ERR_NO_EXISTE (-20001) si el ID no existe.
     */
    PROCEDURE SP_ACTUALIZAR (
        p_id              IN DATOS.ID%TYPE,
        p_codigo          IN DATOS.CODIGO%TYPE,
        p_secuencia       IN DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN DATOS.FECHARECEPCION%TYPE,
        p_peso            IN DATOS.PESO%TYPE,
        p_descripcion     IN DATOS.DESCRIPCION%TYPE
    );

    /**
     * Elimina el dato con el ID indicado.
     * Lanza C_ERR_NO_EXISTE (-20001) si el ID no existe.
     */
    PROCEDURE SP_ELIMINAR (
        p_id IN DATOS.ID%TYPE
    );

    /**
     * Cantidad de registros que tiene un código. Usada por SP_LISTAR.
     */
    FUNCTION FN_TOTAL_POR_CODIGO (
        p_codigo IN DATOS.CODIGO%TYPE
    ) RETURN NUMBER;

    -- Página de 20 registros; el total se devuelve aunque la página esté vacía.
    PROCEDURE SP_LISTAR_PAGINADO (
        p_pagina          IN NUMBER,
        p_cursor          OUT SYS_REFCURSOR,
        p_total_registros OUT NUMBER
    );

END PKG_DATOS;
/
CREATE OR REPLACE PACKAGE BODY        PKG_DATOS AS

    PROCEDURE SP_INSERTAR (
        p_codigo          IN  DATOS.CODIGO%TYPE,
        p_secuencia       IN  DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN  DATOS.FECHARECEPCION%TYPE,
        p_peso            IN  DATOS.PESO%TYPE,
        p_descripcion     IN  DATOS.DESCRIPCION%TYPE DEFAULT NULL,
        p_id              OUT DATOS.ID%TYPE
    ) IS
    BEGIN
        INSERT INTO DATOS (ID, CODIGO, SECUENCIA, FECHARECEPCION, PESO, DESCRIPCION)
        VALUES (SEQ_DATOS.NEXTVAL, p_codigo, p_secuencia, p_fecha_recepcion, p_peso, p_descripcion)
        RETURNING ID INTO p_id;
    END SP_INSERTAR;


    PROCEDURE SP_OBTENER (
        p_id     IN  DATOS.ID%TYPE,
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT ID, CODIGO, SECUENCIA, FECHARECEPCION, PESO, DESCRIPCION
              FROM DATOS
             WHERE ID = p_id;
    END SP_OBTENER;


    /*
    PROCEDURE SP_LISTAR (
        p_codigo IN  DATOS.CODIGO%TYPE DEFAULT NULL,
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT d.ID,
                   d.CODIGO,
                   d.SECUENCIA,
                   d.FECHARECEPCION,
                   d.PESO,
                   d.DESCRIPCION,
                   FN_TOTAL_POR_CODIGO(d.CODIGO) AS TOTAL_CODIGO
              FROM DATOS d
             WHERE (p_codigo IS NULL OR UPPER(TRIM(d.CODIGO)) = UPPER(TRIM(p_codigo)))
             ORDER BY TO_CHAR(d.FECHARECEPCION, 'YYYYMMDDHH24MISSFF6') DESC;
    END SP_LISTAR;
    */

    PROCEDURE SP_LISTAR (
        p_codigo IN  DATOS.CODIGO%TYPE DEFAULT NULL,
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT d.ID,
                   d.CODIGO,
                   d.SECUENCIA,
                   d.FECHARECEPCION,
                   d.PESO,
                   d.DESCRIPCION,
                   COUNT(*) OVER (PARTITION BY d.CODIGO) AS TOTAL_CODIGO
              FROM DATOS d
             WHERE (p_codigo IS NULL OR UPPER(TRIM(d.CODIGO)) = UPPER(TRIM(p_codigo)))
             ORDER BY d.FECHARECEPCION DESC, d.ID DESC;
    END SP_LISTAR;


    PROCEDURE SP_ACTUALIZAR (
        p_id              IN DATOS.ID%TYPE,
        p_codigo          IN DATOS.CODIGO%TYPE,
        p_secuencia       IN DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN DATOS.FECHARECEPCION%TYPE,
        p_peso            IN DATOS.PESO%TYPE,
        p_descripcion     IN DATOS.DESCRIPCION%TYPE
    ) IS
    BEGIN
        UPDATE DATOS
           SET CODIGO         = p_codigo,
               SECUENCIA      = p_secuencia,
               FECHARECEPCION = p_fecha_recepcion,
               PESO           = p_peso,
               DESCRIPCION    = p_descripcion
         WHERE ID = p_id;

        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(C_ERR_NO_EXISTE, 'No existe un dato con ID ' || p_id);
        END IF;
    END SP_ACTUALIZAR;


    PROCEDURE SP_ELIMINAR (
        p_id IN DATOS.ID%TYPE
    ) IS
    BEGIN
        DELETE FROM DATOS
         WHERE ID = p_id;

        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(C_ERR_NO_EXISTE, 'No existe un dato con ID ' || p_id);
        END IF;
    END SP_ELIMINAR;


    FUNCTION FN_TOTAL_POR_CODIGO (
        p_codigo IN DATOS.CODIGO%TYPE
    ) RETURN NUMBER IS
        v_total NUMBER;
    BEGIN
        SELECT COUNT(*)
          INTO v_total
          FROM DATOS
         WHERE CODIGO = p_codigo;

        RETURN v_total;
    END FN_TOTAL_POR_CODIGO;

    PROCEDURE SP_LISTAR_PAGINADO (
        p_pagina          IN NUMBER,
        p_cursor          OUT SYS_REFCURSOR,
        p_total_registros OUT NUMBER
    ) IS
        c_tamano_pagina CONSTANT PLS_INTEGER := 20;
        v_offset NUMBER;
    BEGIN
        IF p_pagina IS NULL OR p_pagina < 1 OR p_pagina <> TRUNC(p_pagina) THEN
            RAISE_APPLICATION_ERROR(-20003, 'La pagina debe ser un entero mayor o igual a 1.');
        END IF;

        v_offset := (p_pagina - 1) * c_tamano_pagina;

        SELECT COUNT(*) INTO p_total_registros FROM DATOS;

        OPEN p_cursor FOR
            SELECT d.ID,
                   d.CODIGO,
                   d.SECUENCIA,
                   d.FECHARECEPCION,
                   d.PESO,
                   d.DESCRIPCION,
                   COUNT(*) OVER (PARTITION BY d.CODIGO) AS TOTAL_CODIGO
              FROM DATOS d
             ORDER BY d.FECHARECEPCION DESC, d.ID DESC
             OFFSET v_offset ROWS FETCH NEXT c_tamano_pagina ROWS ONLY;
    END SP_LISTAR_PAGINADO;

END PKG_DATOS;
/
SHOW ERRORS PACKAGE PKG_DATOS
SHOW ERRORS PACKAGE BODY PKG_DATOS
DECLARE
    v_errores NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_errores FROM USER_ERRORS
    WHERE NAME = 'PKG_DATOS' AND ATTRIBUTE = 'ERROR';
    IF v_errores > 0 THEN
        RAISE_APPLICATION_ERROR(-20002, 'PKG_DATOS tiene errores de compilacion.');
    END IF;
END;
/
