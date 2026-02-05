namespace RfidReaderRaspberry.Models;

/// <summary>
/// Modo de inventário RFID
/// </summary>
public enum InventoryMode
{
    /// <summary>Single Target - Ideal para largada</summary>
    SingleTarget,

    /// <summary>Dual Target - Ideal para chegada</summary>
    DualTarget
}

/// <summary>
/// Estado de operação do BOX
/// </summary>
public enum BoxOperationMode
{
    /// <summary>Parado - Sem conexão com leitores</summary>
    Stopped,

    /// <summary>Modo teste - Conta leituras sem persistir</summary>
    Testing,

    /// <summary>Modo leitura - Gera eventos de passagem</summary>
    Reading
}

/// <summary>
/// Estado de conexão do leitor
/// </summary>
public enum ReaderConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Error
}
