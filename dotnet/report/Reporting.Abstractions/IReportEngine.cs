using System.Threading;
using System.Threading.Tasks;

namespace Reporting.Abstractions
{
    /// <summary>
    /// Engine contract that transforms provider data + template into emitted report artifacts.
    /// Implementations must be engine-agnostic from the caller's perspective.
    /// </summary>
    public interface IReportEngine
    {
        /// <summary>
        /// Generates report artifacts for the given provider.
        /// Implementations should ensure deterministic outputs given the same inputs.
        /// </summary>
        Task<ReportResult> GenerateAsync<T>(IReportDataProvider<T> dataProvider, ReportOptions? options = null, CancellationToken ct = default);
    }
}
