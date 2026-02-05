using System.Collections.Concurrent;
using Impinj.OctaneSdk;
using RfidReaderRaspberry.Models;

namespace RfidReaderRaspberry.Services;

/// <summary>
/// Serviço singleton que gerencia os leitores RFID
/// </summary>
public class RfidService : IDisposable
{
    private readonly ILogger<RfidService> _logger;
    private readonly BoxConfig _config;
    private readonly ConcurrentDictionary<int, ReaderInstance> _readers = new();
    private readonly ConcurrentDictionary<string, int> _tagCounts = new();
    private readonly object _lock = new();

    private BoxOperationMode _currentMode = BoxOperationMode.Stopped;
    private DateTime? _startedAt;
    private DateTime? _stoppedAt;
    private int _totalRfReads;

    public event Action<string, int, int>? OnTagRead; // epc, readerId, antenna

    public RfidService(ILogger<RfidService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _config = configuration.GetSection("Box").Get<BoxConfig>() ?? new BoxConfig();
    }

    public BoxConfig Config => _config;
    public BoxOperationMode CurrentMode => _currentMode;
    public DateTime? StartedAt => _startedAt;
    public int TotalRfReads => _totalRfReads;
    public int UniqueTagCount => _tagCounts.Count;

    /// <summary>
    /// Atualiza a configuração do BOX
    /// </summary>
    public void UpdateConfig(BoxConfig newConfig)
    {
        if (_currentMode != BoxOperationMode.Stopped)
            throw new InvalidOperationException("Pare a leitura antes de alterar configuração");

        _config.EventoId = newConfig.EventoId;
        _config.Checkpoint = newConfig.Checkpoint;
        _config.Leitores = newConfig.Leitores;
        _config.ConsolidationWindowMs = newConfig.ConsolidationWindowMs;

        _logger.LogInformation("Configuração atualizada: Evento={EventoId}, Checkpoint={Checkpoint}",
            _config.EventoId, _config.Checkpoint);
    }

    /// <summary>
    /// Inicia modo de teste (conta tags sem persistir)
    /// </summary>
    public async Task StartTestAsync()
    {
        if (_currentMode != BoxOperationMode.Stopped)
            throw new InvalidOperationException($"BOX está em modo {_currentMode}");

        _logger.LogInformation("Iniciando modo TESTE...");

        ClearCounters();
        await ConnectReadersAsync();

        _currentMode = BoxOperationMode.Testing;
        _startedAt = DateTime.UtcNow;

        StartAllReaders();

        _logger.LogInformation("Modo TESTE iniciado");
    }

    /// <summary>
    /// Inicia modo de leitura (gera eventos de passagem)
    /// </summary>
    public async Task StartReadingAsync()
    {
        if (_currentMode != BoxOperationMode.Stopped)
            throw new InvalidOperationException($"BOX está em modo {_currentMode}");

        if (string.IsNullOrEmpty(_config.EventoId) || string.IsNullOrEmpty(_config.Checkpoint))
            throw new InvalidOperationException("Configure EventoId e Checkpoint antes de iniciar");

        _logger.LogInformation("Iniciando modo LEITURA...");

        ClearCounters();
        await ConnectReadersAsync();

        _currentMode = BoxOperationMode.Reading;
        _startedAt = DateTime.UtcNow;

        StartAllReaders();

        _logger.LogInformation("Modo LEITURA iniciado: Evento={EventoId}, Checkpoint={Checkpoint}",
            _config.EventoId, _config.Checkpoint);
    }

    /// <summary>
    /// Para a leitura/teste
    /// </summary>
    public void Stop()
    {
        if (_currentMode == BoxOperationMode.Stopped)
            return;

        _logger.LogInformation("Parando leitura...");

        _stoppedAt = DateTime.UtcNow;

        StopAllReaders();
        DisconnectAll();

        _currentMode = BoxOperationMode.Stopped;

        _logger.LogInformation("Leitura parada. Total RF reads: {Total}, Tags únicas: {Unique}",
            _totalRfReads, _tagCounts.Count);
    }

    /// <summary>
    /// Retorna resultados do modo teste (simples)
    /// </summary>
    public Dictionary<string, int> GetTestResults()
    {
        lock (_lock)
        {
            return new Dictionary<string, int>(_tagCounts);
        }
    }

