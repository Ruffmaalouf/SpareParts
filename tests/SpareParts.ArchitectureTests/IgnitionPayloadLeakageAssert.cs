using System.Collections;
using System.Reflection;

namespace SpareParts.ArchitectureTests;

/// <summary>
/// Report 04 §09 / Report 06 §06–§07 "zero tenant/dbName leakage in responses" rule:
/// no Ignition response DTO — walking the full object graph — may expose a property whose
/// name betrays tenant identity, database/connection internals, or cache metadata
/// (the illustrative <c>cacheMeta</c> block from Report 04's example payload must be stripped).
///
/// The walk only recurses into SpareParts.Domain types so it asserts the wire contract of the
/// server read models, not framework primitives.
/// </summary>
public static class IgnitionPayloadLeakageAssert
{
    private static readonly string[] ForbiddenNameFragments =
    [
        "tenant", "dbname", "database", "connectionstring", "connection",
        "cachekey", "cachemeta", "cache", "etag", "password", "secret", "apikey", "internaldb"
    ];

    public static void AssertNoLeakage(Type root)
    {
        var visited = new HashSet<Type>();
        var offenders = new List<string>();
        Walk(root, visited, offenders);

        Assert.True(
            offenders.Count == 0,
            "Ignition response DTOs must not leak tenant/db/cache internals (Report 04 §09). Offending members: "
                + string.Join(", ", offenders));
    }

    private static void Walk(Type type, HashSet<Type> visited, List<string> offenders)
    {
        type = UnwrapNullable(type);
        if (!IsDomainType(type) || !visited.Add(type))
        {
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            foreach (var fragment in ForbiddenNameFragments)
            {
                if (property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add($"{type.Name}.{property.Name}");
                }
            }

            foreach (var referenced in EnumerateReferencedTypes(property.PropertyType))
            {
                Walk(referenced, visited, offenders);
            }
        }
    }

    private static IEnumerable<Type> EnumerateReferencedTypes(Type type)
    {
        type = UnwrapNullable(type);

        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
        {
            foreach (var argument in type.GetGenericArguments())
            {
                yield return UnwrapNullable(argument);
            }

            yield break;
        }

        if (type.IsArray && type.GetElementType() is { } elementType)
        {
            yield return UnwrapNullable(elementType);
            yield break;
        }

        yield return type;
    }

    private static Type UnwrapNullable(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    private static bool IsDomainType(Type type)
        => type.Namespace?.StartsWith("SpareParts.Domain", StringComparison.Ordinal) == true;
}
