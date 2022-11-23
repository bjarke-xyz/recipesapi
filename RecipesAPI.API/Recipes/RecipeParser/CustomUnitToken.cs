using System.Reflection;
using RecipeIngredientParser.Core.Parser;
using RecipeIngredientParser.Core.Tokens;
using RecipeIngredientParser.Core.Tokens.Abstract;
using RecipesAPI.API.Recipes.RecipeParser;

namespace RecipesAPI.API.Recipes.RecipeParser
{
    public sealed class CustomUnitToken : IToken
    {
        //
        // Summary:
        //     Gets or sets the unit.
        public string Unit { get; set; } = default!;
        //
        // Summary:
        //     Gets or sets the unit type.
        public CustomUnitType Type { get; set; }
        public void Accept(ParserTokenVisitor parserTokenVisitor)
        {
            parserTokenVisitor.Visit(this);
        }
    }

    public static class ParserTokenVisitorExtension
    {
        public static void Visit(this ParserTokenVisitor visitor, CustomUnitToken token)
        {
            // reflection hack
            // TODO: Find better way of doing this
            var field = visitor.GetType().GetField("_parseResult", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Instance);
            var parseResult = field?.GetValue(visitor) as ParseResult;
            if (parseResult != null)
            {
                parseResult.Details.Unit = token.Unit;
            }
        }
    }
}
