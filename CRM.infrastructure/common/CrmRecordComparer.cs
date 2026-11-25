#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CRM.infrastructure.common
{
    /// <summary>
    /// Type-safe centralized comparer for CRM entities and grid rows.
    /// Supports strings, numbers, decimals, ratings, dates, booleans, enums,
    /// record references, contact numbers, deterministic null ordering, and
    /// stable secondary-key tie-breaking.
    /// </summary>
    public sealed class CrmRecordComparer<T> : IComparer<T>
    {
        private readonly PropertyInfo? _prop;
        private readonly string _propName;
        private readonly bool _ascending;
        private readonly Func<T, int>? _secondaryKey;

        // Matches record references like CUST-000025, JO-007215, FB-006204, REQ-000012, etc.
        private static readonly Regex RecordRefRegex = new(@"^[A-Za-z]+[-# ]*0*(\d+)$", RegexOptions.Compiled);

        public CrmRecordComparer(PropertyInfo? prop, bool ascending, Func<T, int>? secondaryKey = null)
        {
            _prop = prop;
            _propName = prop?.Name ?? string.Empty;
            _ascending = ascending;
            _secondaryKey = secondaryKey;
        }

        public CrmRecordComparer(string propName, bool ascending, Func<T, int>? secondaryKey = null)
        {
            _propName = propName;
            _prop = typeof(T).GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            _ascending = ascending;
            _secondaryKey = secondaryKey;
        }

        public int Compare(T? x, T? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return 1; // nulls at the end
            if (y is null) return -1;

            if (_prop != null)
            {
                object? valX = _prop.GetValue(x);
                object? valY = _prop.GetValue(y);

                bool isNullX = valX is null || valX is DBNull;
                bool isNullY = valY is null || valY is DBNull;

                if (isNullX && isNullY)
                {
                    return CompareSecondary(x, y);
                }
                if (isNullX) return 1;  // Nulls sort to the bottom
                if (isNullY) return -1; // Nulls sort to the bottom

                int cmp = CompareNonNullValues(valX!, valY!, _propName);
                if (!_ascending) cmp = -cmp;

                if (cmp != 0) return cmp;
            }

            return CompareSecondary(x, y);
        }

        private int CompareSecondary(T x, T y)
        {
            if (_secondaryKey != null)
            {
                int idX = _secondaryKey(x);
                int idY = _secondaryKey(y);
                int secCmp = idX.CompareTo(idY);
                return _ascending ? secCmp : -secCmp;
            }
            return 0;
        }

        public static int CompareNonNullValues(object valX, object valY, string? propertyName = null)
        {
            // Contact numbers MUST remain strings
            bool isContactNumber = propertyName != null &&
                (propertyName.Contains("Contact", StringComparison.OrdinalIgnoreCase) ||
                 propertyName.Contains("Phone", StringComparison.OrdinalIgnoreCase) ||
                 propertyName.Contains("Mobile", StringComparison.OrdinalIgnoreCase));

            // 1. Strings
            if (valX is string strX && valY is string strY)
            {
                if (isContactNumber)
                {
                    return string.Compare(strX, strY, StringComparison.OrdinalIgnoreCase);
                }

                // Check for Standardized Record Reference (e.g. CUST-000025, JO-007215, FB-006204)
                string trimmedX = strX.Trim();
                string trimmedY = strY.Trim();
                var matchX = RecordRefRegex.Match(trimmedX);
                var matchY = RecordRefRegex.Match(trimmedY);

                if (matchX.Success && matchY.Success)
                {
                    // Extract numeric suffix for natural numeric sort
                    if (long.TryParse(matchX.Groups[1].Value, out long numX) &&
                        long.TryParse(matchY.Groups[1].Value, out long numY))
                    {
                        // Check if prefixes match (e.g. both JO- or both CUST-)
                        string prefixX = trimmedX[..matchX.Groups[1].Index];
                        string prefixY = trimmedY[..matchY.Groups[1].Index];
                        int prefixCmp = string.Compare(prefixX, prefixY, StringComparison.OrdinalIgnoreCase);
                        if (prefixCmp == 0)
                        {
                            return numX.CompareTo(numY);
                        }
                    }
                }

                return string.Compare(strX, strY, StringComparison.CurrentCultureIgnoreCase);
            }

            // 2. Integers (byte, short, int, long, etc.)
            if (IsInteger(valX) && IsInteger(valY))
            {
                long iX = Convert.ToInt64(valX);
                long iY = Convert.ToInt64(valY);
                return iX.CompareTo(iY);
            }

            // 3. Floating-point and Decimal numbers (currency, prices, costs)
            if (IsNumeric(valX) && IsNumeric(valY))
            {
                decimal dX = Convert.ToDecimal(valX);
                decimal dY = Convert.ToDecimal(valY);
                return dX.CompareTo(dY);
            }

            // 4. DateTime and DateOnly
            if (valX is DateTime dtX && valY is DateTime dtY)
            {
                return DateTime.Compare(dtX, dtY);
            }
            if (valX is DateTimeOffset dtoX && valY is DateTimeOffset dtoY)
            {
                return DateTimeOffset.Compare(dtoX, dtoY);
            }

            // 5. Booleans
            if (valX is bool bX && valY is bool bY)
            {
                return bX.CompareTo(bY);
            }

            // 6. Enums
            Type typeX = valX.GetType();
            Type typeY = valY.GetType();
            if (typeX.IsEnum && typeY.IsEnum)
            {
                return Convert.ToInt32(valX).CompareTo(Convert.ToInt32(valY));
            }

            // 7. General IComparable of same type
            if (typeX == typeY && valX is IComparable compX)
            {
                try
                {
                    return compX.CompareTo(valY);
                }
                catch
                {
                    // Fall back to string if types mismatch or throw
                }
            }

            // 8. Safe Fallback
            string sX = valX.ToString() ?? string.Empty;
            string sY = valY.ToString() ?? string.Empty;
            return string.Compare(sX, sY, StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool IsInteger(object val) =>
            val is int or long or short or byte or sbyte or uint or ulong or ushort;

        private static bool IsNumeric(object val) =>
            IsInteger(val) || val is decimal or double or float;
    }
}
