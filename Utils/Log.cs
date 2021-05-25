using System;

namespace Savok.Server.Utils {
    public class Log {
        public static void E(string tag, string message, params object[] args)
            => Print(LogType.Error, tag, message, args);

        public static void I(string tag, string message, params object[] args)
            => Print(LogType.Info, tag, message, args);

        public static void W(string tag, string message, params object[] args)
            => Print(LogType.Warning, tag, message, args);

        public static void U(string tag, string message, params object[] args)
            => Print(LogType.Unknown, tag, message, args);

        public static void WS(string tag, string message, params object[] args)
            => Print(LogType.WebSocket, tag, message, args);

        private static readonly object _consoleLock = new();

        private static void Print(LogType logType, string tag, string message, params object[] args) {
            if (string.IsNullOrWhiteSpace(message)) return;
            
            lock (_consoleLock) {
                var previousColor = Console.ForegroundColor;
                Console.ForegroundColor = logType.Color;
                Console.Write($"[{DateTime.Now:HH:mm:ss}] {logType.Modificator}[{tag}]");
                Console.ForegroundColor = previousColor;
                Console.WriteLine($": {message}", args);
            }
        }
        
        public class LogType {
            public ConsoleColor Color { get; }
            public char Modificator { get; }

            public static LogType Error { get; } = new(ConsoleColor.Red, '!');
            public static LogType Info { get; } = new(ConsoleColor.Blue, '*');
            public static LogType Warning { get; } = new(ConsoleColor.Yellow, '^');
            public static LogType Unknown { get; } = new(ConsoleColor.DarkGray, '?');
            public static LogType WebSocket { get; } = new(ConsoleColor.Cyan, '@');

            private LogType(ConsoleColor color, char modificator) {
                Color = color;
                Modificator = modificator;
            }
        }
    }
}