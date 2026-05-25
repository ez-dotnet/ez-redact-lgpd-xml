using EZ.Redact.Lgpd.Xml;
using EZ.Redact.Lgpd.Xml.Sample.Models;
using EZ.Redact.Lgpd.Xml.XmlSerializer;
using EZ.Redact.Lgpd.Xml.DataContractSerializer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLGPDRedaction()
                .AddXmlRedaction();

var app = builder.Build();

app.MapGet("/xml", (RedactingXmlSerializer redactor) =>
    Results.Content(redactor.Serialize(new Pessoa()), "application/xml"));

app.MapGet("/dc", (RedactingDataContractSerializer redactor) =>
    Results.Content(redactor.Serialize(new PessoaContrato()), "application/xml"));

app.Run();
