using System.Text;
using BoardGameTracker.Core.Rag.Interfaces;

namespace BoardGameTracker.Core.Rag;

public class RulebookChunker : IRulebookChunker
{
    private const int MaxChunkChars = 1000;
    private const int OverlapChars = 200;

    public IReadOnlyList<TextChunk> Chunk(IReadOnlyList<PdfPageText> pages)
    {
        var chunks = new List<TextChunk>();
        var index = 0;

        foreach (var page in pages)
        {
            foreach (var content in SplitPage(page.Text))
            {
                chunks.Add(new TextChunk(index++, content, page.PageNumber));
            }
        }

        return chunks;
    }

    private static IEnumerable<string> SplitPage(string text)
    {
        var normalized = NormalizeWhitespace(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            yield break;
        }

        var start = 0;
        while (start < normalized.Length)
        {
            var length = Math.Min(MaxChunkChars, normalized.Length - start);
            var end = start + length;

            if (end < normalized.Length)
            {
                end = FindBoundary(normalized, start, end);
            }

            var chunk = normalized.Substring(start, end - start).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                yield return chunk;
            }

            if (end >= normalized.Length)
            {
                yield break;
            }

            start = Math.Max(end - OverlapChars, start + 1);
        }
    }

    private static int FindBoundary(string text, int start, int end)
    {
        var min = Math.Max(start + 1, end - OverlapChars);
        for (var i = end - 1; i >= min; i--)
        {
            var c = text[i];
            if (c == '\n')
            {
                return i + 1;
            }

            if ((c == '.' || c == '!' || c == '?') && (i + 1 >= text.Length || char.IsWhiteSpace(text[i + 1])))
            {
                return i + 1;
            }
        }

        return end;
    }

    private static string NormalizeWhitespace(string text)
    {
        var builder = new StringBuilder(text.Length);
        var lastWasSpace = false;

        foreach (var c in text)
        {
            if (c == '\r')
            {
                continue;
            }

            if (c == ' ' || c == '\t')
            {
                if (!lastWasSpace)
                {
                    builder.Append(' ');
                }

                lastWasSpace = true;
            }
            else
            {
                builder.Append(c);
                lastWasSpace = c == '\n';
            }
        }

        return builder.ToString().Trim();
    }
}
