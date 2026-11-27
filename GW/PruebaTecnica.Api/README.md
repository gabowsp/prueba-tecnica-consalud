# Prueba técnica — API .NET 8

El proyecto usa una estructura hexagonal. Incluye el endpoint de prueba y el listado paginado conectado a Oracle. RabbitMQ recibe las notificaciones directamente después de guardar el dato.

## Estructura

```text
PruebaTecnica.sln
src/
  PruebaTecnica.Domain/
  PruebaTecnica.Application/
    Ports/Input/
    Results/
    UseCases/
  PruebaTecnica.Infrastructure/
  PruebaTecnica.Api/
    Controllers/
    Properties/
    Program.cs
```

- **Domain:** entidades y reglas del negocio. No depende de otros proyectos.
- **Application:** casos de uso y puertos (interfaces). Solo depende de Domain.
- **Infrastructure:** adaptadores para servicios externos. Depende de Application, donde se definirán los puertos de salida.
- **Api:** recibe las peticiones HTTP y conecta las implementaciones mediante inyección de dependencias.

El controlador llama al puerto `IObtenerPrueba`. La clase `ObtenerPrueba` implementa ese puerto y devuelve el mensaje. El listado usa `IListarDatos` y `IDatosRepository`; infraestructura implementa el acceso a Oracle.

## Ejecutar

Se necesita el SDK .NET 8 y el runtime ASP.NET Core 8. Desde esta carpeta:

```bash
dotnet build PruebaTecnica.sln
dotnet run --project src/PruebaTecnica.Api
```

Probar en http://localhost:5080/api/prueba o con:

```bash
curl -i http://localhost:5080/api/prueba
```

Respuesta HTTP 200:

```json
{"mensaje":"La API está funcionando"}
```

## Visual Studio Code

Abrir esta carpeta completa en VS Code. Con la extensión C# de Microsoft, presionar F5 para ejecutar y depurar. Las rutas de compilación y depuración ya apuntan a la estructura nueva.

El archivo `PruebaTecnica.Api.http` mantiene la petición de ejemplo. Swagger y las demás operaciones se agregarán después.

## Listado paginado

Con los contenedores levantados y el script `GW/02_agregar_sp_listar_paginado.sql` aplicado, iniciar la API y consultar:

```bash
curl -i 'http://localhost:5080/api/datos?pagina=1'
```

La respuesta incluye `items`, `paginaActual`, `tamanoPagina` (siempre 20), `totalRegistros` y `totalPaginas`. Cada registro incluye `totalCodigo`. Se ordena por fecha e ID descendentes.

- Sin parámetro, devuelve la página 1.
- Página cero, negativa, decimal o texto: HTTP 400 con `ValidationProblemDetails`.
- Página fuera del rango: HTTP 200 con lista vacía y los totales.
- Error de Oracle u otro error interno: HTTP 500 con `ProblemDetails`, sin detalles de la conexión. El error se registra en el servidor.

La conexión local está en `ConnectionStrings:Oracle` de `appsettings.json`, con las credenciales del entorno de la prueba. Se puede reemplazar con la variable de entorno `ConnectionStrings__Oracle`.

El flujo es `DatosController` → `IListarDatos` / `ListarDatos` → `IDatosRepository` / `OracleDatosRepository` → `PKG_DATOS.SP_LISTAR_PAGINADO`. Solo se reciben las filas de la página desde Oracle. Las fechas conservan el valor de `TIMESTAMP`, sin asignar una zona horaria que la base no almacena.

El total y los registros se consultan por separado dentro del procedimiento; con escrituras concurrentes pueden reflejar momentos diferentes.

## Consulta por ID

```bash
curl -i http://localhost:5080/api/datos/1
```

`GET /api/datos/{id}` ejecuta `PKG_DATOS.SP_OBTENER` y devuelve ID, código, secuencia, fecha de recepción, peso y descripción. `totalCodigo` corresponde solo al listado.

- ID existente: HTTP 200.
- ID inexistente: HTTP 404 con `ProblemDetails`.
- ID cero, negativo, decimal, texto o fuera del rango de un entero de 64 bits: HTTP 400 con `ValidationProblemDetails`, antes de acceder al repositorio.
- Error interno: HTTP 500 con `ProblemDetails`.

