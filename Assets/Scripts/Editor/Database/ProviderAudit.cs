using System.Collections.Generic;
using System.Linq;
using Crookedile.EditorTools;
using Object = UnityEngine.Object;

namespace Crookedile.Editor.Database
{
    /// <summary>
    /// Turns a <see cref="ContentChecks.IContentProvider"/>'s rows into per-asset issues, so a
    /// type's tab shows exactly the audit the provider defines instead of a second copy of it.
    /// </summary>
    internal static class ProviderAudit
    {
        public static Dictionary<Object, List<Issue>> ByAsset(ContentChecks.IContentProvider provider)
        {
            var result = new Dictionary<Object, List<Issue>>();
            List<ContentChecks.Row> rows;
            try
            {
                rows = provider.Rows().ToList();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[Database] {provider.Category} audit threw: {e.Message}");
                return result;
            }
            foreach (var row in rows)
            {
                if (row.Context == null)
                    continue;
                if (!result.TryGetValue(row.Context, out var list))
                    result[row.Context] = list = new List<Issue>();
                list.AddRange(row.Issues.Select(ToIssue));
            }
            return result;
        }

        public static Issue ToIssue(ContentChecks.AuditIssue issue) =>
            new Issue(
                issue.Severity switch
                {
                    ContentChecks.Severity.Error => IssueLevel.Error,
                    ContentChecks.Severity.Warning => IssueLevel.Warning,
                    _ => IssueLevel.Info,
                },
                issue.Message
            );

        public static IEnumerable<Issue> For(Dictionary<Object, List<Issue>> byAsset, Object asset) =>
            asset != null && byAsset != null && byAsset.TryGetValue(asset, out var list) ? list : Enumerable.Empty<Issue>();
    }
}
