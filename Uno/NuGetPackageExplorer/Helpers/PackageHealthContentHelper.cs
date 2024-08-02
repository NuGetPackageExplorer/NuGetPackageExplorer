using System.Resources;

using NuGet.Packaging.Signing;

using NuGetPe;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PackageExplorer
{
    public static class PackageHealthContentHelper
    {
        private static ResourceManager resManager => Resources.ResourceManager;

        public static string ValidationResultToText(SignatureVerificationStatus result)
        {
            switch (result)
            {
                case SignatureVerificationStatus.Valid:
                    return resManager.GetString("Validation_Valid");
                case SignatureVerificationStatus.Disallowed:
                    return resManager.GetString("Validation_Disallowed");
                case SignatureVerificationStatus.Unknown:
                    return resManager.GetString("Validation_Unknown");
                case SignatureVerificationStatus.Suspect:
                    return resManager.GetString("Validation_Suspect");
                default:
                    return resManager.GetString("Validation_Unknown");
            }
        }

        public static PackageHealthIconVisibilityInfo ValidationResultToIcon(SignatureVerificationStatus result)
        {
            switch (result)
            {
                case SignatureVerificationStatus.Valid:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK);
                case SignatureVerificationStatus.Disallowed:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical);
                case SignatureVerificationStatus.Unknown:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Warning);
                case SignatureVerificationStatus.Suspect:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Warning);
                default:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Info);
            }
        }

        public static string SourceLinkResultToText(SymbolValidationResult result)
        {
            switch (result)
            {
                case SymbolValidationResult.Valid:
                    return resManager.GetString("Validation_Valid");
                case SymbolValidationResult.ValidExternal:
                    return resManager.GetString("Validation_ValidExternal");
                case SymbolValidationResult.NothingToValidate:
                    return resManager.GetString("Validation_NothingToValidate");
                case SymbolValidationResult.NoSourceLink:
                    return resManager.GetString("Validation_MissingSourceLink");
                case SymbolValidationResult.HasUntrackedSources:
                    return resManager.GetString("Validation_HasUntrackedSources");
                case SymbolValidationResult.InvalidSourceLink:
                    return resManager.GetString("Validation_InvalidSourceLink");
                case SymbolValidationResult.NoSymbols:
                    return resManager.GetString("Validation_MissingSymbols");
                default:
                    return null;

            }
        }

        public static PackageHealthIconVisibilityInfo SourceLinkResultToIcon(SymbolValidationResult result)
        {
            switch (result)
            {
                case SymbolValidationResult.Valid:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK);
                case SymbolValidationResult.ValidExternal:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK);
                case SymbolValidationResult.NothingToValidate:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK);
                case SymbolValidationResult.NoSourceLink:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical);
                case SymbolValidationResult.HasUntrackedSources:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Warning);
                case SymbolValidationResult.InvalidSourceLink:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical);
                case SymbolValidationResult.NoSymbols:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical);
            }
            return null;
        }

        public static string DeterministicResultToText(DeterministicResult result)
        {
            switch (result)
            {
                case DeterministicResult.Valid:
                    return resManager.GetString("Validation_Valid");
                case DeterministicResult.NonDeterministic:
                    return resManager.GetString("Validation_NonDeterministic");
                case DeterministicResult.HasUntrackedSources:
                    return resManager.GetString("Validation_HasUntrackedSources");
                case DeterministicResult.NothingToValidate:
                    return resManager.GetString("Validation_NothingToValidate");
                default:
                    return null;
            }
        }

        public static PackageHealthIconVisibilityInfo DeterministicResultToIcon(DeterministicResult result)
        {
            switch (result)
            {
                case DeterministicResult.Valid:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK);
                case DeterministicResult.NonDeterministic:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical);
                case DeterministicResult.HasUntrackedSources:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Warning);
                case DeterministicResult.NothingToValidate:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK);
            }
            return null;
        }

        public static string CompilerFlagsResultToText(HasCompilerFlagsResult result)
        {
            switch (result)
            {
                case HasCompilerFlagsResult.Valid:
                    return resManager.GetString("Validation_Valid");
                case HasCompilerFlagsResult.Present:
                    return resManager.GetString("Validation_Present");
                case HasCompilerFlagsResult.Missing:
                    return resManager.GetString("Validation_MissingCompilerFlags");
                case HasCompilerFlagsResult.NothingToValidate:
                    return resManager.GetString("Validation_NothingToValidate");
                default:
                    return null;
            }
        }

        public static PackageHealthIconVisibilityInfo CompilerFlagsResultToIcon(HasCompilerFlagsResult result)
        {
            switch (result)
            {
                case HasCompilerFlagsResult.Valid:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK);
                case HasCompilerFlagsResult.Present:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Warning);
                case HasCompilerFlagsResult.Missing:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.Critical);
                case HasCompilerFlagsResult.NothingToValidate:
                    return new PackageHealthIconVisibilityInfo(PackageHealthIconVisibilityInfo.IconTypes.OK);
            }
            return null;
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
