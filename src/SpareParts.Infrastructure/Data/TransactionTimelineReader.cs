using Dapper;
using SpareParts.Domain.Transactions;

namespace SpareParts.Infrastructure.Data
{
    internal sealed class TransactionTimelineReader
    {
        private const string StatusComplete = "Complete";
        private const string StatusPartial = "Partial";
        private const string StatusPending = "Pending";

        private readonly DbSession _session;

        public TransactionTimelineReader(DbSession session)
        {
            _session = session;
        }

        public List<TransactionTimelineStepDto> Build(string transactionTypeKey, int referenceId)
        {
            var transaction = LoadTransaction(transactionTypeKey, referenceId);
            if (transaction == null)
            {
                return new List<TransactionTimelineStepDto>();
            }

            var journalFacts = LoadJournalFacts(transactionTypeKey, transaction);
            var timeline = new List<TransactionTimelineStepDto>
            {
                CreatedStep(transaction),
                ReceivedStep(transactionTypeKey, transaction),
                PostedStep(transactionTypeKey, transaction, journalFacts),
                PaidStep(transaction),
                JournalCreatedStep(journalFacts),
                AccountAffectedStep(journalFacts)
            };

            return timeline;
        }

        private TransactionTimelineSnapshot? LoadTransaction(string transactionTypeKey, int referenceId)
        {
            const string sql = @"SELECT TOP (1)
                                        tt.TypeKey,
                                        t.ReferenceId,
                                        t.TransactionNumber,
                                        t.TransactionDate,
                                        ISNULL(t.TotalAmount, 0) AS TotalAmount,
                                        ISNULL(t.PaidAmount, 0) AS PaidAmount,
                                        ISNULL(t.PaymentStatus, N'Unpaid') AS PaymentStatus,
                                        ISNULL(t.PostingStatus, N'') AS PostingStatus,
                                        t.PostedAt,
                                        t.CreatedAt,
                                        t.ModifiedAt,
                                        t.UsedCarId,
                                        uc.ReceivedAt
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 LEFT JOIN dbo.UsedCars uc ON uc.Id = t.UsedCarId
                                 WHERE tt.TypeKey = @TransactionTypeKey
                                   AND t.ReferenceId = @ReferenceId;";

            return _session.Connection.QueryFirstOrDefault<TransactionTimelineSnapshot>(
                sql,
                new { TransactionTypeKey = transactionTypeKey, ReferenceId = referenceId },
                _session.Transaction);
        }

        private static TransactionTimelineStepDto CreatedStep(TransactionTimelineSnapshot transaction)
            => new()
            {
                Sequence = 1,
                StageKey = "created",
                Title = "Created",
                Status = StatusComplete,
                OccurredAt = transaction.CreatedAt,
                Summary = "Transaction record created",
                Detail = $"Transaction {transaction.TransactionNumber} was captured for {transaction.TransactionDate:yyyy-MM-dd}."
            };

        private TransactionTimelineStepDto ReceivedStep(string transactionTypeKey, TransactionTimelineSnapshot transaction)
        {
            if (string.Equals(transactionTypeKey, TransactionTypeKeys.UsedCarPurchase, StringComparison.OrdinalIgnoreCase))
            {
                return new TransactionTimelineStepDto
                {
                    Sequence = 2,
                    StageKey = "received",
                    Title = "Received",
                    Status = transaction.ReceivedAt.HasValue ? StatusComplete : StatusPending,
                    OccurredAt = transaction.ReceivedAt,
                    Summary = transaction.ReceivedAt.HasValue ? "Linked vehicle received" : "Vehicle receipt pending",
                    Detail = transaction.ReceivedAt.HasValue
                        ? "The linked used car was marked received before purchase posting."
                        : "The linked used car has not been marked received yet."
                };
            }

            var referenceType = string.Equals(transactionTypeKey, TransactionTypeKeys.Sale, StringComparison.OrdinalIgnoreCase)
                ? "Sale"
                : "Purchase";
            var movement = LoadStockMovementFact(referenceType, transaction.ReferenceId);
            var isComplete = movement.MovementCount > 0;
            var isSale = string.Equals(transactionTypeKey, TransactionTypeKeys.Sale, StringComparison.OrdinalIgnoreCase);

            return new TransactionTimelineStepDto
            {
                Sequence = 2,
                StageKey = "received",
                Title = "Received",
                Status = isComplete ? StatusComplete : StatusPending,
                OccurredAt = movement.FirstMovementAt,
                Summary = isComplete
                    ? (isSale ? "Stock issue recorded" : "Inventory receipt recorded")
                    : (isSale ? "Stock issue pending" : "Inventory receipt pending"),
                Detail = isComplete
                    ? $"{movement.MovementCount:N0} inventory movement(s) were recorded with net quantity {movement.NetQuantity:N0}."
                    : "No inventory movement has been recorded for this transaction yet."
            };
        }

