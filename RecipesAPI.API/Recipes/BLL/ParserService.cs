using RecipeIngredientParser.Core.Parser;
using RecipeIngredientParser.Core.Parser.Extensions;
using RecipeIngredientParser.Core.Parser.Sanitization;
using RecipeIngredientParser.Core.Parser.Sanitization.Abstract;
using RecipeIngredientParser.Core.Parser.Strategy;
using RecipeIngredientParser.Core.Templates;
using RecipeIngredientParser.Core.Tokens;
using RecipeIngredientParser.Core.Tokens.Abstract;
using RecipeIngredientParser.Core.Tokens.Readers;
using RecipesAPI.Recipes.Common;
using RecipesAPI.Recipes.RecipeParser;

namespace RecipesAPI.Recipes.BLL;

public class ParserService
{
    private readonly ILogger<ParserService> logger;

    public ParserService(ILogger<ParserService> logger)
    {
        this.logger = logger;
    }

    public RecipeIngredient? Parse(string ingredient)
    {
        var parser = CreateParser();
        if (!parser.TryParseIngredient(ingredient, out var parseResult))
        {
            return null;
        }
        if (!double.TryParse(parseResult.Details.Amount ?? "", out var amountDouble) && parseResult.Details.Amount?.Contains("/") == true)
        {
            var fractionalToken = parseResult.Metadata.Tokens.FirstOrDefault(x => x is FractionalAmountToken) as FractionalAmountToken;
            if (fractionalToken != null)
            {
                try
                {
                    var result = fractionalToken.Numerator.Amount / fractionalToken.Denominator.Amount;
                    if (fractionalToken.WholeNumber != null)
                    {
                        result += fractionalToken.WholeNumber.Amount;
                    }
                    amountDouble = (double)result;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "failed to convert fractional token to double");
                }
            }
        }
        var recipeIngredient = new RecipeIngredient
        {
            Original = ingredient,
            Unit = parseResult.Details.Unit,
            Volume = amountDouble,
            Title = parseResult.Details.Ingredient,
            Meta = new List<string>()
        };
        if (!string.IsNullOrEmpty(parseResult.Details.Form))
        {
            recipeIngredient.Meta.Add(parseResult.Details.Form);
        }
        return recipeIngredient;
    }

    private static IngredientParser CreateParser()
    {
        return IngredientParser
            .Builder
            .New
                .WithTemplateDefinitions(TemplateDefinitions.DefaultTemplateDefinitions)
                .WithTokenReaderFactory(new TokenReaderFactory(new ITokenReader[]
                {
                    new AmountTokenReader(),
                    new CustomUnitTokenReader(),
                    new FormTokenReader(),
                    new IngredientTokenReader()
                }))
                .WithParserStrategy(new FirstFullMatchParserStrategy())
                .WithSanitizationRules(new IInputSanitizationRule[]
                {
                    new RemoveExtraneousSpacesRule(),
                    new RangeSubstitutionRule(),
                    new RemoveBracketedTextRule(),
                    new RemoveAlternateIngredientsRule(),
                    new ReplaceUnicodeFractionsRule(),
                    new RemoveExtraneousSpacesRule(),
                    new ConvertToLowerCaseRule()
                })
            .WithParserStrategy(new BestFullMatchParserStrategy(
                BestMatchHeuristics.WeightedTokenHeuristic(TokenWeightResolver)))
            .Build();
    }

    private static Func<IToken, decimal> TokenWeightResolver => token =>
        {
            switch (token)
            {
                case LiteralToken literalToken:
                    // Longer literals score more - the assumption being that
                    // a longer literal means a more specific value.
                    return 0.1m * literalToken.Value.Length;

                case LiteralAmountToken _:
                case FractionalAmountToken _:
                case RangeAmountToken _:
                    return 1.0m;

                case CustomUnitToken unitToken:
                    return unitToken.Type == CustomUnitType.Unknown ?
                        // Punish unknown unit types
                        -1.0m :
                        1.0m;

                case FormToken _:
                    return 1.0m;

                case IngredientToken _:
                    return 2.0m;
            }

            return 0.0m;
        };
}