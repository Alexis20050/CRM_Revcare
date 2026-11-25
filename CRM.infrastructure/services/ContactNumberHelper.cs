#nullable enable
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CRM.infrastructure.services;

public static class ContactNumberHelper
{
    private static readonly Regex FictionalPattern = new(@"09XX|XX|\b555-\b|000-0000|0000000", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsFictionalOrPlaceholder(string? contactNumber)
    {
        if (string.IsNullOrWhiteSpace(contactNumber)) return false;
        return FictionalPattern.IsMatch(contactNumber);
    }

    public static string ExtractDigits(string? contactNumber)
    {
        if (string.IsNullOrWhiteSpace(contactNumber)) return "";
        return new string(contactNumber.Where(char.IsDigit).ToArray());
    }

    public static string Normalize(string? contactNumber)
    {
        if (string.IsNullOrWhiteSpace(contactNumber)) return "";
        string trimmed = contactNumber.Trim();

        // Preserve fictional historical placeholders intact
        if (IsFictionalOrPlaceholder(trimmed))
            return trimmed;

        string digits = ExtractDigits(trimmed);

        // International Philippine mobile: +639XXXXXXXXX or 639XXXXXXXXX (12 digits)
        if ((trimmed.StartsWith("+639", StringComparison.Ordinal) || trimmed.StartsWith("639", StringComparison.Ordinal)) && digits.Length == 12 && digits.StartsWith("639", StringComparison.Ordinal))
        {
            return "09" + digits.Substring(3);
        }

        // Standard Philippine mobile: 09XXXXXXXXX (11 digits)
        if (digits.Length == 11 && digits.StartsWith("09", StringComparison.Ordinal))
        {
            return digits;
        }

        // Philippine 8-digit landline with 2-digit area code (e.g., 0281234567 -> 10 digits)
        if (digits.Length == 10 && digits.StartsWith("0", StringComparison.Ordinal))
        {
            return digits;
        }

        // Other international number starting with +
        if (trimmed.StartsWith("+", StringComparison.Ordinal))
        {
            return "+" + digits;
        }

        // Default: return trimmed characters without extraneous whitespace
        return trimmed;
    }

    public static string FormatDisplay(string? contactNumber)
    {
        if (string.IsNullOrWhiteSpace(contactNumber)) return "";
        string trimmed = contactNumber.Trim();

        if (IsFictionalOrPlaceholder(trimmed))
            return trimmed + " [Fictional / Demo]";

        string norm = Normalize(trimmed);
        string digits = ExtractDigits(norm);

        // Format Philippine mobile: 0917-123-4567
        if (digits.Length == 11 && digits.StartsWith("09", StringComparison.Ordinal))
        {
            return $"{digits.Substring(0, 4)}-{digits.Substring(4, 3)}-{digits.Substring(7, 4)}";
        }

        // Format Metro Manila landline: (02) 8123-4567
        if (digits.Length == 10 && digits.StartsWith("02", StringComparison.Ordinal))
        {
            return $"(02) {digits.Substring(2, 4)}-{digits.Substring(6, 4)}";
        }

        // Format provincial landline: (0XX) XXX-XXXX
        if (digits.Length == 10 && digits.StartsWith("0", StringComparison.Ordinal))
        {
            return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
        }

        return trimmed;
    }

    public static (bool IsValid, string? ErrorMessage) Validate(string? contactNumber, bool allowExistingPlaceholder = false)
    {
        if (string.IsNullOrWhiteSpace(contactNumber))
            return (false, "Contact number is required.");

        string trimmed = contactNumber.Trim();

        // Check for placeholder/fictional format
        if (IsFictionalOrPlaceholder(trimmed))
        {
            if (allowExistingPlaceholder)
                return (true, null);

            return (false, "Fictional placeholder contact numbers (containing 'XX') cannot be newly assigned to customers.");
        }

        // Reject alphabetical characters in real customer numbers
        if (trimmed.Any(char.IsLetter))
            return (false, "Contact number cannot contain letters or placeholder characters.");

        string digits = ExtractDigits(trimmed);

        if (digits.Length < 7 || digits.Length > 15)
            return (false, "Contact number must contain between 7 and 15 digits.");

        // If user entered something resembling Philippine mobile, enforce Philippine mobile rules
        if (trimmed.StartsWith("09", StringComparison.Ordinal) || trimmed.StartsWith("+639", StringComparison.Ordinal) || trimmed.StartsWith("639", StringComparison.Ordinal))
        {
            string normalized = Normalize(trimmed);
            string normDigits = ExtractDigits(normalized);

            if (normDigits.Length != 11 || !normDigits.StartsWith("09", StringComparison.Ordinal))
                return (false, "Philippine mobile numbers must be exactly 11 digits starting with 09 (e.g., 0917 123 4567 or +63 917 123 4567).");
        }

        return (true, null);
    }

    public const string ClassificationValid = "Valid format";
    public const string ClassificationDemo = "Demo placeholder";
    public const string ClassificationInvalid = "Invalid";
    public const string ClassificationMissing = "Missing";

    public static string Classify(string? contactNumber)
    {
        if (string.IsNullOrWhiteSpace(contactNumber))
            return ClassificationMissing;

        string trimmed = contactNumber.Trim();
        if (IsFictionalOrPlaceholder(trimmed))
            return ClassificationDemo;

        var val = Validate(trimmed, allowExistingPlaceholder: false);
        return val.IsValid ? ClassificationValid : ClassificationInvalid;
    }

    public static bool Matches(string? contactNumber, string? query)
    {
        if (string.IsNullOrWhiteSpace(contactNumber) || string.IsNullOrWhiteSpace(query))
            return false;

        string contactTrimmed = contactNumber.Trim();
        string queryTrimmed = query.Trim();

        // 1. Direct case-insensitive substring
        if (contactTrimmed.Contains(queryTrimmed, StringComparison.OrdinalIgnoreCase))
            return true;

        // 2. Unformatted digit matching (e.g. searching 09171234567 matches 0917-123-4567)
        string contactDigits = ExtractDigits(contactTrimmed);
        string queryDigits = ExtractDigits(queryTrimmed);

        if (queryDigits.Length >= 3 && contactDigits.Contains(queryDigits, StringComparison.Ordinal))
            return true;

        // 3. Normalized format matching (e.g. query +63917... matching 0917...)
        string normContact = Normalize(contactTrimmed);
        string normQuery = Normalize(queryTrimmed);

        if (normContact.Contains(normQuery, StringComparison.OrdinalIgnoreCase) ||
            normQuery.Contains(normContact, StringComparison.OrdinalIgnoreCase))
            return true;

        // 4. Normalized digit matching for local (+639 vs 09) interoperability
        string normContactDigits = ExtractDigits(normContact);
        string normQueryDigits = ExtractDigits(normQuery);

        if (normQueryDigits.Length >= 4 && normContactDigits.Contains(normQueryDigits, StringComparison.Ordinal))
            return true;
        if (normContactDigits.Length >= 4 && normQueryDigits.Contains(normContactDigits, StringComparison.Ordinal))
            return true;

        return false;
    }

    public static bool IsEligibleForOutreach(string? contactNumber, bool isActive = true, bool isOptedOut = false)
    {
        if (!isActive || isOptedOut) return false;
        if (string.IsNullOrWhiteSpace(contactNumber)) return false;
        if (IsFictionalOrPlaceholder(contactNumber)) return false;

        var val = Validate(contactNumber, allowExistingPlaceholder: false);
        return val.IsValid;
    }
}
