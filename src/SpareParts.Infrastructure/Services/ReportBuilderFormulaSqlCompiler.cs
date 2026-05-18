using System.Globalization;
using System.Text;

namespace SpareParts.Infrastructure.Services;

internal static class ReportBuilderFormulaSqlCompiler
{
    public static string CompileArithmeticExpression(
        string expression,
        IReadOnlyDictionary<string, (string SqlExpression, bool SupportsArithmetic)> bindings,
        string contextName)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ValidationException($"{contextName} formula cannot be empty.");
        }

        var tokens = Tokenize(expression);
        if (tokens.Count == 0)
        {
            throw new ValidationException($"{contextName} formula cannot be empty.");
        }

        var index = 0;
        var sql = ParseExpression();
        if (index < tokens.Count)
        {
            throw new ValidationException($"{contextName} formula contains an unexpected token '{tokens[index].Value}'.");
        }

        return sql;

        string ParseExpression()
        {
            var left = ParseTerm();
            while (TryMatch("+") || TryMatch("-"))
            {
                var op = tokens[index - 1].Value;
                var right = ParseTerm();
                left = $"({left} {op} {right})";
            }

            return left;
        }

        string ParseTerm()
        {
            var left = ParseFactor();
            while (TryMatch("*") || TryMatch("/"))
            {
                var op = tokens[index - 1].Value;
                var right = ParseFactor();
                left = op == "/"
                    ? $"({left} / NULLIF({right}, 0))"
                    : $"({left} * {right})";
            }

            return left;
        }

        string ParseFactor()
        {
            if (TryMatch("+"))
            {
                return ParseFactor();
            }

            if (TryMatch("-"))
            {
                return $"(-1 * {ParseFactor()})";
            }

            if (TryMatch("("))
            {
                var inner = ParseExpression();
                Expect(")");
                return $"({inner})";
            }

            if (TryRead("number", out var numberToken))
            {
                return numberToken.Value;
            }

            if (TryRead("identifier", out var identifierToken))
            {
                var key = NormalizeIdentifier(identifierToken.Value);
                if (!bindings.TryGetValue(key, out var binding))
                {
                    throw new ValidationException($"{contextName} formula references '{identifierToken.Value}', but that field is not available.");
                }

                if (!binding.SupportsArithmetic)
                {
                    throw new ValidationException($"{contextName} formula field '{identifierToken.Value}' is not numeric.");
                }

                return $"TRY_CONVERT(DECIMAL(38, 10), {binding.SqlExpression})";
            }

            throw new ValidationException($"{contextName} formula contains an invalid token near '{tokens[index].Value}'.");
        }

        bool TryMatch(string value)
        {
            if (index >= tokens.Count || !string.Equals(tokens[index].Value, value, StringComparison.Ordinal))
            {
                return false;
            }

            index++;
            return true;
        }

        bool TryRead(string kind, out (string Kind, string Value) token)
        {
            if (index < tokens.Count && string.Equals(tokens[index].Kind, kind, StringComparison.Ordinal))
            {
                token = tokens[index++];
                return true;
            }

            token = default;
            return false;
        }

        void Expect(string value)
        {
            if (!TryMatch(value))
            {
                throw new ValidationException($"{contextName} formula is missing '{value}'.");
            }
        }
    }

    public static string NormalizeIdentifier(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            trimmed = trimmed[1..^1].Trim();
        }

        return trimmed;
    }

    private static List<(string Kind, string Value)> Tokenize(string expression)
    {
        var tokens = new List<(string Kind, string Value)>();
        var index = 0;

        while (index < expression.Length)
        {
            var current = expression[index];
            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if ("+-*/()".IndexOf(current) >= 0)
            {
                tokens.Add(("symbol", current.ToString()));
                index++;
                continue;
            }

            if (current == '[')
            {
                var closeIndex = expression.IndexOf(']', index + 1);
                if (closeIndex < 0)
                {
                    throw new ValidationException("Formula contains '[' without a closing ']'.");
                }

                var identifier = expression[(index + 1)..closeIndex].Trim();
                if (string.IsNullOrWhiteSpace(identifier))
                {
                    throw new ValidationException("Formula contains an empty [identifier].");
                }

                tokens.Add(("identifier", identifier));
                index = closeIndex + 1;
                continue;
            }

            if (char.IsDigit(current) || (current == '.' && index + 1 < expression.Length && char.IsDigit(expression[index + 1])))
            {
                var builder = new StringBuilder();
                var hasDecimalPoint = false;

                while (index < expression.Length)
                {
                    current = expression[index];
                    if (char.IsDigit(current))
                    {
                        builder.Append(current);
                        index++;
                        continue;
                    }

                    if (current == '.' && !hasDecimalPoint)
                    {
                        builder.Append(current);
                        hasDecimalPoint = true;
                        index++;
                        continue;
                    }

                    break;
                }

                if (!decimal.TryParse(builder.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                {
                    throw new ValidationException($"Formula number '{builder}' is invalid.");
                }

                tokens.Add(("number", builder.ToString()));
                continue;
            }

            var startIndex = index;
            while (index < expression.Length
                   && !char.IsWhiteSpace(expression[index])
                   && "+-*/()".IndexOf(expression[index]) < 0)
            {
                index++;
            }

            var token = expression[startIndex..index].Trim();
            if (!string.IsNullOrWhiteSpace(token))
            {
                tokens.Add(("identifier", token));
            }
        }

        return tokens;
    }
}
