using System.Text.Json;
using RfidReaderRaspberry.Models;

namespace RfidReaderRaspberry.Services;

/// <summary>
/// Serviço de log de operações
/// Persiste em arquivo JSON (será migrado para SQLite depois)
/// </summary>
public class LogService
{
    private readonly ILogger<LogService> _logger;
    private readonly string _logFilePath;
    private readonly List<OperationLog> _logs = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    public LogService(ILogger<LogService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Caminho do arquivo de log
        var dataPath = configuration["DataPath"] ?? "data";
        Directory.CreateDirectory(dataPath);
        _logFilePath = Path.Combine(dataPath, "operation_log.json");

        LoadLogs();
    }

    /// <summary>
    /// Registra uma operação de configuração
    /// </summary>
    public void LogConfig(string? eventoId, string? checkpoint, int leitoresCount)
    {
        var details = new
        {
            eventoId,
            checkpoint,
            leitores = leitoresCount
        };

        AddLog(OperationType.Config, details, eventoId, checkpoint);
    }

    /// <summary>
    /// Registra início de teste
    /// </summary>
    public void LogTestStart(string? eventoId, string? checkpoint)
    {
        AddLog(OperationType.TestStart, new { }, eventoId, checkpoint);
    }

    /// <summary>
    /// Registra fim de teste com resultados
    /// </summary>
    public void LogTestStop(TestResults results, string? eventoId, string? checkpoint)
    {
        var details = new
        {
            duracao_seg = results.DuracaoSegundos,
            total_reads = results.TotalRfReads,
            unique_tags = results.UniqueTags,
            leitores = results.Leitores.Select(l => new
            {
                id = l.Id,
                ip = l.Ip,
                total = l.TotalReads,
                antenas = l.Antenas.ToDictionary(a => a.Numero.ToString(), a => a.Reads)
            })
        };

        AddLog(OperationType.TestStop, details, eventoId, checkpoint);
    }

    /// <summary>
    /// Registra início de leitura
    /// </summary>
    public void LogStart(string? eventoId, string? checkpoint)
    {
        AddLog(OperationType.Start, new { eventoId, checkpoint }, eventoId, checkpoint);
    }

    /// <summary>
    /// Registra fim de leitura
    /// </summary>
    public void LogStop(double duracaoSeg, int totalEventos, string? eventoId, string? checkpoint)
    {
        var details = new
        {
            duracao_seg = duracaoSeg,
            total_eventos = totalEventos
        };

        AddLog(OperationType.Stop, details, eventoId, checkpoint);
    }

    /// <summary>
    /// Registra erro
    /// </summary>
    public void LogError(string message, string? eventoId = null, string? checkpoint = null)
    {
        AddLog(OperationType.Error, new { message }, eventoId, checkpoint);
    }

    /// <summary>
    /// Retorna os últimos N registros do log
    /// </summary>
    public List<OperationLog> GetLogs(int? last = null)
    {
        lock (_lock)
        {
            var logs = _logs.OrderByDescending(l => l.Timestamp).AsEnumerable();

            if (last.HasValue && last.Value > 0)
            {
                logs = logs.Take(last.Value);
            }

            return logs.ToList();
        }
    }

    /// <summary>
    /// Limpa o log
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _logs.Clear();
            _nextId = 1;
            SaveLogs();
        }
    }

    private void AddLog(string operation, object details, string? eventoId, string? checkpoint)
    {
        var log = new OperationLog
        {
            Timestamp = DateTime.UtcNow,
            Operation = operation,
            Details = JsonSerializer.Serialize(details),
            EventoId = eventoId,
            Checkpoint = checkpoint
        };

        lock (_lock)
        {
            log.Id = _nextId++;
            _logs.Add(log);
            SaveLogs();
        }

        _logger.LogInformation("LOG: {Operation} - Evento={EventoId}, Checkpoint={Checkpoint}",
            operation, eventoId ?? "-", checkpoint ?? "-");
    }

    private void LoadLogs()
    {
        try
        {
            if (File.Exists(_logFilePath))
            {
                var json = File.ReadAllText(_logFilePath);
                var logs = JsonSerializer.Deserialize<List<OperationLog>>(json);
                if (logs != null)
                {
                    _logs.AddRange(logs);
                    _nextId = _logs.Any() ? _logs.Max(l => l.Id) + 1 : 1;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao carregar log de operações");
        }
    }

    private void SaveLogs()
    {
        try
        {
            var json = JsonSerializer.Serialize(_logs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_logFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao salvar log de operações");
        }
    }
}
