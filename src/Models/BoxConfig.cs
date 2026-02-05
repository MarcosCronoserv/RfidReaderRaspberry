namespace RfidReaderRaspberry.Models;

/// <summary>
/// Configuração geral do BOX
/// </summary>
public class BoxConfig
{
    /// <summary>ID único do BOX</summary>
    public string BoxId { get; set; } = "BOX-01";

    /// <summary>ID do evento atual</summary>
    public string? EventoId { get; set; }

    /// <summary>Tipo do ponto de controle (P01, C01, V01, etc.)</summary>
    public string? Checkpoint { get; set; }

    /// <summary>Lista de leitores configurados (1 ou 2)</summary>
    public List<LeitorConfig> Leitores { get; set; } = new();

    /// <summary>
    /// Janela de tempo (ms) para consolidar leituras da mesma tag
    /// Leituras da mesma tag dentro dessa janela são consideradas uma única passagem
    /// </summary>
    public int ConsolidationWindowMs { get; set; } = 5000;

    /// <summary>Porta HTTP da API</summary>
    public int HttpPort { get; set; } = 5000;
}
