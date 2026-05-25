using System.Runtime.Serialization;
using EZ.Redact.Lgpd.Core.Attributes;

namespace EZ.Redact.Lgpd.Xml.Tests.Models;

public class Pessoa
{
    [CPFData]
    public string? Documento { get; set; }

    [EmailData]
    public string? Email { get; set; }

    [TelefoneData]
    public string? Telefone { get; set; }

    [NomeData]
    public string? Nome { get; set; }

    [EnderecoData]
    public string? Endereco { get; set; }

    [GuidData]
    public Guid Chave { get; set; }

    [PixData]
    public Guid ChavePix { get; set; }

    public string? SemAtributo { get; set; }
}

[DataContract]
public class PessoaContrato
{
    [DataMember]
    [CPFData]
    public string? Documento { get; set; }

    [DataMember]
    [EmailData]
    public string? Email { get; set; }

    [DataMember]
    [TelefoneData]
    public string? Telefone { get; set; }

    [DataMember]
    [NomeData]
    public string? Nome { get; set; }

    [DataMember]
    [EnderecoData]
    public string? Endereco { get; set; }

    [DataMember]
    public string? SemAtributo { get; set; }
}
