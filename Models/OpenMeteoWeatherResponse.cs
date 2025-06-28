namespace MyPortfolioAPI.Models
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Current
    {
        public string? Time { get; set; }
        public int Interval { get; set; }
        public double Temperature2m { get; set; }
        public int IsDay { get; set; }
        public double Rain { get; set; }
        public double Precipitation { get; set; }
        public double Showers { get; set; }
        public int WeatherCode { get; set; }
        public double WindSpeed10m { get; set; }
        public int WindDirection10m { get; set; }
    }

    public class CurrentUnits
    {
        public string? Time { get; set; }
        public string? Interval { get; set; }
        public string? Temperature2m { get; set; }
        public string? IsDay { get; set; }
        public string? Rain { get; set; }
        public string? Precipitation { get; set; }
        public string? Showers { get; set; }
        public string? WeatherCode { get; set; }
        public string? WindSpeed10m { get; set; }
        public string? WindDirection10m { get; set; }
    }

    public class Hourly
    {
        public List<string>? Time { get; set; }
        public List<double>? Temperature2m { get; set; }
    }

    public class HourlyUnits
    {
        public string? Time { get; set; }
        public string? Temperature2m { get; set; }
    }

    public class Root
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double GenerationtimeMs { get; set; }
        public int UtcOffsetSeconds { get; set; }
        public string? Timezone { get; set; }
        public string? TimezoneAbbreviation { get; set; }
        public double Elevation { get; set; }
        public CurrentUnits? CurrentUnits { get; set; }
        public Current? Current { get; set; }
        public HourlyUnits? HourlyUnits { get; set; }
        public Hourly? Hourly { get; set; }
    }
}

