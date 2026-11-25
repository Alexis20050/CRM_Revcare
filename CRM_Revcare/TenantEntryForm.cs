using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_Revcare_;

// Shared binding and validation; individual forms own their controls in Designer files.
public partial class TenantEntryForm : Form
{
    private JsonObject initial;
    private List<TenantField> definitions;
    private Func<JsonObject, Task> commit;
    private bool saving;
    private bool hasServices;
    private bool needsPassword;
    private bool readOnly;
    protected void ShowReadOnlyReceipt()
    {
        readOnly = true;
        saveButton.Visible = false;
        cancelButton.Text = "Close";
        titleLabel.Text = "View Receipt";
        recordLabel.Text = "Saved receipt - read only";
        resultLabel.Text = "Additional money requires a new payment. To correct a mistake, void this receipt with a reason and record a replacement.";
    }
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal Func<JsonObject, string> ValidateBeforeSave { get; set; }
    public TenantEntryForm() { InitializeComponent(); }
    internal void Bind(string title, JsonObject original, List<TenantField> fields, Func<JsonObject, Task> save,
        string[] offered = null, string[] selected = null, bool initialPassword = false)
    {
        Text = "RevCare Auto Hub CRM - " + title; titleLabel.Text = title;
        initial = (JsonObject)original.DeepClone(); definitions = fields; commit = save;
        recordLabel.Text = title.StartsWith("Add ") ? "New record - ID assigned on save" : "Edit selected record - generated IDs and history are preserved";
        foreach (var field in definitions)
        {
            var input = Find(field.Key);
            if (input is ComboBox combo)
            {
                combo.DataSource = field.Choices; combo.DisplayMember = "Label";
                if (original[field.Key] != null)
                    combo.SelectedItem = field.Choices?.FirstOrDefault(c => c.Value?.ToJsonString() == original[field.Key]?.ToJsonString());
                else
                    combo.SelectedIndex = -1;
            }
            else if (input is CheckBox check) check.Checked = original[field.Key]?.GetValue<bool>() ?? true;
            else if (input is DateTimePicker date)
            {
                DateTime parsedDate = DateTime.Now;
                if (original[field.Key] != null && DateTime.TryParse(original[field.Key]!.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    parsedDate = parsed;
                }
                date.Value = (parsedDate < DateTimePicker.MinimumDateTime || parsedDate > DateTimePicker.MaximumDateTime)
                    ? DateTime.Now
                    : parsedDate;
            }
            else input.Text = original[field.Key]?.ToString() ?? "";
        }
        hasServices = offered != null;
        if (hasServices)
        {
            var list = (CheckedListBox)Find("requestedServices");
            foreach (string service in offered.Union(selected ?? Array.Empty<string>())) list.Items.Add(service, selected?.Contains(service) == true);
        }
        needsPassword = initialPassword;
        var password = Controls.Find("initialPassword", true).FirstOrDefault();
        if (password != null) password.Visible = initialPassword;
        var passwordLabel = Controls.Find("initialPasswordLabel", true).FirstOrDefault();
        if (passwordLabel != null) passwordLabel.Visible = initialPassword;
        // Staff usernames and converted inquiry status are intentionally read-only on edit.
        foreach (string key in new[] { "username", "status", "jobOrderId", "isActive" })
        {
            var input = Controls.Find(key, true).FirstOrDefault();
            if (input != null && !definitions.Any(f => f.Key == key))
            {
                input.Enabled = false;
                if (input is CheckBox check) check.Checked = original[key]?.GetValue<bool>() ?? true;
                else if (input is ComboBox combo) { combo.Items.Add(original[key]?.ToString() ?? ""); combo.SelectedIndex = 0; }
                else input.Text = original[key]?.ToString() ?? "";
            }
        }
        saveButton.Click += async (_, _) => await SaveAsync();
        FormClosing += (_, e) => { if (saving) e.Cancel = true; };
    }
    protected Control Find(string name) => Controls.Find(name, true).Single();
    private async Task SaveAsync()
    {
        if (saving || readOnly) return;
        try
        {
            var body = (JsonObject)initial.DeepClone();
            foreach (var field in definitions)
            {
                var input = Find(field.Key);
                if (input is ComboBox combo)
                {
                    var choice = combo.SelectedItem as TenantChoice;
                    body[field.Key] = choice?.Value?.DeepClone();
                    if (field.Required && body[field.Key] == null) throw new InvalidOperationException("Choose " + field.Label + ".");
                }
                else if (input is CheckBox check) body[field.Key] = check.Checked;
                else if (input is DateTimePicker date) body[field.Key] = date.Value;
                else
                {
                    string text = input.Text.Trim();
                    if (field.Required && text.Length == 0) throw new InvalidOperationException(field.Label + " is required.");
                    if (field.Kind == "decimal")
                    {
                        if (text.Length == 0 && !field.Required) body[field.Key] = null;
                        else if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal amount)) body[field.Key] = amount;
                        else throw new InvalidOperationException("Enter a valid amount for " + field.Label + ".");
                    }
                    else body[field.Key] = text;
                }
            }
            if (hasServices) body["services"] = new JsonArray(((CheckedListBox)Find("requestedServices")).CheckedItems.Cast<string>().Select(s => (JsonNode)JsonValue.Create(s)).ToArray());
            if (needsPassword) body["password"] = Find("initialPassword").Text;
            string validationMessage = ValidateBeforeSave?.Invoke(body);
            if (!string.IsNullOrEmpty(validationMessage))
            {
                resultLabel.Text = validationMessage;
                return;
            }
            saving = true; saveButton.Enabled = false;
            await commit(body);
            if (needsPassword) Find("initialPassword").Text = "";
            saving = false; DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { resultLabel.Text = ex.Message; }
        finally { saving = false; saveButton.Enabled = true; }
    }
}
