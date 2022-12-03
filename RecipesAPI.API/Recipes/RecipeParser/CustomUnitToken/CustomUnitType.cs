namespace RecipesAPI.API.Recipes.RecipeParser.CustomUnitToken;

public enum CustomUnitType
{
    //
    // Summary:
    //     A teaspoon.
    Teaspoon = 0,
    //
    // Summary:
    //     A tablespoon.
    Tablespoon = 1,
    //
    // Summary:
    //     A cup.
    Cup = 2,
    //
    // Summary:
    //     A gram.
    Gram = 3,
    //
    // Summary:
    //     A kilogram.
    Kilogram = 4,
    //
    // Summary:
    //     A handful.
    Handful = 5,
    //
    // Summary:
    //     An ounce.
    Ounce = 6,
    //
    // Summary:
    //     A can.
    Can = 7,
    //
    // Summary:
    //     A bunch.
    Bunch = 8,
    //
    // Summary:
    //     A pound.
    Pound = 9,

    Bag,
    Box,
    Clove,
    Piece,
    Milliliter,
    Deciliter,
    Liter,
    Milligram,
    Package,
    Stick,
    Pinch, // Knivspids




    //
    // Summary:
    //     A catch-all for unknown unit types.
    Unknown = 1000
}