using DrinksInfo.Hillgrove.Common;
using DrinksInfo.Hillgrove.Models;
using Spectre.Console;

namespace DrinksInfo.Hillgrove.UI;

record MenuOption(
    MenuAction Action,
    string Label,
    Category? Category = null,
    FavoriteDrink? Favorite = null,
    DrinkSummary? Drink = null
);

static class MenuDisplay
{
    public static MenuOption SelectMainMenu(List<Category> categories, bool categoriesAvailable)
    {
        List<MenuOption> menuOptions = new();

        foreach (Category category in categories)
        {
            menuOptions.Add(
                new MenuOption(
                    MenuAction.SelectCategory,
                    Markup.Escape(category.Name),
                    Category: category
                )
            );
        }

        menuOptions.Add(new MenuOption(MenuAction.ViewFavorites, "[cyan]View favorites[/]"));
        menuOptions.Add(new MenuOption(MenuAction.Exit, "[red]Exit[/]"));

        string title = categoriesAvailable ? "Select a [green]category[/]:" : "Select an option:";

        return PromptMenu(title, menuOptions);
    }

    public static Result<DrinkSummary> SelectDrinkMenu(Category category, List<DrinkSummary> drinks)
    {
        List<MenuOption> drinkOptions = new();

        foreach (DrinkSummary drink in drinks)
        {
            drinkOptions.Add(
                new MenuOption(MenuAction.SelectDrink, Markup.Escape(drink.Name), Drink: drink)
            );
        }

        drinkOptions.Add(new MenuOption(MenuAction.Back, "[red]Back[/]"));

        MenuOption choice = PromptMenu(
            $"Select a [green]drink[/] from category [yellow]{Markup.Escape(category.Name)}[/]:",
            drinkOptions
        );

        if (choice.Action == MenuAction.Back)
        {
            return Result<DrinkSummary>.Cancelled();
        }

        return Result<DrinkSummary>.Success(choice.Drink!);
    }

    public static Result<FavoriteDrink> SelectFavoriteMenu(List<FavoriteDrink> favorites)
    {
        List<MenuOption> favoriteOptions = new();

        foreach (FavoriteDrink favorite in favorites)
        {
            favoriteOptions.Add(
                new MenuOption(
                    MenuAction.OpenFavorite,
                    GetFavoriteLabel(favorite),
                    Favorite: favorite
                )
            );
        }

        favoriteOptions.Add(new MenuOption(MenuAction.Back, "[red]Back[/]"));

        MenuOption choice = PromptMenu("Select a [green]favorite drink[/]:", favoriteOptions);

        if (choice.Action == MenuAction.Back)
        {
            return Result<FavoriteDrink>.Cancelled();
        }

        return Result<FavoriteDrink>.Success(choice.Favorite!);
    }

    public static Result<MenuAction> SelectDrinkDetailsAction(bool isFavorite)
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

    public static MenuOption PromptMenu(string title, IEnumerable<MenuOption> options)
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

    static string GetFavoriteLabel(FavoriteDrink favorite)
    {
        if (!string.IsNullOrWhiteSpace(favorite.Category))
        {
            return $"{Markup.Escape(favorite.Name)} [grey]({Markup.Escape(favorite.Category)})[/]";
        }

        return Markup.Escape(favorite.Name);
    }
}
