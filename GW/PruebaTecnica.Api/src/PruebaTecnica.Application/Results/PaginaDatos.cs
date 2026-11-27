using PruebaTecnica.Domain.Entities;

namespace PruebaTecnica.Application.Results;

public sealed record DatoListado(Dato Dato, long TotalCodigo);
public sealed record PaginaDatos(IReadOnlyList<DatoListado> Items, long TotalRegistros);

/// <summary>Registro del listado con la cantidad de registros de su código.</summary>
public sealed record DatoRespuesta(decimal Id, string Codigo, decimal Secuencia,
    DateTime FechaRecepcion, decimal Peso, string? Descripcion, long TotalCodigo);

/// <summary>Resultado del listado paginado.</summary>
/// <param name="Items">Registros de la página solicitada.</param>
/// <param name="PaginaActual">Número de página actual.</param>
/// <param name="TamanoPagina">Cantidad fija de 20 registros por página.</param>
/// <param name="TotalRegistros">Cantidad total de registros.</param>
/// <param name="TotalPaginas">Cantidad total de páginas.</param>
public sealed record ListadoDatosRespuesta(IReadOnlyList<DatoRespuesta> Items,
    int PaginaActual, int TamanoPagina, long TotalRegistros, long TotalPaginas);
