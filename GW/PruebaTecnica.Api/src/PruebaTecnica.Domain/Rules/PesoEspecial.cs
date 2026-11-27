namespace PruebaTecnica.Domain.Rules;

public static class PesoEspecial
{
    public static bool RequiereNotificacion(decimal peso) => peso > 100.64m;
}
