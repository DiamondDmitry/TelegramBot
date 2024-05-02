using Microsoft.AspNetCore.Authentication;
using Tele.Bot.Client;
using Tele.Bot.Models;
using Tele.Bot.Services;

var builder = WebApplication.CreateBuilder(args);

// Dependency Injections

builder.Services.AddTransient<IWeatherService, WeatherService>();
builder.Services.AddSingleton<ITelegramService, TelegramService>();
builder.Services.AddTransient<IRestApiClient, RestApiClient>();
builder.Services.AddTransient<IWeatherDbService, WEatherDbService>();


builder.Services.AddHttpClient<RestApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.openweathermap.org/");
});

builder.Services.AddEntityFrameworkSqlite()
                .AddDbContext<WeatherContext>();

var serviceProvide = builder.Services.BuildServiceProvider();
var context = serviceProvide.GetRequiredService<WeatherContext>();

//Check database exists, if not create it
context.Database.EnsureCreated();

var app = builder.Build();

using var cts = new CancellationTokenSource();

var provider = builder.Services.BuildServiceProvider();
var teleBot = provider.GetService<ITelegramService>();

teleBot.StartBot(cts);
app.Run();

// Send cancellation request to stop bot
cts.Cancel();

