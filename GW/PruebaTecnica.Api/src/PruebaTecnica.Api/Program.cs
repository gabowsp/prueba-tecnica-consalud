using PruebaTecnica.Infrastructure.Messaging;
using PruebaTecnica.Application.Ports.Input;
using PruebaTecnica.Application.Ports.Output;
using PruebaTecnica.Application.UseCases;
using PruebaTecnica.Infrastructure.Oracle;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IObtenerPrueba, ObtenerPrueba>();
builder.Services.AddScoped<IListarDatos, ListarDatos>();
builder.Services.AddScoped<IObtenerDato, ObtenerDato>();
builder.Services.AddScoped<IActualizarDato, ActualizarDato>();
builder.Services.AddScoped<ICrearDato, CrearDato>();
builder.Services.AddScoped<IEliminarDato, EliminarDato>();
var oracleConnection = builder.Configuration.GetConnectionString("Oracle")
    ?? throw new InvalidOperationException("Falta configurar ConnectionStrings:Oracle.");
builder.Services.AddScoped<IDatosRepository>(_ => new OracleDatosRepository(oracleConnection));

var rabbitOptions = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMqOptions>()
    ?? new RabbitMqOptions();
builder.Services.AddSingleton(rabbitOptions);
builder.Services.AddSingleton<IPesosEspecialesPublisher, RabbitMqPesosEspecialesPublisher>();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Prueba Técnica GWE",
        Version = "v1",
        Description = "API para manejo de datos."
    });

    options.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, "PruebaTecnica.Api.xml"),
        includeControllerXmlComments: true);

    options.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, "PruebaTecnica.Application.xml"));
});

var app = builder.Build();

// Devuelve un error uniforme sin exponer consultas, credenciales ni detalles internos.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    await Results.Problem(statusCode: 500, title: "No se pudo completar la solicitud.",
        detail: "Ocurrió un error interno. Intenta nuevamente más tarde.")
        .ExecuteAsync(context);
}));

app.UseStatusCodePages(async statusContext =>
{
    var context = statusContext.HttpContext;
    await Results.Problem(statusCode: context.Response.StatusCode)
        .ExecuteAsync(context);
});

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Prueba Técnica GWE API v1");

    options.RoutePrefix = "swagger";
});
app.MapControllers();

app.Run();
