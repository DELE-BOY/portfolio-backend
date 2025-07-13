using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MyPortfolioAPI.Models;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace MyPortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(HttpClient httpClient, ILogger<WeatherController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
 /// <summary>
        /// Gets current weather and hourly temperature for a specified geographical location.
        /// </summary>
        /// <param name="latitude">The latitude of the location</param>
        /// <param name="longitude">The longitude of the location </param>
        /// <returns>Current weather details and hourly forecast.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWeather(
            [FromQuery] double latitude,
            [FromQuery] double longitude)
        {
            if (latitude < -90 || latitude > 90)
            {
                return BadRequest("Latitude must be between -90 and 90.");
            }
            if (longitude < -180 || longitude > 180)
            {
                return BadRequest("Longitude must be between -180 and 180.");
            }

            try
            {
                // Construct the request URL for Open-Meteo API
                // Note: Open-Meteo API does not require an API key for basic usage (The API endpoint and parameters are based on the Open-Meteo documentation).
                string requestUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,is_day,weather_code,wind_speed_10m,wind_direction_10m&hourly=temperature_2m&timezone=auto";
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync();

                Root? weatherData = JsonSerializer.Deserialize<Root>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (weatherData != null)
                {
                    // Map to Weather ResponseDTO
                    var weatherResponse = new WeatherResponseDto
                    {
                        Location = new LocationDto
                        {
                            Latitude = weatherData.Latitude,
                            Longitude = weatherData.Longitude,
                            Timezone = weatherData.Timezone
                        },
                        CurrentWeather = weatherData.Current != null ? new CurrentWeatherDto
                        {
                            Time = weatherData.Current.Time,
                            Temperature = $"{weatherData.Current.Temperature2m}{weatherData.CurrentUnits?.Temperature2m}",
                            IsDay = weatherData.Current.IsDay == 1 ? "Day" : "Night",
                            WeatherDescription = GetWeatherCodeDescription(weatherData.Current.WeatherCode),
                            WindSpeed = $"{weatherData.Current.WindSpeed10m}{weatherData.CurrentUnits?.WindSpeed10m}",
                            WindDirection = $"{weatherData.Current.WindDirection10m}°"
                        } : null,
                        HourlyForecast = weatherData.Hourly?.Time?.Select((time, index) => new HourlyForecastItemDto
                        {
                            Time = time,
                            Temperature = weatherData.Hourly.Temperature2m?.ElementAtOrDefault(index) != null ?
                                          $"{weatherData.Hourly.Temperature2m.ElementAtOrDefault(index)}{weatherData.HourlyUnits?.Temperature2m}" : "N/A"
                        }).Take(24).ToList() // .ToList() ensures it's a List<T> for the DTO
                    };

                    return Ok(weatherResponse); // Return the DTO
                }
                else
                {
                    _logger.LogError("Failed to deserialize weather data from Open-Meteo API. JSON response: {JsonResponse}", jsonResponse);
                    return StatusCode(500, "Failed to deserialize weather data from Open-Meteo API.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                string errorMessage = $"Error calling Open-Meteo API: {httpEx.Message}";
                if (httpEx.StatusCode.HasValue)
                {
                    errorMessage += $" (HTTP Status: {(int)httpEx.StatusCode.Value} {httpEx.StatusCode.Value})";
                }
                _logger.LogError(httpEx, "HttpRequestException occurred: {ErrorMessage}", errorMessage);
                return StatusCode(500, errorMessage);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JsonException occurred while processing Open-Meteo API response.");
                return StatusCode(500, $"Error processing Open-Meteo API response: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in GetWeather method.");
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        private string GetWeatherCodeDescription(int code)
        {
            return code switch
            {
                0 => "Clear sky",
                1 => "Mainly clear",
                2 => "Partly cloudy",
                3 => "Overcast",
                45 => "Fog",
                48 => "Depositing rime fog",
                51 => "Drizzle: Light",
                53 => "Drizzle: Moderate",
                55 => "Drizzle: Dense intensity",
                56 => "Freezing Drizzle: Light",
                57 => "Freezing Drizzle: Dense intensity",
                61 => "Rain: Light",
                63 => "Rain: Moderate",
                65 => "Rain: Heavy intensity",
                66 => "Freezing Rain: Light",
                67 => "Freezing Rain: Heavy intensity",
                71 => "Snow fall: Light",
                73 => "Snow fall: Moderate",
                75 => "Snow fall: Heavy intensity",
                77 => "Snow grains",
                80 => "Rain showers: Light",
                81 => "Rain showers: Moderate",
                82 => "Rain showers: Violent",
                85 => "Snow showers: Light",
                86 => "Snow showers: Heavy",
                95 => "Thunderstorm: Slight or moderate",
                96 => "Thunderstorm with slight hail",
                99 => "Thunderstorm with heavy hail",
                _ => "Unknown weather code"
            };
        }
    }
}