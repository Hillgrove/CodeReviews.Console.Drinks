using System.Text.Json;
using System.Text.Json.Serialization;

namespace DrinksInfo.Hillgrove.Models;

class DrinkDetails
{
    [JsonPropertyName("idDrink")]
    public string Id { get; init; } = "";

    [JsonPropertyName("strDrink")]
    public string Name { get; init; } = "";

    [JsonPropertyName("strCategory")]
    public string? Category { get; init; }

    [JsonPropertyName("strAlcoholic")]
    public string? Alcoholic { get; init; }

    [JsonPropertyName("strGlass")]
    public string? Glass { get; init; }

    [JsonPropertyName("strInstructions")]
    public string? Instructions { get; init; }

    [JsonPropertyName("strDrinkThumb")]
    public string? Thumbnail { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }

    public List<(string ingredient, string measure)> Ingredients =>
        Enumerable
            .Range(1, 15)
            .Select(index =>
            {
                string? ingredient = GetExtensionDataValue($"strIngredient{index}");
                string? measure = GetExtensionDataValue($"strMeasure{index}");
                return (ingredient, measure);
            })
            .Where(pair => !string.IsNullOrWhiteSpace(pair.ingredient))
            .Select(pair => (pair.ingredient!.Trim(), pair.measure?.Trim() ?? string.Empty))
            .ToList();

    string? GetExtensionDataValue(string key)
    {
        if (ExtensionData is null || !ExtensionData.TryGetValue(key, out JsonElement element))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return element.GetString();
    }
}
