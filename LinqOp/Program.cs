using LinqOp.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContextPool<OrderContext>((DbContextOptionsBuilder dbContextOptionsBuilder) =>
{
    dbContextOptionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("LinqOpConnection"), options =>
    {
        options.CommandTimeout(180);
    });
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin()  // Allows any origin
                  .AllowAnyMethod()  // Allows any HTTP method (GET, POST, etc.)
                  .AllowAnyHeader(); // Allows any header
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles(); // Enable serving static files

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
