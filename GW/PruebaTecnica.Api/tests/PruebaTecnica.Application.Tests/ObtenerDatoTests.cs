using PruebaTecnica.Application.Results;
using PruebaTecnica.Application.UseCases;
using PruebaTecnica.Domain.Entities;

namespace PruebaTecnica.Application.Tests;

public sealed class ObtenerDatoTests
{
    [Fact]
    public async Task DevuelveNullCuandoElIdNoExiste()
    {
        var repo = new RepositorioPrueba { Obtener = (_, _) => Task.FromResult<Dato?>(null) };
        Assert.Null(await new ObtenerDato(repo).EjecutarAsync(123, CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Recepción en Ñuñoa")]
    public async Task DevuelveLosDatosDelRegistro(string? descripcion)
    {
        var dato = new Dato(123, "COD", 2, new DateTime(2026, 11, 26), 12.3456m, descripcion);
        using var cts = new CancellationTokenSource();
        var repo = new RepositorioPrueba { Obtener = (id, ct) =>
        {
            Assert.Equal(123, id);
            Assert.Equal(cts.Token, ct);
            return Task.FromResult<Dato?>(dato);
        }};
        var resultado = await new ObtenerDato(repo).EjecutarAsync(123, cts.Token);
        Assert.Equal(new DetalleDatoRespuesta(123, "COD", 2, dato.FechaRecepcion, 12.3456m, descripcion), resultado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RechazaIdInvalidoAntesDelRepositorio(long id)
    {
        var caso = new ObtenerDato(new RepositorioPrueba());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => caso.EjecutarAsync(id, CancellationToken.None));
    }
}
