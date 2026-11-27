using PruebaTecnica.Application.Results;
using PruebaTecnica.Application.UseCases;
using PruebaTecnica.Domain.Entities;

namespace PruebaTecnica.Application.Tests;

public sealed class ListarDatosTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(40, 2)]
    [InlineData(100000, 5000)]
    public async Task CalculaLasPaginasConTamanoFijo(long total, long paginas)
    {
        var repo = new RepositorioPrueba { Listar = (_, _) => Task.FromResult(new PaginaDatos([], total)) };
        var resultado = await new ListarDatos(repo).EjecutarAsync(1, CancellationToken.None);
        Assert.Equal(20, resultado.TamanoPagina);
        Assert.Equal(total, resultado.TotalRegistros);
        Assert.Equal(paginas, resultado.TotalPaginas);
    }

    [Fact]
    public async Task ConservaTotalCodigoYDatosDeLaUltimaPagina()
    {
        var dato = new Dato(21, "COD", 2, new DateTime(2026, 11, 26), 1.2345m, null);
        using var cts = new CancellationTokenSource();
        var repo = new RepositorioPrueba { Listar = (pagina, ct) =>
        {
            Assert.Equal(2, pagina);
            Assert.Equal(cts.Token, ct);
            return Task.FromResult(new PaginaDatos([new(dato, 20)], 21));
        }};
        var resultado = await new ListarDatos(repo).EjecutarAsync(2, cts.Token);
        Assert.Equal(2, resultado.PaginaActual);
        Assert.Equal(2, resultado.TotalPaginas);
        Assert.Equal(new DatoRespuesta(21, "COD", 2, dato.FechaRecepcion, 1.2345m, null, 20),
            Assert.Single(resultado.Items));
    }

    [Fact]
    public async Task PaginaFueraDelRangoConservaLosTotales()
    {
        var repo = new RepositorioPrueba { Listar = (_, _) => Task.FromResult(new PaginaDatos([], 21)) };
        var resultado = await new ListarDatos(repo).EjecutarAsync(3, CancellationToken.None);
        Assert.Empty(resultado.Items);
        Assert.Equal(3, resultado.PaginaActual);
        Assert.Equal(21, resultado.TotalRegistros);
        Assert.Equal(2, resultado.TotalPaginas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RechazaPaginaInvalidaAntesDelRepositorio(int pagina)
    {
        // El doble lanza un error distinto si se intenta consultar.
        var caso = new ListarDatos(new RepositorioPrueba());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => caso.EjecutarAsync(pagina, CancellationToken.None));
    }
}
