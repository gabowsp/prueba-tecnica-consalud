using PruebaTecnica.Application.Commands;
using PruebaTecnica.Api.Contracts;
using PruebaTecnica.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PruebaTecnica.Application.Ports.Input;
using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Api.Controllers;

/// <summary>Consulta de los datos guardados en Oracle.</summary>
[ApiController]
[Route("api/datos")]
public sealed class DatosController(IListarDatos listarDatos, IObtenerDato obtenerDato, IActualizarDato actualizarDato, ICrearDato crearDato, IEliminarDato eliminarDato) : ControllerBase
{
    /// <summary>Lista los datos del más reciente al más antiguo, de 20 en 20.</summary>
    /// <param name="pagina">Número de página, desde 1. Por defecto es 1.</param>
    /// <param name="cancellationToken">Cancelación de la petición.</param>
    /// <response code="200">Página solicitada, vacía si supera el total de páginas.</response>
    /// <response code="400">El número de página no es válido.</response>
    /// <response code="500">No se pudo completar la consulta.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ListadoDatosRespuesta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ListadoDatosRespuesta>> Get(
        [FromQuery, Range(1, int.MaxValue)] int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        return Ok(await listarDatos.EjecutarAsync(pagina, cancellationToken));
    }

    /// <summary>Obtiene un dato por su ID.</summary>
    /// <param name="id">Entero positivo de 64 bits que identifica al registro.</param>
    /// <param name="cancellationToken">Cancelación de la petición.</param>
    /// <response code="200">Registro encontrado.</response>
    /// <response code="400">El ID no es un entero positivo válido.</response>
    /// <response code="404">No existe un registro con ese ID.</response>
    /// <response code="500">No se pudo completar la consulta.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DetalleDatoRespuesta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DetalleDatoRespuesta>> ObtenerPorId(
        [FromRoute, Range(typeof(long), "1", "9223372036854775807")] long id,
        CancellationToken cancellationToken = default)
    {
        var dato = await obtenerDato.EjecutarAsync(id, cancellationToken);
        if (dato is null)
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Dato no encontrado.", detail: $"No existe un dato con ID {id}.");

        return Ok(dato);
    }

    /// <summary>Actualiza todos los campos de un registro existente.</summary>
    /// <param name="id">Identificador positivo del registro.</param>
    /// <param name="request">Nuevos valores del registro.</param>
    /// <param name="cancellationToken">Cancelación de la petición.</param>
    /// <response code="204">Registro actualizado.</response>
    /// <response code="400">ID o datos de entrada inválidos.</response>
    /// <response code="404">El registro no existe.</response>
    /// <response code="409">El código y la secuencia ya están en uso.</response>
    /// <response code="415">El cuerpo debe enviarse como JSON.</response>
    /// <response code="500">Error interno al actualizar.</response>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Actualizar(
        [FromRoute, Range(typeof(long), "1", "9223372036854775807")] long id,
        [FromBody] ActualizarDatoRequest request, CancellationToken cancellationToken = default)
    {
        var dato = new Dato(id, request.Codigo!, request.Secuencia!.Value,
            request.FechaRecepcion!.Value, request.Peso!.Value, request.Descripcion);
        var resultado = await actualizarDato.EjecutarAsync(dato, cancellationToken);
        return resultado switch
        {
            ResultadoActualizacion.Actualizado => NoContent(),
            ResultadoActualizacion.NoEncontrado => Problem(statusCode: 404,
                title: "Dato no encontrado.", detail: $"No existe un dato con ID {id}."),
            ResultadoActualizacion.Conflicto => Problem(statusCode: 409,
                title: "Código y secuencia duplicados.", detail: "Ya existe otro registro con ese código y secuencia."),
            _ => throw new InvalidOperationException("Resultado de actualización desconocido.")
        };
    }

    /// <summary>Crea un dato y devuelve el ID generado por Oracle.</summary>
    /// <param name="request">Datos del nuevo registro.</param>
    /// <param name="cancellationToken">Cancelación de la petición.</param>
    /// <response code="201">Registro guardado. Location indica la URL para consultarlo.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="409">El código y la secuencia ya están en uso.</response>
    /// <response code="415">El cuerpo debe enviarse como JSON.</response>
    /// <response code="500">Error interno al crear el registro.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DatoCreadoRespuesta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DatoCreadoRespuesta>> Crear(
        [FromBody] CrearDatoRequest request, CancellationToken cancellationToken = default)
    {
        var datos = new CrearDatoCommand(request.Codigo!, request.Secuencia!.Value,
            request.FechaRecepcion!.Value, request.Peso!.Value, request.Descripcion);
        var resultado = await crearDato.EjecutarAsync(datos, cancellationToken);
        if (resultado.Id is not long id)
            return Problem(statusCode: 409, title: "Código y secuencia duplicados.",
                detail: "Ya existe un registro con ese código y secuencia.");

        return CreatedAtAction(nameof(ObtenerPorId), new { id }, new DatoCreadoRespuesta(id));
    }

    /// <summary>Elimina un dato por su ID.</summary>
    /// <param name="id">Identificador positivo del registro.</param>
    /// <param name="cancellationToken">Cancelación de la petición.</param>
    /// <response code="204">Registro eliminado.</response>
    /// <response code="400">El ID no es válido.</response>
    /// <response code="404">El registro no existe.</response>
    /// <response code="500">Error interno al eliminar.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Eliminar(
        [FromRoute, Range(typeof(long), "1", "9223372036854775807")]
        long id,
        CancellationToken cancellationToken = default)
    {
        var resultado = await eliminarDato.EjecutarAsync(
            id,
            cancellationToken);

        return resultado switch
        {
            ResultadoEliminacion.Eliminado => NoContent(),

            ResultadoEliminacion.NoEncontrado => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Dato no encontrado.",
                detail: $"No existe un dato con ID {id}."),

            _ => throw new InvalidOperationException(
                "Resultado de eliminación desconocido.")
        };
    }
}
