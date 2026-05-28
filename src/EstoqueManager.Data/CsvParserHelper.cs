using System;
using System.Collections.Generic;
using System.Text;

namespace EstoqueManager.Data
{
    public static class CsvParserHelper
    {
        public static string[] SplitCsvLine(string line, char separator)
        {
            var list = new List<string>();
            var inQuotes = false;
            var current = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == separator && !inQuotes)
                {
                    list.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            list.Add(current.ToString());
            return list.ToArray();
        }
    }
}
