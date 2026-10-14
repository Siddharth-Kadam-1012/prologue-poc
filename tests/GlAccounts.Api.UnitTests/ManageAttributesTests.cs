using GlAccounts.Api.Common;
using GlAccounts.Api.Domain;
using GlAccounts.Api.Features.GlAccounts.CreateAccount;
using GlAccounts.Api.Features.GlAccounts.ManageAttributes;
using GlAccounts.Api.Features.GlAccounts.StubData;
using GlAccounts.Api.Features.GlAccounts.UpdateAccount;

namespace GlAccounts.Api.UnitTests;

public class ManageAttributesTests
{
    private readonly InMemoryAccountStore _accountStore;
    private readonly InMemoryAttributeCatalogStore _catalogStore;
    private readonly InMemoryAuditStore _auditStore;
    private readonly ManageAttributesHandler _handler;
    private readonly UpdateAccountHandler _updateHandler;
    private readonly CreateAccountHandler _createHandler;

    public ManageAttributesTests()
    {
        _accountStore = new InMemoryAccountStore();
        _catalogStore = new InMemoryAttributeCatalogStore();
        _auditStore = new InMemoryAuditStore();
        _handler = new ManageAttributesHandler(_accountStore, _catalogStore, _auditStore);
        _updateHandler = new UpdateAccountHandler(_accountStore, _catalogStore, _auditStore);
        _createHandler = new CreateAccountHandler(_accountStore, _catalogStore, _auditStore);
    }

    [Fact]
    public async Task AC_GLA_014_AddAttribute_ValidCatalogEntry_SucceedsAndAudits()
    {
        // REQ-GLA-009, REQ-GLA-010, AC-GLA-014
        var input = new AttributeInput("project", "proj-alpha");

        var result = await _handler.AddAttributeAsync("1000-100-01", input, "user-attr");

        var created = Assert.IsType<ManageAttributeResult.Created>(result);
        Assert.Equal("project", created.Attribute.AttributeTypeId);
        Assert.Equal("proj-alpha", created.Attribute.AttributeValueId);
        Assert.Contains(created.Attribute.AttributeId, created.Location);

        var stored = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(stored);
        Assert.Contains(stored.Attributes, a => a.AttributeId == created.Attribute.AttributeId);

        var audits = _auditStore.GetEntries("1000-100-01");
        Assert.Contains(audits, a => a.Kind == AuditEntryKind.AttributeAdded && a.Field == "project" && a.After == "proj-alpha");
    }

    [Fact]
    public async Task AC_GLA_014_AddAttribute_InvalidCatalogEntry_ReturnsValidationFailed()
    {
        // REQ-GLA-010, AC-GLA-014
        var input = new AttributeInput("nonexistent-type", "nonexistent-value");

        var result = await _handler.AddAttributeAsync("1000-100-01", input, "user-attr");

        var failed = Assert.IsType<ManageAttributeResult.ValidationFailed>(result);
        Assert.True(failed.Errors.ContainsKey("attributeValueId"));
    }

    [Fact]
    public async Task AC_GLA_015_UpdateAttribute_ValidCatalogEntry_SucceedsAndAudits()
    {
        // REQ-GLA-009, REQ-GLA-010, AC-GLA-015
        var input = new AttributeInput("dept", "engineering");

        var result = await _handler.UpdateAttributeAsync("1000-100-01", "att-1", input, "user-attr");

        var updated = Assert.IsType<ManageAttributeResult.Updated>(result);
        Assert.Equal("att-1", updated.Attribute.AttributeId);
        Assert.Equal("engineering", updated.Attribute.AttributeValueId);

        var stored = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(stored);
        var attr = stored.Attributes.First(a => a.AttributeId == "att-1");
        Assert.Equal("engineering", attr.AttributeValueId);

        var audits = _auditStore.GetEntries("1000-100-01");
        Assert.Contains(audits, a => a.Kind == AuditEntryKind.AttributeChanged && a.Field == "dept");
    }

