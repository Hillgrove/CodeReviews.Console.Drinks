# Drinks Info - Console App

A C# console application that lets you browse cocktails by category, view drink details, and save your favorites - powered by [TheCocktailDB API](https://www.thecocktaildb.com/api.php).

## Requirements

- [x] Present the user with a Drinks Category Menu on startup
- [x] Allow the user to choose a category and then a drink from that category
- [x] Display full drink details with no empty/null properties shown
- [x] Handle API errors gracefully — the app does not crash if the API is unavailable
- [x] Favorites list — add and remove drinks, persisted between sessions

## Features

- **Category browser** — fetches all drink categories from the API and lets you pick one from an interactive menu
- **Drink list** — shows all drinks in the selected category; select one to see its full details
- **Drink details** — displays name, category, glass type, alcoholic/non-alcoholic, ingredients with measures, and preparation instructions; null fields are filtered out
- **Favorites** — add or remove any drink as a favorite; favorites are saved to a local JSON file and reload automatically on next launch
- **Error handling** — if the API is unreachable or returns unexpected data, the app shows a message and continues rather than crashing

## How It Works

On startup the app fetches all available drink categories from the API and presents them in a menu. You pick a category, browse the drinks in it, and select one to see its full details: ingredients, measurements, glass type, instructions, and whether it's alcoholic. From the details view you can add or remove the drink from your personal favorites list. Favorites are saved to a local JSON file so they persist between sessions.

The app is built in layers:

- **Models** — plain data classes for API responses (`Category`, `DrinkSummary`, `DrinkDetails`, `FavoriteDrink`)
- **Services** — `DrinkService` handles all HTTP calls; `FavoritesService` handles reading and writing the favorites file
- **UI** — `MenuDisplay` builds all the interactive menus using Spectre.Console; `DrinkDisplay` renders drink details to the terminal
- **Common** — a generic `Result<T>` type used throughout to represent success, failure, or cancellation without throwing exceptions

## Technologies Used

- .NET 10 / C#
- [TheCocktailDB API](https://www.thecocktaildb.com/api.php) — free, public cocktail database
- [Spectre.Console](https://spectreconsole.net/) — rich terminal UI (interactive menus, styled output)

## Challenges

**`JsonExtensionData` and the messy ingredients API**
The hardest problem was deserializing drink details. TheCocktailDB returns ingredients and measures as 15 separate nullable properties (`strIngredient1`…`strIngredient15`) rather than a proper array. Most of them are null for any given drink. I solved this with `[JsonExtensionData]`, which captures all the leftover JSON properties into a `Dictionary<string, JsonElement>` at deserialization time. I could then loop over the numbered keys and filter out the nulls to build a clean `List<(ingredient, measure)>`.

**The `Result<T>` pattern**
First time using this pattern. The concept clicked fairly quickly, but the static factory methods (`Result<T>.Success(data)`, `Result<T>.Failure(error)`, `Result<T>.Cancelled()`) took a moment to feel natural. it looks a bit unusual at first that a generic type is constructing instances of itself. Wiring the pattern through the menu layer (checking `IsCancelled`, `IsError`, `IsSuccess` at every decision point) also required deliberate thought; it's not second nature yet.

**Error path thinking in services**
Services are becoming familiar, but the discipline of handling every failure path - not just the happy path - still takes conscious effort. The current implementation covers the main cases but is probably not exhaustive.

## What Came Easily

JSON serialization and deserialization came back quickly even though it had been a while. `[JsonPropertyName]` was new but straightforward once I saw the pattern.

## What I Learned

- How to handle poorly structured API responses using `[JsonExtensionData]`
- The `Result<T>` pattern as an alternative to exceptions for control flow

