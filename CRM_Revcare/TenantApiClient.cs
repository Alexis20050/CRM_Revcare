#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CRM_Revcare_;

internal sealed record TenantResolutionInfo(string TenantId, string DisplayName, string Role, string Status, bool IsActive);
internal sealed record ModuleEntitlementInfo(string ModuleId, string DisplayName, string Description, bool IsImplemented, string RequiredDependencies, bool IsEnabled, string Reason, DateTime? UpdatedAtUtc, string UpdatedBy);

internal sealed class TenantApiClient : IDisposable
{
    private readonly HttpClient http;

    public string Role { get; private set; } = "";
    public string Username { get; private set; } = "";
    public string TenantId { get; private set; } = "";
    public string? CompanyName { get; set; }
    public string? BranchCode { get; private set; }
    public string? BranchId { get; private set; }
    public string TenantStatus { get; private set; } = "";
    public bool IsSuperAdmin { get; private set; }
    public string[] Permissions { get; private set; } = Array.Empty<string>();
    public string[] EnabledModules { get; private set; } = Array.Empty<string>();
    public string EnvironmentDisplayName => Environment.GetEnvironmentVariable("REVCARE_ENVIRONMENT")?.ToUpperInvariant() ?? (http.BaseAddress?.Host.Contains("localhost") == true ? "LOCAL" : "CLOUD");

    public bool Has(string permission) => Permissions.Contains(permission);
    public bool HasModule(string moduleId) => EnabledModules.Contains(moduleId, StringComparer.OrdinalIgnoreCase);

