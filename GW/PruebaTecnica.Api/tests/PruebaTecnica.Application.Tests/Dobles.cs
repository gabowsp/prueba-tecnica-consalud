using Microsoft.Extensions.Logging;
using PruebaTecnica.Application.Commands;
using PruebaTecnica.Application.Ports.Output;
using PruebaTecnica.Application.Results;
using PruebaTecnica.Domain.Entities;

namespace PruebaTecnica.Application.Tests;

internal sealed class RepositorioPrueba : IDatosRepository
{
    public Func<CrearDatoCommand, CancellationToken, Task<ResultadoCreacion>> Insertar { get; set; }
        = (_, _) => throw new InvalidOperationException("Inserción no esperada.");
    public Func<int, CancellationToken, Task<PaginaDatos>> Listar { get; set; }
        = (_, _) => throw new InvalidOperationException("Listado no esperado.");
    public Func<long, CancellationToken, Task<Dato?>> Obtener { get; set; }
        = (_, _) => throw new InvalidOperationException("Consulta no esperada.");
    public Task<ResultadoCreacion> InsertarAsync(CrearDatoCommand datos, CancellationToken ct) => Insertar(datos, ct);
    public Task<PaginaDatos> ListarAsync(int pagina, CancellationToken ct) => Listar(pagina, ct);
    public Task<Dato?> ObtenerAsync(long id, CancellationToken ct) => Obtener(id, ct);
    public Task<ResultadoActualizacion> ActualizarAsync(Dato dato, CancellationToken ct) => throw new NotSupportedException();
    public Task<ResultadoEliminacion> EliminarAsync(long id, CancellationToken ct) => throw new NotSupportedException();
}

internal sealed class PublicadorPrueba : IPesosEspecialesPublisher
{
    public List<long> Ids { get; } = [];
    public Exception? Error { get; init; }
    public Task PublicarAsync(long id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Ids.Add(id);
        return Error is null ? Task.CompletedTask : Task.FromException(Error);
    }
}

internal sealed class LoggerPrueba<T> : ILogger<T>
{
    public List<(LogLevel Nivel, Exception? Error, string Mensaje)> Entradas { get; } = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => true;
    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error,
        Func<TState, Exception?, string> formatter) => Entradas.Add((level, error, formatter(state, error)));
}
