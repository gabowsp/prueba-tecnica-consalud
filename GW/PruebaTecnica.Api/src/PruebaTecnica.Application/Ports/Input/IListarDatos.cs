using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Application.Ports.Input;

public interface IListarDatos
{
    Task<ListadoDatosRespuesta> EjecutarAsync(int pagina, CancellationToken cancellationToken);
}
