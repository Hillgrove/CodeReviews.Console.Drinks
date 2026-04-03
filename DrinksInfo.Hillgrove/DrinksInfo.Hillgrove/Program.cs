/*
When the users open the application, they should be presented with:
    1. the Drinks Category Menu and invited to choose a category
    2.Then they'll have the chance to choose a drink and see information about it.

When the users visualise the drink detail, there shouldn't be any properties with empty values.

You should handle errors so that if the API is down, the application doesn't crash.
*/


using Spectre.Console;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

string baseApiUrl = "https://www.thecocktaildb.com/api/json/v1/1/";
string favoritesFilePath = Path.Combine(AppContext.BaseDirectory, "favorites.json");

using HttpClient client = new();
client.BaseAddress = new Uri(baseApiUrl);

List<FavoriteDrink> favorites = new List<FavoriteDrink>();
Result<List<FavoriteDrink>> favoritesResult = LoadFavorites(favoritesFilePath);
if (favoritesResult.IsSuccess && favoritesResult.Data is not null)
{
    favorites = favoritesResult.Data;
}
else if (favoritesResult.IsError)
{
    AnsiConsole.MarkupLine($"[red]{Markup.Escape(favoritesResult.Error!)}[/]");
    PauseAndWaitForKey("Press any key to continue with an empty favorites list...");
}

while (true)
{
    AnsiConsole.Clear();

    Result<List<Category>> categoriesResult = await GetCategoriesAsync(client);
    List<Category> categories = categoriesResult.IsSuccess && categoriesResult.Data is not null
        ? categoriesResult.Data
        : new List<Category>();

    if (categoriesResult.IsError)
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(categoriesResult.Error!)}[/]");
    }

    MenuOption selectedOption = SelectMainMenu(categories, categoriesResult.IsSuccess);

    if (selectedOption.Action == MenuAction.Exit)
    {
        AnsiConsole.MarkupLine("[yellow]Closing application...[/]");
        PauseAndWaitForKey("Press any key to close the app...");
        return;
    }

    if (selectedOption.Action == MenuAction.ViewFavorites)
    {
        await ShowFavoritesMenuAsync(client, favorites, favoritesFilePath);
        continue;
    }

    if (selectedOption.Action == MenuAction.SelectCategory && selectedOption.Category is not null)
    {
        await ShowCategoryMenuAsync(client, selectedOption.Category, favorites, favoritesFilePath);
    }
}

async Task ShowCategoryMenuAsync(HttpClient client, Category category, List<FavoriteDrink> favorites, string favoritesFilePath)
{
    while (true)
    {
        AnsiConsole.Clear();

        Result<List<DrinkSummary>> drinksResult = await GetDrinkSummariesAsync(client, category);
        if (drinksResult.IsError)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(drinksResult.Error!)}[/]");
            PauseAndWaitForKey("Press any key to return to the main menu...");
            return;
        }

        Result<DrinkSummary> selectedDrink = SelectDrinkMenu(category, drinksResult.Data!);
        if (selectedDrink.IsCancelled)
        {
            return;
        }

        await ShowDrinkDetailsMenuAsync(
            client,
            selectedDrink.Data!,
            favorites,
            favoritesFilePath,
            "Press any key to return to the drink list...");
    }
}

async Task ShowFavoritesMenuAsync(HttpClient client, List<FavoriteDrink> favorites, string favoritesFilePath)
{
    while (true)
    {
        AnsiConsole.Clear();

        if (favorites.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]You do not have any favorites yet.[/]");
            PauseAndWaitForKey("Press any key to return to the main menu...");
            return;
        }

        Result<FavoriteDrink> selectedFavorite = SelectFavoriteMenu(favorites);
        if (selectedFavorite.IsCancelled)
        {
            return;
        }

        await ShowDrinkDetailsMenuAsync(
            client,
            ToDrinkSummary(selectedFavorite.Data!),
            favorites,
            favoritesFilePath,
            "Press any key to return to the favorites menu...");
    }
}

