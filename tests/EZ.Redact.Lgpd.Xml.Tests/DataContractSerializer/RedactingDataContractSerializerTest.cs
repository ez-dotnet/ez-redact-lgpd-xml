using System.Runtime.Serialization;
using System.Xml.Linq;
using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.Xml.DataContractSerializer;
using EZ.Redact.Lgpd.Xml.Tests.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EZ.Redact.Lgpd.Xml.Tests.DataContractSerializer;

public class RedactingDataContractSerializerTest
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
        var serializer = new RedactingDataContractSerializer(redactService);

        var pessoa = new PessoaContrato
        {
            Documento = "123.456.789-09",
            Email = "joao@email.com",
            Telefone = "(11) 9 8888-4444",
            Nome = "João Silva",
            Endereco = "Rua das Flores, 123 - São Paulo/SP",
            SemAtributo = "texto normal"
        };

        var xml = serializer.Serialize(pessoa);
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        Assert.Equal("123.***.***-09", FindValue(root, "Documento"));
        Assert.Equal("j***@email.com", FindValue(root, "Email"));
        Assert.Equal("(11) 9 ****-4444", FindValue(root, "Telefone"));
        Assert.Equal("J*** S****", FindValue(root, "Nome"));
        Assert.Equal("R** d** F*****, *** - S** P*******", FindValue(root, "Endereco"));
        Assert.Equal("texto normal", FindValue(root, "SemAtributo"));
    }

    [Fact]
    public void Deve_Redatar_Propriedade_Nula_Sem_Erro()
    {
        var redactService = CreateRedactService();
        var serializer = new RedactingDataContractSerializer(redactService);

        var pessoa = new PessoaContrato
        {
            Documento = null,
            Nome = null,
        };

        var xml = serializer.Serialize(pessoa);
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        var documento = FindElement(root, "Documento");
        Assert.NotNull(documento);
        Assert.True((bool?)documento!.Attribute("{http://www.w3.org/2001/XMLSchema-instance}nil") ?? false);

        var nome = FindElement(root, "Nome");
        Assert.NotNull(nome);
        Assert.True((bool?)nome!.Attribute("{http://www.w3.org/2001/XMLSchema-instance}nil") ?? false);
    }

    [Fact]
    public void Deve_Manter_Propriedades_Sem_Atributo()
    {
        var redactService = CreateRedactService();
        var serializer = new RedactingDataContractSerializer(redactService);

        var pessoa = new PessoaContrato
        {
            SemAtributo = "informação pública",
        };

        var xml = serializer.Serialize(pessoa);
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        Assert.Equal("informação pública", FindValue(root, "SemAtributo"));
    }

    private static string? FindValue(XElement root, string localName)
    {
        return (string?)FindElement(root, localName);
    }

    private static XElement? FindElement(XElement root, string localName)
    {
        return root.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
    }
}
