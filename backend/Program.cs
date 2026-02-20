using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using BlazorApp1.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddDbContext<KnjiznicaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5001", "http://localhost:5007", "https://localhost:7139")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KnjiznicaDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        await db.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Pogreška pri spajanju na PostgreSQL.");
        throw;
    }
}

app.UseCors();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapGet("/api/knjige", async (KnjiznicaDbContext db) =>
{
    var knjige = await db.Knjige
        .Include(k => k.KnjiznicaKnjige)
        .ThenInclude(kk => kk.Knjiznica)
        .ToListAsync();
    var dtos = knjige.Select(k => new KnjigaResponseDto
    {
        Id = k.Id,
        Naziv = k.Naziv,
        Autor = k.Autor,
        ISBN = k.ISBN,
        KnjiznicaKnjige = k.KnjiznicaKnjige.Select(kk => new KnjiznicaKnjigaResponseDto
        {
            Id = kk.Id,
            KnjiznicaId = kk.KnjiznicaId,
            KnjigaId = kk.KnjigaId,
            Knjiznica = kk.Knjiznica == null ? null : new KnjiznicaResponseDto { Id = kk.Knjiznica.Id, Naziv = kk.Knjiznica.Naziv }
        }).ToList()
    }).ToList();
    return Results.Ok(dtos);
});

app.MapGet("/api/knjige/{id:int}", async (int id, KnjiznicaDbContext db) =>
{
    var knjiga = await db.Knjige
        .Include(k => k.KnjiznicaKnjige)
        .ThenInclude(kk => kk.Knjiznica)
        .FirstOrDefaultAsync(k => k.Id == id);
    if (knjiga is null) return Results.NotFound();
    var dto = new KnjigaResponseDto
    {
        Id = knjiga.Id,
        Naziv = knjiga.Naziv,
        Autor = knjiga.Autor,
        ISBN = knjiga.ISBN,
        KnjiznicaKnjige = knjiga.KnjiznicaKnjige.Select(kk => new KnjiznicaKnjigaResponseDto
        {
            Id = kk.Id,
            KnjiznicaId = kk.KnjiznicaId,
            KnjigaId = kk.KnjigaId,
            Knjiznica = kk.Knjiznica == null ? null : new KnjiznicaResponseDto { Id = kk.Knjiznica.Id, Naziv = kk.Knjiznica.Naziv }
        }).ToList()
    };
    return Results.Ok(dto);
});

app.MapPost("/api/knjige", async (HttpContext ctx, KnjiznicaDbContext db) =>
{
    var req = await ctx.Request.ReadFromJsonAsync<CreateKnjigaRequest>();
    if (req is null) return Results.BadRequest(new { error = "Tijelo zahtjeva je prazno ili nevažeći JSON." });
    if (string.IsNullOrWhiteSpace(req.Naziv)) return Results.BadRequest(new { error = "Naziv je obavezan." });
    if (string.IsNullOrWhiteSpace(req.Autor)) return Results.BadRequest(new { error = "Autor je obavezan." });

    var knjiga = new Knjige
    {
        Naziv = req.Naziv,
        Autor = req.Autor,
        ISBN = req.ISBN
    };
    db.Knjige.Add(knjiga);
    await db.SaveChangesAsync();

    foreach (var knjiznicaId in req.KnjizniceIds ?? [])
    {
        db.KnjiznicaKnjige.Add(new KnjiznicaKnjige
        {
            KnjigaId = knjiga.Id,
            KnjiznicaId = knjiznicaId,
            BrojPrimjeraka = 1
        });
    }
    await db.SaveChangesAsync();
    return Results.Created($"/api/knjige/{knjiga.Id}", knjiga);
});

app.MapPut("/api/knjige/{id:int}", async (int id, HttpContext ctx, KnjiznicaDbContext db) =>
{
    var req = await ctx.Request.ReadFromJsonAsync<UpdateKnjigaRequest>();
    if (req is null) return Results.BadRequest(new { error = "Tijelo zahtjeva je prazno ili nevažeći JSON." });
    if (string.IsNullOrWhiteSpace(req.Naziv)) return Results.BadRequest(new { error = "Naziv je obavezan." });
    if (string.IsNullOrWhiteSpace(req.Autor)) return Results.BadRequest(new { error = "Autor je obavezan." });

    var knjiga = await db.Knjige.FindAsync(id);
    if (knjiga is null) return Results.NotFound();

    knjiga.Naziv = req.Naziv;
    knjiga.Autor = req.Autor;
    knjiga.ISBN = req.ISBN;

    var existing = await db.KnjiznicaKnjige.Where(kk => kk.KnjigaId == id).ToListAsync();
    db.KnjiznicaKnjige.RemoveRange(existing);

    foreach (var knjiznicaId in req.KnjizniceIds ?? [])
    {
        db.KnjiznicaKnjige.Add(new KnjiznicaKnjige
        {
            KnjigaId = id,
            KnjiznicaId = knjiznicaId,
            BrojPrimjeraka = 1
        });
    }
    await db.SaveChangesAsync();
    return Results.Ok(knjiga);
});

app.MapDelete("/api/knjige/{id:int}", async (int id, KnjiznicaDbContext db) =>
{
    var knjiga = await db.Knjige.FindAsync(id);
    if (knjiga is null) return Results.NotFound();
    var veze = await db.KnjiznicaKnjige.Where(kk => kk.KnjigaId == id).ToListAsync();
    db.KnjiznicaKnjige.RemoveRange(veze);
    db.Knjige.Remove(knjiga);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/api/knjiznice", async (KnjiznicaDbContext db) =>
{
    var knjiznice = await db.Knjiznice.OrderBy(k => k.Naziv).ToListAsync();
    return Results.Ok(knjiznice);
});

app.MapGet("/api/knjiznice/{id:int}", async (int id, KnjiznicaDbContext db) =>
{
    var knjiznica = await db.Knjiznice
        .Include(k => k.KnjiznicaKnjige)
        .ThenInclude(kk => kk.Knjiga)
        .FirstOrDefaultAsync(k => k.Id == id);
    return knjiznica is null ? Results.NotFound() : Results.Ok(knjiznica);
});

app.MapGet("/", () => Results.Json(new { message = "Knjižnica API", endpoints = new[] { "/api/knjige", "/api/knjiznice" } }));

app.Run();

public class CreateKnjigaRequest
{
    public string Naziv { get; set; } = "";
    public string Autor { get; set; } = "";
    public string ISBN { get; set; } = "";
    public List<int>? KnjizniceIds { get; set; }
}
public class UpdateKnjigaRequest
{
    public string Naziv { get; set; } = "";
    public string Autor { get; set; } = "";
    public string ISBN { get; set; } = "";
    public List<int>? KnjizniceIds { get; set; }
}

class KnjigaResponseDto { public int Id { get; set; } public string Naziv { get; set; } = ""; public string Autor { get; set; } = ""; public string ISBN { get; set; } = ""; public List<KnjiznicaKnjigaResponseDto> KnjiznicaKnjige { get; set; } = new(); }
class KnjiznicaKnjigaResponseDto { public int Id { get; set; } public int KnjiznicaId { get; set; } public int KnjigaId { get; set; } public KnjiznicaResponseDto? Knjiznica { get; set; } }
class KnjiznicaResponseDto { public int Id { get; set; } public string Naziv { get; set; } = ""; }
