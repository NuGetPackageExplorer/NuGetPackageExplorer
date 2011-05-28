using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.IO;

namespace NuGet {
    public static class PackageExtensions {
        private const string TagsProperty = "Tags";
        private static readonly string[] _packagePropertiesToSearch = new[] { "Id", "Description", TagsProperty };

        public static IEnumerable<IPackageFile> GetFiles(this IPackage package, string directory) {
            return package.GetFiles().Where(file => file.Path.StartsWith(directory, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<IPackageFile> GetContentFiles(this IPackage package) {
            return package.GetFiles(Constants.ContentDirectory);
        }

        public static string GetHash(this IPackage package) {
            return GetHash(package, new CryptoHashProvider());
        }

        public static string GetHash(this IPackage package, IHashProvider hashProvider) {
            using (Stream stream = package.GetStream()) {
                byte[] packageBytes = stream.ReadAllBytes();
                return Convert.ToBase64String(hashProvider.CalculateHash(packageBytes));
            }
        }

        /// <summary>
        /// Returns true if a package has no content that applies to a project.
        /// </summary>
        public static bool HasProjectContent(this IPackage package) {
            return package.FrameworkAssemblies.Any() ||
                   package.AssemblyReferences.Any() ||
                   package.GetContentFiles().Any();
        }

        /// <summary>
        /// Returns true if a package has dependencies but no files.
        /// </summary>
        public static bool IsDependencyOnly(this IPackage package) {
            return !package.GetFiles().Any() && package.Dependencies.Any();
        }

        public static string GetFullName(this IPackageMetadata package) {
            return package.Id + " " + package.Version;
        }

        public static IQueryable<DataServicePackage> Find(this IQueryable<DataServicePackage> packages, params string[] searchTerms) {
            if (searchTerms == null) {
                return packages;
            }

            IEnumerable<string> nonNullTerms = searchTerms.Where(s => s != null);
            if (!nonNullTerms.Any()) {
                return packages;
            }

            return packages.Where(BuildSearchExpression(nonNullTerms));
        }

        /// <summary>
        /// Constructs an expression to search for individual tokens in a search term in the Id and Description of packages
        /// </summary>
        private static Expression<Func<DataServicePackage, bool>> BuildSearchExpression(IEnumerable<string> searchTerms) {
            Debug.Assert(searchTerms != null);
            var parameterExpression = Expression.Parameter(typeof(IPackageMetadata));
            // package.Id.ToLower().Contains(term1) || package.Id.ToLower().Contains(term2)  ...
            Expression condition = (from term in searchTerms
                                    from property in _packagePropertiesToSearch
                                    select BuildExpressionForTerm(parameterExpression, term, property)).Aggregate(Expression.OrElse);
            return Expression.Lambda<Func<DataServicePackage, bool>>(condition, parameterExpression);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower",
            Justification = "The expression is remoted using Odata which does not support the culture parameter")]
        private static Expression BuildExpressionForTerm(ParameterExpression packageParameterExpression, string term, string propertyName) {
            // For tags we want to prepend and append spaces to do an exact match
            if (propertyName.Equals(TagsProperty, StringComparison.OrdinalIgnoreCase)) {
                term = " " + term + " ";
            }

            MethodInfo stringContains = typeof(String).GetMethod("Contains", new Type[] { typeof(string) });
            MethodInfo stringToLower = typeof(String).GetMethod("ToLower", Type.EmptyTypes);

            // package.Id / package.Description
            var propertyExpression = Expression.Property(packageParameterExpression, propertyName);
            // .ToLower()
            var toLowerExpression = Expression.Call(propertyExpression, stringToLower);

            // Handle potentially null properties
            // package.{propertyName} != null && package.{propertyName}.ToLower().Contains(term.ToLower())
            return Expression.AndAlso(Expression.NotEqual(propertyExpression,
                                                      Expression.Constant(null)),
                                      Expression.Call(toLowerExpression, stringContains, Expression.Constant(term.ToLower())));
        }
    }
}