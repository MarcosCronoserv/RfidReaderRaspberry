using System;
using System.Threading;

namespace RfidReaderRaspberry
{
    /// <summary>
    /// Leitor RFID para Raspberry Pi via Mono.
    /// Build: Windows (.NET Framework 4.8)
    /// Execucao: Raspberry Pi (Mono)
    /// </summary>
    class Program
    {
        // Configuracoes padrao
        private const string DEFAULT_READER_IP = "192.168.0.242";
        private const int DEFAULT_READ_DURATION = 30;

        static void Main(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("  RFID Reader - Raspberry Pi Edition");
            Console.WriteLine("  Mono Compatible");
            Console.WriteLine("===========================================\n");

            string readerIp = DEFAULT_READER_IP;
            int duration = DEFAULT_READ_DURATION;

            // Processa argumentos de linha de comando
            if (args.Length >= 1)
            {
                readerIp = args[0];
            }
            if (args.Length >= 2)
            {
                int.TryParse(args[1], out duration);
                if (duration <= 0) duration = DEFAULT_READ_DURATION;
            }

            Console.WriteLine("IP do Leitor: " + readerIp);
            Console.WriteLine("Duracao da leitura: " + duration + " segundos\n");

            TagCounter tagCounter = new TagCounter();
            RfidReader reader = null;

            try
            {
                reader = new RfidReader(tagCounter);

                // Conecta ao leitor
                reader.Connect(readerIp);

                // Pergunta modo de inventario
                InventoryMode mode = AskInventoryMode();

                // Configura o leitor
                reader.Configure(mode);

                // Aguarda usuario
                Console.WriteLine("\nPressione ENTER para iniciar leitura de " + duration + " segundos...");
                Console.ReadLine();

                // Limpa contadores
                tagCounter.Clear();
                DateTime startTime = DateTime.Now;

                // Inicia leitura
                reader.StartReading();

                // Loop de monitoramento
                for (int i = 0; i < duration; i++)
                {
                    Thread.Sleep(1000);
                    PrintProgress(i + 1, tagCounter.UniqueTagCount, tagCounter.TotalReads);
                }

                // Para leitura
                reader.StopReading();

                // Exibe resultados
                PrintResults(startTime, tagCounter);

                // Desconecta
                reader.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERRO: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Detalhe: " + ex.InnerException.Message);
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }

            Console.WriteLine("\nEncerrado. Pressione qualquer tecla.");
            try
            {
                Console.ReadKey();
            }
            catch
            {
                // No Mono/Linux pode nao ter suporte a ReadKey em alguns terminais
                Console.ReadLine();
            }
        }

        static InventoryMode AskInventoryMode()
        {
            Console.WriteLine("\nEscolha o modo de inventario:");
            Console.WriteLine("1 - Single Target (Largada)");
            Console.WriteLine("2 - Dual Target (Chegada)");
            Console.Write("Opcao: ");

            while (true)
            {
                string input = Console.ReadLine();

                if (input == "1")
                {
                    Console.WriteLine("\nModo selecionado: SINGLE TARGET");
                    return InventoryMode.SingleTarget;
                }
                else if (input == "2")
                {
                    Console.WriteLine("\nModo selecionado: DUAL TARGET");
                    return InventoryMode.DualTarget;
                }
                else
                {
                    Console.Write("Opcao invalida. Digite 1 ou 2: ");
                }
            }
        }

        static void PrintProgress(int seconds, int uniqueTags, int totalReads)
        {
            // Usa \r para sobrescrever a linha (compativel com Mono)
            string line = string.Format(
                "\rTempo: {0:D2}s | Tags unicas: {1:D3} | Total leituras: {2:D5}",
                seconds, uniqueTags, totalReads
            );
            Console.Write(line);
        }

        static void PrintResults(DateTime startTime, TagCounter tagCounter)
        {
            TimeSpan duration = DateTime.Now - startTime;
            int totalReads = tagCounter.TotalReads;
            int uniqueTags = tagCounter.UniqueTagCount;
            double rps = totalReads / duration.TotalSeconds;

            Console.WriteLine("\n");
            Console.WriteLine("========== RESULTADO ==========");
            Console.WriteLine("Duracao: " + duration.TotalSeconds.ToString("F1") + "s");
            Console.WriteLine("Tags unicas: " + uniqueTags);
            Console.WriteLine("Total leituras: " + totalReads);
            Console.WriteLine("Taxa media: " + rps.ToString("F1") + " reads/s");

            if (uniqueTags > 0)
            {
                double avg = tagCounter.GetAverageReadsPerTag();
                Console.WriteLine("Media por tag: " + avg.ToString("F1"));
            }

            Console.WriteLine("================================");
        }
    }
}
