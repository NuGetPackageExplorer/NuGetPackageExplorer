using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuGetPe
{
    public static class PackageExtensions
    {
        private const string TagsProperty = "Tags";
        private static readonly string[] _packagePropertiesToSearch = new[] {"Id", "Description", TagsProperty};

        public static bool IsReleaseVersion(this IPackageMetadata packageMetadata)
        {
            return String.IsNullOrEmpty(packageMetadata.Version.SpecialVersion);
        }

        public static string GetHash(this IPackage package)
        {
            return GetHash(package, new NuGet.CryptoHashProvider());
        }

        public static string GetHash(this IPackage package, IHashProvider hashProvider)
        {
            using (Stream stream = package.GetStream())
                return Convert.ToBase64String(hashProvider.CalculateHash(stream));
        }

        public static string GetFullName(this IPackageMetadata package)
        {
            return package.Id + " " + package.Version;
        }

        public static IQueryable<IPackage> Search(this IQueryable<IPackage> packages, string searchTerm)
        {
            if (searchTerm.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
            {
                string id = searchTerm.Substring(3).Trim();
                if (String.IsNullOrEmpty(id))
                {
                    return new IPackage[0].AsQueryable();
                }

                return FindPackagesById(packages, id);
            }
            else
            {
                return Find(packages, searchTerm.Split(' '));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        public static IQueryable<IPackage> FindPackagesById(this IQueryable<IPackage> packages, string id)
        {
            id = id.ToLowerInvariant();
            return packages.Where(p => p.Id.ToLower() == id);
        }

        public static IQueryable<IPackage> Find(this IQueryable<IPackage> packages, params string[] searchTerms)
        {
            if (searchTerms == null)
            {
                return packages;
            }

            IEnumerable<string> nonNullTerms = searchTerms.Where(s => s != null);
            if (!nonNullTerms.Any())
            {
                return packages;
            }

            return packages.Where(BuildSearchExpression(nonNullTerms));
        }

        public static bool IsListed(this IPackage package)
        {
            return package.Published > Constants.Unpublished;
        }

        /// <summary>
        /// Constructs an expression to search for individual tokens in a search term in the Id and Description of packages
        /// </summary>
        private static Expression<Func<IPackage, bool>> BuildSearchExpression(IEnumerable<string> searchTerms)
        {
            Debug.Assert(searchTerms != null);
            ParameterExpression parameterExpression = Expression.Parameter(typeof(IPackageMetadata));
            // package.Id.ToLower().Contains(term1) || package.Id.ToLower().Contains(term2)  ...
            Expression condition = (from term in searchTerms
                                    from property in _packagePropertiesToSearch
                                    select BuildExpressionForTerm(parameterExpression, term, property)).Aggregate(
                                        Expression.OrElse);
            return Expression.Lambda<Func<IPackage, bool>>(condition, parameterExpression);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower",
            Justification = "The expression is remoted using Odata which does not support the culture parameter")]
        private static Expression BuildExpressionForTerm(ParameterExpression packageParameterExpression, string term,
                                                         string propertyName)
        {
            // For tags we want to prepend and append spaces to do an exact match
            if (propertyName.Equals(TagsProperty, StringComparison.OrdinalIgnoreCase))
            {
                term = " " + term + " ";
            }

            MethodInfo stringContains = typeof(String).GetMethod("Contains", new[] {typeof(string)});
            MethodInfo stringToLower = typeof(String).GetMethod("ToLower", Type.EmptyTypes);

            // package.Id / package.Description
            MemberExpression propertyExpression = Expression.Property(packageParameterExpression, propertyName);
            // .ToLower()
            MethodCallExpression toLowerExpression = Expression.Call(propertyExpression, stringToLower);

            // Handle potentially null properties
            // package.{propertyName} != null && package.{propertyName}.ToLower().Contains(term.ToLower())
            return Expression.AndAlso(Expression.NotEqual(propertyExpression,
                                                          Expression.Constant(null)),
                                      Expression.Call(toLowerExpression, stringContains,
                                                      Expression.Constant(term.ToLower())));
        }
    }
}