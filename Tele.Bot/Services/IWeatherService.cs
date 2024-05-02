using Tele.Bot.Models;

namespace Tele.Bot.Services
{
    public interface IWeatherService
    {
        Task<City> GetCoordinatesByCityName(string city);
        Task<City> GetCityNameByCoordinates(double latitude, double longitude);
        Task<Root> GetWeatherByCoordinates(double lat, double lon);
        public string GetWindDirection(int deg);
        Task<List<City>> GetListOfCities(long usedId);
        public DateTimeOffset GetOffsetTime(int timezoneOffset);
    }
}
