using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using GestioLan.API.Models;
using Serilog;
using System;
using System.Text; // Per Encoding
using Microsoft.AspNetCore.Authentication.JwtBearer; // Per JwtBearerDefaults
using Microsoft.IdentityModel.Tokens; // Per TokenValidationParameters e SymmetricSecurityKey
using GestioLan.API.Utils.JWT; // Per la classe JWT 
using GestioLan.API.Services.Categories; // Per ICategoryService e CategoryService
using GestioLan.API.Services.Images; // Per IImageService e ImageService
using GestioLan.API.Services.Users; // Per IUserService e UserService
using GestioLan.API.Services.Items; // Per IItemService e ItemService



var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURAZIONE SERILOG ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console() // Continua a scrivere in console per Docker
    .WriteTo.File("/app/logs/api_log.txt", // Scrive nel file su Palla2
        rollingInterval: RollingInterval.Day, // Crea un file nuovo ogni giorno
        retainedFileCountLimit: 7) // Tiene solo gli ultimi 7 giorni
    .CreateLogger();

builder.Host.UseSerilog(); // Dice all'API di usare Serilog
// ------------------------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Registrazone dei servizi essenziali per i controller
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IItemService, ItemService>();


// Connection String per il db
var connectionString = builder.Configuration.GetConnectionString("GestioLANConnection");

builder.Services.AddDbContext<GestioLanContext>(options =>
    options.UseMySql(connectionString,
        new MariaDbServerVersion(new Version(10, 11, 13))));



// sezione che gestisce il JWT
var jwtSecret = builder.Configuration["JwtSettings:Key"];
if (string.IsNullOrEmpty(jwtSecret))
{
    //Console.WriteLine("WARNING: JWT_SECRET is not set. Please set it in the environment variables or appsettings.json.");
    throw new InvalidOperationException("FATAL: JWT_SECRET is not set!");
}
else
{
    Console.WriteLine("JWT_SECRET is set.");
}

var keyBytes = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddScoped<JWT>(); // Registra la classe JWT come servizio, così può essere iniettata nei controller



builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("isAdmin", "true"));
});

// qui costruisce l'app con le configurazioni di prima e la esegue
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Fondamentale per far sì che l'API possa leggere/scrivere file 
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("C# API is running...");
Console.WriteLine($"Using connection string: {connectionString}");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

if(app.Environment.IsDevelopment())
{
    // --- MIGRAZIONE AUTOMATICA DEL DATABASE ---
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<GestioLanContext>();

            // Questo comando applica le migrazioni pendenti. 
            // Se il DB non esiste, lo crea.
            if (context.Database.GetPendingMigrations().Any())
                {
                Console.WriteLine("Applicazione migrazioni in corso...");
                context.Database.Migrate();
                Console.WriteLine("Database aggiornato con successo!");
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Si è verificato un errore durante la migrazione del database.");
        }
    }
    // -------------------------------------------
}

app.Run();