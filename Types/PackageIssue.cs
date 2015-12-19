using System;

namespace NuGetPackageExplorer.Types
{
    public class PackageIssue
    {
        public PackageIssue(PackageIssueLevel type, string title, string description, string solution)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("Argument is null or empty.", "title");
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentException("Argument is null or empty.", "description");
            }

            Level = type;
            Title = title;
            Description = description;
            Solution = solution;
        }

        public PackageIssueLevel Level { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Solution { get; private set; }
    }
}