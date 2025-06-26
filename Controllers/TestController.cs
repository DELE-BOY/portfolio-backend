using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return "API is working!";
    }
}