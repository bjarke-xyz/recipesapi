using System.Text;
using RecipeIngredientParser.Core.Parser.Context;
using RecipeIngredientParser.Core.Parser.Extensions;
using RecipeIngredientParser.Core.Tokens;
using RecipeIngredientParser.Core.Tokens.Abstract;

namespace RecipesAPI.Recipes.RecipeParser;

/// <summary>
/// A token reader responsible for the {unit} token type.
/// </summary>
public class CustomUnitTokenReader : ITokenReader
{
    private static readonly Dictionary<string, CustomUnitType> DefaultUnitMappings = new Dictionary<string, CustomUnitType>
        {
            { "tsp", CustomUnitType.Teaspoon },
            { "t.", CustomUnitType.Teaspoon },
            { "t", CustomUnitType.Teaspoon },
            { "teaspoon", CustomUnitType.Teaspoon },
            { "teaspoons", CustomUnitType.Teaspoon },
            {"teske", CustomUnitType.Teaspoon},
            {"tsk", CustomUnitType.Teaspoon},
            {"tsk.", CustomUnitType.Teaspoon},

            { "tbl", CustomUnitType.Tablespoon },
            { "tbsp.", CustomUnitType.Tablespoon },
            { "tbsp", CustomUnitType.Tablespoon },
            { "tablespoon", CustomUnitType.Tablespoon },
            { "tablespoons", CustomUnitType.Tablespoon },
            {"spsk", CustomUnitType.Tablespoon},
            {"spsk.", CustomUnitType.Tablespoon},

            { "cup", CustomUnitType.Cup },
            { "cups", CustomUnitType.Cup },
            { "c.", CustomUnitType.Cup },
            { "c", CustomUnitType.Cup },

            { "gram", CustomUnitType.Gram },
            { "grams", CustomUnitType.Gram },
            { "g.", CustomUnitType.Gram },
            { "g", CustomUnitType.Gram },

            { "kilogram", CustomUnitType.Kilogram },
            { "Kilograms", CustomUnitType.Kilogram },
            { "kg.", CustomUnitType.Kilogram },
            { "kg", CustomUnitType.Kilogram },
            { "Kg", CustomUnitType.Kilogram },
            { "Kg.", CustomUnitType.Kilogram },

            { "handful", CustomUnitType.Handful },
            { "håndfuld", CustomUnitType.Handful },

            { "ounce", CustomUnitType.Ounce },
            { "ounces", CustomUnitType.Ounce },
            { "oz", CustomUnitType.Ounce },
            { "oz.", CustomUnitType.Ounce },

            { "can", CustomUnitType.Can },
            { "cans", CustomUnitType.Can },
            {"dåse", CustomUnitType.Can },
            {"dåser", CustomUnitType.Can },

            { "bunch", CustomUnitType.Bunch },

            { "pound", CustomUnitType.Pound },
            { "pounds", CustomUnitType.Pound },
            { "lb", CustomUnitType.Pound },
            { "lb.", CustomUnitType.Pound },
            {"pund", CustomUnitType.Pound },

            {"pose", CustomUnitType.Bag},
            {"poser", CustomUnitType.Bag},
            {"bag", CustomUnitType.Bag},
            {"bags", CustomUnitType.Bag},
            {"kasse", CustomUnitType.Box},
            {"kasser", CustomUnitType.Box},
            {"box", CustomUnitType.Box},

            {"fed", CustomUnitType.Clove},
            {"clove", CustomUnitType.Clove},

            {"piece", CustomUnitType.Piece},
            {"stykke", CustomUnitType.Piece},
            {"stk", CustomUnitType.Piece},
            {"stk.", CustomUnitType.Piece},

            {"milliliter", CustomUnitType.Milliliter},
            {"ml", CustomUnitType.Milliliter},
            {"ml.", CustomUnitType.Milliliter},
            {"mL", CustomUnitType.Milliliter},
            {"mL.", CustomUnitType.Milliliter},

            {"deciliter", CustomUnitType.Deciliter},
            {"dl", CustomUnitType.Deciliter},
            {"dl.", CustomUnitType.Deciliter},
            {"dL", CustomUnitType.Deciliter},
            {"dL.", CustomUnitType.Deciliter},

            {"liter", CustomUnitType.Liter},
            {"l", CustomUnitType.Liter},
            {"l.", CustomUnitType.Liter},
            {"lt", CustomUnitType.Liter},
            {"Lt", CustomUnitType.Liter},
            {"LT", CustomUnitType.Liter},
            {"L", CustomUnitType.Liter},
            {"L.", CustomUnitType.Liter},

            {"milligram", CustomUnitType.Milligram},
            {"mg", CustomUnitType.Milligram},
            {"mg.", CustomUnitType.Milligram},

            {"pakke", CustomUnitType.Package},
            {"package", CustomUnitType.Package},

            {"stang", CustomUnitType.Stick},
            {"stænger", CustomUnitType.Stick},
            {"stick", CustomUnitType.Stick},
            {"sticks", CustomUnitType.Stick},

            {"knivspids", CustomUnitType.Pinch},
            {"pinch", CustomUnitType.Pinch},

        };

    private readonly IDictionary<string, CustomUnitType> _unitMappings;

    /// <summary>
    /// Initialises a new instance of the <see cref="UnitTokenReader"/> class
    /// that will use the default unit mappings.
    /// </summary>
    public CustomUnitTokenReader()
    {
        _unitMappings = DefaultUnitMappings;
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="UnitTokenReader"/> class
    /// that will use the supplied unit mappings.
    /// </summary>
    /// <param name="unitMappings">A lookup for raw unit values (e.g. grams) to a <see cref="UnitType"/>.</param>
    public CustomUnitTokenReader(IDictionary<string, CustomUnitType> unitMappings)
    {
        _unitMappings = unitMappings;
    }

    /// <inheritdoc/>
    public string TokenType => "{unit}";

    /// <inheritdoc/>
    public bool TryReadToken(ParserContext context, out IToken token)
    {
        var rawUnit = new StringBuilder();

        while (context.Buffer.HasNext() &&
               (context.Buffer.IsLetter() || context.Buffer.Matches(c => c == '.')))
        {
            var c = context.Buffer.Next();

            rawUnit.Append(c);
        }

#pragma warning disable CS8601 // ITokenReader interface does not support nullable IToken
        token = GenerateToken(rawUnit.ToString());
#pragma warning restore CS8601

        return token != null;
    }

    private CustomUnitToken? GenerateToken(string rawUnit)
    {
        if (string.IsNullOrEmpty(rawUnit))
        {
            return null;
        }

        return new CustomUnitToken()
        {
            Unit = rawUnit,
            Type = GetUnitType(rawUnit)
        };
    }

    private CustomUnitType GetUnitType(string rawUnit)
    {
        return _unitMappings.TryGetValue(rawUnit, out var unitType) ?
            unitType :
            CustomUnitType.Unknown;
    }
}