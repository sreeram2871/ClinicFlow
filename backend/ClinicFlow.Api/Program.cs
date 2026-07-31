using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Infrastructure.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Bogus;
using ClinicFlow.Api.Infrastructure.Data.Seeding;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register the tenant provider (temporary version for now)
builder.Services.AddScoped<ICurrentTenantProvider, TemporaryTenantProvider>();

// Register the DbContext, pointing at the connection string
builder.Services.AddDbContext<ClinicFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClinicFlowDbContext>();
    await DbSeeder.SeedAsync(db);
}
app.Run();
