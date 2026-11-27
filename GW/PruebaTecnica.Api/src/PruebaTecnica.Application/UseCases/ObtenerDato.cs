using PruebaTecnica.Application.Ports.Input;
using PruebaTecnica.Application.Ports.Output;
using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Application.UseCases;

/// <summary>Obtiene el detalle de un registro.</summary>
/// <param name="repository">Puerto de acceso a los datos.</param>
public sealed class ObtenerDato(IDatosRepository repository) : IObtenerDato
{
    /// <inheritdoc />
    public async Task<DetalleDatoRespuesta?> EjecutarAsync(long id, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(id, 1);
        var dato = await repository.ObtenerAsync(id, cancellationToken);
        return dato is null ? null : new(dato.Id, dato.Codigo, dato.Secuencia,
            dato.FechaRecepcion, dato.Peso, dato.Descripcion);
    }
}
