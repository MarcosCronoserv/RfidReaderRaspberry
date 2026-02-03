using System;
using Impinj.OctaneSdk;

namespace RfidReaderRaspberry
{
    /// <summary>
    /// Modo de inventario RFID.
    /// </summary>
    public enum InventoryMode
    {
        SingleTarget,   // Ideal para largada
        DualTarget      // Ideal para chegada
    }

    /// <summary>
    /// Wrapper para o leitor RFID Impinj.
    /// Otimizado para performance e compativel com Mono.
    /// </summary>
    public class RfidReader : IDisposable
    {
        private readonly ImpinjReader _reader;
        private readonly TagCounter _tagCounter;
        private volatile bool _isReading;
        private bool _disposed;

        public event Action<string> OnTagRead;
        public event Action<int> OnBatchRead;

        public RfidReader(TagCounter tagCounter)
        {
            _reader = new ImpinjReader();
            _tagCounter = tagCounter;
            _isReading = false;
            _disposed = false;
        }

        public bool IsReading
        {
            get { return _isReading; }
        }

        public void Connect(string hostname)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfidReader));

            Console.WriteLine("Conectando ao leitor...");
            _reader.Connect(hostname);
            Console.WriteLine("Conectado com sucesso!");
        }

        public void Configure(InventoryMode mode, ushort[] antennas = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfidReader));

            Settings settings = _reader.QueryDefaultSettings();

            // Configuracao do modo de inventario
            ApplyInventoryMode(settings, mode);

            // Relatorio enxuto para maxima performance
            settings.Report.Mode = ReportMode.Individual;
            settings.Report.IncludeFirstSeenTime = true;

            // Configuracao de antenas
            settings.Antennas.DisableAll();
            if (antennas == null || antennas.Length == 0)
            {
                antennas = new ushort[] { 1 };
            }
            settings.Antennas.EnableById(antennas);

            // Performance maxima
            try
            {
                settings.ReaderMode = ReaderMode.MaxThroughput;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Aviso: Nao foi possivel definir MaxThroughput: " + ex.Message);
            }

            Console.WriteLine("\nAplicando configuracoes...");
            _reader.ApplySettings(settings);
            Console.WriteLine("Configuracoes aplicadas!");

            // Registra handler de eventos
            _reader.TagsReported += OnTagsReported;
        }

        private void ApplyInventoryMode(Settings settings, InventoryMode mode)
        {
            if (mode == InventoryMode.SingleTarget)
            {
                settings.SearchMode = SearchMode.SingleTarget;
                settings.Session = 0;
                settings.TagPopulationEstimate = 16;

                Console.WriteLine("\nConfiguracao RF:");
                Console.WriteLine("  Single Target");
                Console.WriteLine("  Session 0");
                Console.WriteLine("  Population 16");
            }
            else
            {
                settings.SearchMode = SearchMode.DualTarget;
                settings.Session = 1;
                settings.TagPopulationEstimate = 32;

                Console.WriteLine("\nConfiguracao RF:");
                Console.WriteLine("  Dual Target");
                Console.WriteLine("  Session 1");
                Console.WriteLine("  Population 32");
            }
        }

        public void StartReading()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfidReader));

            _isReading = true;
            _reader.Start();
        }

        public void StopReading()
        {
            _isReading = false;
            if (!_disposed)
            {
                _reader.Stop();
            }
        }

        private void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            if (!_isReading)
                return;

            int batchCount = 0;

            foreach (Tag tag in report)
            {
                string epc = tag.Epc.ToString();
                _tagCounter.AddTag(epc);
                batchCount++;

                // Notifica evento individual (opcional)
                if (OnTagRead != null)
                {
                    OnTagRead(epc);
                }
            }

            // Notifica evento de lote
            if (OnBatchRead != null && batchCount > 0)
            {
                OnBatchRead(batchCount);
            }
        }

        public void Disconnect()
        {
            if (!_disposed)
            {
                _reader.Disconnect();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _isReading = false;
                try
                {
                    _reader.Stop();
                }
                catch { }
                try
                {
                    _reader.Disconnect();
                }
                catch { }
            }
        }
    }
}