async Task ShowDrinkDetailsMenuAsync(
    HttpClient client,
    DrinkSummary drink,
    List<FavoriteDrink> favorites,
    string favoritesFilePath,
    string returnMessage)
{
    Result<DrinkDetails> drinkDetails = await GetDrinkDetailsAsync(client, drink);

    if (drinkDetails.IsError)
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(drinkDetails.Error!)}[/]");
        PauseAndWaitForKey(returnMessage);
        return;
    }

    while (true)
    {
        AnsiConsole.Clear();

        DrinkDetails details = drinkDetails.Data!;
        bool isFavorite = IsFavorite(favorites, details.Id);

        ShowDrinkDetails(details, isFavorite);

        Result<MenuAction> selectedAction = SelectDrinkDetailsAction(isFavorite);
        if (selectedAction.IsCancelled || selectedAction.Data == MenuAction.Back)
        {
            return;
        }

        if (selectedAction.Data == MenuAction.AddFavorite)
        {
            Result<bool> addFavoriteResult = AddFavorite(favorites, favoritesFilePath, details);
            if (addFavoriteResult.IsError)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(addFavoriteResult.Error!)}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(details.Name)} added to favorites.[/]");
            }

            PauseAndWaitForKey(returnMessage);
            return;
        }

        if (selectedAction.Data == MenuAction.RemoveFavorite)
        {
            Result<bool> removeFavoriteResult = RemoveFavorite(favorites, favoritesFilePath, details.Id);
            if (removeFavoriteResult.IsError)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(removeFavoriteResult.Error!)}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(details.Name)} removed from favorites.[/]");
            }

            PauseAndWaitForKey(returnMessage);
            return;
        }
    }
}

async Task<Result<List<Category>>> GetCategoriesAsync(HttpClient client)
{
    Result<CategoryResponse> categoriesResponse = await GetJsonAsync<CategoryResponse>(client, "list.php?c=list", "categories");
    if (categoriesResponse.IsError)
    {
        return Result<List<Category>>.Failure(categoriesResponse.Error!);
    }

    List<Category> categories = categoriesResponse.Data!.Categories;
    if (categories.Count == 0)
    {
        return Result<List<Category>>.Failure("No categories found.");
    }

    return Result<List<Category>>.Success(categories);
}

MenuOption SelectMainMenu(List<Category> categories, bool categoriesAvailable)
{
    List<MenuOption> menuOptions = new();

    foreach (Category category in categories)
    {
        menuOptions.Add(new MenuOption(MenuAction.SelectCategory, Markup.Escape(category.Name), Category: category));
    }

    menuOptions.Add(new MenuOption(MenuAction.ViewFavorites, "[cyan]View favorites[/]"));
    menuOptions.Add(new MenuOption(MenuAction.Exit, "[red]Exit[/]"));

    string title = categoriesAvailable
        ? "Select a [green]category[/]:"
        : "Select an option:";

    return PromptMenu(title, menuOptions);
}

async Task<Result<List<DrinkSummary>>> GetDrinkSummariesAsync(HttpClient client, Category category)
{
    Result<DrinksResponse> drinksResponse = await GetJsonAsync<DrinksResponse>(
        client,
        $"filter.php?c={Uri.EscapeDataString(category.Name)}",
        "drink summaries");

    if (drinksResponse.IsError)
    {
        return Result<List<DrinkSummary>>.Failure(drinksResponse.Error!);
    }

    List<DrinkSummary> drinks = drinksResponse.Data!.Drinks;
    if (drinks.Count == 0)
    {
        return Result<List<DrinkSummary>>.Failure("No drink summaries found.");
    }

    return Result<List<DrinkSummary>>.Success(drinks);
}

Result<DrinkSummary> SelectDrinkMenu(Category category, List<DrinkSummary> drinks)
{
    List<MenuOption> drinkOptions = new();

    foreach (DrinkSummary drink in drinks)
    {
        drinkOptions.Add(new MenuOption(MenuAction.SelectDrink, Markup.Escape(drink.Name), Drink: drink));
    }

    drinkOptions.Add(new MenuOption(MenuAction.Back, "[red]Back[/]"));

    MenuOption choice = PromptMenu($"Select a [green]drink[/] from category [yellow]{Markup.Escape(category.Name)}[/]:", drinkOptions);

    if (choice.Action == MenuAction.Back)
    {
        return Result<DrinkSummary>.Cancelled();
    }

    return Result<DrinkSummary>.Success(choice.Drink!);
}

Result<FavoriteDrink> SelectFavoriteMenu(List<FavoriteDrink> favorites)
{
    List<MenuOption> favoriteOptions = new();

    foreach (FavoriteDrink favorite in favorites)
    {
        favoriteOptions.Add(new MenuOption(MenuAction.OpenFavorite, GetFavoriteLabel(favorite), Favorite: favorite));
    }

    favoriteOptions.Add(new MenuOption(MenuAction.Back, "[red]Back[/]"));

    MenuOption choice = PromptMenu("Select a [green]favorite drink[/]:", favoriteOptions);

    if (choice.Action == MenuAction.Back)
    {
        return Result<FavoriteDrink>.Cancelled();
    }

    return Result<FavoriteDrink>.Success(choice.Favorite!);
}

