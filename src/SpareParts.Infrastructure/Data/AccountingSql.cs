namespace SpareParts.Infrastructure.Data
{
    internal static class AccountingSql
    {
        public static string NormalizeAccountTypeKey(string columnExpression)
        {
            return $@"CASE
    WHEN {columnExpression} IS NULL OR LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))) = ''
        THEN 'asset'
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))), ' ', ''), '_', ''), '-', '')) IN ('ASSET', 'ASSETS', '0')
        THEN 'asset'
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))), ' ', ''), '_', ''), '-', '')) IN ('LIABILITY', 'LIABILITIES', '1')
        THEN 'liability'
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))), ' ', ''), '_', ''), '-', '')) IN ('EQUITY', '2')
        THEN 'equity'
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))), ' ', ''), '_', ''), '-', '')) IN ('INCOME', 'REVENUE', 'REVENUES', 'SALE', 'SALES', 'SALESREVENUE', '3')
        THEN 'income'
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))), ' ', ''), '_', ''), '-', '')) IN ('EXPENSE', 'EXPENSES', 'COGS', 'COSTOFGOODSSOLD', 'COSTOFSALES', '4')
        THEN 'expense'
    ELSE LOWER(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))))
END";
        }

        public static string AccountTypeLabel(string columnExpression)
        {
            return $@"CASE LOWER(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))))
    WHEN 'asset' THEN 'Asset'
    WHEN 'liability' THEN 'Liability'
    WHEN 'equity' THEN 'Equity'
    WHEN 'income' THEN 'Income'
    WHEN 'expense' THEN 'Expense'
    ELSE CONVERT(NVARCHAR(80), {columnExpression})
END";
        }

        public static string NormalizeAccountType(string columnExpression)
        {
            return $@"CASE
    WHEN TRY_CONVERT(INT, CONVERT(NVARCHAR(50), {columnExpression})) IS NOT NULL
        THEN TRY_CONVERT(INT, CONVERT(NVARCHAR(50), {columnExpression}))
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))), ' ', ''), '_', ''), '-', '')) IN ('ASSET', 'ASSETS')
        THEN 0
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))), ' ', ''), '_', ''), '-', '')) IN ('LIABILITY', 'LIABILITIES')
        THEN 1
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))), ' ', ''), '_', ''), '-', '')) = 'EQUITY'
        THEN 2
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))), ' ', ''), '_', ''), '-', '')) IN ('INCOME', 'REVENUE', 'REVENUES', 'SALE', 'SALES', 'SALESREVENUE')
        THEN 3
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), {columnExpression}))), ' ', ''), '_', ''), '-', '')) IN ('EXPENSE', 'EXPENSES', 'COGS', 'COSTOFGOODSSOLD', 'COSTOFSALES')
        THEN 4
    ELSE 0
END";
        }

        public static int ToLegacyAccountType(string? accountTypeKey)
        {
            return (accountTypeKey ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "liability" => 1,
                "equity" => 2,
                "income" => 3,
                "expense" => 4,
                _ => 0
            };
        }
    }
}
