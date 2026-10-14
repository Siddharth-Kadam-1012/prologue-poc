using System.Collections.Concurrent;
using GlAccounts.Api.Domain;

namespace GlAccounts.Api.Features.GlAccounts.StubData;

public class InMemoryAccountStore : IAccountStore
{
    private readonly ConcurrentDictionary<string, Account> _accounts = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryAccountStore()
    {
        Reset();
    }

    public void Reset()
    {
        _accounts.Clear();
        foreach (var account in CreateSeedAccounts())
        {
            _accounts[account.AccountId] = account;
        }
    }

    public Task<Account?> GetByIdAsync(string accountId, CancellationToken ct = default)
    {
        if (_accounts.TryGetValue(accountId, out var account))
        {
            return Task.FromResult<Account?>(account.Clone());
        }

        return Task.FromResult<Account?>(null);
    }

    public Task<bool> ExistsAsync(string accountId, CancellationToken ct = default)
    {
        return Task.FromResult(_accounts.ContainsKey(accountId));
    }

    public Task<IReadOnlyList<Account>> SearchAsync(
        string? query,
        AccountStatus? status,
        AccountType? accountType,
        Category? category,
        CancellationToken ct = default)
    {
        var result = _accounts.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            result = result.Where(a =>
                a.AccountId.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.Description.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (status.HasValue)
        {
            result = result.Where(a => a.Status == status.Value);
        }

        if (accountType.HasValue)
        {
            result = result.Where(a => a.AccountType == accountType.Value);
        }

        if (category.HasValue)
        {
            result = result.Where(a => a.Category == category.Value);
        }

        var list = result.OrderBy(a => a.AccountId).Select(a => a.Clone()).ToList();
        return Task.FromResult<IReadOnlyList<Account>>(list);
    }

    public Task CreateAsync(Account account, CancellationToken ct = default)
    {
        _accounts[account.AccountId] = account.Clone();
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        _accounts[account.AccountId] = account.Clone();
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string accountId, CancellationToken ct = default)
    {
        return Task.FromResult(_accounts.TryRemove(accountId, out _));
    }

    private static List<Account> CreateSeedAccounts()
    {
        return new List<Account>
        {
            // 1. 1000-100-01: Active Financial (has dependencies, balances, attributes, groups)
            new()
            {
                AccountId = "1000-100-01",
                Segments = new List<Segment> { new("Main", "1000"), new("Department", "100"), new("Sub", "01") },
                Description = "Cash and equivalents",
                AccountType = AccountType.Financial,
                Category = Category.Asset,
                CurrencyId = "USD",
                Status = AccountStatus.Active,
                BalanceType = BalanceType.BalanceForward,
                NormalBalance = NormalBalance.Debit,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-1000-100-01",
                Attributes = new List<AccountAttribute>
                {
                    new("att-1", "dept", "finance"),
                    new("att-2", "region", "emea")
                }
            },
            // 2. 1000-100-03: Active Financial with NO dependencies (for successful delete AC-GLA-041)
            new()
            {
                AccountId = "1000-100-03",
                Segments = new List<Segment> { new("Main", "1000"), new("Department", "100"), new("Sub", "03") },
                Description = "Petty cash branch C",
                AccountType = AccountType.Financial,
                Category = Category.Asset,
                CurrencyId = "USD",
                Status = AccountStatus.Active,
                BalanceType = BalanceType.BalanceForward,
                NormalBalance = NormalBalance.Debit,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-1000-100-03",
                Attributes = new List<AccountAttribute>()
            },
            // 3. 1000-200-01: Active Financial with 10,001 posted transactions
            new()
            {
                AccountId = "1000-200-01",
                Segments = new List<Segment> { new("Main", "1000"), new("Department", "200"), new("Sub", "01") },
                Description = "Main operating account",
                AccountType = AccountType.Financial,
                Category = Category.Asset,
                CurrencyId = "USD",
                Status = AccountStatus.Active,
                BalanceType = BalanceType.BalanceForward,
                NormalBalance = NormalBalance.Debit,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-1000-200-01",
                Attributes = new List<AccountAttribute>()
            },
            // 4. 1000-300-01: Active Financial with only unposted transactions (AC-GLA-039)
            new()
            {
                AccountId = "1000-300-01",
                Segments = new List<Segment> { new("Main", "1000"), new("Department", "300"), new("Sub", "01") },
                Description = "Payroll clearing",
                AccountType = AccountType.Financial,
                Category = Category.Asset,
                CurrencyId = "USD",
                Status = AccountStatus.Active,
                BalanceType = BalanceType.BalanceForward,
                NormalBalance = NormalBalance.Debit,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-1000-300-01",
                Attributes = new List<AccountAttribute>()
            },
            // 5. 2000-100-01: Active Financial blocked by budget (AC-GLA-040)
            new()
            {
                AccountId = "2000-100-01",
                Segments = new List<Segment> { new("Main", "2000"), new("Department", "100"), new("Sub", "01") },
                Description = "Accounts payable",
                AccountType = AccountType.Financial,
                Category = Category.Liability,
                CurrencyId = "USD",
                Status = AccountStatus.Active,
                BalanceType = BalanceType.BalanceForward,
                NormalBalance = NormalBalance.Credit,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-2000-100-01",
                Attributes = new List<AccountAttribute>()
            },
            // 6. 2000-200-01: Active Financial blocked by allocation (AC-GLA-040)
            new()
            {
                AccountId = "2000-200-01",
                Segments = new List<Segment> { new("Main", "2000"), new("Department", "200"), new("Sub", "01") },
                Description = "Accrued liabilities",
                AccountType = AccountType.Financial,
                Category = Category.Liability,
                CurrencyId = "USD",
                Status = AccountStatus.Active,
                BalanceType = BalanceType.BalanceForward,
                NormalBalance = NormalBalance.Credit,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-2000-200-01",
                Attributes = new List<AccountAttribute>()
            },
            // 7. 2000-300-01: Active Financial blocked by group membership (AC-GLA-040)
            new()
            {
                AccountId = "2000-300-01",
                Segments = new List<Segment> { new("Main", "2000"), new("Department", "300"), new("Sub", "01") },
                Description = "Tax payable",
                AccountType = AccountType.Financial,
                Category = Category.Liability,
                CurrencyId = "USD",
                Status = AccountStatus.Active,
                BalanceType = BalanceType.BalanceForward,
                NormalBalance = NormalBalance.Credit,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-2000-300-01",
                Attributes = new List<AccountAttribute>()
            },
            // 8. 3000-100-01: Active Financial blocked by multiple blocker types (AC-GLA-042)
            new()
            {
                AccountId = "3000-100-01",
                Segments = new List<Segment> { new("Main", "3000"), new("Department", "100"), new("Sub", "01") },
                Description = "Common stock",
                AccountType = AccountType.Financial,
                Category = Category.Equity,
                CurrencyId = "USD",
                Status = AccountStatus.Active,
                BalanceType = BalanceType.BalanceForward,
                NormalBalance = NormalBalance.Credit,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-3000-100-01",
                Attributes = new List<AccountAttribute>()
            },
            // 9. 4000-100-01: Hold Financial account
            new()
            {
                AccountId = "4000-100-01",
                Segments = new List<Segment> { new("Main", "4000"), new("Department", "100"), new("Sub", "01") },
                Description = "Revenue under audit",
                AccountType = AccountType.Financial,
                Category = Category.Revenue,
                CurrencyId = "USD",
                Status = AccountStatus.Hold,
                BalanceType = BalanceType.Periodic,
                NormalBalance = NormalBalance.Credit,
                CreatedDate = new DateOnly(2025, 2, 1),
                Version = "seed-version-4000-100-01",
                Attributes = new List<AccountAttribute>()
            },
            // 10. 5000-100-01: Inactive Financial account
            new()
            {
                AccountId = "5000-100-01",
                Segments = new List<Segment> { new("Main", "5000"), new("Department", "100"), new("Sub", "01") },
                Description = "Archived office supplies",
                AccountType = AccountType.Financial,
                Category = Category.Expense,
                CurrencyId = "USD",
                Status = AccountStatus.Inactive,
                BalanceType = BalanceType.Periodic,
                NormalBalance = NormalBalance.Debit,
                CreatedDate = new DateOnly(2025, 2, 1),
                Version = "seed-version-5000-100-01",
                Attributes = new List<AccountAttribute>()
            },
            // 11. 7000-100-01: Statistical account (AC-GLA-057)
            new()
            {
                AccountId = "7000-100-01",
                Segments = new List<Segment> { new("Main", "7000"), new("Department", "100"), new("Sub", "01") },
                Description = "Headcount metric",
                AccountType = AccountType.Statistical,
                Category = Category.Statistical,
                CurrencyId = "USD",
                Status = AccountStatus.Active,
                BalanceType = null,
                NormalBalance = null,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-7000-100-01",
                Attributes = new List<AccountAttribute>()
            },
            // 12. 8000-100-01: Account with no group and no balances
            new()
            {
                AccountId = "8000-100-01",
                Segments = new List<Segment> { new("Main", "8000"), new("Department", "100"), new("Sub", "01") },
                Description = "Special project reserve",
                AccountType = AccountType.Financial,
                Category = Category.Asset,
                CurrencyId = "USD",
                Status = AccountStatus.Active,
                BalanceType = BalanceType.BalanceForward,
                NormalBalance = NormalBalance.Debit,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-8000-100-01",
                Attributes = new List<AccountAttribute>()
            },
            // 13. 9000-100-01: Template account (AC-GLA-052..055)
            new()
            {
                AccountId = "9000-100-01",
                Segments = new List<Segment> { new("Main", "9000"), new("Department", "100"), new("Sub", "01") },
                Description = "Standard expense template",
                AccountType = AccountType.Financial,
                Category = Category.Expense,
                CurrencyId = "USD",
                Status = AccountStatus.Template,
                BalanceType = BalanceType.Periodic,
                NormalBalance = NormalBalance.Debit,
                CreatedDate = new DateOnly(2025, 1, 1),
                Version = "seed-version-9000-100-01",
                Attributes = new List<AccountAttribute>()
            }
        };
    }
}
