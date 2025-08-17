using Amazon;
using Amazon.SQS;
using Charles.PayementProcessor.Application.Services;
using Charles.PaymentProcessor.Api.Extension;
using Charles.PaymentProcessor.Domain.Entities;
using Charles.PaymentProcessor.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using PaymentSystem.Infrastructure;
using PaymentSystem.Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddDbContext<PaymentDbContext>(o =>
    o.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var region = RegionEndpoint.GetBySystemName(cfg["AWS:Region"] ?? "us-east-1");
    var serviceUrl = cfg["AWS:ServiceURL"];
    var config = new AmazonSQSConfig { RegionEndpoint = region };
    if (!string.IsNullOrWhiteSpace(serviceUrl)) config.ServiceURL = serviceUrl;
    return new AmazonSQSClient(config);
});

builder.Services.AddScoped<IEventPublisher, SqsEventPublisher>();
builder.Services.AddScoped<ISignatureVerifier, HmacSignatureVerifier>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRepository<Merchant>, Repository<Merchant>>();
builder.Services.AddScoped<IRepository<Payment>, Repository<Payment>>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
await app.SeedDatabaseAsync();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();

