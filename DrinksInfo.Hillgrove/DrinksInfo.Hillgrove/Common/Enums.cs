namespace DrinksInfo.Hillgrove.Common;

enum MenuAction
{
    SelectCategory,
    SelectDrink,
    ViewFavorites,
    OpenFavorite,
    AddFavorite,
    RemoveFavorite,
    Back,
    Exit,
}

enum ResultStatus
{
    Success,
    Cancelled,
    Error,
}