    [Fact]
    public async Task AC_GLA_016_DeleteAttribute_ExistingAttribute_SucceedsAndAudits()
    {
        // REQ-GLA-009, AC-GLA-016
        var result = await _handler.DeleteAttributeAsync("1000-100-01", "att-1", "user-attr");

        Assert.IsType<ManageAttributeResult.Deleted>(result);

        var stored = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(stored);
        Assert.DoesNotContain(stored.Attributes, a => a.AttributeId == "att-1");

        var audits = _auditStore.GetEntries("1000-100-01");
        Assert.Contains(audits, a => a.Kind == AuditEntryKind.AttributeRemoved);
    }

    [Fact]
    public async Task AC_GLA_017_AC_GLA_018_UpdateAccount_StagedAttributeChanges_AppliedAtomically()
    {
        // REQ-GLA-011, AC-GLA-017, AC-GLA-018
        var account = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(account);
        var etag = ETagHelper.FormatETag(account.Version);

        var request = new UpdateAccountRequest
        {
            Description = "Updated with staged attributes",
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            AttributeChanges = new List<AttributeChangeInput>
            {
                new("Add", AttributeTypeId: "region", AttributeValueId: "apac"),
                new("Remove", AttributeId: "att-1")
            }
        };

        var result = await _updateHandler.HandleAsync("1000-100-01", etag, request, "user-atomic");

        var success = Assert.IsType<UpdateAccountResult.Success>(result);
        Assert.Equal("Updated with staged attributes", success.Account.Description);
        Assert.DoesNotContain(success.Account.Attributes, a => a.AttributeId == "att-1");
        Assert.Contains(success.Account.Attributes, a => a.AttributeTypeId == "region" && a.AttributeValueId == "apac");

        var audits = _auditStore.GetEntries("1000-100-01");
        Assert.Contains(audits, a => a.Kind == AuditEntryKind.AttributeAdded && a.UserId == "user-atomic");
        Assert.Contains(audits, a => a.Kind == AuditEntryKind.AttributeRemoved && a.UserId == "user-atomic");
    }

    [Fact]
    public async Task AC_GLA_019_Attributes_DoNotAffectPostingEligibility()
    {
        // REQ-GLA-012, AC-GLA-019
        var account = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(account);
        Assert.True(account.EligibleForPosting);

        // Add attribute
        await _handler.AddAttributeAsync("1000-100-01", new AttributeInput("project", "proj-alpha"), "user");
        var updated = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(updated);
        Assert.True(updated.EligibleForPosting);

        // Remove attributes
        await _handler.DeleteAttributeAsync("1000-100-01", "att-1", "user");
        await _handler.DeleteAttributeAsync("1000-100-01", "att-2", "user");
        var noAttr = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(noAttr);
        Assert.True(noAttr.EligibleForPosting);
    }

    [Fact]
    public async Task AC_GLA_014_AC_GLA_015_AddAttribute_SecondAttributeOfOtherType_BothReturned()
    {
        // REQ-GLA-009, AC-GLA-014, AC-GLA-015 (stub data only): 8000-100-01 starts without attributes
        await _handler.AddAttributeAsync("8000-100-01", new AttributeInput("dept", "sales"), "user-attr");
        await _handler.AddAttributeAsync("8000-100-01", new AttributeInput("region", "na"), "user-attr");

        var stored = await _accountStore.GetByIdAsync("8000-100-01");
        Assert.NotNull(stored);
        Assert.Equal(2, stored.Attributes.Count);
        Assert.Contains(stored.Attributes, a => a.AttributeTypeId == "dept" && a.AttributeValueId == "sales");
        Assert.Contains(stored.Attributes, a => a.AttributeTypeId == "region" && a.AttributeValueId == "na");
    }

    [Fact]
    public async Task AC_GLA_016_AddAttribute_ValueFromOtherType_ReturnsFieldErrorAndChangesNothing()
    {
        // REQ-GLA-010, AC-GLA-016 (negative): "emea" belongs to region, not dept
        var before = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(before);

        var result = await _handler.AddAttributeAsync("1000-100-01", new AttributeInput("dept", "emea"), "user-attr");

        var failed = Assert.IsType<ManageAttributeResult.ValidationFailed>(result);
        Assert.Contains("attributeValueId", failed.Errors.Keys);

        var after = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(after);
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(before.Attributes.Count, after.Attributes.Count);
        Assert.Empty(_auditStore.GetEntries("1000-100-01"));
    }

