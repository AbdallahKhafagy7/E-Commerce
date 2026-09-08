using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderingDatabase")));

builder.Services.AddHttpClient<ICatalogClient, HttpCatalogClient>(client =>
{
    var catalogBaseUrl = builder.Configuration["CatalogApi:BaseUrl"]
        ?? throw new InvalidOperationException("CatalogApi:BaseUrl is not configured.");

    client.BaseAddress = new Uri(catalogBaseUrl);
});

builder.Services.AddScoped<IPaymentGateway, FakePaymentGateway>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
