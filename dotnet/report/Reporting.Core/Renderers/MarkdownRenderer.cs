using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reporting.Abstractions;

namespace Reporting.Core.Renderers
{
    /// <summary>
    /// Renders PreparedModel to Markdown format.
    /// </summary>
    public class MarkdownRenderer
    {
        /// <summary>
        /// Renders the prepared model to Markdown string.
        /// </summary>
        public string Render<T>(PreparedModel<T> model)
        {
            try
            {
                var sb = new StringBuilder();

                sb.AppendLine($"# {EscapeMarkdown(model.Metadata.Title)}");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(model.Metadata.Description))
                {
                    sb.AppendLine(EscapeMarkdown(model.Metadata.Description));
                    sb.AppendLine();
                }

                sb.AppendLine("## Report Information");
                sb.AppendLine();
                sb.AppendLine($"- **Report ID:** {EscapeMarkdown(model.Metadata.Id)}");
                sb.AppendLine($"- **Generated At:** {model.PreparedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")} UTC");
                sb.AppendLine();

                sb.AppendLine("## Data");
                sb.AppendLine();

                if (model.Data != null)
                {
                    if (model.Data is System.Collections.IEnumerable enumerable and not string)
                    {
                        CreateDataTable(sb, enumerable, model.Metadata.Columns);
                    }
                    else
                    {
                        CreateDataTable(sb, new[] { model.Data }, model.Metadata.Columns);
                    }
                }
                else
                {
                    sb.AppendLine("*No data available.*");
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"# Error\n\nFailed to render Markdown for {model.Metadata.Id}: {ex.Message}";
            }
        }

        private static void CreateDataTable(StringBuilder sb, System.Collections.IEnumerable data, IReadOnlyList<ReportColumnInfo> columns)
        {
            var items = data.Cast<object>().ToList();
            if (!items.Any())
            {
                sb.AppendLine("*No data rows found.*");
                sb.AppendLine();
                return;
            }

            var visibleColumns = columns
                .OrderBy(c => c.SortOrder ?? int.MaxValue)
                .ThenBy(c => c.PropertyName)
                .ToList();

            if (!visibleColumns.Any())
            {
                var firstItem = items[0];
                var type = firstItem.GetType();
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Where(p => p.CanRead)
                    .ToArray();

                visibleColumns = properties.Select(p => new ReportColumnInfo
                {
                    PropertyName = p.Name,
                    DisplayName = p.Name
                }).ToList();
            }

            sb.Append("| ");
            sb.Append(string.Join(" | ", visibleColumns.Select(c => EscapeMarkdown(c.DisplayName))));
            sb.AppendLine(" |");

            sb.Append("| ");
            sb.Append(string.Join(" | ", visibleColumns.Select(_ => "---")));
            sb.AppendLine(" |");

            foreach (var item in items)
            {
                sb.Append("| ");
                var cellValues = visibleColumns.Select(col =>
                {
                    var type = item.GetType();
                    var prop = type.GetProperty(col.PropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

                    if (prop != null && prop.CanRead)
                    {
                        var value = prop.GetValue(item);
                        return EscapeMarkdown(FormatValue(value, col.Format));
                    }
                    return "";
                });
                sb.Append(string.Join(" | ", cellValues));
                sb.AppendLine(" |");
            }

            sb.AppendLine();
        }

        private static string FormatValue(object? value, string? format)
        {
            if (value == null)
            {
                return "";
            }

            if (!string.IsNullOrEmpty(format))
            {
                try
                {
                    return value switch
                    {
                        DateTime dt => dt.ToString(format),
                        DateTimeOffset dto => dto.ToString(format),
                        IFormattable formattable => formattable.ToString(format, null),
                        _ => value.ToString() ?? ""
                    };
                }
                catch
                {
                    // Fallback to default formatting
                }
            }

            return value.ToString() ?? "";
        }

        private static string EscapeMarkdown(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            return text
                .Replace("\\", "\\\\")
                .Replace("|", "\\|")
                .Replace("*", "\\*")
                .Replace("_", "\\_")
                .Replace("`", "\\`")
                .Replace("#", "\\#");
        }
    }
}
