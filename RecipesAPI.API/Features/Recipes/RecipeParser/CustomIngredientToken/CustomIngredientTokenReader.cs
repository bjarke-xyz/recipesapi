using System.Text;
using System.Text.RegularExpressions;
using RecipeIngredientParser.Core.Parser.Context;
using RecipeIngredientParser.Core.Parser.Extensions;
using RecipeIngredientParser.Core.Tokens.Abstract;

namespace RecipesAPI.API.Features.Recipes.RecipeParser.CustomIngredientToken;

/// <summary>
/// A token reader responsible for the {ingredient} token type.
/// </summary>
public class CustomIngredientTokenReader : ITokenReader
{
    /// <inheritdoc/>
    public string TokenType => "{ingredient}";

    private readonly Regex percentageRegex = new Regex(@"\d+\s*%");

    /// <inheritdoc/>
    public bool TryReadToken(ParserContext context, out IToken? token)
    {
        var rawIngredient = new StringBuilder();

        while (context.Buffer.HasNext() && NextCharacterIsValid(context))
        {
            var c = context.Buffer.Next();

            rawIngredient.Append(c);
        }

        var rawPercentage = new StringBuilder();
        while (context.Buffer.HasNext() && (context.Buffer.IsDigit() || context.Buffer.Matches(x => x == '%')))
        {
            var c = context.Buffer.Next();
            rawPercentage.Append(c);

            if (percentageRegex.IsMatch(rawPercentage.ToString()))
            {
                break;
            }
        }

        string? percentageStr = null;
        if (percentageRegex.IsMatch(rawPercentage.ToString()))
        {
            percentageStr = rawPercentage.ToString();
        }

        token = GenerateToken(rawIngredient.ToString(), percentageStr);

        return token != null;
    }

    private CustomIngredientToken? GenerateToken(string rawIngredient, string? meta = null)
    {
        if (string.IsNullOrEmpty(rawIngredient))
        {
            return null;
        }

        return new CustomIngredientToken()
        {
            Ingredient = rawIngredient.Trim(),
            Meta = meta?.Trim(),
        };
    }

    private bool NextCharacterIsValid(ParserContext context) =>
        context.Buffer.IsLetter() ||
        context.Buffer.IsWhitespace() ||
        context.Buffer.Matches(c => c == '-');
}