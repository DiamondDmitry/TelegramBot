using System;
using System.Linq;
using Tele.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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
            Console.WriteLine($"{DateTime.Now}: Nearest city is '{locationCity.Name}',  Id {update.Message.Chat.Id}.");
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
        
        // Only process text messages
        if (message.Text is not { } messageText)
            return;

        var userId = update.Message.Chat.Id;
        var chatId = message.Chat.Id;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{DateTime.Now}: Received a '{messageText}' message in chat {userId}.");
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

        if (messageText.StartsWith("/weather"))
        {
            // Get all saved cities from the database by user ID
            var listOfCities = await _weatherService.GetListOfCities(userId);

            if (listOfCities.Count < 1)
            {
                await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"No cities saved. To add a city use the command: <b>/addcity</b> <i>city</i>",
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
                    cancellationToken: cancellationToken);
            }
            return;
        }

        if (messageText.StartsWith("/daily"))
        {
            var city = messageText.Substring(6).Replace("_", " ");
            //var city = cityWithoutSpaces.Replace("_", " ");

            if (string.IsNullOrEmpty(city))
            {
                await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "To get daily weather use the command: <b>/daily</b> <i>city</i>",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                return;
            }

            var coordinate = await _weatherService.GetCoordinatesByCityName(city);
            var weather = await _weatherService.GetWeatherByCoordinates(coordinate.Lat, coordinate.Lon);
            var windDirection = _weatherService.GetWindDirection(weather.Current.WindDeg);
            var iconUrl = InputFile.FromUri($"https://openweathermap.org/img/wn/{weather.Current.Weather[0].Icon}@2x.png");
            var offsetTime = _weatherService.GetOffsetTime(weather.TimezoneOffset);

            await botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: iconUrl,
                    caption: $"City: <a href =\"https://www.google.com/search?q={coordinate.Name}\">{coordinate.Name}</a>, <b>{coordinate.Country}</b>, " +
                                $"<b>{offsetTime.ToString("t")}</b>\n" +
                                $"Temperature: <b>{weather.Current.Temp}</b>°C\n" +
                                $"Feels like: <b>{weather.Current.FeelsLike}</b>°C\n" +
                                $"UV index: <b>{weather.Current.Uvi}</b>\n" +
                                $"Cloudy: <b>{weather.Current.Clouds}</b>%, {weather.Current.Weather[0].Description}\n" +
                                $"Humidity: <b>{weather.Current.Humidity}</b>%\n" +
                                $"Wind : <b>{Math.Round(weather.Current.WindSpeed, 1)}</b> m/s, <b>{windDirection}</b>",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);

            return;
        }
        if (messageText.StartsWith("/addcity"))
        {
            // check the number of saved cities
            var cities = await _weatherService.GetListOfCities(userId);

            if (cities.Count > 4)
            { 
                await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "You can save up to 5 cities. To check the list of saved cities use the command: <b>/list</b>",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                return;
            }

            // add city to the list of saved cities
            var city = messageText.Substring(8);

            if (string.IsNullOrEmpty(city))
            {
                await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "To add a city use the command: <b>/addcity</b> <i>city</i>",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                return;
            }
            else
            {
                var cityWithCoordinates = await _weatherService.GetCoordinatesByCityName(city);

                if (cityWithCoordinates == null)
                {
                    await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"City <b>{city}</b> is not found",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    return;
                }
                else if (cities.Any(x => x.Name == cityWithCoordinates.Name))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"City <b>{city}</b> is already saved",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                    return;
                }
                else
                {
                    var addCity = cityWithCoordinates;
                    addCity.UserId = userId;

                    await _weatherDbService.SaveCityToDb(addCity);

                    await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"City <b>{city}</b> has been added",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    return;
                }
            }
        }

        if (messageText.StartsWith("/list"))
        {
            // Show the list of saved cities
            var listOfCities = await _weatherService.GetListOfCities(userId);
            if (listOfCities.Count < 1)
            {
                await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"No cities saved. To add a city use the command: <b>/addcity</b> <i>city</i>",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                return;
            }

            var text = "";
            foreach (var city in listOfCities)
            {
                string cityWithoutSpaces = city.Name.Replace(" ", "_");

                text += $"/delete_{cityWithoutSpaces}\n";
            }
            text += "/clearlist - clear the list of saved cities";

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: text,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                return;
        }

        if (messageText.StartsWith("/clearlist"))
        {
            // Clear the list of saved cities
            await _weatherDbService.ClearCities(userId);
            await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "The list of saved cities has been cleared.",
                    cancellationToken: cancellationToken);
            return;
        }


        // Delete city from the saved list
        if (messageText.StartsWith("/delete"))
        {
            var cityWithoutSpaces = messageText.Substring(7);
            var city = cityWithoutSpaces.Replace("_", " ");
            var cityToDeleteFromDb = await _weatherService.GetListOfCities(userId);

            if (string.IsNullOrEmpty(city))
            {
                await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "To delete a city from list use the command: <b>/delete</b> <i>city</i>",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                return;
            }
            else
            {
                city = city.Substring(1);
                if (cityToDeleteFromDb.Any(x => x.Name == city))
                {
                    await _weatherDbService.DeleteCityFromDb(city, userId);
                    await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"City <b>{city}</b> has been deleted",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"City <b>{city}</b> is not saved",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    return;
                }
            }
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