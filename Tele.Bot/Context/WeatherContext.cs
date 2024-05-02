using Microsoft.EntityFrameworkCore;

namespace Tele.Bot.Models
{
    public class WeatherContext : DbContext
    {
        public DbSet<City> Cities { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=weatherbotdatabase.db");
        }
    }
}