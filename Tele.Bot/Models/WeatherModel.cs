using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Tele.Bot.Models
{
    public class Current
    {
        public int Dt { get; set; }

        public int Sunrise { get; set; }

        public int Sunset { get; set; }

        public double Temp { get; set; }

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }

        public int Pressure { get; set; }

        public int Humidity { get; set; }

        [JsonPropertyName("dew_point")]
        public double DewPoint { get; set; }

        public int Clouds { get; set; }

        public double Uvi { get; set; }

        public int Visibility { get; set; }

        [JsonPropertyName("wind_speed")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("wind_deg")]
        public int WindDeg { get; set; }

        [JsonPropertyName("wind_gust")]
        public double WindGust { get; set; }

        public List<Weather> Weather { get; set; }
    }

    public class Root
    {
        public double Lat { get; set; }

        public double Lon { get; set; }

        public string Timezone { get; set; }

        [JsonPropertyName("timezone_offset")]
        public int TimezoneOffset { get; set; }

        public Current Current { get; set; }

        public List<Daily> Daily { get; set; }

        public List<Hourly> Hourly { get; set; }
    }

    public class Weather
    {
        public int Id { get; set; }

        public string Main { get; set; }

        public string Description { get; set; }

        public string Icon { get; set; }
    }

    public class Daily
    {
        public int Dt { get; set; }

        public int Sunrise { get; set; }

        public int Sunset { get; set; }

        public int Moonrise { get; set; }

        public int Moonset { get; set; }

        public double MoonPhase { get; set; }

        public string Summary { get; set; }

        public Temp Temp { get; set; }

        [JsonPropertyName("feels_like")]
        public FeelsLike FeelsLike { get; set; }

        public int Pressure { get; set; }

        public int Humidity { get; set; }

        [JsonPropertyName("dew_point")]
        public double DewPoint { get; set; }

        [JsonPropertyName("wind_speed")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("wind_deg")]
        public int WindDeg { get; set; }

        [JsonPropertyName("wind_gust")]
        public double WindGust { get; set; }

        public List<Weather> Weather { get; set; }

        public int Clouds { get; set; }

        public double Pop { get; set; }

        public double Rain { get; set; }

        public double Uvi { get; set; }
    }

    public class FeelsLike
    {
        public double Day { get; set; }

        public double Night { get; set; }

        public double Eve { get; set; }

        public double Morn { get; set; }
    }

    public class Hourly
    {
        public int Dt { get; set; }

        public double Temp { get; set; }

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }

        public int Pressure { get; set; }

        public int Humidity { get; set; }

        [JsonPropertyName("dew_point")]
        public double DewPoint { get; set; }

        public double Uvi { get; set; }

        public int Clouds { get; set; }

        public int Visibility { get; set; }

        [JsonPropertyName("wind_speed")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("wind_deg")]
        public int WindDeg { get; set; }

        [JsonPropertyName("wind_gust")]
        public double WindGust { get; set; }

        public List<Weather> Weather { get; set; }

        public double Pop { get; set; }

        public Rain Rain { get; set; }
    }

    public class Rain
    {
        public double _1h { get; set; }
    }

    public class Temp
    {
        public double Day { get; set; }

        public double Min { get; set; }

        public double Max { get; set; }

        public double Night { get; set; }

        public double Eve { get; set; }

        public double Morn { get; set; }
    }

}
