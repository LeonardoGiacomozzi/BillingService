using FiapOficina.BillingService.Api.Consumers;
using FiapOficina.BillingService.Api.Services;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using MassTransit;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
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
app.UseAuthorization();
app.MapControllers();

app.Run();

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program { }
