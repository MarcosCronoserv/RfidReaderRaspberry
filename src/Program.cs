using System.Text.Json.Serialization;
using RfidReaderRaspberry.Models;
using RfidReaderRaspberry.Services;

var builder = WebApplication.CreateBuilder(args);

// Configura para escutar em todas as interfaces
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// JSON: enums como string, ignora nulls
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Serviços
builder.Services.AddSingleton<RfidService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BOX RFID API", Version = "v1" });
});

var app = builder.Build();

// Swagger (desenvolvimento e produção para facilitar testes)
app.UseSwagger();
app.UseSwaggerUI();

// ==================== ENDPOINTS ====================

// Health check simples
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithTags("Sistema");

// Status completo do BOX
app.MapGet("/status", (RfidService rfid) => Results.Ok(rfid.GetStatus()))
    .WithName("GetStatus")
    .WithTags("Sistema");

// Configurar BOX
app.MapPost("/config", (BoxConfig config, RfidService rfid) =>
{
    try
    {
        rfid.UpdateConfig(config);
        return Results.Ok(new { success = true, message = "Configuração atualizada" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
    .WithName("SetConfig")
    .WithTags("Configuração");

// Iniciar modo teste
app.MapPost("/test/start", async (RfidService rfid) =>
{
    try
    {
        await rfid.StartTestAsync();
        return Results.Ok(new { success = true, message = "Modo teste iniciado" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
    .WithName("StartTest")
    .WithTags("Teste");

// Parar modo teste
app.MapPost("/test/stop", (RfidService rfid) =>
{
    rfid.Stop();
    return Results.Ok(new { success = true, message = "Modo teste parado" });
})
    .WithName("StopTest")
    .WithTags("Teste");

// Resultados do teste
app.MapGet("/test/results", (RfidService rfid) =>
{
    var results = rfid.GetTestResults();
    return Results.Ok(new
    {
        mode = rfid.CurrentMode.ToString(),
        totalRfReads = rfid.TotalRfReads,
        uniqueTags = results.Count,
        tags = results.OrderByDescending(kv => kv.Value)
                      .Select(kv => new { epc = kv.Key, count = kv.Value })
    });
})
    .WithName("GetTestResults")
    .WithTags("Teste");

// Iniciar leitura (modo normal)
app.MapPost("/start", async (RfidService rfid) =>
{
    try
    {
        await rfid.StartReadingAsync();
        return Results.Ok(new { success = true, message = "Leitura iniciada" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
    .WithName("StartReading")
    .WithTags("Leitura");

// Parar leitura
app.MapPost("/stop", (RfidService rfid) =>
{
    rfid.Stop();
    return Results.Ok(new { success = true, message = "Leitura parada" });
})
    .WithName("StopReading")
    .WithTags("Leitura");

// ==================== STARTUP ====================

var config = app.Configuration.GetSection("Box").Get<BoxConfig>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("===========================================");
logger.LogInformation("  BOX RFID - Raspberry Pi");
logger.LogInformation("  API rodando em http://0.0.0.0:5000");
logger.LogInformation("  Swagger: http://localhost:5000/swagger");
logger.LogInformation("===========================================");
logger.LogInformation("BoxId: {BoxId}", config?.BoxId ?? "não configurado");
logger.LogInformation("Leitores configurados: {Count}", config?.Leitores?.Count ?? 0);

app.Run();
