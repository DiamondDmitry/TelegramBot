using Tele.Bot.Models;
using Telegram.Bot;

namespace Tele.Bot.Services
{
    public interface IWeatherService
    {
        Task<City> GetCoordinatesByCityName(string city);
        Task<City> GetCityNameByCoordinates(double lat, double lon);
        Task<Root> GetWeatherByCoordinates(double lat, double lon);
        Task<Root> GetDailyWeatherByCoordinates(double lat, double lon);
        Task<Root> GetHourlyWeatherByCoordinates(double lat, double lon);
        public string GetWindDirection(int deg);
        public MemoryStream GetWeatherIcon(string imageName);
        Task<List<City>> GetListOfCities(long usedId);
        public DateTimeOffset GetOffsetTime(int timezoneOffset);

    }
}
