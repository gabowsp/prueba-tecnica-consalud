using PruebaTecnica.Application.Commands;
using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Application.Ports.Input;

/// <summary>Crea un registro nuevo.</summary>
public interface ICrearDato
{
    /// <summary>Devuelve el ID generado o un conflicto.</summary>
    Task<ResultadoCreacion> EjecutarAsync(CrearDatoCommand datos, CancellationToken cancellationToken);
}
