using System.Text.RegularExpressions;
using GlAccounts.Api.Features.GlAccounts.StubData;

namespace GlAccounts.Api.UnitTests;

public class SyntheticSeedDataTests
{
    [Fact]
    public async Task NFR_GLA_008_SeedData_ContainsNoRealPiiOrEmailAddresses()
    {
        var accountStore = new InMemoryAccountStore();
        var accounts = await accountStore.SearchAsync(null, null, null, null);

        var emailPattern = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
        var ssnPattern = new Regex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
        var creditCardPattern = new Regex(@"\b(?:\d{4}[- ]?){3}\d{4}\b", RegexOptions.Compiled);

        foreach (var account in accounts)
        {
            Assert.DoesNotMatch(emailPattern, account.Description);
            Assert.DoesNotMatch(ssnPattern, account.Description);
            Assert.DoesNotMatch(creditCardPattern, account.Description);
            Assert.DoesNotMatch(emailPattern, account.AccountId);
        }

        var transactionStore = new InMemoryTransactionStore();
        var (transactions, _) = await transactionStore.GetTransactionsAsync("1000-100-01", null, null, null, null, 1, 100);
        foreach (var tx in transactions)
        {
            Assert.DoesNotMatch(emailPattern, tx.Description);
            Assert.DoesNotMatch(ssnPattern, tx.Description);
            Assert.DoesNotMatch(creditCardPattern, tx.Description);
        }

        var catalogStore = new InMemoryAttributeCatalogStore();
        var catalog = await catalogStore.GetCatalogAsync();
        foreach (var type in catalog)
        {
            Assert.DoesNotMatch(emailPattern, type.Name);
            foreach (var val in type.Values)
            {
                Assert.DoesNotMatch(emailPattern, val.Name);
            }
        }
    }

    [Fact]
    public async Task NFR_GLA_008_SeedData_HasDeterministicIdentifiers()
    {
        var accountStore = new InMemoryAccountStore();
        var account = await accountStore.GetByIdAsync("1000-100-01");

        Assert.NotNull(account);
        Assert.Equal("1000-100-01", account.AccountId);
        Assert.Equal(2, account.Attributes.Count);
        Assert.Equal("att-1", account.Attributes[0].AttributeId);
    }
}
