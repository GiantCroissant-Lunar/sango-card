using System.Linq;
using Microsoft.Extensions.Logging;

namespace SangoCard.Build.Editor;

public partial class BuildEntry
{
    private static void CheckIssues()
    {
#if HAS_CODE_STAGE_MAINTAINER
        var issues = CodeStage.Maintainer.Issues.IssuesFinder.StartSearch(false);
        if (!issues.Any())
        {
            return;
        }

        foreach (var issue in issues)
        {
            Log.LogWarning("IssueFinder: {0}", issue.ToString());
        }
#endif
    }
}
