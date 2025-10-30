using System.Globalization;
using System.Resources;

using NuGet.Packaging.Signing;

using NuGetPe;

namespace PackageExplorer
{
    public static class PackageHealthContentHelper
    {
        private static ResourceManager resManager => Resources.ResourceManager;
        private static CultureInfo cultureInfo => CultureInfo.CurrentCulture;

        public static string? ValidationResultToText(SignatureVerificationStatus result)
        {
            return result switch
            {
                SignatureVerificationStatus.Valid => resManager.GetString("Validation_Valid", cultureInfo),
                SignatureVerificationStatus.Disallowed => resManager.GetString("Validation_Disallowed", cultureInfo),
                SignatureVerificationStatus.Unknown => resManager.GetString("Validation_Unknown", cultureInfo),
                SignatureVerificationStatus.Suspect => resManager.GetString("Validation_Suspect", cultureInfo),
                _ => resManager.GetString("Validation_Unknown", cultureInfo),
            };
        }

        public static PackageHealthIconVisibilityInfo ValidationResultToIcon(SignatureVerificationStatus result)
        {
            return result switch
            {
                SignatureVerificationStatus.Valid => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK),
                SignatureVerificationStatus.Disallowed => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical),
                SignatureVerificationStatus.Unknown => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Warning),
                SignatureVerificationStatus.Suspect => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Warning),
                _ => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Info),
            };
        }

        public static string? SourceLinkResultToText(SymbolValidationResult result)
        {
            return result switch
            {
                SymbolValidationResult.Valid => resManager.GetString("Validation_Valid", cultureInfo),
                SymbolValidationResult.ValidExternal => resManager.GetString("Validation_ValidExternal", cultureInfo),
                SymbolValidationResult.NothingToValidate => resManager.GetString("Validation_NothingToValidate", cultureInfo),
                SymbolValidationResult.NoSourceLink => resManager.GetString("Validation_MissingSourceLink", cultureInfo),
                SymbolValidationResult.HasUntrackedSources => resManager.GetString("Validation_HasUntrackedSources", cultureInfo),
                SymbolValidationResult.InvalidSourceLink => resManager.GetString("Validation_InvalidSourceLink", cultureInfo),
                SymbolValidationResult.NoSymbols => resManager.GetString("Validation_MissingSymbols", cultureInfo),
                _ => null,
            };
        }

        public static PackageHealthIconVisibilityInfo? SourceLinkResultToIcon(SymbolValidationResult result)
        {
            return result switch
            {
                SymbolValidationResult.Valid => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK),
                SymbolValidationResult.ValidExternal => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK),
                SymbolValidationResult.NothingToValidate => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK),
                SymbolValidationResult.NoSourceLink => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical),
                SymbolValidationResult.HasUntrackedSources => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Warning),
                SymbolValidationResult.InvalidSourceLink => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical),
                SymbolValidationResult.NoSymbols => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical),
                _ => null,
            };
        }

        public static string? DeterministicResultToText(DeterministicResult result)
        {
            return result switch
            {
                DeterministicResult.Valid => resManager.GetString("Validation_Valid", cultureInfo),
                DeterministicResult.NonDeterministic => resManager.GetString("Validation_NonDeterministic", cultureInfo),
                DeterministicResult.HasUntrackedSources => resManager.GetString("Validation_HasUntrackedSources", cultureInfo),
                DeterministicResult.NothingToValidate => resManager.GetString("Validation_NothingToValidate", cultureInfo),
                _ => null,
            };
        }

        public static PackageHealthIconVisibilityInfo? DeterministicResultToIcon(DeterministicResult result)
        {
            return result switch
            {
                DeterministicResult.Valid => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK),
                DeterministicResult.NonDeterministic => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical),
                DeterministicResult.HasUntrackedSources => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Warning),
                DeterministicResult.NothingToValidate => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK),
                _ => null,
            };
        }

        public static string? CompilerFlagsResultToText(HasCompilerFlagsResult result)
        {
            return result switch
            {
                HasCompilerFlagsResult.Valid => resManager.GetString("Validation_Valid", cultureInfo),
                HasCompilerFlagsResult.Present => resManager.GetString("Validation_Present", cultureInfo),
                HasCompilerFlagsResult.Missing => resManager.GetString("Validation_MissingCompilerFlags", cultureInfo),
                HasCompilerFlagsResult.NothingToValidate => resManager.GetString("Validation_NothingToValidate", cultureInfo),
                _ => null,
            };
        }

        public static PackageHealthIconVisibilityInfo? CompilerFlagsResultToIcon(HasCompilerFlagsResult result)
        {
            return result switch
            {
                HasCompilerFlagsResult.Valid => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK),
                HasCompilerFlagsResult.Present => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Warning),
                HasCompilerFlagsResult.Missing => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical),
                HasCompilerFlagsResult.NothingToValidate => new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK),
                _ => null,
            };
        }
    }

    public class PackageHealthIconVisibilityInfo
    {
        public bool IsOKIcon { get; private set; }
        public bool IsWarningIcon { get; private set; }
        public bool IsCriticalIcon { get; private set; }
        public bool IsInfoIcon { get; private set; }

        public enum IconTypes
        {
            OK,
            Warning,
            Critical,
            Info
        }
        public PackageHealthIconVisibilityInfo(IconTypes type)
        {
            switch (type)
            {
                case IconTypes.OK:
                    IsOKIcon = true;
                    break;
                case IconTypes.Warning:
                    IsWarningIcon = true;
                    break;
                case IconTypes.Critical:
                    IsCriticalIcon = true;
                    break;
                case IconTypes.Info:
                    IsInfoIcon = true;
                    break;
            }
        }
    }
}
