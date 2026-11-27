namespace PruebaTecnica.Application.Ports.Output;

/// <summary>Publica una notificación de creación de peso especial.</summary>
public interface IPesosEspecialesPublisher
{
    /// <summary>Espera la confirmación del broker o lanza un error.</summary>
    Task PublicarAsync(long id, CancellationToken cancellationToken);
}
