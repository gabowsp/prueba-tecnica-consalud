namespace PruebaTecnica.Domain.Entities;

public sealed record Dato(decimal Id, string Codigo, decimal Secuencia,
    DateTime FechaRecepcion, decimal Peso, string? Descripcion);
