using System.Text.Json.Serialization;

namespace DrinksInfo.Hillgrove.Models;

record CategoryResponse([property: JsonPropertyName("drinks")] List<Category> Categories);

record DrinksResponse([property: JsonPropertyName("drinks")] List<DrinkSummary> Drinks);

record DrinkDetailsResponse([property: JsonPropertyName("drinks")] List<DrinkDetails> Drinks);
