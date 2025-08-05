using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MyPortfolioAPI.Models; // Ensure this is pointing to your DTOs
using Microsoft.Extensions.Configuration; // For accessing User Secrets
using Microsoft.Extensions.Logging; // For logging

namespace MyPortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthInsightsController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HealthInsightsController> _logger;
        private readonly string _rapidApiKey;
        private const string EndlessMedicalBaseUrl = "https://endlessmedicalapi1.p.rapidapi.com/";
        private const string EndlessMedicalTermsPassphrase = "I have read, understood and I accept and agree to comply with the Terms of Use of EndlessMedicalAPI and Endless Medical services. The Terms of Use are available on endlessmedical.com";

        public HealthInsightsController(HttpClient httpClient, IConfiguration configuration, ILogger<HealthInsightsController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _rapidApiKey = configuration["RapidAPI:Key"] ?? throw new ArgumentNullException("RapidAPI:Key not found in configuration. Please set it via user secrets or environment variables.");
            _httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", _rapidApiKey);
            _httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "endlessmedicalapi1.p.rapidapi.com");
        }

        /// <summary>
        /// Gets common illnesses and health insights for a specified age.
        /// </summary>
        /// <param name="age">The age of the person (e.g., 25).</param>
        /// <returns>A list of common illnesses and health insights.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetHealthInsights([FromQuery] int age)
        {
            if (age < 0 || age > 120) // Basic age validation
            {
                return BadRequest("Age must be between 0 and 120.");
            }

            string? sessionId = null;
            try
            {
                // 1. InitSession
                _logger.LogInformation("Initializing EndlessMedical API session for age {Age}", age);
                var initResponse = await _httpClient.GetAsync($"{EndlessMedicalBaseUrl}InitSession");
                initResponse.EnsureSuccessStatusCode();
                var initContent = await initResponse.Content.ReadAsStringAsync();
                var sessionResult = JsonSerializer.Deserialize<EndlessMedicalSimpleResponse>(initContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (sessionResult?.Status != "ok" || string.IsNullOrEmpty(sessionResult.SessionID))
                {
                    _logger.LogError("Failed to initialize EndlessMedical API session: {Error}", sessionResult?.Error);
                    return StatusCode(500, $"Failed to initialize medical session: {sessionResult?.Error}");
                }
                sessionId = sessionResult.SessionID;
                _logger.LogInformation("Session initialized with ID: {SessionId}", sessionId);

                // 2. AcceptTermsOfUse
                _logger.LogInformation("Accepting EndlessMedical API terms of use for session {SessionId}", sessionId);
                var termsResponse = await _httpClient.PostAsync($"{EndlessMedicalBaseUrl}AcceptTermsOfUse?SessionID={sessionId}&passphrase={Uri.EscapeDataString(EndlessMedicalTermsPassphrase)}", null);
                termsResponse.EnsureSuccessStatusCode();
                var termsContent = await termsResponse.Content.ReadAsStringAsync();
                var termsResult = JsonSerializer.Deserialize<EndlessMedicalSimpleResponse>(termsContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (termsResult?.Status != "ok")
                {
                    _logger.LogError("Failed to accept EndlessMedical API terms: {Error}", termsResult?.Error);
                    return StatusCode(500, $"Failed to accept medical API terms: {termsResult?.Error}");
                }
                _logger.LogInformation("Terms accepted for session {SessionId}", sessionId);

                // 3. UpdateFeature: Set Age
                _logger.LogInformation("Updating EndlessMedical API feature 'Age' to {Age} for session {SessionId}", age, sessionId);
                var updateFeatureResponse = await _httpClient.PostAsync($"{EndlessMedicalBaseUrl}UpdateFeature?SessionID={sessionId}&name=Age&value={age}", null);
                updateFeatureResponse.EnsureSuccessStatusCode();
                var updateFeatureContent = await updateFeatureResponse.Content.ReadAsStringAsync();
                var updateFeatureResult = JsonSerializer.Deserialize<EndlessMedicalSimpleResponse>(updateFeatureContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (updateFeatureResult?.Status != "ok")
                {
                    _logger.LogError("Failed to update EndlessMedical API feature 'Age': {Error}", updateFeatureResult?.Error);
                    return StatusCode(500, $"Failed to provide age to medical API: {updateFeatureResult?.Error}");
                }
                _logger.LogInformation("Age feature updated for session {SessionId}", sessionId);

                // 4. Analyze
                _logger.LogInformation("Analyzing EndlessMedical API data for session {SessionId}", sessionId);
                var analyzeResponse = await _httpClient.GetAsync($"{EndlessMedicalBaseUrl}Analyze?SessionID={sessionId}");
                analyzeResponse.EnsureSuccessStatusCode();
                var analyzeContent = await analyzeResponse.Content.ReadAsStringAsync();
                var analyzeResult = JsonSerializer.Deserialize<EndlessMedicalSimpleResponse>(analyzeContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (analyzeResult?.Status != "ok")
                {
                    _logger.LogError("Failed to analyze EndlessMedical API data: {Error}", analyzeResult?.Error);
                    return StatusCode(500, $"Failed to analyze medical data: {analyzeResult?.Error}");
                }
                _logger.LogInformation("Analysis complete for session {SessionId}", sessionId);


                // 5. GetDiagnosis
                _logger.LogInformation("Getting EndlessMedical API diagnoses for session {SessionId}", sessionId);
                var getDiagnosisResponse = await _httpClient.GetAsync($"{EndlessMedicalBaseUrl}GetDiagnosis?SessionID={sessionId}");
                getDiagnosisResponse.EnsureSuccessStatusCode();
                var getDiagnosisContent = await getDiagnosisResponse.Content.ReadAsStringAsync();
                var getDiagnosisResult = JsonSerializer.Deserialize<EndlessMedicalApiResponse>(getDiagnosisContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (getDiagnosisResult?.Status != "ok" || getDiagnosisResult.Data == null)
                {
                    _logger.LogError("Failed to get EndlessMedical API diagnoses: {Error}", getDiagnosisResult?.Error);
                    return StatusCode(500, $"Failed to retrieve diagnoses: {getDiagnosisResult?.Error}");
                }
                _logger.LogInformation("Successfully retrieved diagnoses for session {SessionId}", sessionId);


                // Map EndlessMedical API response to your DTO
                var insights = new HealthInsightsResponseDto
                {
                    Age = age,
                    AgeGroup = GetAgeGroup(age),
                    CommonIllnesses = getDiagnosisResult.Data.Select(d => new IllnessDto
                    {
                        Name = d.CommonName ?? d.Name, // Prefer CommonName if available
                        Description = "No description available from this API endpoint.", // EndlessMedical's GetDiagnosis might not provide detailed descriptions directly. You might need another endpoint or manual lookup.
                        PrevalenceNotes = d.Prevalence // If they provide this
                    }).ToList(),
                    SourceNote = "Data provided by EndlessMedicalAPI via RapidAPI. This API is intended for informational purposes and should not be used for medical diagnosis."
                };

                return Ok(insights);
            }
            catch (HttpRequestException httpEx)
            {
                string errorMessage = $"Error calling EndlessMedical API: {httpEx.Message}";
                if (httpEx.StatusCode.HasValue)
                {
                    errorMessage += $" (HTTP Status: {(int)httpEx.StatusCode.Value} {httpEx.StatusCode.Value})";
                }
                _logger.LogError(httpEx, "HttpRequestException occurred while interacting with EndlessMedical API: {ErrorMessage}", errorMessage);
                return StatusCode(500, errorMessage);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JsonException occurred while processing EndlessMedical API response.");
                return StatusCode(500, $"Error processing EndlessMedical API response: {jsonEx.Message}");
            }
            catch (ArgumentNullException argNullEx) // For the RapidAPI key
            {
                 _logger.LogError(argNullEx, "Configuration error: {Message}", argNullEx.Message);
                 return StatusCode(500, argNullEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in GetHealthInsights method.");
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        private string GetAgeGroup(int age)
        {
            return age switch
            {
                <= 1 => "Infants (0-1 year)",
                <= 5 => "Toddlers & Preschoolers (1-5 years)",
                <= 12 => "School-Aged Children (6-12 years)",
                <= 18 => "Adolescents (13-18 years)",
                <= 30 => "Young Adults (19-30 years)",
                <= 50 => "Adults (31-50 years)",
                <= 65 => "Middle-Aged Adults (51-65 years)",
                _ => "Older Adults (65+ years)"
            };
        }
    }
}