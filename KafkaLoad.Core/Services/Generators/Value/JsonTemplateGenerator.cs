using KafkaLoad.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace KafkaLoad.Core.Services.Generators.Value;

public class JsonTemplateGenerator : IDataGenerator
{
    private static readonly Regex PlaceholderRegex =
        new(@"\$\{(\w+)(?::(-?\d+))?(?::(-?\d+))?\}", RegexOptions.Compiled);

    private static readonly string Alphanumeric =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private readonly IReadOnlyList<Segment> _segments;

    public JsonTemplateGenerator(string template)
    {
        if (string.IsNullOrEmpty(template))
            throw new ArgumentException("JSON template must not be null or empty.", nameof(template));

        _segments = Parse(template);
    }

    public byte[]? Next()
    {
        var sb = new StringBuilder();
        foreach (var seg in _segments)
        {
            if (seg is LiteralSegment lit)
                sb.Append(lit.Text);
            else if (seg is PlaceholderSegment ph)
                sb.Append(Resolve(ph));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Resolve(PlaceholderSegment ph) => ph.Kind switch
    {
        PlaceholderKind.Uuid         => Guid.NewGuid().ToString(),
        PlaceholderKind.RandomInt    => Random.Shared.Next(ph.Param1, ph.Param2).ToString(),
        PlaceholderKind.RandomString => RandomString(ph.Param1),
        PlaceholderKind.Timestamp    => DateTime.UtcNow.ToString("o"),
        PlaceholderKind.RandomBool   => Random.Shared.Next(2) == 0 ? "true" : "false",
        _                            => ph.Raw
    };

    private static string RandomString(int length)
    {
        if (length <= 0) return string.Empty;
        var chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = Alphanumeric[Random.Shared.Next(Alphanumeric.Length)];
        return new string(chars);
    }

    private static List<Segment> Parse(string template)
    {
        var segments = new List<Segment>();
        int lastIndex = 0;

        foreach (Match match in PlaceholderRegex.Matches(template))
        {
            if (match.Index > lastIndex)
                segments.Add(new LiteralSegment(template[lastIndex..match.Index]));

            var name = match.Groups[1].Value.ToLowerInvariant();
            var p1str = match.Groups[2].Success ? match.Groups[2].Value : null;
            var p2str = match.Groups[3].Success ? match.Groups[3].Value : null;

            PlaceholderKind kind;
            int param1 = 0, param2 = int.MaxValue;

            switch (name)
            {
                case "uuid":
                    kind = PlaceholderKind.Uuid;
                    break;
                case "randomint":
                    kind = PlaceholderKind.RandomInt;
                    if (p1str != null) param1 = int.Parse(p1str);
                    if (p2str != null) param2 = int.Parse(p2str);
                    break;
                case "randomstring":
                    kind = PlaceholderKind.RandomString;
                    param1 = p1str != null ? int.Parse(p1str) : 10;
                    break;
                case "timestamp":
                    kind = PlaceholderKind.Timestamp;
                    break;
                case "randombool":
                    kind = PlaceholderKind.RandomBool;
                    break;
                default:
                    // Unknown placeholder — emit as literal
                    segments.Add(new LiteralSegment(match.Value));
                    lastIndex = match.Index + match.Length;
                    continue;
            }

            segments.Add(new PlaceholderSegment(kind, param1, param2, match.Value));
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < template.Length)
            segments.Add(new LiteralSegment(template[lastIndex..]));

        return segments;
    }

    // --- Segment types ---

    private abstract record Segment;
    private record LiteralSegment(string Text) : Segment;
    private record PlaceholderSegment(PlaceholderKind Kind, int Param1, int Param2, string Raw) : Segment;

    private enum PlaceholderKind { Uuid, RandomInt, RandomString, Timestamp, RandomBool, Unknown }
}
