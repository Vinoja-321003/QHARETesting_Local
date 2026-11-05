namespace QualiFrame.Runner.Core
{
    internal static class RunnerLog
    {
        public static void Information(string message) => Console.WriteLine($"[INFO] {message}");

        public static void Information(string template, params object?[] args) =>
            Console.WriteLine($"[INFO] {Format(template, args)}");

        public static void Debug(string template, params object?[] args) =>
            Console.WriteLine($"[DEBUG] {Format(template, args)}");

        public static void Warning(string template, params object?[] args) =>
            Console.WriteLine($"[WARN] {Format(template, args)}");

        public static void Error(string template, params object?[] args) =>
            Console.Error.WriteLine($"[ERROR] {Format(template, args)}");

        public static void Error(Exception ex, string template, params object?[] args) =>
            Console.Error.WriteLine($"[ERROR] {Format(template, args)}: {ex.Message}");

        private static string Format(string template, object?[] args)
        {
            if (args.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return $"{template} [{string.Join(", ", args)}]";
            }
        }
    }
}
