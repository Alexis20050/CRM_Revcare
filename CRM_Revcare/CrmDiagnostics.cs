#nullable enable
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CRM_Revcare_
{
    public static class CrmDiagnostics
    {
        private static readonly object _logLock = new object();

        public static string GetLogFilePath()
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RevCareAutoHub", "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            return Path.Combine(logDir, "crm-diagnostics.log");
        }

        public static string Sanitize(string? input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            // Mask Passwords, User IDs, Tokens, and Credentials in connection strings or error messages
            string sanitized = Regex.Replace(input, @"(?i)(Password|Pwd)\s*=\s*[^;]+", "$1=********");
            sanitized = Regex.Replace(sanitized, @"(?i)(User ID|Uid|Username)\s*=\s*[^;]+", "$1=********");
            sanitized = Regex.Replace(sanitized, @"(?i)(bearer\s+)[A-Za-z0-9\-_\.]+", "$1********");
            return sanitized;
        }

        public static void LogException(string context, Exception ex, CrmDatabaseInfo? dbInfo = null)
        {
            try
            {
                lock (_logLock)
                {
                    var filePath = GetLogFilePath();
                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
                    var env = dbInfo?.EnvironmentDisplayName ?? "UNKNOWN";
                    var server = dbInfo?.ServerDisplayName ?? "UNKNOWN";
                    var db = dbInfo?.DatabaseName ?? "UNKNOWN";

                    var sanitizedMsg = Sanitize(ex.Message);
                    var sanitizedStack = Sanitize(ex.StackTrace);

                    using var writer = new StreamWriter(filePath, append: true);
                    writer.WriteLine($"[{timestamp}] [{env}] Context: {context}");
                    writer.WriteLine($"  Target: Server={server}, Database={db}");
                    writer.WriteLine($"  Exception: {ex.GetType().FullName}: {sanitizedMsg}");
                    if (ex.InnerException != null)
                    {
                        writer.WriteLine($"  InnerException: {ex.InnerException.GetType().FullName}: {Sanitize(ex.InnerException.Message)}");
                    }
                    if (!string.IsNullOrWhiteSpace(sanitizedStack))
                    {
                        writer.WriteLine($"  StackTrace:\n{sanitizedStack}");
                    }
                    writer.WriteLine(new string('-', 80));
                }
            }
            catch
            {
                // Diagnostics logging must never crash the host application
            }
        }
    }
}
