using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using MantIA.DAL.Tenancy;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MantIA.DAL.Context.MantIADbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("MantIADb"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<ICurrentTenant, CurrentTenant>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
