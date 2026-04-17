using Dapper;
using SpareParts.Domain.Accounting;
using System.Data;
using System.Globalization;

namespace SpareParts.Infrastructure.Data
{
    public static class AccountingDapperBootstrap
    {
        private static bool _initialized;
        private static readonly object SyncRoot = new();

        public static void EnsureConfigured()
        {
            if (_initialized)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_initialized)
                {
                    return;
                }

                SqlMapper.AddTypeHandler(new AccountTypeTypeHandler());
                _initialized = true;
            }
        }

        private sealed class AccountTypeTypeHandler : SqlMapper.TypeHandler<AccountType>
        {
            public override void SetValue(IDbDataParameter parameter, AccountType value)
            {
                parameter.Value = (int)value;
                parameter.DbType = DbType.Int32;
            }

            public override AccountType Parse(object value)
            {
                return value switch
                {
                    null => AccountType.Asset,
                    AccountType accountType => accountType,
                    byte numeric => (AccountType)numeric,
                    short numeric => (AccountType)numeric,
                    int numeric => (AccountType)numeric,
                    long numeric => (AccountType)numeric,
                    decimal numeric => (AccountType)decimal.ToInt32(numeric),
                    string text => ParseText(text),
                    _ => ParseText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
                };
            }

            private static AccountType ParseText(string text)
            {
                var normalized = Normalize(text);
                return normalized switch
                {
                    "" => AccountType.Asset,
                    "0" or "asset" or "assets" => AccountType.Asset,
                    "1" or "liability" or "liabilities" => AccountType.Liability,
                    "2" or "equity" => AccountType.Equity,
                    "3" or "income" or "revenue" or "revenues" or "salesrevenue" => AccountType.Income,
                    "4" or "expense" or "expenses" or "cogs" or "costofgoodsold" or "costofsales" => AccountType.Expense,
                    _ when Enum.TryParse<AccountType>(text, ignoreCase: true, out var parsed) => parsed,
                    _ => throw new ArgumentException($"Unsupported account type '{text}'.")
                };
            }

            private static string Normalize(string value)
                => new(value
                    .Trim()
                    .Where(char.IsLetterOrDigit)
                    .Select(char.ToLowerInvariant)
                    .ToArray());
        }
    }
}
