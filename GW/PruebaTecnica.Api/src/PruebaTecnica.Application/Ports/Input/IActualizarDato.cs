using PruebaTecnica.Application.Results;
using PruebaTecnica.Domain.Entities;

namespace PruebaTecnica.Application.Ports.Input;

/// <summary>Actualiza los campos de un dato existente.</summary>
public interface IActualizarDato
{
    /// <summary>Ejecuta la actualización.</summary>
    Task<ResultadoActualizacion> EjecutarAsync(Dato dato, CancellationToken cancellationToken);
}
