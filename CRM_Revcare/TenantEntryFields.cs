using System.Collections.Generic;
using System.Text.Json.Nodes;
namespace CRM_Revcare_;
internal sealed record TenantField(string Key, string Label, string Kind = "text", bool Required = false, List<TenantChoice> Choices = null);
internal sealed record TenantChoice(string Label, JsonNode Value) { public override string ToString() => Label; }
