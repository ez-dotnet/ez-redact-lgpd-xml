using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Compliance.Classification;
using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.Core.Taxonomies;

namespace EZ.Redact.Lgpd.Xml.DataContractSerializer;

public sealed class RedactingDataContractSerializer
{
    private readonly ILGPDRedactService _redactService;

    public RedactingDataContractSerializer(ILGPDRedactService redactService)
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
        var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(T));
        serializer.WriteObject(writer, value);
        return doc;
    }

    private void RedactDocument(XDocument doc, Type type)
    {
        if (doc.Root == null) return;
        RedactElement(doc.Root, type);
    }

    private void RedactElement(XElement element, Type type)
    {
        if (!type.GetCustomAttributes<DataContractAttribute>().Any())
            return;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            if (property.GetCustomAttribute<IgnoreDataMemberAttribute>() != null) continue;

            var dataMember = property.GetCustomAttribute<DataMemberAttribute>();
            if (dataMember == null) continue;

            var classificationAttr = property.GetCustomAttribute<DataClassificationAttribute>();

            if (classificationAttr == null && !IsComplexType(property.PropertyType))
                continue;

            var elementName = dataMember.Name ?? property.Name;

            if (classificationAttr != null && TryGetDadoPessoal(classificationAttr.Classification, out var dadoPessoal))
            {
                var childElement = FindChild(element, elementName);
                if (childElement != null && !childElement.HasElements)
                    childElement.Value = _redactService.Redact(dadoPessoal, childElement.Value);
            }
            else if (IsComplexType(property.PropertyType))
            {
                var childElement = FindChild(element, elementName);
                if (childElement != null)
                    RedactElement(childElement, property.PropertyType);
            }
        }
    }

    private static XElement? FindChild(XElement element, string name)
    {
        return element.Elements().FirstOrDefault(e => e.Name.LocalName == name);
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
