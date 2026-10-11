using System.Text.Json.Serialization;
using TicTacToe.Api.Infrastructure;
using TicTacToe.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options => options.Filters.Add<GameExceptionFilter>())
    .AddJsonOptions(options =>
    {
        // Enums travel as strings ("X", "InProgress", "VsComputer") - readable and stable for the frontend.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    });

// All game and scoreboard state lives in memory inside this singleton.
builder.Services.AddSingleton<IGameService, GameService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The Angular dev server normally proxies /api (see frontend/proxy.conf.json); CORS also allows direct calls.
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.MapControllers();

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
