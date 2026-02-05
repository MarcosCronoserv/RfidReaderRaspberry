namespace RfidReaderRaspberry.Models;

/// <summary>
/// Estado atual do BOX (retornado por /status)
/// </summary>
public class BoxStatus
{
    /// <summary>ID do BOX</summary>
    public string BoxId { get; set; } = "";

    /// <summary>Modo de operação atual</summary>
    public BoxOperationMode Mode { get; set; } = BoxOperationMode.Stopped;

    /// <summary>ID do evento configurado</summary>
    public string? EventoId { get; set; }

    /// <summary>Checkpoint configurado</summary>
    public string? Checkpoint { get; set; }

    /// <summary>Timestamp de início da operação atual</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Tempo de operação em segundos</summary>
    public double? UptimeSeconds { get; set; }

    /// <summary>Estado de cada leitor</summary>
    public List<LeitorStatus> Leitores { get; set; } = new();

    /// <summary>Contadores gerais</summary>
    public BoxCounters Counters { get; set; } = new();

    /// <summary>Timestamp do status</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Estado de um leitor específico
/// </summary>
public class LeitorStatus
{
    /// <summary>ID do leitor</summary>
    public int Id { get; set; }

    /// <summary>IP do leitor</summary>
    public string Ip { get; set; } = "";

    /// <summary>Estado da conexão</summary>
    public ReaderConnectionState State { get; set; } = ReaderConnectionState.Disconnected;

    /// <summary>Mensagem de erro (se houver)</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Antenas ativas</summary>
    public int[] ActiveAntennas { get; set; } = [];

    /// <summary>Total de leituras RF deste leitor</summary>
    public int TotalReads { get; set; }
}

/// <summary>
/// Contadores do BOX
/// </summary>
public class BoxCounters
{
    /// <summary>Total de leituras RF brutas</summary>
    public int TotalRfReads { get; set; }

    /// <summary>Total de tags únicas lidas</summary>
    public int UniqueTags { get; set; }

    /// <summary>Total de eventos de passagem gerados</summary>
    public int TotalEvents { get; set; }

    /// <summary>Eventos pendentes de envio</summary>
    public int PendingSync { get; set; }
}
