using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using csharpAPI.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("C# API is running...");
Console.WriteLine($"Using connection string: {connectionString}");

app.Run();