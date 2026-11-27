namespace PruebaTecnica.Application.Commands;

/// <summary>Datos de entrada para la creación, sin ID asignado.</summary>
/// <param name="Codigo">Código del registro.</param>
/// <param name="Secuencia">Secuencia dentro del código.</param>
/// <param name="FechaRecepcion">Fecha sin zona horaria.</param>
/// <param name="Peso">Peso del registro.</param>
/// <param name="Descripcion">Descripción opcional.</param>
public sealed record CrearDatoCommand(string Codigo, long Secuencia,
    DateTime FechaRecepcion, decimal Peso, string? Descripcion);
