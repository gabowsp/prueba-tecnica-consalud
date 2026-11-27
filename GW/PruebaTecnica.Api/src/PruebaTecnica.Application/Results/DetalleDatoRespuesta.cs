namespace PruebaTecnica.Application.Results;

/// <summary>Datos de un registro consultado por su ID.</summary>
/// <param name="Id">Identificador generado por Oracle.</param>
/// <param name="Codigo">Código del registro.</param>
/// <param name="Secuencia">Secuencia dentro del código.</param>
/// <param name="FechaRecepcion">Fecha de recepción, sin zona horaria.</param>
/// <param name="Peso">Peso del registro.</param>
/// <param name="Descripcion">Descripción opcional.</param>
public sealed record DetalleDatoRespuesta(decimal Id, string Codigo, decimal Secuencia,
    DateTime FechaRecepcion, decimal Peso, string? Descripcion);
