namespace EstoqueManager.Data;

/// <summary>
/// Serviço de auditoria e logging responsável pelo registro não volátil de ações críticas do sistema.
/// </summary>
public static class LogService
{
    private static readonly string LogFilePath = "app.log";

    /// <summary>
    /// Registra um evento de auditoria no arquivo local de log de forma assíncrona.
    /// </summary>
    /// <param name="message">Texto ou mensagem descritiva da ação executada.</param>
    public static async Task LogAsync(string message)
    {
        try
        {
            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            // Utiliza gravação assíncrona sem travar a thread de execução principal
            await File.AppendAllTextAsync(LogFilePath, logLine);
        }
        catch (Exception ex)
        {
            // Tratamento preventivo em caso de bloqueio de permissão de gravação de arquivos pelo sistema operacional
            Console.WriteLine($"[CRITICAL] Falha ao registrar log de auditoria: {ex.Message}");
        }
    }

    /// <summary>
    /// Lê as últimas linhas do arquivo de log de auditoria de forma assíncrona.
    /// </summary>
    /// <param name="count">Quantidade máxima de linhas a serem retornadas.</param>
    /// <returns>Uma lista contendo as últimas linhas de log registradas.</returns>
    public static async Task<List<string>> ReadLastLogsAsync(int count = 50)
    {
        try
        {
            if (!File.Exists(LogFilePath))
                return new List<string>();

            var lines = await File.ReadAllLinesAsync(LogFilePath);
            return lines.Skip(Math.Max(0, lines.Length - count)).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] Falha ao ler logs de auditoria: {ex.Message}");
            return new List<string> { $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERRO] Falha ao recuperar logs: {ex.Message}" };
        }
    }
}
