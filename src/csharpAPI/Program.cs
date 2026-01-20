using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using csharpAPI.Models;
using Serilog;
using System;

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

var connectionString = builder.Configuration.GetConnectionString("GestioLANConnection");


builder.Services.AddDbContext<GestioLanContext>(options =>
    options.UseMySql(connectionString,
        new MariaDbServerVersion(new Version(10, 11, 13))));

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
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("C# API is running...");
Console.WriteLine($"Using connection string: {connectionString}");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

app.Run();