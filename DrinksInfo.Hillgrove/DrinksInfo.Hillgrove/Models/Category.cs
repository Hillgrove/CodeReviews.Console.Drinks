using System.Text.Json.Serialization;

namespace DrinksInfo.Hillgrove.Models;

record Category([property: JsonPropertyName("strCategory")] string Name);
