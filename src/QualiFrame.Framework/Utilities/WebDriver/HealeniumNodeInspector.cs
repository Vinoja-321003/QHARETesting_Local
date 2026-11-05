using System.Text.Json;
using OpenQA.Selenium;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Captures Healenium node paths and element attributes from the live DOM.
    /// </summary>
    public static class HealeniumNodeInspector
    {
        public const int InnerTextLimit = 160;

        private const string NodePathScript =
            """
            var items = [];
            var result = {items: null, url: null};
            var a = arguments[0];
            while (a != document) {
              var child = a;
              var i = 0; while (child = child.previousElementSibling) i++;
              var node = {tag:null,id:null,index:null,classes:[],other:{},innerText:""};
              if (a.tagName !== undefined) { node.tag = a.tagName.toLowerCase(); }
              node.id = a.id;
              node.index = i;
              node.innerText = (a.innerText || "").substring(0, 160);
              if (a.hasAttribute("class")) { node.classes = a.attributes.class.value.split(' '); }
              for (var index = 0; index < a.attributes.length; ++index) {
                var attrName = a.attributes[index].name;
                if (["id","class"].indexOf(attrName) == -1) {
                  node.other[attrName] = a.attributes[index].value;
                }
              }
              items.unshift(node);
              a = a.parentNode;
            }
            result.items = items;
            result.url = window.location.href;
            return JSON.stringify(result);
            """;

        public static List<HealeniumNode> ReadNodePath(ISearchContext driver, IWebElement element)
        {
            try
            {
                if (driver is not IJavaScriptExecutor js)
                {
                    return new List<HealeniumNode>();
                }

                var json = js.ExecuteScript(NodePathScript, element) as string;
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<HealeniumNode>();
                }

                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                {
                    return new List<HealeniumNode>();
                }

                var path = new List<HealeniumNode>();
                foreach (var item in items.EnumerateArray())
                {
                    path.Add(new HealeniumNode
                    {
                        Tag = item.TryGetProperty("tag", out var tag) ? tag.GetString() : null,
                        Id = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                        Index = item.TryGetProperty("index", out var index) && index.ValueKind == JsonValueKind.Number ? index.GetInt32() : null,
                        InnerText = Truncate(item.TryGetProperty("innerText", out var text) ? text.GetString() : null),
                        Classes = ReadClasses(item),
                        Other = ReadOther(item)
                    });
                }

                return path;
            }
            catch
            {
                return new List<HealeniumNode>();
            }
        }

        public static Dictionary<string, string> ReadAttributes(IWebElement element)
        {
            var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                var id = element.GetAttribute("id");
                if (!string.IsNullOrEmpty(id))
                {
                    attributes["id"] = id;
                }

                var name = element.GetAttribute("name");
                if (!string.IsNullOrEmpty(name))
                {
                    attributes["name"] = name;
                }

                var type = element.GetAttribute("type");
                if (!string.IsNullOrEmpty(type))
                {
                    attributes["type"] = type;
                }

                var testId = element.GetAttribute("data-testid");
                if (!string.IsNullOrEmpty(testId))
                {
                    attributes["data-testid"] = testId;
                }

                var className = element.GetAttribute("class");
                if (!string.IsNullOrEmpty(className))
                {
                    attributes["class"] = className;
                }
            }
            catch
            {
                // ignore stale elements
            }

            return attributes;
        }

        public static List<string> CaptureElementIds(IWebElement element)
        {
            try
            {
                var htmlId = element.GetAttribute("id");
                return string.IsNullOrEmpty(htmlId) ? new List<string>() : new List<string> { htmlId };
            }
            catch
            {
                return new List<string>();
            }
        }

        public static PendingLocatorCapture Capture(
            By locator,
            string command,
            IWebElement element,
            ISearchContext driver,
            string url,
            string className,
            string methodName)
        {
            var (type, value) = HealeniumLocatorMapper.Split(locator);
            var path = ReadNodePath(driver, element);
            return new PendingLocatorCapture
            {
                LocatorType = type,
                LocatorValue = value,
                Url = url,
                Command = command,
                NodePath = path,
                Attributes = ReadAttributes(element),
                ClassName = className,
                MethodName = methodName,
                ElementIds = CaptureElementIds(element)
            };
        }

        private static List<string> ReadClasses(JsonElement item)
        {
            if (!item.TryGetProperty("classes", out var classes))
            {
                return new List<string>();
            }

            if (classes.ValueKind == JsonValueKind.String)
            {
                var raw = classes.GetString();
                return string.IsNullOrWhiteSpace(raw)
                    ? new List<string>()
                    : raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }

            if (classes.ValueKind == JsonValueKind.Array)
            {
                return classes.EnumerateArray()
                    .Select(c => c.GetString() ?? string.Empty)
                    .Where(c => c.Length > 0)
                    .ToList();
            }

            return new List<string>();
        }

        private static Dictionary<string, string> ReadOther(JsonElement item)
        {
            var other = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!item.TryGetProperty("other", out var node) || node.ValueKind != JsonValueKind.Object)
            {
                return other;
            }

            foreach (var property in node.EnumerateObject())
            {
                other[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }

            return other;
        }

        private static string? Truncate(string? value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= InnerTextLimit)
            {
                return value;
            }

            return value[..InnerTextLimit];
        }
    }
}
