using EZ.Redact.Lgpd.Core.Attributes;
using System.Runtime.Serialization;

namespace EZ.Redact.Lgpd.Xml.Sample.Models;

public class Pessoa
{
    [CPFData]
    public string Documento { get; set; } = "123.456.789-09";

    [EmailData]
    public string Email { get; set; } = "felipe.siqueira@gmail.com";

    [TelefoneData]
    public string Telefone { get; set; } = "(11) 9 8888-4444";

    [NomeData]
    public string Nome { get; set; } = "Felipe Siqueira";

    [EnderecoData]
    public string Endereco { get; set; } = "Avenida Paulista, 1000 - São Paulo/SP";

    [GuidData]
    public Guid Chave { get; set; } = Guid.Parse("e8d26618-2e11-4b22-8d26-66182e114b22");

    public string SemAtributo { get; set; } = "informação pública";
}

[DataContract]
public class PessoaContrato
{
    [DataMember]
    [CPFData]
    public string Documento { get; set; } = "123.456.789-09";

    [DataMember]
    [EmailData]
    public string Email { get; set; } = "felipe.siqueira@gmail.com";

    [DataMember]
    [TelefoneData]
    public string Telefone { get; set; } = "(11) 9 8888-4444";

    [DataMember]
    [NomeData]
    public string Nome { get; set; } = "Felipe Siqueira";

    [DataMember]
    [EnderecoData]
    public string Endereco { get; set; } = "Avenida Paulista, 1000 - São Paulo/SP";

    [DataMember]
    public string SemAtributo { get; set; } = "informação pública";
}
