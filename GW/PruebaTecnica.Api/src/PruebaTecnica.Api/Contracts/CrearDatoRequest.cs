using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace PruebaTecnica.Api.Contracts;

/// <summary>Datos necesarios para crear un registro.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CrearDatoRequest : IValidatableObject
{
    /// <summary>Código obligatorio, hasta 32 bytes UTF-8.</summary>
    [Required, StringLength(32)]
    public string? Codigo { get; init; }

    /// <summary>Secuencia entera dentro del código.</summary>
    [Required]
    public long? Secuencia { get; init; }

    /// <summary>Fecha de recepción sin zona horaria.</summary>
    [Required]
    public DateTime? FechaRecepcion { get; init; }

    /// <summary>Peso con hasta 15 dígitos enteros y 4 decimales.</summary>
    [Required]
    public decimal? Peso { get; init; }

    /// <summary>Descripción opcional. Puede omitirse o enviarse como null.</summary>
    [StringLength(320)]
    public string? Descripcion { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Codigo is not null && Encoding.UTF8.GetByteCount(Codigo) > 32)
            yield return new("El código no puede superar 32 bytes UTF-8.", [nameof(Codigo)]);
        if (Peso is decimal peso && (peso < -999999999999999.9999m || peso > 999999999999999.9999m
            || decimal.Round(peso, 4) != peso))
            yield return new("El peso admite hasta 15 dígitos enteros y 4 decimales.", [nameof(Peso)]);
        if (FechaRecepcion is DateTime fecha && fecha.Kind != DateTimeKind.Unspecified)
            yield return new("La fecha debe enviarse sin zona horaria, por ejemplo 2026-11-26T12:00:00.", [nameof(FechaRecepcion)]);
    }
}
