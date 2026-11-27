# Prueba técnica Backend .NET

Solución desarrollada con .NET 8, Oracle y RabbitMQ.

## Contenido

- `01_corregir_sp_listar.sql`: corrección de rendimiento de SP_LISTAR.
- `02_agregar_sp_listar_paginado.sql`: listado paginado de 20 registros.
- `PruebaTecnica.Api/`: código fuente de la API.

## Preparar el entorno

Desde la carpeta `prueba-tecnica`, ejecutar:

```bash
docker compose up -d
docker compose ps
```

Cuando Oracle y RabbitMQ aparezcan como `healthy`, verificar el entorno:

```bash
bash scripts/verificar.sh
```

Luego aplicar los scripts en este orden:

```bash
docker exec -i prueba-oracle sqlplus -s -L prueba/prueba@//localhost/FREEPDB1 < GW/01_corregir_sp_listar.sql

docker exec -i prueba-oracle sqlplus -s -L prueba/prueba@//localhost/FREEPDB1 < GW/02_agregar_sp_listar_paginado.sql
```

## Corrección de rendimiento

SP_LISTAR llamaba a una función que ejecutaba un COUNT por cada registro. Esto repetía el mismo conteo para las filas del mismo código.

Se reemplazó por `COUNT(*) OVER (PARTITION BY d.CODIGO)`, que calcula los totales dentro de la consulta. También se eliminó la conversión de fecha a texto para ordenar directamente por fecha e ID.

En las pruebas locales con 100.000 registros, la mediana del tiempo bajó de 1.080 ms a 100 ms. La comparación de los datos devueltos no mostró diferencias.

## API

El detalle del desarrollo, la estructura del proyecto, la configuración, los endpoints y las instrucciones de ejecución se encuentran en el [README de la API](PruebaTecnica.Api/README.md).

Una vez iniciada la API:

- Swagger UI: http://localhost:5080/swagger
- Documento OpenAPI: http://localhost:5080/swagger/v1/swagger.json

## Pendiente

El PUT permite modificar todos los campos. Queda pendiente aplicar la restricción del punto 8 para permitir actualizar únicamente la descripción.