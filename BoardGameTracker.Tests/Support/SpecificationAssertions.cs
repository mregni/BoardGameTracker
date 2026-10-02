using System.Collections.Generic;
using System.Linq.Expressions;
using Ardalis.Specification;
using FluentAssertions;

namespace BoardGameTracker.Tests.Support;

public static class SpecificationAssertions
{
    public static void ShouldIncludeExactly<T>(this ISpecification<T> spec, params string[] navigationPaths)
    {
        var paths = new List<string>();
        var current = string.Empty;
        foreach (var include in spec.IncludeExpressions)
        {
            var member = MemberName(include.LambdaExpression);
            current = include.Type == IncludeTypeEnum.ThenInclude ? $"{current}.{member}" : member;
            paths.Add(current);
        }

        paths.Should().BeEquivalentTo(navigationPaths);
    }

    private static string MemberName(LambdaExpression lambda)
    {
        var body = lambda.Body is UnaryExpression unary ? unary.Operand : lambda.Body;
        return ((MemberExpression)body).Member.Name;
    }
}
