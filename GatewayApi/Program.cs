using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using RaftElection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var nodes = Environment.GetEnvironmentVariable("NODES")?.Split(',')?.ToList() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddSingleton(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<Gateway>>();
    return new Gateway(nodes, logger);

});

var serviceName = "Gateway";

builder.Logging.AddOpenTelemetry(options =>
{
    options.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://raft-otel-collector:4300");
    }).SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
});

var app = builder.Build();
app.UseCors("AllowAll");


// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();



app.MapControllers();

app.Run();
