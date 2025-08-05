namespace MyPortfolioAPI.Models
{
    public class HealthInsightsResponseDto
    {
        public int Age { get; set; }
        public string? AgeGroup { get; set; }
        public List<IllnessDto>? CommonIllnesses { get; set; }
        public string? SourceNote { get; set; } // To credit the API or data source
    }

    public class IllnessDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; } // A brief description
        public List<string>? Symptoms { get; set; } // Optional: if EndlessMedical provides symptoms
        public string? PrevalenceNotes { get; set; } // Optional: notes on why it's common for this age
    }

    // You'll also need models to deserialize the *raw* EndlessMedicalAPI responses.
    // We'll create these after we make our first API call and see the structure,
    // or you can pre-generate them using json2csharp if you can get sample responses.
    // For now, let's just make placeholders.
    public class EndlessMedicalApiResponse
    {
        public string? Status { get; set; }
        public string? SessionID { get; set; }
        public string? Error { get; set; }
        public List<EndlessMedicalDiagnosis>? Data { get; set; } // For GetDiagnosis
        // Add other properties as you discover them from their API docs/responses
    }

    public class EndlessMedicalDiagnosis
    {
        public string? Name { get; set; }
        public string? CommonName { get; set; }
        public string? Prevalence { get; set; } // Might be a string like "common", "rare"
        public double? Probability { get; set; } // If they provide a probability score
        // ... any other fields from their diagnosis object
    }

    // For InitSession, AcceptTermsOfUse, UpdateFeature responses,
    // they might just return { "status": "ok", "SessionID": "..." }
    public class EndlessMedicalSimpleResponse
    {
        public string? Status { get; set; }
        public string? SessionID { get; set; } // For InitSession response
        public string? Error { get; set; }
    }
}