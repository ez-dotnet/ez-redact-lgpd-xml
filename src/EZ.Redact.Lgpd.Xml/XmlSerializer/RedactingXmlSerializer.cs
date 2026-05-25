using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Compliance.Classification;
using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.Core.Taxonomies;

namespace EZ.Redact.Lgpd.Xml.XmlSerializer;

public sealed class RedactingXmlSerializer
{
    private readonly ILGPDRedactService _redactService;

    public RedactingXmlSerializer(ILGPDRedactService redactService)
    {
        _redactService = redactService ?? throw new ArgumentNullException(nameof(redactService));
    }

    public string Serialize<T>(T value)
    {
        var doc = SerializeToDocument(value);
        RedactDocument(doc, typeof(T));
        return doc.ToString();
    }

    public void Serialize<T>(T value, Stream stream)
    {
        var doc = SerializeToDocument(value);
        RedactDocument(doc, typeof(T));
        doc.Save(stream);
    }

    public void Serialize<T>(T value, TextWriter textWriter)
    {
        var doc = SerializeToDocument(value);
        RedactDocument(doc, typeof(T));
        doc.Save(textWriter);
    }

    public void Serialize<T>(T value, XmlWriter xmlWriter)
    {
        var doc = SerializeToDocument(value);
        RedactDocument(doc, typeof(T));
        doc.Save(xmlWriter);
    }

    private static XDocument SerializeToDocument<T>(T value)
    {
        var doc = new XDocument();
        using var writer = doc.CreateWriter();
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
        serializer.Serialize(writer, value);
        return doc;
    }

    private void RedactDocument(XDocument doc, Type type)
    {
        if (doc.Root == null) return;
        RedactElement(doc.Root, type);
    }

    private void RedactElement(XElement element, Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            if (property.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

            var classificationAttr = property.GetCustomAttribute<DataClassificationAttribute>();
            if (classificationAttr == null && !IsComplexType(property.PropertyType))
                continue;

            var xmlElementAttr = property.GetCustomAttribute<XmlElementAttribute>();
            var xmlAttributeAttr = property.GetCustomAttribute<XmlAttributeAttribute>();

            var elementName = xmlElementAttr?.ElementName
                           ?? xmlAttributeAttr?.AttributeName
                           ?? property.Name;

            if (classificationAttr != null && TryGetDadoPessoal(classificationAttr.Classification, out var dadoPessoal))
            {
                if (xmlAttributeAttr != null)
                {
                    var attr = element.Attribute(elementName);
                    if (attr != null)
                        attr.Value = _redactService.Redact(dadoPessoal, attr.Value);
                }
                else
                {
                    var childElement = element.Element(elementName);
                    if (childElement != null && !childElement.HasElements)
                        childElement.Value = _redactService.Redact(dadoPessoal, childElement.Value);
                }
            }
            else if (IsComplexType(property.PropertyType))
            {
                var childElement = element.Element(elementName);
                if (childElement != null)
                    RedactElement(childElement, property.PropertyType);
            }
        }
    }

    private static bool TryGetDadoPessoal(DataClassification classification, out DadoPessoal result)
    {
        if (classification.TaxonomyName != "LGPD")
        {
            result = default;
            return false;
        }

        try
        {
            result = LGPDTaxonomy.ToDadoPessoal(classification);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            result = default;
            return false;
        }
    }

    private static bool IsComplexType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string)
            || type == typeof(decimal) || type == typeof(DateTime)
            || type == typeof(Guid) || type == typeof(TimeSpan)
            || type == typeof(DateTimeOffset))
            return false;

        return type.IsClass || (type.IsValueType && !type.IsEnum && !type.IsPrimitive);
    }
}
