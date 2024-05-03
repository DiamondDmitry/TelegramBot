using Tele.Bot.Models;

namespace Tele.Bot.Services
{
    public interface IWeatherService
    {
        Task<City> GetCoordinatesByCityName(string city);
        Task<City> GetCityNameByCoordinates(double lat, double lon);
        Task<Daily> GetDailyWeatherByCoordinates(double lat, double lon);
        Task<Hourly> GetHourlyWeatherByCoordinates(double lat, double lon);
        Task<Root> GetWeatherByCoordinates(double lat, double lon);
        public string GetWindDirection(int deg);
        Task<List<City>> GetListOfCities(long usedId);
        public DateTimeOffset GetOffsetTime(int timezoneOffset);
    }
}