    public TenantApiClient(string address, HttpMessageHandler? handler = null)
    {
        var uri = new Uri(address.TrimEnd('/') + "/");
        if (uri.Scheme != "https" && !uri.IsLoopback) throw new InvalidOperationException("The API address must use HTTPS.");
        http = handler == null ? new HttpClient() : new HttpClient(handler);
        http.BaseAddress = uri;
        http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<(bool IsSuperAdmin, List<TenantResolutionInfo> Tenants)> ResolveTenantsAsync(string username, string password)
    {
        var body = new JsonObject { ["username"] = username, ["password"] = password };
        var res = await Send("POST", "auth/resolve-tenants", body);
        bool isSuperAdmin = res?["isSuperAdmin"]?.GetValue<bool>() ?? false;
        var tenants = new List<TenantResolutionInfo>();
        if (res?["tenants"] is JsonArray arr)
        {
            foreach (var node in arr)
            {
                if (node == null) continue;
                tenants.Add(new TenantResolutionInfo(
                    node["tenantId"]?.GetValue<string>() ?? "",
                    node["displayName"]?.GetValue<string>() ?? "",
                    node["role"]?.GetValue<string>() ?? "",
                    node["status"]?.GetValue<string>() ?? "Ready",
                    node["isActive"]?.GetValue<bool>() ?? true
                ));
            }
        }
        return (isSuperAdmin, tenants);
    }

    public async Task<bool> Login(string username, string password) => await LoginAsync(username, password, "tenant-a");

    public async Task<bool> LoginAsync(string username, string password, string? tenantId)
    {
        http.DefaultRequestHeaders.Authorization = null;
        var body = new JsonObject { ["username"] = username, ["password"] = password };
        if (!string.IsNullOrWhiteSpace(tenantId)) body["tenantId"] = tenantId;

        var result = await Send("POST", "auth/login", body);
        string token = result?["accessToken"]?.GetValue<string>() ?? "";
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var identity = await Send("GET", "auth/me");
        Username = username;
        TenantId = identity?["tenantId"]?.GetValue<string>() ?? (tenantId ?? "");
        Role = identity?["role"]?.GetValue<string>() ?? "";
        BranchCode = identity?["branchCode"]?.GetValue<string>();
        BranchId = identity?["branchId"]?.GetValue<string>();
        TenantStatus = identity?["status"]?.GetValue<string>() ?? "Ready";
        IsSuperAdmin = string.Equals(Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        Permissions = identity?["permissions"] is JsonArray pArr
            ? pArr.Select(n => n?.GetValue<string>() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToArray()
            : Array.Empty<string>();

        EnabledModules = identity?["enabledModules"] is JsonArray mArr
            ? mArr.Select(n => n?.GetValue<string>() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToArray()
            : Array.Empty<string>();

        return true;
    }

    public void ClearSession()
    {
        http.DefaultRequestHeaders.Authorization = null;
        Role = "";
        Username = "";
        TenantId = "";
        CompanyName = null;
        BranchCode = null;
        BranchId = null;
        TenantStatus = "";
        IsSuperAdmin = false;
        Permissions = Array.Empty<string>();
        EnabledModules = Array.Empty<string>();
    }

    // Tenant Module Metadata
    public async Task<JsonArray> GetModulesAsync()
    {
        var node = await Send("GET", "crm/modules");
        return node as JsonArray ?? new JsonArray();
    }

    // Staff Profile Retry
    public async Task<JsonNode?> RetryStaffProfileAsync(string userId) =>
        await Send("POST", $"crm/staff/{userId}/retry-profile");

    // Master Admin API
    public async Task<JsonArray> GetMasterTenantsAsync() =>
        (await Send("GET", "master/tenants")) as JsonArray ?? new JsonArray();

    public async Task<JsonNode?> RegisterMasterTenantAsync(string tenantId, string displayName, string connectionKey) =>
        await Send("POST", "master/tenants", new JsonObject
        {
            ["tenantId"] = tenantId,
            ["displayName"] = displayName,
            ["connectionKey"] = connectionKey
        });

    public async Task<JsonNode?> UpdateMasterTenantStatusAsync(string tenantId, string status, bool? isActive = null)
    {
        var body = new JsonObject { ["status"] = status };
        if (isActive.HasValue) body["isActive"] = isActive.Value;
        return await Send("PUT", $"master/tenants/{tenantId}/status", body);
    }

    public async Task<JsonNode?> GetMasterTenantHealthAsync(string tenantId) =>
        await Send("GET", $"master/tenants/{tenantId}/health");

    public async Task<JsonNode?> ProvisionMasterTenantOwnerAsync(string tenantId, string username, string password, string displayName) =>
        await Send("POST", $"master/tenants/{tenantId}/owner", new JsonObject
        {
            ["username"] = username,
            ["password"] = password,
            ["displayName"] = displayName
        });

    public async Task<JsonArray> GetMasterModulesAsync() =>
        (await Send("GET", "master/modules")) as JsonArray ?? new JsonArray();

    public async Task<List<ModuleEntitlementInfo>> GetTenantModulesAsync(string tenantId)
    {
        var node = await Send("GET", $"master/tenants/{tenantId}/modules");
        var list = new List<ModuleEntitlementInfo>();
        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item == null) continue;
                list.Add(new ModuleEntitlementInfo(
                    item["moduleId"]?.GetValue<string>() ?? "",
                    item["displayName"]?.GetValue<string>() ?? "",
                    item["description"]?.GetValue<string>() ?? "",
                    item["isImplemented"]?.GetValue<bool>() ?? false,
                    item["requiredDependencies"]?.GetValue<string>() ?? "",
                    item["isEnabled"]?.GetValue<bool>() ?? false,
                    item["reason"]?.GetValue<string>() ?? "",
                    item["updatedAtUtc"] != null ? DateTime.Parse(item["updatedAtUtc"]!.ToString()) : null,
                    item["updatedBy"]?.GetValue<string>() ?? ""
                ));
            }
        }
        return list;
    }

    public async Task<JsonNode?> UpdateTenantModuleAsync(string tenantId, string moduleId, bool isEnabled, string reason) =>
        await Send("PUT", $"master/tenants/{tenantId}/modules/{moduleId}", new JsonObject
        {
            ["isEnabled"] = isEnabled,
            ["reason"] = reason
        });

    public async Task<JsonArray> GetMasterAuditAsync(string? tenantId = null)
    {
        string path = "master/audit" + (string.IsNullOrWhiteSpace(tenantId) ? "" : $"?tenantId={Uri.EscapeDataString(tenantId)}");
        return (await Send("GET", path)) as JsonArray ?? new JsonArray();
    }

    public async Task<JsonNode?> Send(string method, string path, JsonNode? body = null)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path.TrimStart('/'));
        if (body != null) request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request);
        string text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized) throw new InvalidOperationException("Sign-in failed or your session expired. Sign in again.");
            if (response.StatusCode == HttpStatusCode.Forbidden) throw new InvalidOperationException("Your current role does not permit this action. Sign in again if your access has changed.");
            string message = "The request could not be completed. Reload the record and try again.";
            try
            {
                var json = JsonNode.Parse(text);
                message = json is JsonValue ? json.GetValue<string>() : json?["error"]?.ToString() ?? json?["detail"]?.ToString() ?? message;
            }
            catch { }
            throw new InvalidOperationException(message);
        }
        return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
    }

    public void Dispose() => http.Dispose();
}
