using PruebaTecnica.Application.Ports.Input;
using PruebaTecnica.Application.Ports.Output;
using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Application.UseCases;

/// <summary>Coordina la eliminación de un registro.</summary>
/// <param name="repository">Puerto de acceso a los datos.</param>
public sealed class EliminarDato(IDatosRepository repository)
    : IEliminarDato
{
    public Task<ResultadoEliminacion> EjecutarAsync(
        long id,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(id, 1);

        return repository.EliminarAsync(id, cancellationToken);
    }
}