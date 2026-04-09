using DrinksInfo.Hillgrove.Models;
using Spectre.Console;

namespace DrinksInfo.Hillgrove.UI;

static class DrinkDisplay
{
    public static void ShowDrinkDetails(DrinkDetails details, bool isFavorite)
    {
        if (isFavorite)
        {
            AnsiConsole.MarkupLine("[yellow]This drink is in your favorites.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]This drink is not in your favorites.[/]");
        }

        Table table = new Table().Title("[yellow]Drink[/]").AddColumn("Field").AddColumn("Value");

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

    public static void PauseAndWaitForKey(string message = "Press any key to continue...")
    {
        AnsiConsole.Markup($"\n[grey]{Markup.Escape(message)}[/]");
        Console.ReadKey(true);
    }
}
