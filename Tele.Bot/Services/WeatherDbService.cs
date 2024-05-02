using Tele.Bot.Models;

namespace Tele.Bot.Services
{
    public class WEatherDbService : IWeatherDbService
    {
        private readonly WeatherContext _context;

        public WEatherDbService(WeatherContext context)
        {
            _context = context;
        }

        public async Task SaveCityToDb(City city)
        {
            _context.Cities.Add(city);
            await _context.SaveChangesAsync();
        }

        public async Task ClearCities(long userId)
        {
            var cities = _context.Cities.Where(x => x.UserId == userId).ToList();
            _context.Cities.RemoveRange(cities);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCityFromDb(string city, long userId)
        {
            var cityToDelete = _context.Cities.FirstOrDefault(x => x.Name == city && x.UserId == userId);
            if (cityToDelete != null)
            {
                _context.Cities.Remove(cityToDelete);
                await _context.SaveChangesAsync();
            }
        }
    }
}
