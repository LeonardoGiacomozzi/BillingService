using FiapOficina.BillingService.Api.Consumers;
using FiapOficina.BillingService.Api.Services;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using MassTransit;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var authEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled");
if (authEnabled)
{
    var jwtKey = builder.Configuration["JWT_KEY"] ?? throw new InvalidOperationException("JWT_KEY is not configured.");
    var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "fiap-oficina-auth";
    var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "fiap-oficina-services";

    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
            };
        });
    builder.Services.AddAuthorization();
}

builder.Services.AddControllers(options =>
{
    if (authEnabled)
    {
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
    }
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IPaymentService, MercadoPagoService>();
builder.Services.AddAWSService<Amazon.DynamoDBv2.IAmazonDynamoDB>();
builder.Services.AddScoped<DynamoPaymentRepository>();
builder.Services.AddSingleton<IMercadoPagoClientWrapper, MercadoPagoClientWrapper>();

var accessToken = builder.Configuration["MercadoPago:AccessToken"];
if (!string.IsNullOrEmpty(accessToken))
{
    MercadoPago.Config.MercadoPagoConfig.AccessToken = accessToken;
}

builder.Services.AddMassTransit(x =>
{
    // Consumidores de Eventos e Comandos da SAGA
    x.AddConsumer<OrderOpenedConsumer>();
    x.AddConsumer<BudgetApprovedConsumer>();

    x.UsingAmazonSqs((context, cfg) =>
    {
        var sqsUrl = builder.Configuration["AWS:Service:SQS:ServiceURL"];
        cfg.Host("us-east-1", h => 
        { 
            if (!string.IsNullOrEmpty(sqsUrl))
            {
                h.Config(new AmazonSQSConfig { ServiceURL = sqsUrl });
                h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = sqsUrl });
            }
        });
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}
else
{
    app.UseAuthorization();
}

app.MapControllers();

app.Run();

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program { }
