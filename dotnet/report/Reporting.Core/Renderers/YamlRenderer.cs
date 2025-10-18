using System;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Reporting.Abstractions;

namespace Reporting.Core.Renderers
{
    /// <summary>
    /// Renders PreparedModel to YAML format.
    /// </summary>
    public class YamlRenderer
    {
        private static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        /// <summary>
        /// Renders the prepared model to YAML string.
        /// </summary>
        public string Render<T>(PreparedModel<T> model)
        {
            try
            {
                return Serializer.Serialize(model);
            }
            catch (Exception ex)
            {
                return Serializer.Serialize(new
                {
                    Error = $"Failed to render YAML: {ex.Message}",
                    ReportId = model.Metadata.Id
                });
            }
        }
    }
}
