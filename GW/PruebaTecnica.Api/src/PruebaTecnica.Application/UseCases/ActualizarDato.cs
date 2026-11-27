using PruebaTecnica.Application.Ports.Input;
using PruebaTecnica.Application.Ports.Output;
using PruebaTecnica.Application.Results;
using PruebaTecnica.Domain.Entities;

namespace PruebaTecnica.Application.UseCases;

/// <summary>Coordina la actualización del registro.</summary>
/// <param name="repository">Puerto de acceso a los datos.</param>
public sealed class ActualizarDato(IDatosRepository repository) : IActualizarDato
{
    /// <inheritdoc />
    public Task<ResultadoActualizacion> EjecutarAsync(Dato dato, CancellationToken cancellationToken)
        => repository.ActualizarAsync(dato, cancellationToken);
}
