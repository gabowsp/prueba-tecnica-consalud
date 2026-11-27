using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Application.Ports.Input;

/// <summary>Consulta un registro por su identificador.</summary>
public interface IObtenerDato
{
    /// <summary>Devuelve el registro o null si no existe.</summary>
    Task<DetalleDatoRespuesta?> EjecutarAsync(long id, CancellationToken cancellationToken);
}