    [Fact]
    public async Task AC_GLA_016_AddAttribute_EmptyRequiredFields_ReturnsFieldErrors()
    {
        // REQ-GLA-010, AC-GLA-016 (negative)
        var result = await _handler.AddAttributeAsync("1000-100-01", new AttributeInput("", ""), "user-attr");

        var failed = Assert.IsType<ManageAttributeResult.ValidationFailed>(result);
        Assert.Contains("attributeTypeId", failed.Errors.Keys);
        Assert.Contains("attributeValueId", failed.Errors.Keys);
    }

    [Fact]
    public async Task AC_GLA_014_AC_GLA_015_AC_GLA_016_StandaloneOperations_ChangeAccountVersion()
    {
        // REQ-GLA-009, REQ-GLA-031: add, edit and remove each return a new ETag (stub data only)
        var original = ETagHelper.FormatETag((await _accountStore.GetByIdAsync("1000-100-01"))!.Version);

        var added = Assert.IsType<ManageAttributeResult.Created>(
            await _handler.AddAttributeAsync("1000-100-01", new AttributeInput("project", "proj-beta"), "user-attr"));
        Assert.NotEqual(original, added.ETag);

        var edited = Assert.IsType<ManageAttributeResult.Updated>(
            await _handler.UpdateAttributeAsync("1000-100-01", added.Attribute.AttributeId, new AttributeInput("project", "proj-alpha"), "user-attr"));
        Assert.NotEqual(added.ETag, edited.ETag);

        var removed = Assert.IsType<ManageAttributeResult.Deleted>(
            await _handler.DeleteAttributeAsync("1000-100-01", added.Attribute.AttributeId, "user-attr"));
        Assert.NotEqual(edited.ETag, removed.ETag);

        var stored = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.Equal(removed.ETag, ETagHelper.FormatETag(stored!.Version));
    }

    [Fact]
    public async Task AC_GLA_051_AttributeChanges_AreAuditedWithUserAndTimestamp()
    {
        // REQ-GLA-031, AC-GLA-051 (attribute part)
        var added = Assert.IsType<ManageAttributeResult.Created>(
            await _handler.AddAttributeAsync("8000-100-01", new AttributeInput("dept", "sales"), "auditor-01"));
        await _handler.UpdateAttributeAsync("8000-100-01", added.Attribute.AttributeId, new AttributeInput("dept", "finance"), "auditor-01");
        await _handler.DeleteAttributeAsync("8000-100-01", added.Attribute.AttributeId, "auditor-01");

        var entries = _auditStore.GetEntries("8000-100-01");
        Assert.Equal(3, entries.Count);
        Assert.Equal(
            new[] { AuditEntryKind.AttributeAdded, AuditEntryKind.AttributeChanged, AuditEntryKind.AttributeRemoved },
            entries.Select(e => e.Kind).ToArray());
        Assert.All(entries, e =>
        {
            Assert.Equal("auditor-01", e.UserId);
            Assert.NotEqual(default, e.Timestamp);
        });
    }

    [Fact]
    public async Task AC_GLA_017_UpdateAccount_StagedAdd_EditAndRemove_AppliedTogether()
    {
        // REQ-GLA-011, AC-GLA-017: edit att-1 (dept finance -> sales), add region apac, remove att-2
        var account = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(account);

        var result = await _updateHandler.HandleAsync(
            "1000-100-01",
            ETagHelper.FormatETag(account.Version),
            new UpdateAccountRequest
            {
                Description = account.Description,
                Category = account.Category,
                CurrencyId = account.CurrencyId,
                Status = account.Status,
                AttributeChanges = new List<AttributeChangeInput>
                {
                    new("Edit", AttributeId: "att-1", AttributeTypeId: "dept", AttributeValueId: "sales"),
                    new("Add", AttributeTypeId: "region", AttributeValueId: "apac"),
                    new("Remove", AttributeId: "att-2")
                }
            },
            "user-staged");

        var success = Assert.IsType<UpdateAccountResult.Success>(result);
        Assert.Contains(success.Account.Attributes, a => a.AttributeId == "att-1" && a.AttributeValueId == "sales");
        Assert.Contains(success.Account.Attributes, a => a.AttributeTypeId == "region" && a.AttributeValueId == "apac");
        Assert.DoesNotContain(success.Account.Attributes, a => a.AttributeId == "att-2");

        var kinds = _auditStore.GetEntries("1000-100-01").Select(e => e.Kind).ToList();
        Assert.Contains(AuditEntryKind.AttributeChanged, kinds);
        Assert.Contains(AuditEntryKind.AttributeAdded, kinds);
        Assert.Contains(AuditEntryKind.AttributeRemoved, kinds);
    }

