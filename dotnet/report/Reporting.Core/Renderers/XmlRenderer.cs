using System;
using System.Xml.Linq;
using Reporting.Abstractions;

namespace Reporting.Core.Renderers
{
    /// <summary>
    /// Renders PreparedModel to XML format.
    /// </summary>
    public class XmlRenderer
    {
        /// <summary>
        /// Renders the prepared model to XML string.
        /// </summary>
        public string Render<T>(PreparedModel<T> model)
        {
            try
            {
                var root = new XElement("Report",
                    new XAttribute("id", model.Metadata.Id ?? ""),
                    new XAttribute("title", model.Metadata.Title ?? ""),
                    new XAttribute("generatedAt", model.PreparedAt?.ToString("O") ?? DateTime.UtcNow.ToString("O"))
                );

                if (model.Metadata.Description != null)
                {
                    root.Add(new XElement("Description", model.Metadata.Description));
                }

                var dataElement = new XElement("Data");
                if (model.Data != null)
                {
                    if (model.Data is System.Collections.IEnumerable enumerable and not string)
                    {
                        foreach (var item in enumerable)
                        {
                            dataElement.Add(CreateItemElement(item, "Item"));
                        }
                    }
                    else
                    {
                        dataElement.Add(CreateItemElement(model.Data, "Item"));
                    }
                }
                root.Add(dataElement);

                var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
                return doc.ToString();
            }
            catch (Exception ex)
            {
                return $"<error reportId=\"{System.Security.SecurityElement.Escape(model.Metadata.Id ?? "")}\"><message>{System.Security.SecurityElement.Escape($"Failed to render XML: {ex.Message}")}</message></error>";
            }
        }

        private static XElement CreateItemElement(object? item, string name)
        {
            if (item == null)
            {
                return new XElement(name);
            }

            var element = new XElement(name);
            var type = item.GetType();

            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal) || type == typeof(Guid))
            {
                element.Value = item.ToString() ?? "";
            }
            else
            {
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    if (!prop.CanRead) continue;

                    try
                    {
                        var value = prop.GetValue(item);
                        if (value != null)
                        {
                            element.Add(new XElement(prop.Name, value.ToString() ?? ""));
                        }
                    }
                    catch
                    {
                        // Skip properties that throw on access
                    }
                }
            }

            return element;
        }
    }
}
