using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderDb>(options =>
    options.UseSqlite("Data Source=order.db"));

builder.Services.AddHttpClient("inventory", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InventoryUrl"] ?? "http://localhost:5001");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDb>();
    db.Database.EnsureCreated();
}

app.MapPost("/order", async (CreateOrder request, OrderDb db, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("inventory");

    var response = await client.GetAsync($"/items/{request.Sku}");
    if (!response.IsSuccessStatusCode)
    {
        return Results.BadRequest("Unknown SKU");
    }

    var stock = await response.Content.ReadFromJsonAsync<StockDto>();
    if (stock is null || stock.Quantity < request.Quantity)
    {
        return Results.BadRequest("Insufficient stock");
    }

    var order = new Order { Sku = request.Sku, Quantity = request.Quantity };
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Created($"/order/{order.Id}", order);
});

app.MapGet("/order/{id}", async (int id, OrderDb db) =>
    await db.Orders.FindAsync(id) is Order order
        ? Results.Ok(order)
        : Results.NotFound());

app.Run();

public record CreateOrder(string Sku, int Quantity);
public record StockDto(string Sku, int Quantity);

public class Order
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class OrderDb(DbContextOptions<OrderDb> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}