        private static TransactionTimelineStepDto PostedStep(
            string transactionTypeKey,
            TransactionTimelineSnapshot transaction,
            IReadOnlyCollection<JournalFact> journalFacts)
        {
            var firstJournalAt = journalFacts
                .Select(fact => (DateTime?)fact.CreatedAt)
                .OrderBy(value => value)
                .FirstOrDefault();

            if (string.Equals(transactionTypeKey, TransactionTypeKeys.UsedCarPurchase, StringComparison.OrdinalIgnoreCase))
            {
                var isPosted = string.Equals(transaction.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase);
                return new TransactionTimelineStepDto
                {
                    Sequence = 3,
                    StageKey = "posted",
                    Title = "Posted",
                    Status = isPosted ? StatusComplete : StatusPending,
                    OccurredAt = transaction.PostedAt ?? firstJournalAt,
                    Summary = isPosted ? "Posting confirmed" : "Posting pending",
                    Detail = isPosted
                        ? "The purchase was marked posted and locked into accounting."
                        : "The purchase is still a draft until it is posted from purchase history."
                };
            }

            var hasJournal = journalFacts.Count > 0;
            return new TransactionTimelineStepDto
            {
                Sequence = 3,
                StageKey = "posted",
                Title = "Posted",
                Status = hasJournal ? StatusComplete : StatusPending,
                OccurredAt = firstJournalAt,
                Summary = hasJournal ? "Auto posted" : "Posting pending",
                Detail = hasJournal
                    ? "Automatic posting created accounting journal activity for this transaction."
                    : "No accounting journal has been created for this transaction yet."
            };
        }

        private static TransactionTimelineStepDto PaidStep(TransactionTimelineSnapshot transaction)
        {
            var amountDue = decimal.Round(transaction.TotalAmount, 4, MidpointRounding.AwayFromZero);
            var amountPaid = decimal.Round(transaction.PaidAmount, 4, MidpointRounding.AwayFromZero);
            var isNoAmountDue = amountDue <= 0m;
            var isPaid = isNoAmountDue || amountPaid >= amountDue;
            var hasPayment = amountPaid > 0m;

            return new TransactionTimelineStepDto
            {
                Sequence = 4,
                StageKey = "paid",
                Title = "Paid",
                Status = isPaid ? StatusComplete : hasPayment ? StatusPartial : StatusPending,
                OccurredAt = hasPayment || isNoAmountDue ? transaction.ModifiedAt ?? transaction.CreatedAt : null,
                Summary = isNoAmountDue
                    ? "No payment due"
                    : isPaid
                        ? "Paid in full"
                        : hasPayment
                            ? "Partially paid"
                            : "Unpaid",
                Detail = isNoAmountDue
                    ? "The transaction total is zero."
                    : $"{amountPaid:N2} paid of {amountDue:N2}. Payment status: {transaction.PaymentStatus}."
            };
        }

        private static TransactionTimelineStepDto JournalCreatedStep(IReadOnlyCollection<JournalFact> journalFacts)
        {
            var entryCount = journalFacts.Select(fact => fact.JournalEntryId).Distinct().Count();
            var firstJournalAt = journalFacts
                .Select(fact => (DateTime?)fact.CreatedAt)
                .OrderBy(value => value)
                .FirstOrDefault();
            var references = journalFacts
                .Select(fact => fact.ReferenceType)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();

            return new TransactionTimelineStepDto
            {
                Sequence = 5,
                StageKey = "journal_created",
                Title = "Journal Created",
                Status = entryCount > 0 ? StatusComplete : StatusPending,
                OccurredAt = firstJournalAt,
                Summary = entryCount > 0
                    ? $"{entryCount:N0} journal entr{(entryCount == 1 ? "y" : "ies")} created"
                    : "No journal yet",
                Detail = entryCount > 0
                    ? $"Journal reference type(s): {string.Join(", ", references)}."
                    : "Accounting has not created a journal for this transaction yet."
            };
        }

