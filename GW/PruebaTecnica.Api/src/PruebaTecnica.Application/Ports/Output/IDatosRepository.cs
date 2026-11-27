using PruebaTecnica.Application.Commands;
using PruebaTecnica.Domain.Entities;
using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Application.Ports.Output;

public interface IDatosRepository
{
    Task<Dato?> ObtenerAsync(long id, CancellationToken cancellationToken);

    Task<ResultadoActualizacion> ActualizarAsync(Dato dato, CancellationToken cancellationToken);

    Task<ResultadoCreacion> InsertarAsync(CrearDatoCommand datos, CancellationToken cancellationToken);

    Task<PaginaDatos> ListarAsync(int pagina, CancellationToken cancellationToken);

    Task<ResultadoEliminacion> EliminarAsync(long id, CancellationToken cancellationToken);
}
