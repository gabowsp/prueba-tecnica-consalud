using Microsoft.Extensions.Logging;
using PruebaTecnica.Application.Commands;
using PruebaTecnica.Application.Results;
using PruebaTecnica.Application.UseCases;

namespace PruebaTecnica.Application.Tests;

public sealed class CrearDatoTests
{
    private static CrearDatoCommand Datos(decimal peso) => new("TEST", 1,
        new DateTime(2026, 11, 26, 12, 0, 0), peso, null);

    [Theory]
    [InlineData("100.6399", false)]
    [InlineData("100.64", false)]
    [InlineData("100.6401", true)]
    [InlineData("150", true)]
    public async Task PublicaSoloCuandoElPesoSuperaElLimite(string valor, bool debePublicar)
    {
        var repo = new RepositorioPrueba { Insertar = (_, _) => Task.FromResult(new ResultadoCreacion(123)) };
        var publisher = new PublicadorPrueba();
        var caso = new CrearDato(repo, publisher, new LoggerPrueba<CrearDato>());

        var resultado = await caso.EjecutarAsync(Datos(decimal.Parse(valor,
            System.Globalization.CultureInfo.InvariantCulture)), CancellationToken.None);

        Assert.Equal(123L, resultado.Id);
        Assert.Equal(debePublicar ? new long[] { 123 } : Array.Empty<long>(), publisher.Ids);
    }

    [Fact]
    public async Task EsperaLaInsercionAntesDePublicar()
    {
        var insercion = new TaskCompletionSource<ResultadoCreacion>(TaskCreationOptions.RunContinuationsAsynchronously);
        var datos = Datos(150);
        using var cancelacion = new CancellationTokenSource();
        var repo = new RepositorioPrueba { Insertar = (entrada, ct) =>
        {
            Assert.Same(datos, entrada);
            Assert.Equal(cancelacion.Token, ct);
            return insercion.Task;
        }};
        var publisher = new PublicadorPrueba();
        var caso = new CrearDato(repo, publisher, new LoggerPrueba<CrearDato>());

        var operacion = caso.EjecutarAsync(datos, cancelacion.Token);
        Assert.False(operacion.IsCompleted);
        Assert.Empty(publisher.Ids);
        insercion.SetResult(new ResultadoCreacion(456));
        await operacion;
        Assert.Equal(new long[] { 456 }, publisher.Ids);
    }

    [Fact]
    public async Task UnDuplicadoNoPublica()
    {
        var repo = new RepositorioPrueba { Insertar = (_, _) => Task.FromResult(new ResultadoCreacion(null)) };
        var publisher = new PublicadorPrueba();
        var resultado = await new CrearDato(repo, publisher, new LoggerPrueba<CrearDato>())
            .EjecutarAsync(Datos(150), CancellationToken.None);
        Assert.Null(resultado.Id);
        Assert.Empty(publisher.Ids);
    }

    [Fact]
    public async Task ErrorDeInsercionSePropagaSinPublicar()
    {
        var error = new InvalidOperationException("Fallo de persistencia");
        var repo = new RepositorioPrueba { Insertar = (_, _) => Task.FromException<ResultadoCreacion>(error) };
        var publisher = new PublicadorPrueba();
        var caso = new CrearDato(repo, publisher, new LoggerPrueba<CrearDato>());
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            caso.EjecutarAsync(Datos(150), CancellationToken.None)));
        Assert.Empty(publisher.Ids);
    }

    [Fact]
    public async Task FalloDeRabbitConservaElIdYRegistraElError()
    {
        var error = new IOException("RabbitMQ no disponible");
        var repo = new RepositorioPrueba { Insertar = (_, _) => Task.FromResult(new ResultadoCreacion(789)) };
        var publisher = new PublicadorPrueba { Error = error };
        var logger = new LoggerPrueba<CrearDato>();
        var resultado = await new CrearDato(repo, publisher, logger)
            .EjecutarAsync(Datos(150), CancellationToken.None);
        Assert.Equal(789L, resultado.Id);
        Assert.Equal(new long[] { 789 }, publisher.Ids);
        var entrada = Assert.Single(logger.Entradas);
        Assert.Equal(LogLevel.Error, entrada.Nivel);
        Assert.Same(error, entrada.Error);
        Assert.Contains("789", entrada.Mensaje);
    }

    [Fact]
    public async Task CancelacionAntesDeGuardarNoPublica()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var repo = new RepositorioPrueba { Insertar = (_, ct) => Task.FromCanceled<ResultadoCreacion>(ct) };
        var publisher = new PublicadorPrueba();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new CrearDato(repo, publisher, new LoggerPrueba<CrearDato>()).EjecutarAsync(Datos(150), cts.Token));
        Assert.Empty(publisher.Ids);
    }

    [Fact]
    public async Task CancelacionDespuesDeGuardarNoCancelaLaNotificacion()
    {
        using var cts = new CancellationTokenSource();
        var repo = new RepositorioPrueba { Insertar = (_, _) =>
        {
            cts.Cancel();
            return Task.FromResult(new ResultadoCreacion(123));
        }};
        var publisher = new PublicadorPrueba();
        var resultado = await new CrearDato(repo, publisher, new LoggerPrueba<CrearDato>())
            .EjecutarAsync(Datos(150), cts.Token);
        Assert.Equal(123L, resultado.Id);
        Assert.Equal(new long[] { 123 }, publisher.Ids);
    }
}
