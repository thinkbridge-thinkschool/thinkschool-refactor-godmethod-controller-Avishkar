using LegacyOrderApi.Models;
using LegacyOrderApi.Repositories;
using LegacyOrderApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure In-Memory Database for simplicity
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("LegacyOrderDb"));

// Register layers via DI
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// Seed data for manual testing
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!db.Users.Any())
    {
        db.Users.Add(new User { Id = 1, Email = "test@example.com", IsActive = true, AccountBalance = 5000m });
        db.Products.Add(new Product { Id = 1, Name = "Test Product", Price = 100m, StockQuantity = 20 });
        db.Products.Add(new Product { Id = 2, Name = "Premium Product", Price = 600m, StockQuantity = 5 });
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Required for integration tests
public partial class Program { }
