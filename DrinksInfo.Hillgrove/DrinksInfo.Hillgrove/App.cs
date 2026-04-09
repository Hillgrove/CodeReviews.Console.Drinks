using DrinksInfo.Hillgrove.Common;
using DrinksInfo.Hillgrove.Models;
using DrinksInfo.Hillgrove.Services;
using DrinksInfo.Hillgrove.UI;
using Spectre.Console;

namespace DrinksInfo.Hillgrove;

internal class App(HttpClient client, string favoritesFilePath)
{
    private List<FavoriteDrink> _favorites = [];

    public async Task RunAsync()
    {
        Result<List<FavoriteDrink>> favoritesResult = FavoritesService.LoadFavorites(
            favoritesFilePath
        );
        if (favoritesResult.IsSuccess && favoritesResult.Data is not null)
        {
            _favorites = favoritesResult.Data;
        }
        else if (favoritesResult.IsError)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(favoritesResult.Error!)}[/]");
            DrinkDisplay.PauseAndWaitForKey(
                "Press any key to continue with an empty favorites list..."
            );
        }

        while (true)
        {
            AnsiConsole.Clear();

            Result<List<Category>> categoriesResult = await DrinkService.GetCategoriesAsync(client);
            List<Category> categories =
                categoriesResult.IsSuccess && categoriesResult.Data is not null
                    ? categoriesResult.Data
                    : [];

            if (categoriesResult.IsError)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(categoriesResult.Error!)}[/]");
            }

            MenuOption selectedOption = MenuDisplay.SelectMainMenu(
                categories,
                categoriesResult.IsSuccess
            );

            if (selectedOption.Action == MenuAction.Exit)
            {
                AnsiConsole.MarkupLine("[yellow]Closing application...[/]");
                DrinkDisplay.PauseAndWaitForKey("Press any key to close the app...");
                return;
            }

            if (selectedOption.Action == MenuAction.ViewFavorites)
            {
                await ShowFavoritesMenuAsync();
                continue;
            }

            if (
                selectedOption.Action == MenuAction.SelectCategory
                && selectedOption.Category is not null
            )
            {
                await ShowCategoryMenuAsync(selectedOption.Category);
            }
        }
    }

    private async Task ShowCategoryMenuAsync(Category category)
    {
        while (true)
        {
            AnsiConsole.Clear();

            Result<List<DrinkSummary>> drinksResult = await DrinkService.GetDrinkSummariesAsync(
                client,
                category
            );
            if (drinksResult.IsError)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(drinksResult.Error!)}[/]");
                DrinkDisplay.PauseAndWaitForKey("Press any key to return to the main menu...");
                return;
            }

            Result<DrinkSummary> selectedDrink = MenuDisplay.SelectDrinkMenu(
                category,
                drinksResult.Data!
            );
            if (selectedDrink.IsCancelled)
            {
                return;
            }

            await ShowDrinkDetailsMenuAsync(
                selectedDrink.Data!,
                "Press any key to return to the drink list..."
            );
        }
    }

    private async Task ShowFavoritesMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();

            if (_favorites.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]You do not have any favorites yet.[/]");
                DrinkDisplay.PauseAndWaitForKey("Press any key to return to the main menu...");
                return;
            }

            Result<FavoriteDrink> selectedFavorite = MenuDisplay.SelectFavoriteMenu(_favorites);
            if (selectedFavorite.IsCancelled)
            {
                return;
            }

            await ShowDrinkDetailsMenuAsync(
                selectedFavorite.Data!.ToDrinkSummary(),
                "Press any key to return to the favorites menu..."
            );
        }
    }

    private async Task ShowDrinkDetailsMenuAsync(DrinkSummary drink, string returnMessage)
    {
        Result<DrinkDetails> drinkDetails = await DrinkService.GetDrinkDetailsAsync(client, drink);

        if (drinkDetails.IsError)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(drinkDetails.Error!)}[/]");
            DrinkDisplay.PauseAndWaitForKey(returnMessage);
            return;
        }

        while (true)
        {
            AnsiConsole.Clear();

            DrinkDetails details = drinkDetails.Data!;
            bool isFavorite = FavoritesService.IsFavorite(_favorites, details.Id);

            DrinkDisplay.ShowDrinkDetails(details, isFavorite);

            Result<MenuAction> selectedAction = MenuDisplay.SelectDrinkDetailsAction(isFavorite);
            if (selectedAction.IsCancelled || selectedAction.Data == MenuAction.Back)
            {
                return;
            }

            if (selectedAction.Data == MenuAction.AddFavorite)
            {
                Result<bool> addFavoriteResult = FavoritesService.AddFavorite(
                    _favorites,
                    favoritesFilePath,
                    details
                );
                if (addFavoriteResult.IsError)
                {
                    AnsiConsole.MarkupLine($"[red]{Markup.Escape(addFavoriteResult.Error!)}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine(
                        $"[green]{Markup.Escape(details.Name)} added to favorites.[/]"
                    );
                }

                DrinkDisplay.PauseAndWaitForKey(returnMessage);
                return;
            }

            if (selectedAction.Data == MenuAction.RemoveFavorite)
            {
                Result<bool> removeFavoriteResult = FavoritesService.RemoveFavorite(
                    _favorites,
                    favoritesFilePath,
                    details.Id
                );
                if (removeFavoriteResult.IsError)
                {
                    AnsiConsole.MarkupLine($"[red]{Markup.Escape(removeFavoriteResult.Error!)}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine(
                        $"[green]{Markup.Escape(details.Name)} removed from favorites.[/]"
                    );
                }

                DrinkDisplay.PauseAndWaitForKey(returnMessage);
                return;
            }
        }
    }
}
