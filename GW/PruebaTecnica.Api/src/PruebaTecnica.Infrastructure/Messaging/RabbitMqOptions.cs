namespace PruebaTecnica.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "prueba";
    public string Password { get; set; } = "prueba";
    public string VirtualHost { get; set; } = "/";
    public string Queue { get; set; } = "pesos_especiales";
}
