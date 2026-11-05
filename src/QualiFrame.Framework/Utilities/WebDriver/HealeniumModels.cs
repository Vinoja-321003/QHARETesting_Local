using System.Text.Json;
using System.Text.Json.Serialization;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Official hlm-backend selector payload (POST /healenium).
    /// </summary>
    public sealed class HealeniumSelectorRequest
    {
        public string? Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public List<List<HealeniumNode>> NodePath { get; set; } = new();
        public List<string> ElementIds { get; set; } = new();
        public string? SessionId { get; set; }
        public bool EnableHealing { get; set; } = true;
        public bool UrlKey { get; set; }
        public string Locator { get; set; } = string.Empty;
        public string ClassName { get; set; } = "QualiFrame.Tests";
        public string MethodName { get; set; } = "FindElement";
        public string Command { get; set; } = "findElement";
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// Selector row returned by GET /healenium/selector/all.
    /// </summary>
    public sealed class HealeniumStoredSelector
    {
        public string? Locator { get; set; }
        public string? Type { get; set; }
        public string? Url { get; set; }
        public string? ClassName { get; set; }
        public string? MethodName { get; set; }
        public string? Command { get; set; }
    }

    /// <summary>
    /// Official hlm-backend healing payload (POST /healenium/healing).
    /// </summary>
    public sealed class HealeniumHealingRequest
    {
        public string Locator { get; set; } = string.Empty;
        public string ClassName { get; set; } = "QualiFrame.Tests";
        public string MethodName { get; set; } = "FindElement";
        public string Command { get; set; } = "findElement";
        public string Url { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? PageContent { get; set; }
        public List<HealeniumHealingResult> Results { get; set; } = new();
        public HealeniumHealingResult? UsedResult { get; set; }
        public byte[]? Screenshot { get; set; }
        public string? Metrics { get; set; }
        public List<string> ElementIds { get; set; } = new();
    }

    public sealed class HealeniumHealingResult
    {
        public HealeniumLocatorDto Locator { get; set; } = new();
        public double Score { get; set; }
        public bool SuccessHealing { get; set; } = true;
    }

    public sealed class HealeniumLocatorDto
    {
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public sealed class HealeniumReferenceElements
    {
        public string? PageContent { get; set; }
        public List<List<HealeniumNode>> Paths { get; set; } = new();
        public List<HealeniumLocatorDto> UnsuccessfulLocators { get; set; } = new();
    }

    public sealed class HealeniumNode
    {
        public string? Tag { get; set; }
        public string? Id { get; set; }
        public int? Index { get; set; }
        public string? InnerText { get; set; }

        /// <summary>
        /// Official Healenium NodeSerializer writes classes as a space-separated string.
        /// </summary>
        [JsonConverter(typeof(HealeniumClassesJsonConverter))]
        public List<string> Classes { get; set; } = new();

        [JsonPropertyName("other")]
        public Dictionary<string, string> Other { get; set; } = new();
    }

    public sealed class HealeniumImitateRequest
    {
        public HealeniumLocatorDto UserSelector { get; set; } = new();
        public HealeniumNode? TargetNode { get; set; }
    }

    /// <summary>
    /// hlm-backend NodeDeserializer reads <c>classes</c> as a string, not a JSON array.
    /// </summary>
    internal sealed class HealeniumClassesJsonConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new List<string>();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                return Split(reader.GetString());
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Healenium classes must be a string or an array of strings.");
            }

            var values = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(value.Trim());
                    }
                }
            }

            return values;
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            var joined = string.Join(" ", (value ?? new List<string>()).Where(c => !string.IsNullOrWhiteSpace(c)));
            writer.WriteStringValue(joined);
        }

        private static List<string> Split(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? new List<string>()
                : raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
