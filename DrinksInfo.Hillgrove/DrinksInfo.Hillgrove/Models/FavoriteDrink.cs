using System.Text.Json.Serialization;

namespace DrinksInfo.Hillgrove.Models;

record FavoriteDrink(
    [property: JsonPropertyName("idDrink")] string Id,
    [property: JsonPropertyName("strDrink")] string Name,
    [property: JsonPropertyName("strCategory")] string? Category,
    [property: JsonPropertyName("strDrinkThumb")] string? Thumbnail
)
{
    public DrinkSummary ToDrinkSummary() => new(Name, Thumbnail ?? string.Empty, Id);
}