La operación está documentada en Swagger (`/swagger`) y en el documento OpenAPI (`/swagger/v1/swagger.json`). No requiere cambios en el package.

## Actualización completa (PUT)

`PUT /api/datos/{id}` llama a `PKG_DATOS.SP_ACTUALIZAR` y confirma la transacción si termina correctamente. Permite cambiar código, secuencia, fecha, peso y descripción. No publica mensajes en RabbitMQ.

```json
{
  "codigo": "COD-00001",
  "secuencia": 1,
  "fechaRecepcion": "2026-11-26T12:00:00",
  "peso": 120.5,
  "descripcion": "Descripción actualizada"
}
```

Todos los campos son obligatorios salvo descripción. Omitirla o enviar null la borra. La fecha se envía sin zona horaria. Código admite 32 bytes UTF-8, descripción hasta 320 caracteres y peso hasta 15 dígitos enteros y 4 decimales. Secuencia usa un entero de 64 bits.

Respuestas: 204 actualizado, 400 entrada inválida, 404 inexistente, 409 código y secuencia duplicados, 415 formato distinto de JSON y 500 error interno. Los errores usan ProblemDetails. La operación está disponible en Swagger.

Pendiente: este PUT permite actualizar todos los campos. La restricción del punto 8 (modificar solo descripción) se implementará por separado.

## Crear un registro

`POST /api/datos` llama a `PKG_DATOS.SP_INSERTAR`. Oracle obtiene el ID de `SEQ_DATOS`; la API confirma la transacción antes de devolver HTTP 201 con `{"id": 100123}` y la cabecera `Location` para consultar el registro.

```json
{
  "codigo": "NUEVO-001",
  "secuencia": 1,
  "fechaRecepcion": "2026-11-26T12:00:00",
  "peso": 90.25,
  "descripcion": "Registro nuevo"
}
```

Se aplican las mismas validaciones de campos que en el PUT. No se envía ID. La descripción puede omitirse o enviarse como null.

Respuestas: 201 creado, 400 datos inválidos, 409 código y secuencia duplicados, 415 formato distinto de JSON y 500 error interno. Los errores usan ProblemDetails. Un conflicto no deja una nueva fila, aunque la secuencia puede consumir un número.

El POST publica directamente en RabbitMQ cuando el peso supera 100,64, después de confirmar la inserción.

## RabbitMQ: publicación directa

El POST llama a `PKG_DATOS.SP_INSERTAR` y confirma la transacción. Si el peso es mayor que `100.64`, el caso de uso `CrearDato` publica `{"id":100123}` en `pesos_especiales` mediante `RabbitMQ.Client`. No llama a ningún package adicional ni guarda pendientes.

El mensaje usa `application/json`, persistencia y confirmación del broker. La cola durable ya viene creada en el entorno. El publicador reutiliza la conexión y serializa el uso del canal para permitir peticiones concurrentes.

Si RabbitMQ falla, el dato permanece guardado y el POST responde 201 con el ID y Location. El error se registra en el log. El envío dispone de un token con límite de 15 segundos; el POST espera el intento de publicación. No hay reintentos automáticos ni recuperación de mensajes después de reiniciar. Un error de confirmación no permite asegurar si el mensaje llegó. Una caída entre el commit y el envío también puede perder la notificación.

Las siguientes peticiones intentan reconectarse si la conexión anterior falló. El PUT, DELETE, pesos iguales o inferiores a 100,64 e inserciones fallidas no publican mensajes.

La configuración está en `RabbitMQ` de `appsettings.json`. Puede reemplazarse con variables de entorno, por ejemplo `RabbitMQ__HostName`. La consola está en http://localhost:15672, con usuario y contraseña `prueba`.

Para una instalación nueva solo se requieren los scripts 01 y 02 de GW después de levantar el entorno. No se necesita el script de outbox. Si sus objetos ya existen en Oracle, esta API no los usa y no procesa los pendientes antiguos.
