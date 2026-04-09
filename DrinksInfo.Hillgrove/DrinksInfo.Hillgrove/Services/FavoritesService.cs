using System.Text.Json;
using DrinksInfo.Hillgrove.Common;
using DrinksInfo.Hillgrove.Models;

namespace DrinksInfo.Hillgrove.Services;

static class FavoritesService
{
    public static Result<List<FavoriteDrink>> LoadFavorites(string filePath)
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

    public static Result<bool> AddFavorite(
        List<FavoriteDrink> favorites,
        string filePath,
        DrinkDetails details
    )
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

    public static Result<bool> RemoveFavorite(
        List<FavoriteDrink> favorites,
        string filePath,
        string drinkId
    )
    {
        FavoriteDrink? favorite = favorites.FirstOrDefault(existingFavorite =>
            existingFavorite.Id == drinkId
        );
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

    public static bool IsFavorite(List<FavoriteDrink> favorites, string drinkId)
    {
        return favorites.Any(favorite => favorite.Id == drinkId);
    }

    static Result<bool> SaveFavorites(string filePath, List<FavoriteDrink> favorites)
    {
        try
        {
            string json = JsonSerializer.Serialize(
                favorites,
                new JsonSerializerOptions { WriteIndented = true }
            );
            File.WriteAllText(filePath, json);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Unable to save favorites: {ex.Message}");
        }
    }
}
