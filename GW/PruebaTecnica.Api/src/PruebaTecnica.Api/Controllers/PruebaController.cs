using Microsoft.AspNetCore.Mvc;
using PruebaTecnica.Application.Ports.Input;
using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Api.Controllers;

[ApiController]
[Route("api/prueba")]
public class PruebaController(IObtenerPrueba obtenerPrueba) : ControllerBase
{
    /// <summary>Comprueba que la API está funcionando.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PruebaResultado), StatusCodes.Status200OK)]
    public ActionResult<PruebaResultado> Get() => Ok(obtenerPrueba.Ejecutar());
}
