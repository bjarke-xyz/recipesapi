using System.Reflection;
using RecipeIngredientParser.Core.Parser;
using RecipeIngredientParser.Core.Tokens;
using RecipeIngredientParser.Core.Tokens.Abstract;

namespace RecipesAPI.API.Recipes.RecipeParser.CustomIngredientToken
{
    /// <summary>
    /// Represents an ingredient token.
    /// </summary>
    public sealed class CustomIngredientToken : IToken
    {
        /// <summary>
        /// Gets or sets the ingredient.
        /// </summary>
        public string? Ingredient { get; set; }

        public string? Meta { get; set; }

        /// <inheritdoc/>
        public void Accept(ParserTokenVisitor parserTokenVisitor)
        {
            parserTokenVisitor.Visit(this);
        }
    }

    public static class ParserTokenVisitorExtension
    {
        public static void Visit(this ParserTokenVisitor visitor, CustomIngredientToken token)
        {
            // reflection hack
            // TODO: Find better way of doing this
            var field = visitor.GetType().GetField("_parseResult", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Instance);
            var parseResult = field?.GetValue(visitor) as ParseResult;
            if (parseResult != null)
            {
                parseResult.Details.Ingredient = token.Ingredient;

                if (!string.IsNullOrEmpty(token.Meta))
                {
                    var meta = token.Meta;
                    if (!string.IsNullOrEmpty(parseResult.Details.Form))
                    {
                        meta = $"¤{token.Meta}";
                    }
                    parseResult.Details.Form = meta;
                }
            }
        }
    }
}


// /// <summary>
// /// The implementation of <see cref="ParserTokenVisitor"/> for the ingredient token.
// /// </summary>
// public partial class ParserTokenVisitor
// {
//     /// <summary>
//     /// Visits a <see cref="IngredientToken"/>.
//     /// </summary>
//     /// <param name="token">A <see cref="IngredientToken"/> instance.</param>
//     public void Visit(CustomIngredientToken token)
//     {
//         _parseResult.Details.Ingredient = token.Ingredient;
//     }
// }
