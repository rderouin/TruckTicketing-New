using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TruckTicketingCLI;

public abstract class Mustache
{
    public static IEnumerable<string> GetContents(string value)
    {
        return GetTokens(value).Select(t => t.Content);
    }

    public static string ReplaceContents(string value, params string[] replacements)
    {
        return ReplaceContentsImpl(value, true, replacements);
    }

    private static IEnumerable<MustacheStringToken> GetTokens(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var inBraces = false;
        var sb = new StringBuilder();
        var startIndex = 0;
        var tokenIndex = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            // skip the first
            if (i == 0)
            {
                continue;
            }

            var target = string.Concat(value[i - 1], c);

            // starting a new token
            if (!inBraces && target == "{{")
            {
                inBraces = true;
                startIndex = i;
                continue;
            }

            if (inBraces)
            {
                // append content to result
                if (target != "}}")
                {
                    sb.Append(c);
                }
                // end current token
                else
                {
                    inBraces = false;
                    var content = sb.ToString();
                    content = content.Substring(0, content.Length - 1);
                    var trimmedContent = content.TrimStart();
                    var startWhiteSpaceCount = content.Length - trimmedContent.Length;
                    trimmedContent = content.TrimEnd();
                    var endWhiteSpaceCount = content.Length - trimmedContent.Length;
                    var startBraceIndex = startIndex - 1;
                    var startContextIndex = startIndex + 1 + startWhiteSpaceCount;
                    var endContextIndex = i - endWhiteSpaceCount - 1;
                    var token = new MustacheStringToken(tokenIndex++, content.Trim(), startBraceIndex, startContextIndex, i, endContextIndex);
                    yield return token;
                    sb = new();
                }
            }
        }
    }

    private static string ReplaceContentsImpl(string value,
                                              bool ignoreExtraReplacements,
                                              params string[] replacements)
    {
        var tokens = GetTokens(value).ToList();

        if (tokens.Count > replacements.Length)
        {
            throw new NotImplementedException();
        }

        if (tokens.Count != replacements.Length && !ignoreExtraReplacements)
        {
            throw new NotImplementedException();
        }

        var sb = new StringBuilder();
        var i = 0;
        var consumedTokens = new List<MustacheStringToken>();

        while (i < value.Length)
        {
            var tokenIdx = tokens.FindIndex(t => t.StartBraceIndex <= i && t.EndBraceIndex >= i);
            if (tokenIdx == -1)
            {
                sb.Append(value[i]);
                i++;
                continue;
            }

            var token = tokens[tokenIdx];
            if (consumedTokens.Contains(token))
            {
                i++;
                continue;
            }

            var replacement = replacements[token.Index];
            consumedTokens.Add(token);
            sb.Append(replacement);
            i++;
        }

        var result = sb.ToString();
        return result;
    }

    private readonly struct MustacheStringToken
    {
        public int Index { get; }

        public int StartBraceIndex { get; }

        public int StartContextIndex { get; }

        public int EndBraceIndex { get; }

        public int EndContextIndex { get; }

        public string Content { get; }

        public MustacheStringToken(int index,
                                   string content,
                                   int startBraceIndex,
                                   int startContextIndex,
                                   int endBraceIndex,
                                   int endContextIndex)
        {
            Index = index;
            Content = content;
            StartBraceIndex = startBraceIndex;
            StartContextIndex = startContextIndex;
            EndBraceIndex = endBraceIndex;
            EndContextIndex = endContextIndex;
        }
    }
}
