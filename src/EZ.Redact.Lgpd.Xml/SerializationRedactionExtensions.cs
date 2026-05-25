using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.Xml.XmlSerializer;
using EZ.Redact.Lgpd.Xml.DataContractSerializer;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EZ.Redact.Lgpd.Xml;

public static class SerializationRedactionExtensions
{
    public static ILGPDRedactionBuilder AddXmlRedaction(this ILGPDRedactionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<RedactingXmlSerializer>();
        builder.Services.TryAddSingleton<RedactingDataContractSerializer>();

        return builder;
    }
}