        private static TransactionTimelineStepDto AccountAffectedStep(IReadOnlyCollection<JournalFact> journalFacts)
        {
            var accountSummaries = journalFacts
                .GroupBy(fact => new { fact.AccountId, fact.AccountCode, fact.AccountName })
                .Select(group => new
                {
                    group.Key.AccountCode,
                    group.Key.AccountName,
                    Debit = group.Sum(fact => fact.Debit),
                    Credit = group.Sum(fact => fact.Credit)
                })
                .OrderBy(account => account.AccountCode)
                .ThenBy(account => account.AccountName)
                .ToList();

            var detail = string.Join(
                "; ",
                accountSummaries.Take(5).Select(account =>
                    $"{account.AccountCode} {account.AccountName} Dr {account.Debit:N2} Cr {account.Credit:N2}"));

            if (accountSummaries.Count > 5)
            {
                detail += $"; +{accountSummaries.Count - 5:N0} more";
            }

            return new TransactionTimelineStepDto
            {
                Sequence = 6,
                StageKey = "account_affected",
                Title = "Account Affected",
                Status = accountSummaries.Count > 0 ? StatusComplete : StatusPending,
                OccurredAt = journalFacts
                    .Select(fact => (DateTime?)fact.CreatedAt)
                    .OrderByDescending(value => value)
                    .FirstOrDefault(),
                Summary = accountSummaries.Count > 0
                    ? $"{accountSummaries.Count:N0} account{(accountSummaries.Count == 1 ? string.Empty : "s")} affected"
                    : "No account impact yet",
                Detail = accountSummaries.Count > 0
                    ? detail
                    : "No journal lines are available to show affected accounts."
            };
        }

        private StockMovementFact LoadStockMovementFact(string referenceType, int referenceId)
        {
            const string sql = @"SELECT MIN(CreatedAt) AS FirstMovementAt,
                                        COUNT(1) AS MovementCount,
                                        ISNULL(SUM(Quantity), 0) AS NetQuantity
                                 FROM dbo.StockMovements
                                 WHERE ReferenceType = @ReferenceType
                                   AND ReferenceId = @ReferenceId;";

            return _session.Connection.QuerySingle<StockMovementFact>(
                sql,
                new { ReferenceType = referenceType, ReferenceId = referenceId },
                _session.Transaction);
        }

        private List<JournalFact> LoadJournalFacts(string transactionTypeKey, TransactionTimelineSnapshot transaction)
        {
            var references = ResolveJournalReferences(transactionTypeKey, transaction);
            var facts = new List<JournalFact>();

            const string sql = @"SELECT je.Id AS JournalEntryId,
                                        je.ReferenceType,
                                        je.ReferenceId,
                                        je.EntryDate,
                                        je.CreatedAt,
                                        ISNULL(je.Description, N'') AS Description,
                                        jl.AccountId,
                                        ISNULL(a.Code, N'') AS AccountCode,
                                        ISNULL(a.Name, N'') AS AccountName,
                                        ISNULL(jl.Debit, 0) AS Debit,
                                        ISNULL(jl.Credit, 0) AS Credit
                                 FROM dbo.JournalEntries je
                                 INNER JOIN dbo.JournalLines jl ON jl.JournalEntryId = je.Id
                                 INNER JOIN dbo.Accounts a ON a.Id = jl.AccountId
                                 WHERE je.ReferenceType = @ReferenceType
                                   AND je.ReferenceId = @ReferenceId
                                 ORDER BY je.CreatedAt, je.Id, jl.Id;";

            foreach (var reference in references)
            {
                facts.AddRange(_session.Connection.Query<JournalFact>(
                    sql,
                    new { reference.ReferenceType, reference.ReferenceId },
                    _session.Transaction));
            }

            return facts;
        }

        private static List<ReferenceKey> ResolveJournalReferences(
            string transactionTypeKey,
            TransactionTimelineSnapshot transaction)
        {
            if (string.Equals(transactionTypeKey, TransactionTypeKeys.Sale, StringComparison.OrdinalIgnoreCase))
            {
                return new List<ReferenceKey>
                {
                    new("Sale", transaction.ReferenceId),
                    new("SalePayment", transaction.ReferenceId)
                };
            }

            if (string.Equals(transactionTypeKey, TransactionTypeKeys.Purchase, StringComparison.OrdinalIgnoreCase))
            {
                return new List<ReferenceKey>
                {
                    new("Purchase", transaction.ReferenceId),
                    new("PurchasePayment", transaction.ReferenceId)
                };
            }

            var references = new List<ReferenceKey>
            {
                new("UsedCarPurchase", transaction.ReferenceId),
                new("UsedCarPurchasePayment", transaction.ReferenceId)
            };

            if (transaction.UsedCarId is > 0)
            {
                references.Add(new ReferenceKey("UsedCar", transaction.UsedCarId.Value));
            }

            return references;
        }

    }
}
