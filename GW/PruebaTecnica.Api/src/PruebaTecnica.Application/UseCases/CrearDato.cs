using Microsoft.Extensions.Logging;
using PruebaTecnica.Application.Commands;
using PruebaTecnica.Application.Ports.Input;
using PruebaTecnica.Application.Ports.Output;
using PruebaTecnica.Application.Results;
using PruebaTecnica.Domain.Rules;

namespace PruebaTecnica.Application.UseCases;

/// <summary>Guarda el dato y publica su notificación después del commit.</summary>
/// <param name="repository">Puerto de persistencia.</param>
/// <param name="publisher">Publicador de pesos especiales.</param>
/// <param name="logger">Registro de fallos de publicación.</param>
public sealed class CrearDato(IDatosRepository repository,
    IPesosEspecialesPublisher publisher, ILogger<CrearDato> logger) : ICrearDato
{
    /// <inheritdoc />
    public async Task<ResultadoCreacion> EjecutarAsync(CrearDatoCommand datos,
        CancellationToken cancellationToken)
    {
        var resultado = await repository.InsertarAsync(datos, cancellationToken);
        if (resultado.Id is long id && PesoEspecial.RequiereNotificacion(datos.Peso))
        {
            try
            {
                // El dato ya está confirmado: una desconexión HTTP no cancela el envío.
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await publisher.PublicarAsync(id, timeout.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "El dato {Id} quedó guardado, pero no se pudo confirmar su publicación en RabbitMQ.", id);
            }
        }
        return resultado;
    }
}
