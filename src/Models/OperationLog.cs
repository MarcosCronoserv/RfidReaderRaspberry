namespace RfidReaderRaspberry.Models;

/// <summary>
/// Registro de operação no log
/// </summary>
public class OperationLog
{
    public int Id { get; set; }

    /// <summary>Timestamp da operação</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Tipo de operação: CONFIG, TEST_START, TEST_STOP, START, STOP, ERROR</summary>
    public string Operation { get; set; } = "";

    /// <summary>Detalhes da operação em JSON</summary>
    public string? Details { get; set; }

    /// <summary>ID do evento (se aplicável)</summary>
    public string? EventoId { get; set; }

    /// <summary>Checkpoint (se aplicável)</summary>
    public string? Checkpoint { get; set; }
}

/// <summary>
/// Tipos de operação para o log
/// </summary>
public static class OperationType
{
    public const string Config = "CONFIG";
    public const string TestStart = "TEST_START";
    public const string TestStop = "TEST_STOP";
    public const string Start = "START";
    public const string Stop = "STOP";
    public const string Error = "ERROR";
}
