using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GlAccounts.Application.Ports;
using GlAccounts.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GlAccounts.Tests.Integration;

/// <summary>
/// AC-GLA-015: POST account → audit entry logged with caller UserId, ChangeType=Created, AfterValues.
/// AC-GLA-016: PUT account → audit entry logged with ChangeType=Updated, BeforeValues and AfterValues.
/// </summary>
public sealed class AuditTrailTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuditTrailTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── AC-GLA-015: CREATE audit entry ────────────────────────────────────────

    [Fact]
    public async Task Post_Account_ProducesAuditEntry_WithCreatedAndUserId()
    {
        var accountId = "01-300-4401";
        var request   = new HttpRequestMessage(HttpMethod.Post, "/api/gl/accounts");
        request.Headers.Add("X-User-Id", "test-user");
        request.Content = JsonContent.Create(new
        {
            accountId, description = "Audit test account",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Inspect the audit store directly via the service scope
        using var scope = _factory.Services.CreateScope();
        var audit  = scope.ServiceProvider.GetRequiredService<IAuditStore>();
        var entries = await audit.GetByEntityIdAsync(accountId);

        entries.Should().NotBeEmpty();
        var entry = entries.First();
        entry.ChangeType.Should().Be(ChangeType.Created);
        entry.UserId.Should().Be("test-user");
        entry.EntityType.Should().Be("GLAccount");
        entry.EntityId.Should().Be(accountId);
        entry.AfterValues.Should().NotBeNull();
        entry.AfterValues!.Should().ContainKey("accountId");
        entry.AfterValues!.Should().ContainKey("description");
        entry.BeforeValues.Should().BeNull();
    }

    [Fact]
    public async Task Post_Account_NoUserIdHeader_AuditEntryUsesAnonymous()
    {
        var accountId = "01-300-4402";
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId, description = "Anonymous audit test",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        using var scope = _factory.Services.CreateScope();
        var audit   = scope.ServiceProvider.GetRequiredService<IAuditStore>();
        var entries = await audit.GetByEntityIdAsync(accountId);

        entries.Should().NotBeEmpty();
        entries.First().UserId.Should().Be("anonymous");
    }

    // ── AC-GLA-016: UPDATE audit entry ────────────────────────────────────────

    [Fact]
    public async Task Put_Account_ProducesAuditEntry_WithUpdatedAndBeforeAfterValues()
    {
        // Create a fresh account
        var accountId = "01-300-4403";
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId, description = "Before update",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        // Update description
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/gl/accounts/{accountId}");
        updateRequest.Headers.Add("X-User-Id", "updater");
        updateRequest.Content = JsonContent.Create(new { description = "After update" });
        await _client.SendAsync(updateRequest);

        using var scope = _factory.Services.CreateScope();
        var audit   = scope.ServiceProvider.GetRequiredService<IAuditStore>();
        var entries = await audit.GetByEntityIdAsync(accountId);

        var updateEntry = entries.FirstOrDefault(e => e.ChangeType == ChangeType.Updated);
        updateEntry.Should().NotBeNull();
        updateEntry!.UserId.Should().Be("updater");
        updateEntry.BeforeValues.Should().NotBeNull();
        updateEntry.BeforeValues!["description"].Should().Be("Before update");
        updateEntry.AfterValues.Should().NotBeNull();
        updateEntry.AfterValues!["description"].Should().Be("After update");
    }

    [Fact]
    public async Task Delete_Account_ProducesAuditEntry_WithDeletedChangeType()
    {
        var accountId = "01-300-4404";
        await _client.PostAsJsonAsync("/api/gl/accounts", new
        {
            accountId, description = "Delete audit test",
            accountType = "Financial", category = "Asset",
            status = "Active", balanceType = "BalanceForward", normalBalance = "Debit"
        });

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/gl/accounts/{accountId}");
        deleteRequest.Headers.Add("X-User-Id", "deleter");
        await _client.SendAsync(deleteRequest);

        using var scope = _factory.Services.CreateScope();
        var audit   = scope.ServiceProvider.GetRequiredService<IAuditStore>();
        var entries = await audit.GetByEntityIdAsync(accountId);

        var deleteEntry = entries.FirstOrDefault(e => e.ChangeType == ChangeType.Deleted);
        deleteEntry.Should().NotBeNull();
        deleteEntry!.UserId.Should().Be("deleter");
        deleteEntry.BeforeValues.Should().NotBeNull();
        deleteEntry.BeforeValues!["accountId"].Should().Be(accountId);
    }
}
