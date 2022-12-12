using System.Linq.Expressions;
using System.Reflection;

namespace RecipesAPI.API.Utils;

public static class ClassUtils
{
    public static bool IsPropertyOf<T>(string propertyName, out PropertyInfo? propertyInfo)
    {
        var type = typeof(T);
        propertyInfo = null;
        if (type == null) return false;
        var properties = type.GetProperties();
        propertyInfo = properties.FirstOrDefault(x => string.Equals(x.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        return propertyInfo != null;
    }

    public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string orderByProperty, bool desc)
    {
        string command = desc ? "OrderByDescending" : "OrderBy";
        var type = typeof(TEntity);
        var property = type.GetProperty(orderByProperty);
        if (property == null) throw new ArgumentException($"Property '{orderByProperty}' does not exist on type '{type.FullName}'");

        var parameter = Expression.Parameter(type, "p");
        var propertyAccess = Expression.MakeMemberAccess(parameter, property);
        var orderByExpression = Expression.Lambda(propertyAccess, parameter);
        var resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, property.PropertyType },
                                      source.Expression, Expression.Quote(orderByExpression));
        return source.Provider.CreateQuery<TEntity>(resultExpression);
    }
}