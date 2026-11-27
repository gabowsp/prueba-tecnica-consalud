namespace PruebaTecnica.Application.Results;

/// <summary>Resultado de la inserción. Un ID nulo indica código y secuencia duplicados.</summary>
/// <param name="Id">ID generado por Oracle o null si hubo conflicto.</param>
public sealed record ResultadoCreacion(long? Id);

/// <summary>Identificador del registro creado.</summary>
/// <param name="Id">ID obtenido de la secuencia de Oracle.</param>
public sealed record DatoCreadoRespuesta(long Id);
