using DrinksInfo.Hillgrove;

string baseApiUrl = "https://www.thecocktaildb.com/api/json/v1/1/";
string favoritesFilePath = Path.Combine(AppContext.BaseDirectory, "favorites.json");

using HttpClient client = new();
client.BaseAddress = new Uri(baseApiUrl);

await new App(client, favoritesFilePath).RunAsync();
