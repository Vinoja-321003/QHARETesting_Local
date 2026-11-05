using OpenQA.Selenium;
using Qhare.Selenium;

namespace QualiFrame.Framework.Utilities.WebDriver;

/// <summary>
/// PageSource stops at an open shadow boundary. These helpers send that tree to QHARE
/// and resolve a healed locator inside it. Closed roots stay unreachable.
/// </summary>
internal static class OpenShadowDom
{
    public static bool ContainsOpenShadow(IWebDriver driver)
    {
        if (driver is not IJavaScriptExecutor js)
        {
            return false;
        }

        const string script = """
            function visit(root) {
              if (!root || !root.querySelectorAll) return false;
              var nodes = root.querySelectorAll('*');
              for (var i = 0; i < nodes.length; i++) {
                if (nodes[i].shadowRoot) return true;
              }
              return false;
            }
            return visit(document);
            """;
        try
        {
            return js.ExecuteScript(script) is true;
        }
        catch
        {
            return false;
        }
    }

    public static string? Capture(IWebDriver driver)
    {
        if (driver is not IJavaScriptExecutor js)
        {
            return null;
        }

        const string flattenJs = """
            var roots = [];
            function visit(root) {
              if (!root || !root.querySelectorAll) return;
              var nodes = root.querySelectorAll('*');
              for (var i = 0; i < nodes.length; i++) {
                var el = nodes[i];
                if (el.shadowRoot) {
                  roots.push(el.shadowRoot);
                  visit(el.shadowRoot);
                }
              }
            }
            visit(document);
            var rootEl = document.documentElement;
            if (!rootEl) return '';
            var hasFrame = !!document.querySelector('iframe, frame');
            try {
              if (!hasFrame && rootEl.getHTML) {
                var html = rootEl.getHTML({serializableShadowRoots: true, shadowRoots: roots});
                if (html && (html.indexOf('shadowrootmode') >= 0 || roots.length === 0)) return '<!DOCTYPE html>' + html;
              }
            } catch (err) {}
            function textSafe(value) {
              return String(value || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            }
            function attrs(el) {
              var out = '';
              if (!el || !el.attributes) return out;
              for (var i = 0; i < el.attributes.length; i++) {
                var name = el.attributes[i].name;
                if (!name || name.indexOf('on') === 0) continue;
                var val = String(el.attributes[i].value || '').replace(/&/g, '&amp;').replace(/"/g, '&quot;');
                if (val.length > 500) val = val.slice(0, 500);
                out += ' ' + name + '="' + val + '"';
              }
              return out;
            }
            function serialize(node) {
              if (!node || node.nodeType !== 1) {
                return node && node.nodeType === 3 ? textSafe(node.nodeValue) : '';
              }
              var tag = String(node.tagName || '').toLowerCase();
              if (!tag || tag === 'script' || tag === 'noscript' || tag === 'style') return '';
              var html = '<' + tag + attrs(node) + '>';
              var child = node.firstChild;
              while (child) { html += serialize(child); child = child.nextSibling; }
              if (node.shadowRoot) {
                html += '<shadow-root data-qhare-shadow="open" data-shadow-host="' + textSafe(node.id || tag) + '">';
                var shadowChild = node.shadowRoot.firstChild;
                while (shadowChild) { html += serialize(shadowChild); shadowChild = shadowChild.nextSibling; }
                html += '</shadow-root>';
              }
              if (tag === 'iframe' || tag === 'frame') {
                try {
                  var doc = node.contentDocument;
                  if (doc && doc.documentElement) {
                    html += '<div data-qhare-frame="' + textSafe(node.id || node.name || 'iframe') + '">';
                    html += serialize(doc.documentElement);
                    html += '</div>';
                  }
                } catch (frameError) {}
              }
              return html + '</' + tag + '>';
            }
            try { return '<!DOCTYPE html>' + serialize(rootEl); }
            catch (err2) { return rootEl.outerHTML || ''; }
            """;
        try
        {
            var result = js.ExecuteScript(flattenJs) as string;
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }

    public static IWebElement? Find(IWebDriver driver, By by)
    {
        if (driver is not IJavaScriptExecutor js)
        {
            return null;
        }

        var (locatorType, locatorValue) = HealingWebDriver.Describe(by);
        const string pierceJs = """
            var type = String(arguments[0] || ''), value = String(arguments[1] || '');
            function matches(el) {
              if (!el || el.nodeType !== 1) return false;
              if (type === 'id') {
                var id = value.charAt(0) === '#' ? value.slice(1) : value;
                return el.id === id;
              }
              if (type === 'name') return el.getAttribute('name') === value;
              if (type === 'testid' || type === 'data-testid') return el.getAttribute('data-testid') === value;
              try { return el.matches(value); } catch (err) { return false; }
            }
            function walk(root) {
              if (!root || !root.querySelectorAll) return null;
              var nodes = root.querySelectorAll('*');
              for (var i = 0; i < nodes.length; i++) {
                var el = nodes[i];
                if (matches(el)) return el;
                if (el.shadowRoot) {
                  var nested = walk(el.shadowRoot);
                  if (nested) return nested;
                }
              }
              return null;
            }
            return walk(document);
            """;
        try
        {
            return js.ExecuteScript(pierceJs, locatorType, locatorValue) as IWebElement;
        }
        catch
        {
            return null;
        }
    }
}
