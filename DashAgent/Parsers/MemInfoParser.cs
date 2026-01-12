using System.Text.RegularExpressions;

namespace DashAgent.Parsers;

/// <summary>
/// Parser for /proc/meminfo files
/// </summary>
public static class MemInfoParser
{
    /// <summary>
    /// Location of the meminfo file
    /// </summary>
    public const string MeminfoFile = "/proc/meminfo";

    private const int BytesInKb = 1024;

    /// <summary>
    /// eg. "MemTotal:      12345 kB"
    /// </summary>
    private static readonly Regex LineRegex = new Regex(
        @"^(?<name>[^:]+):\s+(?<value>\d+) kB",
        RegexOptions.Multiline | RegexOptions.Compiled
    );

    /// <summary>
    /// Parses the /proc/meminfo file.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<(string field, ulong value)> Parse()
    {
        return Parse(File.ReadAllText(MeminfoFile));
    }

    /// <summary>
    /// Parses the specified contents of the meminfo file.
    /// </summary>
    /// <param name="file">Content to parse</param>
    /// <returns>Parsed rows</returns>
    public static IEnumerable<(string field, ulong value)> Parse(string file)
    {
        return LineRegex.Matches(file)
                         .Select(
                             match => (
                                 match.Groups["name"].Value.TrimStart(),
                                 ulong.Parse(match.Groups["value"].Value) * BytesInKb
                             )
                         );
    }
}