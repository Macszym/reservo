using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Reservo.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Dodaj obsługę kontrolerów API
builder.Services.AddControllers();

// Konfiguracja Swagger/OpenAPI dla dokumentacji API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Reservo API", 
        Version = "v1",
        Description = "API systemu rezerwacji zasobów Reservo"
    });
    
    // Konfiguracja autoryzacji API Key
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Klucz API wymagany do autoryzacji",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });
    
    var key = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            Id = "ApiKey"
        },
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    };
    
    var requirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { key, new List<string>() }
    };
    
    c.AddSecurityRequirement(requirement);
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout       = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly   = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

// Mapowanie kontrolerów MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapowanie kontrolerów API
app.MapControllers();

// Konfiguracja Swagger dla środowiska deweloperskiego
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Reservo API v1");
        c.RoutePrefix = "api-docs";
    });
}
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
db.Database.Migrate(); // upewnij się, że struktura jest aktualna

// Inicjalizuj starego admina dla kompatybilności
if (!db.Loginy.Any())
{
    var admin = new Login { User = "admin" };
    var hasher = new PasswordHasher<Login>();
    admin.Password = hasher.HashPassword(admin, "admin");
    db.Loginy.Add(admin);
}

// Inicjalizuj nowy system użytkowników
if (!db.Users.Any())
{
    var admin = new User 
    { 
        Username = "admin", 
        Role = "Admin",
        ApiKey = AuthHelper.GenerateApiKey()
    };
    admin.Password = AuthHelper.HashPassword(admin, "admin");
    db.Users.Add(admin);
    
    var user = new User 
    { 
        Username = "user", 
        Role = "User",
        ApiKey = AuthHelper.GenerateApiKey()
    };
    user.Password = AuthHelper.HashPassword(user, "user");
    db.Users.Add(user);
}

// Inicjalizuj kategorie
if (!db.Categories.Any())
{
    db.Categories.AddRange(
        new Category { Name = "Sale konferencyjne", Description = "Sale do spotkań i prezentacji", Color = "#007bff" },
        new Category { Name = "Sprzęt IT", Description = "Komputery, projektory, sprzęt audio-video", Color = "#28a745" },
        new Category { Name = "Pojazdy", Description = "Samochody służbowe", Color = "#dc3545" },
        new Category { Name = "Narzędzia", Description = "Narzędzia i sprzęt warsztatowy", Color = "#ffc107" }
    );
    db.SaveChanges(); // Zapisz kategorie przed dodawaniem zasobów
}

// Inicjalizuj przykładowe zasoby
if (!db.Resources.Any())
{
    var categories = db.Categories.ToList();
    var saleCategory = categories.FirstOrDefault(c => c.Name == "Sale konferencyjne");
    var sprzętCategory = categories.FirstOrDefault(c => c.Name == "Sprzęt IT");
    var pojazdyCategory = categories.FirstOrDefault(c => c.Name == "Pojazdy");
    
    var resources = new List<Resource>();
    
    if (saleCategory != null)
    {
        resources.Add(new Resource { Name = "Sala A1", Description = "Sala konferencyjna na 20 osób", Location = "Budynek A, piętro 1", CategoryId = saleCategory.Id });
    }
    
    if (sprzętCategory != null)
    {
        resources.Add(new Resource { Name = "Projektor Sony", Description = "Projektor Full HD", Location = "Magazyn IT", CategoryId = sprzętCategory.Id });
    }
    
    if (pojazdyCategory != null)
    {
        resources.Add(new Resource { Name = "Samochód służbowy #1", Description = "Toyota Corolla 2020", Location = "Parking", CategoryId = pojazdyCategory.Id });
    }
    
    if (resources.Any())
    {
        db.Resources.AddRange(resources);
    }
}

if (!db.Dane.Any())
{
    db.Dane.Add(new Dane { Text = "Przykładowy wpis 1" });
    db.Dane.Add(new Dane { Text = "Drugi wpis z bazy" });
}

db.SaveChanges();


app.Run();
