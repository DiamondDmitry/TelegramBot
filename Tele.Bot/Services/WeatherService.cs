﻿using SQLitePCL;
using System.Collections.Generic;
using Tele.Bot.Client;
using Tele.Bot.Models;

namespace Tele.Bot.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly WeatherContext _context;

        private readonly RestApiClient _restApiClient;
        public WeatherService(RestApiClient restApiClient, WeatherContext context)
        {
            _restApiClient = restApiClient;
            _context = context;
        }

        private readonly string? apiKey = Environment.GetEnvironmentVariable("OpenWeatherApiKey");

        public async Task<City> GetCoordinatesByCityName(string city)
        {
            var result = await _restApiClient.SendGetRequest<List<City>>($"geo/1.0/direct?q={city}&limit=1&appid={apiKey}");
            if (result == null || result.Count == 0)
            {
                return null;
            }
            return result[0];
        }

        public async Task<City> GetCityNameByCoordinates(double lat, double lon)
        {
            var result = await _restApiClient.SendGetRequest<List<City>>($"geo/1.0/reverse?lat={lat}&lon={lon}&limit=1&appid={apiKey}");
            if (result == null || result.Count == 0)
            {
                return null;
            }
            return result[0];
        }

        public async Task<Root> GetWeatherByCoordinates(double lat, double lon)
        {
            var result = await _restApiClient.SendGetRequest<Root>($"data/3.0/onecall?lat={lat}&lon={lon}&units=metric&exclude=minutely,hourly,daily&appid={apiKey}");
            result.Current.Temp = Math.Round(result.Current.Temp, 1);
            result.Current.FeelsLike = Math.Round(result.Current.FeelsLike, 1);
            result.Current.Uvi = Math.Round(result.Current.Uvi, 1);
            return result;
        }

        public async Task<Daily> GetDailyWeatherByCoordinates(double lat, double lon)
        {
            var result = await _restApiClient.SendGetRequest<Daily>($"data/3.0/onecall?lat={lat}&lon={lon}&units=metric&exclude=minutely,hourly&appid={apiKey}");
            //result.Current.Temp = Math.Round(result.Current.Temp, 1);
            //result.Current.FeelsLike = Math.Round(result.Current.FeelsLike, 1);
            //result.Current.Uvi = Math.Round(result.Current.Uvi, 1);
            return result;
        }

        public async Task<Hourly> GetHourlyWeatherByCoordinates(double lat, double lon)
        {
            var result = await _restApiClient.SendGetRequest<Hourly>($"data/3.0/onecall?lat={lat}&lon={lon}&units=metric&exclude=minutely,daily&appid={apiKey}");
            //result.Current.Temp = Math.Round(result.Current.Temp, 1);
            //result.Current.FeelsLike = Math.Round(result.Current.FeelsLike, 1);
            //result.Current.Uvi = Math.Round(result.Current.Uvi, 1);
            return result;
        }

        public string GetWindDirection(int windDeg)
        {
            var directions = new string[] {"N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW", "N"};

            var currentDirection = (windDeg + 11) / 22;
            return directions[currentDirection];
        }

        public async Task<List<City>> GetListOfCities(long usedId)
        {
            var cities = _context.Cities.Where(x => x.UserId == usedId).ToList();
            return cities;
        }

        public DateTimeOffset GetOffsetTime(int offsetSeconds)
        {
            // Get the current UTC time
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;

            // Add the offset in seconds to the current time
            DateTimeOffset offsetTime = currentTime.AddSeconds(offsetSeconds);

            return offsetTime;
        }

    }
}
