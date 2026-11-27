using PruebaTecnica.Application.Ports.Input;
using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Application.UseCases;

public sealed class ObtenerPrueba : IObtenerPrueba
{
    public PruebaResultado Ejecutar() => new("La API está funcionando");
}
