using System.Text.Json;
using TestShcheduler.Models;

namespace TestShcheduler.Services;

public sealed class CountryDataService
{
    private readonly IReadOnlyList<Country> countries;

    public CountryDataService(IWebHostEnvironment environment)
    {
        string path = Path.Combine(environment.ContentRootPath, "Data", "countries.json");
        string json = File.ReadAllText(path);
        countries = JsonSerializer.Deserialize<List<Country>>(json, JsonOptions)
            ?? throw new InvalidOperationException("Country data file is empty or invalid.");
    }

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<Country> GetAll() => countries;
}
