# API de la prueba técnica

Desarrollé la API en .NET 8 para listar, consultar, crear, actualizar y eliminar registros en Oracle. Todas estas operaciones usan los procedimientos de `PKG_DATOS`.

También agregué Swagger para probar los endpoints y el envío de mensajes a RabbitMQ cuando se crea un registro con peso mayor que 100,64.

## Estructura

Separé el proyecto en cuatro capas:

- **Domain:** datos y reglas del negocio.
- **Application:** lógica de cada operación e interfaces.
- **Infrastructure:** conexión a Oracle y publicación en RabbitMQ.
- **Api:** controladores, validaciones de entrada y configuración.

El controlador recibe la petición, llama al caso de uso y este utiliza el repositorio para acceder a Oracle.

## Cómo ejecutar

Se necesita el SDK .NET 8, el runtime ASP.NET Core 8 y los contenedores de Oracle y RabbitMQ levantados. La preparación del entorno y los scripts SQL están en el [README principal](../README.md).

Desde esta carpeta, ejecutar:

```bash
dotnet build PruebaTecnica.sln
dotnet run --project src/PruebaTecnica.Api
```

Para trabajar en VS Code, abrir esta carpeta completa e instalar la extensión C# de Microsoft. También se puede ejecutar y depurar con F5.

Las conexiones se configuran en `src/PruebaTecnica.Api/appsettings.json`, en las secciones `ConnectionStrings:Oracle` y `RabbitMQ`. Los valores incluidos corresponden al entorno local de la prueba. Se pueden reemplazar con variables como `ConnectionStrings__Oracle` y `RabbitMQ__HostName`.

## Swagger

Con la API ejecutándose:

- [Swagger UI](http://localhost:5080/swagger): permite probar los endpoints.
- [Documento OpenAPI](http://localhost:5080/swagger/v1/swagger.json): contiene la documentación en JSON.

También dejé ejemplos de peticiones en `PruebaTecnica.Api.http`.

## Endpoints

| Método | Ruta | Qué hace | Respuesta correcta |
| --- | --- | --- | --- |
| GET | `/api/prueba` | Comprueba que la API funciona | 200 |
| GET | `/api/datos?pagina=1` | Lista los registros de 20 en 20 | 200 |
| GET | `/api/datos/{id}` | Consulta un registro por ID | 200 |
| POST | `/api/datos` | Crea un registro y devuelve su ID | 201 |
| PUT | `/api/datos/{id}` | Actualiza un registro | 204 |
| DELETE | `/api/datos/{id}` | Elimina un registro | 204 |

El listado devuelve la página actual, el tamaño de página, el total de registros y el total de páginas. Cada registro incluye `totalCodigo`. Si no se indica página, se usa la 1. Si supera la última, devuelve una lista vacía con los totales.

Los registros se ordenan por fecha e ID, del más reciente al más antiguo. El total y la página se consultan por separado; si se agregan o eliminan datos al mismo tiempo, pueden reflejar momentos distintos.

## Crear y actualizar

Este es un ejemplo del JSON para POST y PUT:

```json
{
  "codigo": "NUEVO-001",
  "secuencia": 1,
  "fechaRecepcion": "2026-11-26T12:00:00",
  "peso": 120.5,
  "descripcion": "Registro de prueba"
}
```

Todos los campos son obligatorios salvo la descripción. Esta acepta hasta 320 caracteres; en el PUT, omitirla o enviar `null` la borra.

El código admite hasta 32 bytes UTF-8. La secuencia es un entero de 64 bits, la fecha se envía sin zona horaria y el peso admite hasta 15 dígitos enteros y 4 decimales.

El POST obtiene el ID de la secuencia de Oracle y confirma la transacción antes de responder. Devuelve el ID y la cabecera `Location`, con la URL para consultar el registro. El ID no se envía en el cuerpo de la petición.

## Errores

Los errores se devuelven en formato `ProblemDetails`:

- **400:** datos, ID o página inválidos.
- **404:** el registro solicitado no existe.
- **409:** ya existe otro registro con el mismo código y secuencia.
- **415:** el POST o PUT no se envió como JSON.
- **500:** error interno. El detalle queda en el log del servidor.

## RabbitMQ

Después de guardar un registro, si el peso es mayor que `100.64`, la API envía su ID a la cola `pesos_especiales`:

```json
{"id":100123}
```

El mensaje es persistente, usa `application/json` y se espera la confirmación de RabbitMQ. El envío se hace directamente desde C#, sin llamar a un package adicional ni guardar notificaciones pendientes.

Si RabbitMQ falla, el dato queda guardado y el POST responde 201. El error queda en el log. No hay reenvío automático, por lo que la notificación podría perderse. Si falla la confirmación, no se puede asegurar si el mensaje llegó.

Solo se publica al crear. El PUT, el DELETE, las inserciones fallidas y los pesos iguales o menores que 100,64 no generan mensajes.

Los mensajes se pueden revisar en la [consola de RabbitMQ](http://localhost:15672), con usuario y contraseña `prueba`.

## Pendientes

- Aplicar el punto 8: permitir actualizar solo la descripción y rechazar otros campos antes de acceder a Oracle. Actualmente el PUT permite modificar todos los campos del ejemplo.
- Completar los comentarios XML que todavía generan advertencias al compilar.

## Pruebas unitarias

Agregué el proyecto `tests/PruebaTecnica.Application.Tests` con xUnit. Para ejecutarlo desde esta carpeta:

```bash
dotnet test PruebaTecnica.sln
```
