#nullable enable
using System;
using System.IO;
using System.Text.Json.Nodes;

namespace CRM_Revcare_
{
    public static class LegacyBusinessConfig
    {
        public static BusinessDetails Current => Load();
        public static BusinessDetails Load()
        {
            try
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var doc = JsonNode.Parse(json);
                    var section = doc?["LegacyBusiness"] ?? doc?["BusinessDetails"];
                    if (section != null)
                    {
                        return new BusinessDetails
                        {
                            Name = section["Name"]?.ToString() ?? "RevCare Auto Hub",
                            Tagline = section["Tagline"]?.ToString() ?? "Automotive Care, Repair & Maintenance Specialists",
                            Address = section["Address"]?.ToString(),
                            Contact = section["Contact"]?.ToString()
                        };
                    }
                }
            }
            catch
            {
                // Fall back to default RevCare details
            }

            return new BusinessDetails();
        }
    }
}
