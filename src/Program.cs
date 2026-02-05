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
builder.Services.AddSingleton<LogService>();
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
app.MapPost("/config", (BoxConfig config, RfidService rfid, LogService log) =>
{
    try
    {
        rfid.UpdateConfig(config);
        log.LogConfig(config.EventoId, config.Checkpoint, config.Leitores?.Count ?? 0);
        return Results.Ok(new { success = true, message = "Configuração atualizada" });
    }
    catch (Exception ex)
    {
        log.LogError(ex.Message);
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
    .WithName("SetConfig")
    .WithTags("Configuração");

// Iniciar modo teste
app.MapPost("/test/start", async (RfidService rfid, LogService log) =>
{
    try
    {
        await rfid.StartTestAsync();
        log.LogTestStart(rfid.Config.EventoId, rfid.Config.Checkpoint);
        return Results.Ok(new { success = true, message = "Modo teste iniciado" });
    }
    catch (Exception ex)
    {
        log.LogError(ex.Message, rfid.Config.EventoId, rfid.Config.Checkpoint);
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
    .WithName("StartTest")
    .WithTags("Teste");

// Parar modo teste
app.MapPost("/test/stop", (RfidService rfid, LogService log) =>
{
    rfid.Stop();
    var results = rfid.GetDetailedTestResults();
    log.LogTestStop(results, rfid.Config.EventoId, rfid.Config.Checkpoint);
    return Results.Ok(new { success = true, message = "Modo teste parado", results });
})
    .WithName("StopTest")
    .WithTags("Teste");

// Resultados do teste (detalhados por leitor e antena)
app.MapGet("/test/results", (RfidService rfid) => Results.Ok(rfid.GetDetailedTestResults()))
    .WithName("GetTestResults")
    .WithTags("Teste");

// Iniciar leitura (modo normal)
app.MapPost("/start", async (RfidService rfid, LogService log) =>
{
    try
    {
        await rfid.StartReadingAsync();
        log.LogStart(rfid.Config.EventoId, rfid.Config.Checkpoint);
        return Results.Ok(new { success = true, message = "Leitura iniciada" });
    }
    catch (Exception ex)
    {
        log.LogError(ex.Message, rfid.Config.EventoId, rfid.Config.Checkpoint);
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
    .WithName("StartReading")
    .WithTags("Leitura");

// Parar leitura
app.MapPost("/stop", (RfidService rfid, LogService log) =>
{
    var duracao = rfid.StartedAt.HasValue ? (DateTime.UtcNow - rfid.StartedAt.Value).TotalSeconds : 0;
    rfid.Stop();
    log.LogStop(duracao, 0, rfid.Config.EventoId, rfid.Config.Checkpoint); // TODO: passar total de eventos quando tiver persistência
    return Results.Ok(new { success = true, message = "Leitura parada" });
})
    .WithName("StopReading")
    .WithTags("Leitura");

// ==================== LOG ====================

// Histórico de operações
app.MapGet("/log", (LogService log, int? last) => Results.Ok(log.GetLogs(last)))
    .WithName("GetLog")
    .WithTags("Log");

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
