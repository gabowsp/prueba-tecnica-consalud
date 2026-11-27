using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Application.Ports.Input;

/// <summary>Elimina un dato existente.</summary>
public interface IEliminarDato
{
        Task<ResultadoEliminacion> EjecutarAsync(long id, CancellationToken cancellationToken);
}