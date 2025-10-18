using System;
using System.Text.Json;
using Reporting.Abstractions;

namespace Reporting.Core.Renderers
{
    /// <summary>
    /// Renders PreparedModel to JSON format.
    /// </summary>
    public class JsonRenderer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Renders the prepared model to JSON string.
        /// </summary>
        public string Render<T>(PreparedModel<T> model)
        {
            try
            {
                return JsonSerializer.Serialize(model, Options);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    error = $"Failed to render JSON: {ex.Message}",
                    reportId = model.Metadata.Id
                }, Options);
            }
        }
    }
}
