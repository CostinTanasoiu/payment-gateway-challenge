using FluentValidation;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var bankHttpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:8080")
};
builder.Services.AddSingleton<IAcquiringBankService>(new AcquiringBankService(bankHttpClient));

builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddTransient<IValidator<PostPaymentRequest>, PaymentRequestValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
