using System.Text;

namespace BoardGameTracker.Core.Games.Import;

public static class CsvReader
{
    public static List<string[]> Read(string text)
    {
        var separator = DetectSeparator(text);
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var i = 0;

        while (i < text.Length)
        {
            var c = text[i];
            if (c == '"')
            {
                i = ReadQuoted(text, i + 1, field);
                continue;
            }

            if (c == separator)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (c is '\r' or '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                rows.Add(row.ToArray());
                row.Clear();
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
            }
            else
            {
                field.Append(c);
            }

            i++;
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row.ToArray());
        }

        return rows;
    }

    private static int ReadQuoted(string text, int start, StringBuilder field)
    {
        var i = start;
        while (i < text.Length)
        {
            if (text[i] != '"')
            {
                field.Append(text[i]);
                i++;
            }
            else if (i + 1 < text.Length && text[i + 1] == '"')
            {
                field.Append('"');
                i += 2;
            }
            else
            {
                return i + 1;
            }
        }

        return i;
    }

    private static char DetectSeparator(string text)
    {
        var commas = 0;
        var semicolons = 0;
        var inQuotes = false;
        foreach (var c in text)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && c is '\r' or '\n')
            {
                break;
            }
            else if (!inQuotes && c == ',')
            {
                commas++;
            }
            else if (!inQuotes && c == ';')
            {
                semicolons++;
            }
        }

        return semicolons > commas ? ';' : ',';
    }
}
