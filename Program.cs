using EduConnect.Repositories;
using EduConnect.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<ClassRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<StudentDashboardRepository>();
builder.Services.AddScoped<OwnerRepository>();
builder.Services.AddScoped<TopperRepository>();
builder.Services.AddSingleton<PasswordResetService>();
builder.Services.AddHttpClient();
builder.Services.AddControllers(); 
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();     // ADD THIS — serves index.html automatically
app.UseStaticFiles();      // ADD THIS — enables serving files from wwwroot

app.MapControllers();      //// ADD THIS — routes requests to ClassesController etc.

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
