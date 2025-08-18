using System.Text;
using Amazon;
using Amazon.SQS;
using Charles.PayementProcessor.Application.Services;
using Charles.PaymentProcessor.Api.Extension;
using Charles.PaymentProcessor.Domain.Entities;
using Charles.PaymentProcessor.Domain.Interfaces;
using Charles.PaymentProcessor.Worker.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PaymentSystem.Infrastructure;
using PaymentSystem.Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddDbContext<PaymentDbContext>(o =>
    o.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var region = RegionEndpoint.GetBySystemName(cfg["AWS:Region"] ?? "eu-north-1");
    var serviceUrl = cfg["AWS:ServiceURL"];
    var config = new AmazonSQSConfig { RegionEndpoint = region };
    if (!string.IsNullOrWhiteSpace(serviceUrl)) config.ServiceURL = serviceUrl;
    return new AmazonSQSClient(config);
});

builder.Services.AddScoped<IEventPublisher, SqsEventPublisher>();
builder.Services.AddScoped<ISignatureVerifier, HmacSignatureVerifier>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IRepository<Merchant>, Repository<Merchant>>();
builder.Services.AddScoped<IRepository<Payment>, Repository<Payment>>();
builder.Services.AddScoped<IRepository<PaymentMethod>, Repository<PaymentMethod>>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = cfg["Jwt:Issuer"],
            ValidAudience = cfg["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Secret"]))
        };
    });

builder.Services.AddHostedService<PaymentEventConsumer>(sp =>
{
    var sqs = sp.GetRequiredService<IAmazonSQS>();
    var logger = sp.GetRequiredService<ILogger<PaymentEventConsumer>>();
    var queueUrl = builder.Configuration["AWS:ServiceURL"];

    return new PaymentEventConsumer(sqs, queueUrl, logger, sp);
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddControllers(setupAction => { setupAction.Filters.Add<ValidateModelAttribute>(); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\""
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        },
    });
});;

var app = builder.Build();
await app.SeedDatabaseAsync();
app.ConfigureException(builder.Environment);
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();


app.Run();

