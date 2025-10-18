using System.Threading;
using System.Threading.Tasks;

namespace Reporting.Abstractions
{
    /// <summary>
    /// Provides data for a specific report type.
    /// </summary>
    public interface IReportDataProvider<T>
    {
        /// <summary>
        /// Asynchronously retrieves the prepared data model for the report.
        /// </summary>
        Task<PreparedModel<T>> GetDataAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets metadata describing the report structure.
        /// </summary>
        ReportMetadata GetMetadata();
    }
}
