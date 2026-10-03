using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<InventoryDb>(options =>
    options.UseSqlite("Data Source=inventory.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDb>();
    db.Database.EnsureCreated();

    if (!db.Items.Any())
    {
        db.Items.AddRange(
            new Item { Sku = "BOOK-1", Quantity = 10 },
            new Item { Sku = "PEN-1", Quantity = 50 });

        db.SaveChanges();
    }
}

app.MapGet("/items/{sku}", async (string sku, InventoryDb db) =>
    await db.Items.FirstOrDefaultAsync(i => i.Sku == sku) is Item item
        ? Results.Ok(item)
        : Results.NotFound());

app.Run();

public class Item
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class InventoryDb(DbContextOptions<InventoryDb> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();
}