Result<MenuAction> SelectDrinkDetailsAction(bool isFavorite)
{
    List<MenuOption> actions = new();

    if (isFavorite)
    {
        actions.Add(new MenuOption(MenuAction.RemoveFavorite, "[red]Remove from favorites[/]"));
    }
    else
    {
        actions.Add(new MenuOption(MenuAction.AddFavorite, "[green]Add to favorites[/]"));
    }

    actions.Add(new MenuOption(MenuAction.Back, "[yellow]Back[/]"));

    MenuOption choice = PromptMenu("Choose an action:", actions);
    if (choice.Action == MenuAction.Back)
    {
        return Result<MenuAction>.Cancelled();
    }

    return Result<MenuAction>.Success(choice.Action);
}

async Task<Result<DrinkDetails>> GetDrinkDetailsAsync(HttpClient client, DrinkSummary drink)
{
    Result<DrinkDetailsResponse> detailsResponse = await GetJsonAsync<DrinkDetailsResponse>(
        client,
        $"lookup.php?i={Uri.EscapeDataString(drink.Id)}",
        "drink details");

    if (detailsResponse.IsError)
    {
        return Result<DrinkDetails>.Failure(detailsResponse.Error!);
    }

    DrinkDetails? details = detailsResponse.Data?.Drinks.FirstOrDefault();

    if (details is null)
    {
        return Result<DrinkDetails>.Failure("No drink details found.");
    }

    return Result<DrinkDetails>.Success(details);
}

void ShowDrinkDetails(DrinkDetails details, bool isFavorite)
{
    if (isFavorite)
    {
        AnsiConsole.MarkupLine("[yellow]This drink is in your favorites.[/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[grey]This drink is not in your favorites.[/]");
    }

    Table table = new Table()
        .Title("[yellow]Drink[/]")
        .AddColumn("Field")
        .AddColumn("Value");

    if (!string.IsNullOrWhiteSpace(details.Name))
    {
        table.AddRow("Name", Markup.Escape(details.Name));
    }

    if (!string.IsNullOrWhiteSpace(details.Category))
    {
        table.AddRow("Category", Markup.Escape(details.Category));
    }

    if (!string.IsNullOrWhiteSpace(details.Alcoholic))
    {
        table.AddRow("Alcoholic", Markup.Escape(details.Alcoholic));
    }

    if (!string.IsNullOrWhiteSpace(details.Glass))
    {
        table.AddRow("Glass", Markup.Escape(details.Glass));
    }

    if (!string.IsNullOrWhiteSpace(details.Instructions))
    {
        table.AddRow("Instructions", Markup.Escape(details.Instructions));
    }

    AnsiConsole.Write(table);

    if (details.Ingredients.Count > 0)
    {
        Table ingredientsTable = new Table()
            .Title("\n[yellow]Ingredients[/]")
            .AddColumn("Ingredient")
            .AddColumn("Measure");

        foreach (var (ingredient, measure) in details.Ingredients)
        {
            ingredientsTable.AddRow(Markup.Escape(ingredient), Markup.Escape(measure));
        }

        AnsiConsole.Write(ingredientsTable);
    }
}

Result<List<FavoriteDrink>> LoadFavorites(string filePath)
{
    try
    {
        if (!File.Exists(filePath))
        {
            return Result<List<FavoriteDrink>>.Success(new List<FavoriteDrink>());
        }

        string json = File.ReadAllText(filePath);
        List<FavoriteDrink>? favorites = JsonSerializer.Deserialize<List<FavoriteDrink>>(json);

        return Result<List<FavoriteDrink>>.Success(favorites ?? new List<FavoriteDrink>());
    }
    catch (Exception ex)
    {
        return Result<List<FavoriteDrink>>.Failure($"Unable to load favorites: {ex.Message}");
    }
}

Result<bool> AddFavorite(List<FavoriteDrink> favorites, string filePath, DrinkDetails details)
{
    if (favorites.Any(favorite => favorite.Id == details.Id))
    {
        return Result<bool>.Failure("This drink is already in your favorites.");
    }

    FavoriteDrink favorite = new(details.Id, details.Name, details.Category, details.Thumbnail);
    favorites.Add(favorite);

    Result<bool> saveResult = SaveFavorites(filePath, favorites);
    if (saveResult.IsError)
    {
        favorites.RemoveAll(existingFavorite => existingFavorite.Id == favorite.Id);
        return Result<bool>.Failure(saveResult.Error!);
    }

    return Result<bool>.Success(true);
}

Result<bool> RemoveFavorite(List<FavoriteDrink> favorites, string filePath, string drinkId)
{
    FavoriteDrink? favorite = favorites.FirstOrDefault(existingFavorite => existingFavorite.Id == drinkId);
    if (favorite is null)
    {
        return Result<bool>.Failure("This drink is not in your favorites.");
    }

    favorites.Remove(favorite);

    Result<bool> saveResult = SaveFavorites(filePath, favorites);
    if (saveResult.IsError)
    {
        favorites.Add(favorite);
        return Result<bool>.Failure(saveResult.Error!);
    }

    return Result<bool>.Success(true);
}

