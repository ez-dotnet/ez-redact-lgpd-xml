using System.Xml.Linq;
using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.Xml.Tests.Models;
using EZ.Redact.Lgpd.Xml.XmlSerializer;
using Microsoft.Extensions.DependencyInjection;

namespace EZ.Redact.Lgpd.Xml.Tests.XmlSerializer;

public class RedactingXmlSerializerTest
{
    private static ILGPDRedactService CreateRedactService()
    {
        var services = new ServiceCollection();
        services.AddLGPDRedaction();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<ILGPDRedactService>();
    }

    [Fact]
    public void Deve_Redatar_Propriedades_Com_Atributo()
    {
        var redactService = CreateRedactService();
        var serializer = new RedactingXmlSerializer(redactService);

        var pessoa = new Pessoa
        {
            Documento = "123.456.789-09",
            Email = "joao@email.com",
            Telefone = "(11) 9 8888-4444",
            Nome = "João Silva",
            Endereco = "Rua das Flores, 123 - São Paulo/SP",
            Chave = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
            ChavePix = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
            SemAtributo = "texto normal"
        };

        var xml = serializer.Serialize(pessoa);
        var doc = XDocument.Parse(xml);

        Assert.Equal("123.***.***-09", (string?)doc.Root!.Element("Documento"));
        Assert.Equal("j***@email.com", (string?)doc.Root.Element("Email"));
        Assert.Equal("(11) 9 ****-4444", (string?)doc.Root.Element("Telefone"));
        Assert.Equal("J*** S****", (string?)doc.Root.Element("Nome"));
        Assert.Equal("R** d** F*****, *** - S** P*******", (string?)doc.Root.Element("Endereco"));
        Assert.Equal("1234****-****-****-****-********9abc", (string?)doc.Root.Element("Chave"));
        Assert.Equal("1234****-****-****-****-****56789abc", (string?)doc.Root.Element("ChavePix"));
        Assert.Equal("texto normal", (string?)doc.Root.Element("SemAtributo"));
    }

    [Fact]
    public void Deve_Redatar_Propriedade_Nula_Sem_Erro()
    {
        var redactService = CreateRedactService();
        var serializer = new RedactingXmlSerializer(redactService);

        var pessoa = new Pessoa
        {
            Documento = null,
            Nome = null,
        };

        var xml = serializer.Serialize(pessoa);
        var doc = XDocument.Parse(xml);

        Assert.Null(doc.Root!.Element("Documento"));
        Assert.Null(doc.Root.Element("Nome"));
    }

    [Fact]
    public void Deve_Manter_Propriedades_Sem_Atributo()
    {
        var redactService = CreateRedactService();
        var serializer = new RedactingXmlSerializer(redactService);

        var pessoa = new Pessoa
        {
            SemAtributo = "informação pública",
        };

        var xml = serializer.Serialize(pessoa);
        var doc = XDocument.Parse(xml);

        Assert.Equal("informação pública", (string?)doc.Root!.Element("SemAtributo"));
    }
}
