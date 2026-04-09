using System.Text.Json.Serialization;

namespace DrinksInfo.Hillgrove.Models;

record DrinkSummary(
    [property: JsonPropertyName("strDrink")] string Name,
    [property: JsonPropertyName("strDrinkThumb")] string Thumbnail,
    [property: JsonPropertyName("idDrink")] string Id
);