    /// <summary>
    /// Retorna resultados detalhados do modo teste (por leitor e antena)
    /// </summary>
    public TestResults GetDetailedTestResults()
    {
        var duracao = 0.0;
        if (_startedAt.HasValue)
        {
            var end = _stoppedAt ?? DateTime.UtcNow;
            duracao = (end - _startedAt.Value).TotalSeconds;
        }

        var results = new TestResults
        {
            Mode = _currentMode,
            DuracaoSegundos = Math.Round(duracao, 1),
            TotalRfReads = _totalRfReads,
            UniqueTags = _tagCounts.Count
        };

        // Resultados por leitor
        foreach (var kvp in _readers)
        {
            var reader = kvp.Value;
            var leitorResult = new LeitorTestResult
            {
                Id = kvp.Key,
                Ip = reader.Ip,
                TotalReads = reader.TotalReads
            };

            // Resultados por antena
            foreach (var antenaKvp in reader.AntennaReads)
            {
                leitorResult.Antenas.Add(new AntennaCount
                {
                    Numero = antenaKvp.Key,
                    Reads = antenaKvp.Value
                });
            }

            // Ordenar antenas por número
            leitorResult.Antenas = leitorResult.Antenas.OrderBy(a => a.Numero).ToList();

            results.Leitores.Add(leitorResult);
        }

        // Tags com contagem (ordenadas por contagem decrescente)
        lock (_lock)
        {
            results.Tags = _tagCounts
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new TagCount { Epc = kv.Key, Count = kv.Value })
                .ToList();
        }

