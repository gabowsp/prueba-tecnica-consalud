using PruebaTecnica.Application.Results;

namespace PruebaTecnica.Application.Ports.Input;

public interface IObtenerPrueba
{
    PruebaResultado Ejecutar();
}
