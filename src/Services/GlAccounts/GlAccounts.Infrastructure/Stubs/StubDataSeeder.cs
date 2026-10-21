using GlAccounts.Domain.Entities;
using GlAccounts.Domain.Enums;

namespace GlAccounts.Infrastructure.Stubs;

/// <summary>
/// Seeds all in-memory stub stores with deterministic test data as specified in data-model.md.
/// All IDs are fixed so integration and unit tests are deterministic.
/// </summary>
public static class StubDataSeeder
{
    public static void Seed(
        InMemoryAccountStore accounts,
        InMemoryAccountGroupStore accountGroups,
        InMemoryAttributeCatalogStore attributeCatalog,
        InMemoryAccountAttributeStore accountAttributes,
        InMemoryPeriodBalanceStore periodBalances,
        InMemoryTransactionStore transactions)
    {
        SeedAccounts(accounts);
        SeedAccountGroups(accountGroups);
        SeedAttributeCatalog(attributeCatalog);
        SeedAccountAttributes(accountAttributes);
        SeedPeriodBalances(periodBalances);
        SeedTransactions(transactions);
    }

    private static void SeedAccounts(InMemoryAccountStore store)
    {
        var today = new DateOnly(2026, 1, 1);

        store.Seed(new GlAccount { AccountId = "01-100-1000", Description = "Cash Account",            AccountType = AccountType.Financial, Category = "Asset",     Status = AccountStatus.Active,    BalanceType = BalanceType.BalanceForward, NormalBalance = NormalBalance.Debit,  CreatedDate = today, CreatedBy = "seed" });
        store.Seed(new GlAccount { AccountId = "01-100-2000", Description = "Accounts Receivable",     AccountType = AccountType.Financial, Category = "Asset",     Status = AccountStatus.Active,    BalanceType = BalanceType.BalanceForward, NormalBalance = NormalBalance.Debit,  CreatedDate = today, CreatedBy = "seed" });
        store.Seed(new GlAccount { AccountId = "01-200-1000", Description = "Accounts Payable",        AccountType = AccountType.Financial, Category = "Liability", Status = AccountStatus.Active,    BalanceType = BalanceType.BalanceForward, NormalBalance = NormalBalance.Credit, CreatedDate = today, CreatedBy = "seed" });
        store.Seed(new GlAccount { AccountId = "01-100-3000", Description = "Inactive Account",        AccountType = AccountType.Financial, Category = "Asset",     Status = AccountStatus.Inactive,  BalanceType = BalanceType.BalanceForward, NormalBalance = NormalBalance.Debit,  CreatedDate = today, CreatedBy = "seed" });
        store.Seed(new GlAccount { AccountId = "01-100-4000", Description = "Hold Account",            AccountType = AccountType.Financial, Category = "Asset",     Status = AccountStatus.Hold,      BalanceType = BalanceType.BalanceForward, NormalBalance = NormalBalance.Debit,  CreatedDate = today, CreatedBy = "seed" });
        store.Seed(new GlAccount { AccountId = "01-100-5000", Description = "Template Account",        AccountType = AccountType.Financial, Category = "Asset",     Status = AccountStatus.Template,  BalanceType = BalanceType.BalanceForward, NormalBalance = NormalBalance.Debit,  CreatedDate = today, CreatedBy = "seed" });
        store.Seed(new GlAccount { AccountId = "01-200-2000", Description = "Account with Transactions", AccountType = AccountType.Financial, Category = "Asset",   Status = AccountStatus.Active,    BalanceType = BalanceType.BalanceForward, NormalBalance = NormalBalance.Debit,  CreatedDate = today, CreatedBy = "seed" });
    }

    private static void SeedAccountGroups(InMemoryAccountGroupStore store)
    {
        store.Seed(new AccountGroup
        {
            GroupId     = "GRP-001",
            GroupName   = "Current Assets",
            Description = "Cash and near-cash accounts",
            AccountIds  = ["01-100-1000", "01-100-2000"]
        });
    }

    private static void SeedAttributeCatalog(InMemoryAttributeCatalogStore store)
    {
        store.Seed(new AttributeDefinition
        {
            AttributeTypeId   = "COST-CENTRE",
            AttributeTypeName = "Cost Centre",
            Values =
            [
                new AttributeValue { ValueId = "CC-100", Description = "Head Office" },
                new AttributeValue { ValueId = "CC-200", Description = "Operations" },
                new AttributeValue { ValueId = "CC-300", Description = "Sales" }
            ]
        });

        store.Seed(new AttributeDefinition
        {
            AttributeTypeId   = "REPORTING-UNIT",
            AttributeTypeName = "Reporting Unit",
            Values =
            [
                new AttributeValue { ValueId = "RU-CORP", Description = "Corporate" },
                new AttributeValue { ValueId = "RU-DIV1", Description = "Division 1" }
            ]
        });
    }

    private static void SeedAccountAttributes(InMemoryAccountAttributeStore store)
    {
        // 01-100-1000 has one cost-centre attribute
        store.Seed(new AccountAttribute
        {
            AttributeId               = "attr-001",
            AccountId                 = "01-100-1000",
            AttributeTypeId           = "COST-CENTRE",
            AttributeTypeDescription  = "Cost Centre",
            AttributeValueId          = "CC-100",
            AttributeValueDescription = "Head Office"
        });
    }

    private static void SeedPeriodBalances(InMemoryPeriodBalanceStore store)
    {
        // FY2026 periods 1–4 for account 01-100-1000
        store.Seed(new PeriodBalance { BalanceId = "pb-001", AccountId = "01-100-1000", FiscalYear = 2026, FiscalPeriod = 1, Balance = 10000m });
        store.Seed(new PeriodBalance { BalanceId = "pb-002", AccountId = "01-100-1000", FiscalYear = 2026, FiscalPeriod = 2, Balance = 12500m });
        store.Seed(new PeriodBalance { BalanceId = "pb-003", AccountId = "01-100-1000", FiscalYear = 2026, FiscalPeriod = 3, Balance = 11000m });
        store.Seed(new PeriodBalance { BalanceId = "pb-004", AccountId = "01-100-1000", FiscalYear = 2026, FiscalPeriod = 4, Balance = 13750m });
    }

    private static void SeedTransactions(InMemoryTransactionStore store)
    {
        // 3 posted transactions for 01-200-2000 (used in deletion-protection tests)
        store.Seed(new GlTransaction { TransactionId = "txn-001", AccountId = "01-200-2000", BatchId = "BATCH-2026-001", Description = "Opening entry",      Amount = 5000m,  PostingDate = new DateOnly(2026, 1, 31), PostingStatus = PostingStatus.Posted });
        store.Seed(new GlTransaction { TransactionId = "txn-002", AccountId = "01-200-2000", BatchId = "BATCH-2026-002", Description = "Q1 journal entry",   Amount = 2500m,  PostingDate = new DateOnly(2026, 3, 31), PostingStatus = PostingStatus.Posted });
        store.Seed(new GlTransaction { TransactionId = "txn-003", AccountId = "01-200-2000", BatchId = "BATCH-2026-003", Description = "Correction entry",   Amount = -500m,  PostingDate = new DateOnly(2026, 4, 15), PostingStatus = PostingStatus.Posted });
    }
}
