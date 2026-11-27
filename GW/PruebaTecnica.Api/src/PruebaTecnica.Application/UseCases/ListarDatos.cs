using PruebaTecnica.Application.Ports.Input;
using PruebaTecnica.Application.Ports.Output;
using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Application.UseCases;

public sealed class ListarDatos(IDatosRepository repository) : IListarDatos
{
    public async Task<ListadoDatosRespuesta> EjecutarAsync(int pagina, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pagina, 1);
        const int tamanoPagina = 20;
        var resultado = await repository.ListarAsync(pagina, cancellationToken);
        var items = resultado.Items.Select(x => new DatoRespuesta(x.Dato.Id, x.Dato.Codigo,
            x.Dato.Secuencia, x.Dato.FechaRecepcion, x.Dato.Peso, x.Dato.Descripcion,
            x.TotalCodigo)).ToArray();
        var totalPaginas = resultado.TotalRegistros / tamanoPagina
            + (resultado.TotalRegistros % tamanoPagina == 0 ? 0 : 1);
        return new(items, pagina, tamanoPagina, resultado.TotalRegistros, totalPaginas);
    }
}
