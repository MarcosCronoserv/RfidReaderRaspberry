namespace RfidReaderRaspberry.Models;

/// <summary>
/// Resultados detalhados do modo teste
/// </summary>
public class TestResults
{
    /// <summary>Modo atual (deve ser Stopped após parar o teste)</summary>
    public BoxOperationMode Mode { get; set; }

    /// <summary>Duração do teste em segundos</summary>
    public double DuracaoSegundos { get; set; }

    /// <summary>Total de leituras RF brutas</summary>
    public int TotalRfReads { get; set; }

    /// <summary>Total de tags únicas detectadas</summary>
    public int UniqueTags { get; set; }

    /// <summary>Resultados por leitor</summary>
    public List<LeitorTestResult> Leitores { get; set; } = new();

    /// <summary>Tags detectadas com contagem</summary>
    public List<TagCount> Tags { get; set; } = new();
}

/// <summary>
/// Resultado de teste por leitor
/// </summary>
public class LeitorTestResult
{
    /// <summary>ID do leitor</summary>
    public int Id { get; set; }

    /// <summary>IP do leitor</summary>
    public string Ip { get; set; } = "";

    /// <summary>Total de leituras deste leitor</summary>
    public int TotalReads { get; set; }

    /// <summary>Leituras por antena</summary>
    public List<AntennaCount> Antenas { get; set; } = new();
}

/// <summary>
/// Contagem de leituras por antena
/// </summary>
public class AntennaCount
{
    /// <summary>Número da antena</summary>
    public int Numero { get; set; }

    /// <summary>Quantidade de leituras</summary>
    public int Reads { get; set; }
}

/// <summary>
/// Contagem de leituras por tag
/// </summary>
public class TagCount
{
    /// <summary>EPC da tag</summary>
    public string Epc { get; set; } = "";

    /// <summary>Quantidade de leituras</summary>
    public int Count { get; set; }
}
