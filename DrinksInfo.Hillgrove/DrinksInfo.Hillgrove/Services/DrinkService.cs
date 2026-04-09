using System.Net.Http.Json;
using DrinksInfo.Hillgrove.Common;
using DrinksInfo.Hillgrove.Models;

namespace DrinksInfo.Hillgrove.Services;

static class DrinkService
{
    public static async Task<Result<List<Category>>> GetCategoriesAsync(HttpClient client)
    {
        Result<CategoryResponse> categoriesResponse = await GetJsonAsync<CategoryResponse>(
            client,
            "list.php?c=list",
            "categories"
        );
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

    public static async Task<Result<List<DrinkSummary>>> GetDrinkSummariesAsync(
        HttpClient client,
        Category category
    )
    {
        Result<DrinksResponse> drinksResponse = await GetJsonAsync<DrinksResponse>(
            client,
            $"filter.php?c={Uri.EscapeDataString(category.Name)}",
            "drink summaries"
        );

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

    public static async Task<Result<DrinkDetails>> GetDrinkDetailsAsync(
        HttpClient client,
        DrinkSummary drink
    )
    {
        Result<DrinkDetailsResponse> detailsResponse = await GetJsonAsync<DrinkDetailsResponse>(
            client,
            $"lookup.php?i={Uri.EscapeDataString(drink.Id)}",
            "drink details"
        );

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

    static async Task<Result<TResponse>> GetJsonAsync<TResponse>(
        HttpClient client,
        string endpoint,
        string errorContext
    )
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
}