Result<bool> SaveFavorites(string filePath, List<FavoriteDrink> favorites)
{
    try
    {
        string json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
        return Result<bool>.Success(true);
    }
    catch (Exception ex)
    {
        return Result<bool>.Failure($"Unable to save favorites: {ex.Message}");
    }
}

bool IsFavorite(List<FavoriteDrink> favorites, string drinkId)
{
    return favorites.Any(favorite => favorite.Id == drinkId);
}

string GetFavoriteLabel(FavoriteDrink favorite)
{
    if (!string.IsNullOrWhiteSpace(favorite.Category))
    {
        return $"{Markup.Escape(favorite.Name)} [grey]({Markup.Escape(favorite.Category)})[/]";
    }

    return Markup.Escape(favorite.Name);
}

DrinkSummary ToDrinkSummary(FavoriteDrink favorite)
{
    return new DrinkSummary(favorite.Name, favorite.Thumbnail ?? string.Empty, favorite.Id);
}

MenuOption PromptMenu(string title, IEnumerable<MenuOption> options)
{
    return AnsiConsole.Prompt(
        new SelectionPrompt<MenuOption>()
            .Title(title)
            .PageSize(20)
            .WrapAround()
            .EnableSearch()
            .UseConverter(option => option.Label)
            .AddChoices(options)
    );
}

async Task<Result<TResponse>> GetJsonAsync<TResponse>(HttpClient client, string endpoint, string errorContext)
{
    try
    {
        using var response = await client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        TResponse? result = await response.Content.ReadFromJsonAsync<TResponse>();
        if (result is null)
        {
            return Result<TResponse>.Failure($"No data returned for {errorContext}.");
        }

        return Result<TResponse>.Success(result);
    }
    catch (HttpRequestException ex)
    {
        return Result<TResponse>.Failure($"Error fetching {errorContext}: {ex.Message}");
    }
    catch (Exception ex)
    {
        return Result<TResponse>.Failure($"An unexpected error occurred: {ex.Message}");
    }
}

void PauseAndWaitForKey(string message = "Press any key to continue...")
{
    AnsiConsole.Markup($"\n[grey]{Markup.Escape(message)}[/]");
    Console.ReadKey(true);
}

enum MenuAction
{
    SelectCategory,
    SelectDrink,
    ViewFavorites,
    OpenFavorite,
    AddFavorite,
    RemoveFavorite,
    Back,
    Exit
}

enum ResultStatus
{
    Success,
    Cancelled,
    Error
}

record Result<T>(ResultStatus Status, T? Data = default, string? Error = null)
{
    public bool IsSuccess => Status == ResultStatus.Success;
    public bool IsCancelled => Status == ResultStatus.Cancelled;
    public bool IsError => Status == ResultStatus.Error;

    public static Result<T> Success(T data) => new(ResultStatus.Success, data);
    public static Result<T> Cancelled() => new(ResultStatus.Cancelled);
    public static Result<T> Failure(string error) => new(ResultStatus.Error, default, error);
}

record MenuOption(MenuAction Action, string Label, Category? Category = null, FavoriteDrink? Favorite = null, DrinkSummary? Drink = null);

record CategoryResponse([property: JsonPropertyName("drinks")] List<Category> Categories);

record DrinksResponse([property: JsonPropertyName("drinks")] List<DrinkSummary> Drinks);

record DrinkDetailsResponse([property: JsonPropertyName("drinks")] List<DrinkDetails> Drinks);

record Category([property: JsonPropertyName("strCategory")] string Name);

record DrinkSummary(
    [property: JsonPropertyName("strDrink")] string Name,
    [property: JsonPropertyName("strDrinkThumb")] string Thumbnail,
    [property: JsonPropertyName("idDrink")] string Id
);

record FavoriteDrink(
    [property: JsonPropertyName("idDrink")] string Id,
    [property: JsonPropertyName("strDrink")] string Name,
    [property: JsonPropertyName("strCategory")] string? Category,
    [property: JsonPropertyName("strDrinkThumb")] string? Thumbnail
);

class DrinkDetails
{
    [JsonPropertyName("idDrink")] public string Id { get; init; } = "";
    [JsonPropertyName("strDrink")] public string Name { get; init; } = "";
    [JsonPropertyName("strCategory")] public string? Category { get; init; }
    [JsonPropertyName("strAlcoholic")] public string? Alcoholic { get; init; }
    [JsonPropertyName("strGlass")] public string? Glass { get; init; }
    [JsonPropertyName("strInstructions")] public string? Instructions { get; init; }
    [JsonPropertyName("strDrinkThumb")] public string? Thumbnail { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }

    public List<(string ingredient, string measure)> Ingredients =>
        Enumerable.Range(1, 15)
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
