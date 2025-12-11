using System.Text.RegularExpressions;

namespace App.Core.Validation;

/// <summary>
/// Validation helper for input validation
/// </summary>
public static class ValidationHelper
{
    // Regex patterns
    private static readonly Regex CnicRegex = new(@"^\d{5}-\d{7}-\d$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^03\d{9}$", RegexOptions.Compiled);

    // ============================================
    // CNIC VALIDATION
    // ============================================

    /// <summary>
    /// Validate CNIC format: 12345-1234567-1
    /// </summary>
    public static bool ValidateCNIC(string cnic)
    {
        if (string.IsNullOrWhiteSpace(cnic))
            return false;

        return CnicRegex.IsMatch(cnic);
    }

    /// <summary>
    /// Get CNIC validation error message
    /// </summary>
    public static string GetCNICErrorMessage(string cnic)
    {
        if (string.IsNullOrWhiteSpace(cnic))
            return "CNIC is required";

        if (!CnicRegex.IsMatch(cnic))
            return "CNIC must be in format: 12345-1234567-1";

        return string.Empty;
    }

    // ============================================
    // PHONE VALIDATION
    // ============================================

    /// <summary>
    /// Validate phone number format: 03XXXXXXXXX
    /// </summary>
    public static bool ValidatePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Remove dashes and spaces
        phone = phone.Replace("-", "").Replace(" ", "");

        return PhoneRegex.IsMatch(phone);
    }

    /// <summary>
    /// Get phone validation error message
    /// </summary>
    public static string GetPhoneErrorMessage(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "Phone number is required";

        phone = phone.Replace("-", "").Replace(" ", "");

        if (!PhoneRegex.IsMatch(phone))
            return "Phone must be in format: 03XXXXXXXXX";

        return string.Empty;
    }

    // ============================================
    // WEIGHT VALIDATION
    // ============================================

    /// <summary>
    /// Validate weight is within acceptable range
    /// </summary>
    public static bool ValidateWeight(decimal weight, decimal min = 0.1m, decimal max = 10000m)
    {
        return weight >= min && weight <= max;
    }

    /// <summary>
    /// Get weight validation error message
    /// </summary>
    public static string GetWeightErrorMessage(decimal weight, decimal min = 0.1m, decimal max = 10000m)
    {
        if (weight < min)
            return $"Weight must be at least {min} kg";

        if (weight > max)
            return $"Weight cannot exceed {max} kg";

        return string.Empty;
    }

    // ============================================
    // REQUIRED FIELD VALIDATION
    // ============================================

    /// <summary>
    /// Validate required string field
    /// </summary>
    public static bool ValidateRequired(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Get required field error message
    /// </summary>
    public static string GetRequiredErrorMessage(string fieldName)
    {
        return $"{fieldName} is required";
    }

    // ============================================
    // PRICE VALIDATION
    // ============================================

    /// <summary>
    /// Validate price is positive
    /// </summary>
    public static bool ValidatePrice(decimal price)
    {
        return price >= 0;
    }

    /// <summary>
    /// Get price validation error message
    /// </summary>
    public static string GetPriceErrorMessage(decimal price)
    {
        if (price < 0)
            return "Price cannot be negative";

        return string.Empty;
    }

    // ============================================
    // GENERAL VALIDATION
    /// </summary>

    /// <summary>
    /// Validate ID is positive
    /// </summary>
    public static bool ValidateID(int id)
    {
        return id > 0;
    }

    /// <summary>
    /// Sanitize string input (trim and remove extra spaces)
    /// </summary>
    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Trim and replace multiple spaces with single space
        return Regex.Replace(input.Trim(), @"\s+", " ");
    }
}