    [Fact]
    public async Task AC_GLA_018_UpdateAccount_OneInvalidStagedChange_AppliesNothingAndWritesNoAudit()
    {
        // REQ-GLA-011, AC-GLA-018 (negative): valid description change + valid add + invalid add
        var before = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(before);

        var result = await _updateHandler.HandleAsync(
            "1000-100-01",
            ETagHelper.FormatETag(before.Version),
            new UpdateAccountRequest
            {
                Description = "Must not be saved",
                Category = before.Category,
                CurrencyId = before.CurrencyId,
                Status = before.Status,
                AttributeChanges = new List<AttributeChangeInput>
                {
                    new("Add", AttributeTypeId: "region", AttributeValueId: "apac"),
                    new("Add", AttributeTypeId: "dept", AttributeValueId: "not-in-catalog")
                }
            },
            "user-staged");

        var failed = Assert.IsType<UpdateAccountResult.ValidationFailed>(result);
        Assert.Contains("attributeChanges[1].attributeValueId", failed.Errors.Keys);

        var after = await _accountStore.GetByIdAsync("1000-100-01");
        Assert.NotNull(after);
        Assert.Equal(before.Description, after.Description);
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(
            before.Attributes.Select(a => a.AttributeId).OrderBy(x => x),
            after.Attributes.Select(a => a.AttributeId).OrderBy(x => x));
        Assert.Empty(_auditStore.GetEntries("1000-100-01"));
    }

    [Fact]
    public async Task AC_GLA_017_AC_GLA_018_CreateAccount_StagedAttributes_ValidSavedInvalidRejected()
    {
        // REQ-GLA-011, AC-GLA-017, AC-GLA-018: staged attributes on create are all or nothing
        var bad = new CreateAccountRequest
        {
            AccountId = "1000-100-05",
            Description = "Rejected account",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit,
            Attributes = new List<AttributeInput> { new("dept", "finance"), new("dept", "emea") }
        };

        var result = await _createHandler.HandleAsync(bad, "user-creator");

        Assert.IsType<CreateAccountResult.ValidationFailed>(result);
        Assert.False(await _accountStore.ExistsAsync("1000-100-05"));
        Assert.Empty(_auditStore.GetEntries("1000-100-05"));
    }

    [Fact]
    public async Task CreateAccount_WithInitialAttributes_ValidatesAndSavesAttributes()
    {
        // REQ-GLA-009
        var request = new CreateAccountRequest
        {
            AccountId = "1000-100-04",
            Description = "Account with initial attributes",
            AccountType = AccountType.Financial,
            Category = Category.Asset,
            CurrencyId = "USD",
            Status = AccountStatus.Active,
            BalanceType = BalanceType.BalanceForward,
            NormalBalance = NormalBalance.Debit,
            Attributes = new List<AttributeInput>
            {
                new("dept", "finance"),
                new("region", "na")
            }
        };

        var result = await _createHandler.HandleAsync(request, "user-creator");

        var success = Assert.IsType<CreateAccountResult.Success>(result);
        Assert.Equal(2, success.Account.Attributes.Count);
        Assert.Contains(success.Account.Attributes, a => a.AttributeTypeId == "dept" && a.AttributeValueId == "finance");
        Assert.Contains(success.Account.Attributes, a => a.AttributeTypeId == "region" && a.AttributeValueId == "na");
    }
}
