namespace RfidReaderRaspberry.Models;

/// <summary>
/// Configuração de um leitor RFID
/// </summary>
public class LeitorConfig
{
    /// <summary>Identificador do leitor (1 ou 2)</summary>
    public int Id { get; set; } = 1;

    /// <summary>IP do leitor RFID</summary>
    public string Ip { get; set; } = "192.168.0.242";

    /// <summary>Modo de inventário</summary>
    public InventoryMode Mode { get; set; } = InventoryMode.DualTarget;

    /// <summary>Antenas habilitadas (ex: [1], [1,2], [1,2,3,4])</summary>
    public int[] Antennas { get; set; } = [1];

    /// <summary>Potência de transmissão em dBm (opcional)</summary>
    public double? TxPowerDbm { get; set; }

    /// <summary>Habilitado para uso</summary>
    public bool Enabled { get; set; } = true;
}
