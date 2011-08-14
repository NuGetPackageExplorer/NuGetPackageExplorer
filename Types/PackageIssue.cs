using System;

namespace NuGetPackageExplorer.Types {
    public class PackageIssue {
        public PackageIssue(
            PackageIssueLevel type, string title, string problem, string solution, string target) {

            if (string.IsNullOrEmpty(title)) {
                throw new ArgumentException("Argument is null or empty.", "title");
            }

            if (string.IsNullOrEmpty(problem)) {
                throw new ArgumentException("Argument is null or empty.", "problem");
            }

            Level = type;
            Title = title;
            Problem = problem;
            Solution = solution;
            Target = target;
        }

        public PackageIssueLevel Level { get; private set; }
        public string Title { get; private set; }
        public string Problem { get; private set; }
        public string Solution { get; private set; }
        public string Target { get; set; }
    }
}