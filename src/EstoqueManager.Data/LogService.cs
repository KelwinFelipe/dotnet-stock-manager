using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstoqueManager.Core;

namespace EstoqueManager.Data
{
    /// <summary>
    /// Serviço de auditoria e logging responsável pelo registro não volátil de ações críticas do sistema.
    /// </summary>
    public class LogService : ILogService
    {
        private readonly string _logFilePath = Path.Combine("data", "app.log");
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public LogService()
        {
            if (!Directory.Exists("data"))
            {
                Directory.CreateDirectory("data");
            }
        }

        /// <summary>
        /// Registra um evento de auditoria no arquivo local de log de forma assíncrona.
        /// </summary>
        /// <param name="message">Texto ou mensagem descritiva da ação executada.</param>
        public async Task LogAsync(string message)
        {
            await _semaphore.WaitAsync();
            try
            {
                var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                // Utiliza gravação assíncrona sem travar a thread de execução principal
                await File.AppendAllTextAsync(_logFilePath, logLine);
            }
            catch (Exception ex)
            {
                // Tratamento preventivo em caso de bloqueio de permissão de gravação de arquivos pelo sistema operacional
                Console.WriteLine($"[CRITICAL] Falha ao registrar log de auditoria: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Lê as últimas linhas do arquivo de log de auditoria de forma assíncrona.
        /// </summary>
        /// <param name="count">Quantidade máxima de linhas a serem retornadas.</param>
        /// <returns>Uma lista contendo as últimas linhas de log registradas.</returns>
        public async Task<List<string>> ReadLastLogsAsync(int count = 50)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(_logFilePath))
                    return new List<string>();

                var lines = await File.ReadAllLinesAsync(_logFilePath);
                return lines.Skip(Math.Max(0, lines.Length - count)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL] Falha ao ler logs de auditoria: {ex.Message}");
                return new List<string> { $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERRO] Falha ao recuperar logs: {ex.Message}" };
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
