using System.Collections.Generic;
using System.Threading.Tasks;

namespace EstoqueManager.Core
{
    public interface ILogService
    {
        Task LogAsync(string message);
        Task<List<string>> ReadLastLogsAsync(int count = 50);
    }
}
