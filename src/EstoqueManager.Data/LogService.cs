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
}
