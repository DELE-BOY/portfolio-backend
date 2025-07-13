namespace MyPortfolioAPI.Models
{
    // Data Transfer Objects for the overall weather response
    public class WeatherResponseDto
    {
        public LocationDto? Location { get; set; }
        public CurrentWeatherDto? CurrentWeather { get; set; }
        public List<HourlyForecastItemDto>? HourlyForecast { get; set; }
    }

    //location details
    public class LocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Timezone { get; set; }
    }

    // current weather details
    public class CurrentWeatherDto
    {
        public string? Time { get; set; }
        public string? Temperature { get; set; } // Already formatted with unit
        public string? IsDay { get; set; } // "Day" or "Night"
        public string? WeatherDescription { get; set; }
        public string? WindSpeed { get; set; }
        public string? WindDirection { get; set; } // Formatted with degree symbol
    }

    //hourly forecast item
    public class HourlyForecastItemDto
    {
        public string? Time { get; set; }
        public string? Temperature { get; set; } // Already formatted with unit
    }
}