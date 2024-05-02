using Tele.Bot.Models;

namespace Tele.Bot.Services
{
    public interface IWeatherDbService
    {
        Task ClearCities(long userId);
        Task SaveCityToDb(City city);
        Task DeleteCityFromDb(string city, long userId);
    }
}