        return results;
    }

    /// <summary>
    /// Retorna o status atual do BOX
    /// </summary>
    public BoxStatus GetStatus()
    {
        var status = new BoxStatus
        {
            BoxId = _config.BoxId,
            Mode = _currentMode,
            EventoId = _config.EventoId,
            Checkpoint = _config.Checkpoint,
            StartedAt = _startedAt,
            UptimeSeconds = _startedAt.HasValue ? (DateTime.UtcNow - _startedAt.Value).TotalSeconds : null,
            Timestamp = DateTime.UtcNow,
            Counters = new BoxCounters
            {
                TotalRfReads = _totalRfReads,
                UniqueTags = _tagCounts.Count,
                TotalEvents = 0, // TODO: implementar quando tiver persistência
                PendingSync = 0
            }
        };

        foreach (var leitorConfig in _config.Leitores)
        {
            var leitorStatus = new LeitorStatus
            {
                Id = leitorConfig.Id,
                Ip = leitorConfig.Ip,
                ActiveAntennas = leitorConfig.Antennas
            };

            if (_readers.TryGetValue(leitorConfig.Id, out var reader))
            {
                leitorStatus.State = reader.State;
                leitorStatus.ErrorMessage = reader.ErrorMessage;
                leitorStatus.TotalReads = reader.TotalReads;
            }

            status.Leitores.Add(leitorStatus);
        }

        return status;
    }

    private async Task ConnectReadersAsync()
    {
        var leitoresHabilitados = _config.Leitores.Where(l => l.Enabled).ToList();

        if (leitoresHabilitados.Count == 0)
            throw new InvalidOperationException("Nenhum leitor configurado");

        foreach (var leitorConfig in leitoresHabilitados)
        {
            var reader = new ReaderInstance(leitorConfig, _logger);
            reader.OnTagsReported += HandleTagsReported;

            try
            {
                await Task.Run(() => reader.Connect());
                _readers[leitorConfig.Id] = reader;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar leitor {Id} ({Ip})", leitorConfig.Id, leitorConfig.Ip);
                reader.State = ReaderConnectionState.Error;
                reader.ErrorMessage = ex.Message;
                _readers[leitorConfig.Id] = reader;
            }
        }

        if (_readers.Values.All(r => r.State == ReaderConnectionState.Error))
            throw new InvalidOperationException("Nenhum leitor conectado com sucesso");
    }

    private void StartAllReaders()
    {
        foreach (var reader in _readers.Values.Where(r => r.State == ReaderConnectionState.Connected))
        {
            reader.Start();
        }
    }

    private void StopAllReaders()
    {
        foreach (var reader in _readers.Values)
        {
            try { reader.Stop(); } catch { }
        }
    }

    private void DisconnectAll()
    {
        foreach (var reader in _readers.Values)
        {
            try { reader.Disconnect(); } catch { }
        }
        _readers.Clear();
    }

    private void ClearCounters()
    {
        _tagCounts.Clear();
        Interlocked.Exchange(ref _totalRfReads, 0);
    }

    private void HandleTagsReported(int readerId, List<TagData> tags)
    {
        foreach (var tag in tags)
        {
            lock (_lock)
            {
                if (_tagCounts.ContainsKey(tag.Epc))
                    _tagCounts[tag.Epc]++;
                else
                    _tagCounts[tag.Epc] = 1;
            }

            Interlocked.Increment(ref _totalRfReads);
            OnTagRead?.Invoke(tag.Epc, readerId, tag.Antenna);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}

/// <summary>
/// Dados de uma tag lida
/// </summary>
public class TagData
{
    public string Epc { get; set; } = "";
    public int Antenna { get; set; }
    public double Rssi { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Instância de um leitor RFID
/// </summary>
internal class ReaderInstance
{
    private readonly LeitorConfig _config;
    private readonly ILogger _logger;
    private readonly ImpinjReader _reader;
    private volatile bool _isReading;
    private readonly ConcurrentDictionary<int, int> _antennaReads = new();

    public ReaderConnectionState State { get; set; } = ReaderConnectionState.Disconnected;
    public string? ErrorMessage { get; set; }
    public int TotalReads { get; private set; }
    public string Ip => _config.Ip;
    public IReadOnlyDictionary<int, int> AntennaReads => _antennaReads;

    public event Action<int, List<TagData>>? OnTagsReported;

    public ReaderInstance(LeitorConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _reader = new ImpinjReader();
    }

    public void Connect()
    {
        State = ReaderConnectionState.Connecting;
        _logger.LogInformation("Conectando ao leitor {Id} ({Ip})...", _config.Id, _config.Ip);

        _reader.Connect(_config.Ip);

        var settings = _reader.QueryDefaultSettings();

        // Modo de inventário
        if (_config.Mode == InventoryMode.SingleTarget)
        {
            settings.SearchMode = SearchMode.SingleTarget;
            settings.Session = 0;
            settings.TagPopulationEstimate = 16;
        }
        else
        {
            settings.SearchMode = SearchMode.DualTarget;
            settings.Session = 1;
            settings.TagPopulationEstimate = 32;
        }

        // Relatório
        settings.Report.Mode = ReportMode.Individual;
        settings.Report.IncludeFirstSeenTime = true;
        settings.Report.IncludeAntennaPortNumber = true;
        settings.Report.IncludePeakRssi = true;

        // Antenas
        settings.Antennas.DisableAll();
        var antennas = _config.Antennas.Select(a => (ushort)a).ToArray();
        settings.Antennas.EnableById(antennas);

        // Performance
        try { settings.ReaderMode = ReaderMode.MaxThroughput; } catch { }

        _reader.ApplySettings(settings);
        _reader.TagsReported += OnReaderTagsReported;

        State = ReaderConnectionState.Connected;
        _logger.LogInformation("Leitor {Id} conectado com sucesso", _config.Id);
    }

    public void Start()
    {
        _isReading = true;
        _reader.Start();
    }

    public void Stop()
    {
        _isReading = false;
        try { _reader.Stop(); } catch { }
    }

    public void Disconnect()
    {
        try { _reader.Disconnect(); } catch { }
        State = ReaderConnectionState.Disconnected;
    }

    private void OnReaderTagsReported(ImpinjReader sender, TagReport report)
    {
        if (!_isReading) return;

        var tags = new List<TagData>();

        foreach (Tag tag in report)
        {
            var antenna = tag.AntennaPortNumber;

            tags.Add(new TagData
            {
                Epc = tag.Epc.ToString(),
                Antenna = antenna,
                Rssi = tag.PeakRssiInDbm,
                Timestamp = DateTime.UtcNow
            });

            TotalReads++;

            // Contagem por antena
            _antennaReads.AddOrUpdate(antenna, 1, (_, count) => count + 1);
        }

        if (tags.Count > 0)
        {
            OnTagsReported?.Invoke(_config.Id, tags);
        }
    }
}
