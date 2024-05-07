using System;
using System.Linq;
using System.Reflection;
using Tele.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;
using InputFile = Telegram.Bot.Types.InputFile;

namespace Tele.Bot.Services;

public class TelegramService : ITelegramService
{
    // DI services
    // private readonly interface _interface;
    private readonly IWeatherDbService _weatherDbService;
    private readonly IWeatherService _weatherService;

    private readonly string? WeatherBotKey = Environment.GetEnvironmentVariable("TelegramWeatherBotKey");
    public TelegramService(IWeatherService weatherService, IWeatherDbService weatherDbService)
    {
        _weatherService = weatherService;
        _weatherDbService = weatherDbService;

    }

    public Task StartBot(CancellationTokenSource cts)
    {
        var botClient = new TelegramBotClient(WeatherBotKey);

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery != null)
        {
            var callbackData = update.CallbackQuery.Data;
            var CallbackChatId = update.CallbackQuery.Message.Chat.Id;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{DateTime.Now}: Received a '{callbackData}' callback data in chat '{CallbackChatId}'.");
            Console.ResetColor();

            if (callbackData.StartsWith("/daily "))
            {
                var city = callbackData.Substring(7);
                var сoordinates = await _weatherService.GetCoordinatesByCityName(city);
                var dailyWeather = await _weatherService.GetDailyWeatherByCoordinates(сoordinates.Lat, сoordinates.Lon);

                if (dailyWeather.Daily == null)
                {
                    await botClient.SendTextMessageAsync(
                            chatId: CallbackChatId,
                            text: $"No daily weather for <b>{city}</b>",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    return;
                }

                var text = $"Daily weather for <b>{city}</b>\n";
                foreach (var daily in dailyWeather.Daily.Skip(1).Take(5))
                {
                    var date = DateTimeOffset.FromUnixTimeSeconds(daily.Dt).Date;
                    text += "-----------------------------------\n" +
                            $"<b>{date.DayOfWeek}, {date.ToString("MMMM d")}</b>:\n" +
                            $"<b>{daily.Weather[0].Description}</b> \n" +
                            $"Day:<b>{Math.Round(daily.Temp.Day, 0)}°C</b>, Night: <b>{Math.Round(daily.Temp.Night, 0)}°C</b>\n" +
                            $"Cloudy: <b>{daily.Clouds}</b>%, UV: <b>{daily.Uvi}</b>\n" +
                            $"Wind : <b>{Math.Round(daily.WindSpeed, 1)}</b> m/s, <b>{_weatherService.GetWindDirection(daily.WindDeg)}</b>\n";
                }
                await botClient.SendTextMessageAsync(
                        chatId: CallbackChatId,
                        text: text,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                return;
            }

            if (callbackData.StartsWith("/hourly "))
            {
                var city = callbackData.Substring(8);
                var сoordinates = await _weatherService.GetCoordinatesByCityName(city);
                var hourlyWeather = await _weatherService.GetHourlyWeatherByCoordinates(сoordinates.Lat, сoordinates.Lon);

                if (hourlyWeather.Hourly == null)
                {
                    await botClient.SendTextMessageAsync(
                            chatId: CallbackChatId,
                            text: $"No hourly weather for <b>{city}</b>",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    return;
                }

                var text = $"Hourly weather for <b>{city}</b>\n";
                foreach (var hourly in hourlyWeather.Hourly.Take(12))
                {
                    var date = DateTimeOffset.FromUnixTimeSeconds(hourly.Dt);
                    text += "-----------------------------------\n" +
                            $"<b>{date.ToString("t")}</b>: " +
                            $"<b>{hourly.Weather[0].Description}</b> \n" +
                            $"Temperature: <b>{Math.Round(hourly.Temp, 0)}</b>°C\n" +
                            $"Cloudy: <b>{hourly.Clouds}</b>%, UV: <b>{hourly.Uvi}</b>\n" +
                            $"Wind : <b>{Math.Round(hourly.WindSpeed, 1)}</b> m/s, <b>{_weatherService.GetWindDirection(hourly.WindDeg)}</b>\n";
                }
                await botClient.SendTextMessageAsync(
                        chatId: CallbackChatId,
                        text: text,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                return;
            }

            if (callbackData.StartsWith("/addcity "))
            {
                // check the number of saved cities
                var cities = await _weatherService.GetListOfCities(CallbackChatId);

                if (cities.Count > 4)
                {
                    await botClient.SendTextMessageAsync(
                            chatId: CallbackChatId,
                            text: "You can save up to 5 cities.",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    return;
                }

                // add city to the list of saved cities
                var city = callbackData.Substring(9);
                var сoordinates = await _weatherService.GetCoordinatesByCityName(city);

                if (cities.Any(x => x.Name == сoordinates.Name))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: CallbackChatId,
                        text: $"City <b>{city}</b> is already saved",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                    return;
                }
                else
                {
                    var addCity = сoordinates;
                    addCity.UserId = CallbackChatId;

                    await _weatherDbService.SaveCityToDb(addCity);

                    await botClient.SendTextMessageAsync(
                            chatId: CallbackChatId,
                            text: $"City <b>{city}</b> has been added",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    return;
                }
            }

            // Delete city from the saved list
            if (callbackData.StartsWith("/delete "))
            {
                var city = callbackData.Substring(8);
                var cityToDeleteFromDb = await _weatherService.GetListOfCities(CallbackChatId);

                await _weatherDbService.DeleteCityFromDb(city, CallbackChatId);
                await botClient.SendTextMessageAsync(
                        chatId: CallbackChatId,
                        text: $"City <b>{city}</b> has been deleted",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                return;
            }

            return;
        }

        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;

        // Only process location messages
        if (update.Message.Location is { } location)
        {
            var locationWeather = await _weatherService.GetWeatherByCoordinates(location.Latitude, location.Longitude);
            var locationWindDirection = _weatherService.GetWindDirection(locationWeather.Current.WindDeg);
            var locationOffsetTime = _weatherService.GetOffsetTime(locationWeather.TimezoneOffset);
            var locationCity = await _weatherService.GetCityNameByCoordinates(location.Latitude, location.Longitude);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{DateTime.Now}: Recive a '{locationCity.Name}' location data,  Id '{update.Message.Chat.Id}'.");
            Console.ResetColor();

            var testIconUrl = InputFile.FromUri($"https://openweathermap.org/img/wn/{locationWeather.Current.Weather[0].Icon}@2x.png");
            await botClient.SendPhotoAsync(
                    chatId: update.Message.Chat.Id,
                    photo: testIconUrl,
                    caption: $"<b>Weather in the current location</b>\n" +
                             $"<a href =\"https://www.google.com/search?q={locationCity.Name}\">{locationCity.Name}</a>, <b>{locationCity.Country}</b>, " +
                             $"<b>{locationOffsetTime.ToString("t")}</b>\n" +
                             $"Temperature: <b>{locationWeather.Current.Temp}</b>°C\n" +
                             $"Feels like: <b>{locationWeather.Current.FeelsLike}</b>°C\n" +
                             $"UV index: <b>{locationWeather.Current.Uvi}</b>\n" +
                             $"Cloudy: <b>{locationWeather.Current.Clouds}</b>%, {locationWeather.Current.Weather[0].Description}\n" +
                             $"Humidity: <b>{locationWeather.Current.Humidity}</b>%\n" +
                             $"Wind : <b>{Math.Round(locationWeather.Current.WindSpeed, 1)}</b> m/s, <b>{locationWindDirection}</b>",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            return;
        }

        //Only process text messages
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{DateTime.Now}: Received a '{messageText}' message in chat {chatId}.");
        Console.ResetColor();


        // Working with commands
        // Start command
        if (messageText.StartsWith("/start"))
        {
            // Generate weather button
            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
            {
                KeyboardButton.WithRequestLocation("Get weather by your Location"),
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "📍",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);

            await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "To get the weather, please enter the city name.",
                    cancellationToken: cancellationToken);
            return;

        }

        // Show weather for saved cities
        if (messageText.StartsWith("/weather"))
        {
            // Get all saved cities from the database by user ID
            var listOfCities = await _weatherService.GetListOfCities(chatId);

            if (listOfCities.Count < 1)
            {
                await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"No cities saved. To add a city to list please enter the city name and than press ➕",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                return;
            }

            // Show weather for each saved city
            foreach (var city in listOfCities)
            {
                var weather = await _weatherService.GetWeatherByCoordinates(city.Lat, city.Lon);
                var iconUrl = InputFile.FromUri($"https://openweathermap.org/img/wn/{weather.Current.Weather[0].Icon}@2x.png");
                var windDirection = _weatherService.GetWindDirection(weather.Current.WindDeg);
                var offsetTime = _weatherService.GetOffsetTime(weather.TimezoneOffset);
                var dailyButton = $"/daily {city.Name}";
                var hourlyButton = $"/hourly {city.Name}";

                await botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: iconUrl,
                    caption: $"City: <a href =\"https://www.google.com/search?q={city.Name}\">{city.Name}</a>, <b>{city.Country}</b>, " +
                                $"<b>{offsetTime.ToString("t")}</b>\n" +
                                $"Temperature: <b>{weather.Current.Temp}</b>°C\n" +
                                $"Feels like: <b>{weather.Current.FeelsLike}</b>°C\n" +
                                $"UV index: <b>{weather.Current.Uvi}</b>\n" +
                                $"Cloudy: <b>{weather.Current.Clouds}</b>%, {weather.Current.Weather[0].Description}\n" +
                                $"Humidity: <b>{weather.Current.Humidity}</b>%\n" +
                                $"Wind : <b>{Math.Round(weather.Current.WindSpeed, 1)}</b> m/s, <b>{windDirection}</b>",
                    parseMode: ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new []
                            {
                                InlineKeyboardButton.WithCallbackData("Daily", dailyButton),
                                InlineKeyboardButton.WithCallbackData("Hourly", hourlyButton),
                                InlineKeyboardButton.WithCallbackData("❌", $"/delete {city.Name}"),
                            }
                    }),
            cancellationToken: cancellationToken);
            }
            return;
        }

        // Show weather by entered city name
        var cityCoordinates = await _weatherService.GetCoordinatesByCityName(messageText);

        if (cityCoordinates == null)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"City {messageText} is not found",
                cancellationToken: cancellationToken);
            return;
        }

        var cityWeather = await _weatherService.GetWeatherByCoordinates(cityCoordinates.Lat, cityCoordinates.Lon);
        var cityIconUrl = InputFile.FromUri($"https://openweathermap.org/img/wn/{cityWeather.Current.Weather[0].Icon}@2x.png");
        var cityWindDirection = _weatherService.GetWindDirection(cityWeather.Current.WindDeg);
        var cityOffsetTime = _weatherService.GetOffsetTime(cityWeather.TimezoneOffset);

        await botClient.SendPhotoAsync(
                chatId: chatId,
                photo: cityIconUrl,
                replyToMessageId: update.Message.MessageId,
                caption: $"City: <a href =\"https://www.google.com/search?q={cityCoordinates.Name}\">{cityCoordinates.Name}</a>, " +
                         $"<b>{cityCoordinates.Country}</b>, <b>{cityOffsetTime.ToString("t")}</b>\n" +
                         $"Temperature: <b>{cityWeather.Current.Temp}</b>°C\n" +
                         $"Feels like: <b>{cityWeather.Current.FeelsLike}</b>°C\n" +
                         $"UV index: <b>{cityWeather.Current.Uvi}</b>\n" +
                         $"Cloudy: <b>{cityWeather.Current.Clouds}</b>%, {cityWeather.Current.Weather[0].Description}\n" +
                         $"Humidity: <b>{cityWeather.Current.Humidity}</b>%\n" +
                         $"Wind : <b>{Math.Round(cityWeather.Current.WindSpeed, 1)}</b> m/s, <b>{cityWindDirection}</b>",
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new []
                            {
                                InlineKeyboardButton.WithCallbackData("Daily", $"/daily {cityCoordinates.Name}"),
                                InlineKeyboardButton.WithCallbackData("Hourly", $"/hourly {cityCoordinates.Name}"),
                                InlineKeyboardButton.WithCallbackData("➕", $"/addcity {cityCoordinates.Name}")
                            }
                    }),
                cancellationToken: cancellationToken);
        return;
    }


        // Method to handle errors. Outputs error to console if it occurs
        private async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);

        }
    }