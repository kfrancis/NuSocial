using Microsoft.Extensions.Localization;
using Nostr.Client.Keys;
using NuSocial.Localization;
using System.ComponentModel.DataAnnotations;

namespace NuSocial.Core.Validation
{
    /// <summary>
    /// Attribute class for validating Nostr Key format.
    /// </summary>
    public sealed class NostrKeyValidAttribute : ValidationAttribute
    {
        /// <inheritdoc/>
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (validationContext is null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }

            var instance = validationContext.ObjectInstance;
            var loc = (IStringLocalizer<NuSocialResource>?)instance.GetType().GetProperty("L")?.GetValue(instance);
            var invalidMessage = loc?["InvalidNostrKey"] ?? "Invalid Key";

            if (value is string valueString && !string.IsNullOrEmpty(valueString))
            {
                bool isValid = false;

                if (valueString.StartsWith("npub", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _ = NostrPublicKey.FromBech32(valueString);
                        isValid = true;
                    }
                    catch (ArgumentException) { }
                }
                else if (valueString.StartsWith("nsec", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _ = NostrPrivateKey.FromBech32(valueString);
                        isValid = true;
                    }
                    catch (ArgumentException) { }
                }
                else
                {
                    try
                    {
                        _ = NostrPublicKey.FromHex(valueString);
                        isValid = true;
                    }
                    catch (ArgumentException) { }

                    if (!isValid)
                    {
                        try
                        {
                            _ = NostrPrivateKey.FromHex(valueString);
                            isValid = true;
                        }
                        catch (ArgumentException) { }
                    }
                }

                if (!isValid)
                {
                    return new(invalidMessage);
                }
            }

            return ValidationResult.Success!;
        }
    }
}