# EZ.Redact.Lgpd.Xml

[![NuGet Version](https://img.shields.io/badge/nuget-v1.0.0-blue.svg)](https://www.nuget.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 8.0+](https://img.shields.io/badge/.NET-8.0%2B%20|%209.0%2B%20|%2010.0%2B-512bd4.svg)](https://dotnet.microsoft.com/download)

Extensão de serialização para o [EZ.Redact.Lgpd.Core](https://github.com/ez-dotnet/ez-redact-lgpd-core). Redige dados pessoais automaticamente durante a serialização XML com **XmlSerializer** e **DataContractSerializer**, sem precisar chamar `ILGPDRedactService` manualmente.

Basta decorar suas models com os atributos do `EZ.Redact.Lgpd.Core` e usar os serializadores — a redação acontece de forma transparente.

> **Para classes geradas a partir de XSD**: os atributos LGPD precisam ser adicionados manualmente, pois o gerador do `xsd.exe` / `dotnet xsd` não os inclui. Você pode usar `partial class` para estender os tipos gerados e decorar as propriedades com os atributos de redação.

---

## Instalação

```bash
dotnet add package EZ.Redact.Lgpd.Xml
```

Registre os servicos no DI:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLGPDRedaction()
                .AddXmlRedaction();
builder.Logging.EnableRedaction(options => options.ApplyDiscriminator = false);
```

> `AddXmlRedaction()` registra os serializadores `RedactingXmlSerializer` e `RedactingDataContractSerializer` como singletons no DI.

## Configuração

### `LGPDRedactOptions`

| Propriedade | Padrão | Descrição |
| :--- | :--- | :--- |
| `MaskChar` | `'*'` | Caractere usado no mascaramento |
| `Guid` | `new()` | Opções de redação de GUID (ver abaixo) |
| `HmacKey` | `null` | Chave HMAC em Base64 (obrigatória se `HmacFor` não estiver vazio) |
| `HmacKeyId` | `1` | Identificador da chave para rotação |
| `HmacFor` | `HashSet<>` vazio | Tipos de dado que devem usar HMAC em vez de masking |

### `GuidOptions`

| Propriedade | Padrão | Descrição |
| :--- | :--- | :--- |
| `PrefixHexCount` | `4` | Quantidade de hex digits preservados no prefixo |
| `SuffixHexCount` | `4` | Quantidade de hex digits preservados no sufixo |

### Três formas de configurar

**1. Em código (`Action<LGPDRedactOptions>`)**
```csharp
builder.Services.AddLGPDRedaction(options =>
{
    options.MaskChar = '#';
    options.Guid.PrefixHexCount = 6;
    options.HmacKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    options.HmacFor.Add(DadoPessoal.CPF);
});
```

**2. Via `IConfiguration` (appsettings.json + env vars)**
```csharp
builder.Services.AddLGPDRedaction(builder.Configuration);
```

```json
{
  "LGPD": {
    "MaskChar": "#",
    "Guid": { "PrefixHexCount": 6 },
    "HmacFor": ["CPF"],
    "HmacKeyId": 1
  }
}
```

A `HmacKey` **não deve** ficar no `appsettings.json`. Use variável de ambiente ou User Secrets:

```bash
export LGPD__HmacKey="suachavebase64aqui=="
```

**3. Combinando ambas**
```csharp
builder.Services.AddLGPDRedaction(options =>
{
    options.HmacKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
});
builder.Services.PostConfigure<LGPDRedactOptions>(opts =>
{
    opts.HmacFor.Add(DadoPessoal.CPF);
});
```

---

## Uso com XmlSerializer

Utilize `RedactingXmlSerializer` — registrado automaticamente pelo `AddXmlRedaction()`:

```csharp
using EZ.Redact.Lgpd.Xml.XmlSerializer;

app.MapGet("/xml", (RedactingXmlSerializer redactor) =>
{
    var pessoa = new Pessoa
    {
        Nome = "Felipe Siqueira",
        Documento = "123.456.789-09",
        Email = "felipe.siqueira@gmail.com"
    };

    return Results.Content(redactor.Serialize(pessoa), "application/xml");
});
```

### Saída redigida

```xml
<Pessoa>
  <Nome>F***** S*******</Nome>
  <Documento>123.***.***-09</Documento>
  <Email>f**************@gmail.com</Email>
</Pessoa>
```

### Serializar para Stream ou TextWriter

```csharp
using var stream = new MemoryStream();
redactor.Serialize(pessoa, stream);
```

---

## Uso com DataContractSerializer

Utilize `RedactingDataContractSerializer` — também registrado pelo `AddXmlRedaction()`:

```csharp
using EZ.Redact.Lgpd.Xml.DataContractSerializer;

app.MapGet("/dc", (RedactingDataContractSerializer redactor) =>
{
    var pessoa = new PessoaContrato
    {
        Nome = "Felipe Siqueira",
        Documento = "123.456.789-09"
    };

    return Results.Content(redactor.Serialize(pessoa), "application/xml");
});
```

O modelo precisa dos atributos `[DataContract]` / `[DataMember]`:

```csharp
[DataContract]
public class PessoaContrato
{
    [DataMember]
    [NomeData]
    public string? Nome { get; set; }

    [DataMember]
    [CPFData]
    public string? Documento { get; set; }
}
```

### Saída redigida

```xml
<PessoaContrato>
  <Nome>F***** S*******</Nome>
  <Documento>123.***.***-09</Documento>
</PessoaContrato>
```

---

## Atributos Suportados

Os atributos são definidos pelo pacote [EZ.Redact.Lgpd.Core](https://github.com/ez-dotnet/ez-redact-lgpd-core) e funcionam com qualquer serializador XML.

### Identificação Pessoal

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[NomeData]` | Mantem apenas as iniciais de cada palavra | `Maria da Silva` | `M**** d* S****` |
| `[CPFData]` | Preserva 3 primeiros e 2 ultimos digitos | `123.456.789-01` | `123.***.***-01` |
| `[CNPJData]` | Preserva raiz (2 caracteres) e radical (6 ultimos) | `12.345.678/0001-90` | `12.***.***/0001-90` |
| `[EmailData]` | Preserva inicial e dominio | `felipe.siqueira@gmail.com` | `f**************@gmail.com` |
| `[TelefoneData]` | Preserva DDD, 1 digito apos DDD e 4 ultimos | `(11) 98888-4444` | `(11) 9****-4444` |
| `[EnderecoData]` | Mantem apenas as iniciais, oculta numeros | `Avenida Paulista, 1000` | `A****** P*******, ****` |
| `[DataGenericaData]` | Preserva ano, mascara dia/mes | `15/03/1990` | `**/**/1990` |

### Documentos Oficiais

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[CNHData]` | Preserva 3 primeiros e 2 ultimos digitos | `12345678901` | `123******01` |
| `[TituloEleitorData]` | Preserva 4 primeiros e 4 ultimos digitos | `1234.5678.9012` | `1234.****.9012` |
| `[PISData]` | Preserva 3 primeiros e digito verificador | `123.45678.90-1` | `123.*****.**-1` |
| `[CNSData]` | Preserva 3 primeiros e 4 ultimos | `123 4567 8901 2345` | `123 **** **** 2345` |
| `[CTPSData]` | Preserva 3 primeiros e 3 ultimos | `1234567890` | `123****890` |
| `[CertidaoData]` | Preserva 6 primeiros e 2 verificadores | `123456.78.1234.5.6.7890.1.12345-67` | `123456.**.****.*.*.****.*.*****-67` |
| `[PassaporteData]` | Preserva prefixo letras e 2 ultimos digitos | `AB123456` | `AB****56` |
| `[RNEData]` | Preserva letra prefixo e digito verificador | `V1234567-8` | `V*******-8` |

### Financeiro

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[CartaoCreditoData]` | Preserva 4 primeiros e 4 ultimos digitos | `4532 1178 9012 3456` | `4532 **** **** 3456` |
| `[ContaBancariaData]` | Preserva operacao e digito, mascara conta | `013.123456-7` | `013.******-7` |
| `[PixData]` | Mascara chave aleatoria mantendo 4 primeiros e 8 ultimos | `e8d26618-2e11-4b22-8d26-66182e114b22` | `e8d2****-****-****-****-****2e114b22` |

### Redes e Localização

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[EnderecoIPData]` | Mascara os 2 ultimos octetos (IPv4) e os ultimos 3 grupos (IPv6) | `192.168.1.100` | `192.168.*.***` |
| `[MacAddressData]` | Preserva prefixo OUI (3 primeiros bytes) | `00:1A:2B:3C:4D:5E` | `00:1A:2B:**:**:**` |
| `[CEPData]` | Mascara os 3 ultimos digitos | `01310-900` | `01310-***` |
| `[GeolocalizacaoData]` | Mascara parte decimal de latitude e longitude | `-23.5505, -46.6333` | `-23.****, -46.****` |

### Veículo

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[PlacaData]` | Mascara numeros (padrao antigo) e caracteres apos prefixo (Mercosul) | `ABC-1234` | `ABC-****` |
| `[RenavamData]` | Preserva 3 primeiros e 3 ultimos digitos | `12345678901` | `123*****901` |

### Técnico

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[GuidData]` | Mascara GUID mantendo 4 primeiros e 4 ultimos hex digitos | `e8d26618-2e11-4b22-8d26-66182e114b22` | `e8d2****-****-****-****-*******4b22` |

---

## Samples

Um projeto de exemplo na pasta `samples/`:

| Projeto | Descrição |
| :--- | :--- |
| [`EZ.Redact.Lgpd.Xml.Sample`](samples/EZ.Redact.Lgpd.Xml.Sample) | Minimal API com endpoints `/xml` (XmlSerializer) e `/dc` (DataContractSerializer) |

```bash
dotnet run --project samples/EZ.Redact.Lgpd.Xml.Sample
curl http://localhost:5000/xml
curl http://localhost:5000/dc
```

---

## Licenca

Distribuido sob a licenca MIT.
