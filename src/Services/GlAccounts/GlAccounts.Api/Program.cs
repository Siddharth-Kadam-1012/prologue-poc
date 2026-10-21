using System.Text.Json.Serialization;
using FluentValidation;
using GlAccounts.Application;
using GlAccounts.Application.Features.GlAccounts.CreateAccount;
using GlAccounts.Application.Features.GlAccounts.DeleteAccount;
using GlAccounts.Application.Features.GlAccounts.GetAccount;
using GlAccounts.Application.Features.GlAccounts.ListAccountGroups;
using GlAccounts.Application.Features.GlAccounts.ManageAttributes;
using GlAccounts.Application.Features.GlAccounts.PeriodSummary;
using GlAccounts.Application.Features.GlAccounts.SearchAccounts;
using GlAccounts.Application.Features.GlAccounts.Templates;
using GlAccounts.Application.Features.GlAccounts.Transactions;
using GlAccounts.Application.Features.GlAccounts.UpdateAccount;
using GlAccounts.Api.Features.GlAccounts.CreateAccount;
using GlAccounts.Api.Features.GlAccounts.DeleteAccount;
using GlAccounts.Api.Features.GlAccounts.GetAccount;
using GlAccounts.Api.Features.GlAccounts.ListAccountGroups;
using GlAccounts.Api.Features.GlAccounts.ManageAttributes;
using GlAccounts.Api.Features.GlAccounts.PeriodSummary;
using GlAccounts.Api.Features.GlAccounts.SearchAccounts;
using GlAccounts.Api.Features.GlAccounts.Templates;
using GlAccounts.Api.Features.GlAccounts.Transactions;
using GlAccounts.Api.Features.GlAccounts.UpdateAccount;
using GlAccounts.Infrastructure;
using GlAccounts.Infrastructure.Stubs;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// JSON — enums as strings, null suppression
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddProblemDetails();

// CORS — allow Angular dev origin
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()));

// Infrastructure stubs
builder.Services.AddGlAccountsInfrastructure();

// Application handlers
builder.Services.AddScoped<CreateAccountHandler>();
builder.Services.AddScoped<GetAccountHandler>();
builder.Services.AddScoped<UpdateAccountHandler>();
builder.Services.AddScoped<DeleteAccountHandler>();
builder.Services.AddScoped<SearchAccountsHandler>();
builder.Services.AddScoped<ListAccountGroupsHandler>();
builder.Services.AddScoped<PeriodSummaryHandler>();
builder.Services.AddScoped<TransactionsHandler>();
builder.Services.AddScoped<AddAttributeHandler>();
builder.Services.AddScoped<UpdateAttributeHandler>();
builder.Services.AddScoped<RemoveAttributeHandler>();
builder.Services.AddScoped<CopyFromTemplateHandler>();

// FluentValidation validators from Application assembly
builder.Services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();

var app = builder.Build();

app.UseCors();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Seed stub data — StubDataSeeder is a static class, resolve concrete stores and call directly
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    StubDataSeeder.Seed(
        sp.GetRequiredService<InMemoryAccountStore>(),
        sp.GetRequiredService<InMemoryAccountGroupStore>(),
        sp.GetRequiredService<InMemoryAttributeCatalogStore>(),
        sp.GetRequiredService<InMemoryAccountAttributeStore>(),
        sp.GetRequiredService<InMemoryPeriodBalanceStore>(),
        sp.GetRequiredService<InMemoryTransactionStore>());
}

// Endpoint mappings
app.MapCreateAccountEndpoint();
app.MapSearchAccountsEndpoint();
app.MapGetAccountEndpoint();
app.MapUpdateAccountEndpoint();
app.MapDeleteAccountEndpoint();
app.MapManageAttributesEndpoints();
app.MapListAccountGroupsEndpoints();
app.MapPeriodSummaryEndpoint();
app.MapTransactionsEndpoints();
app.MapCopyFromTemplateEndpoint();

app.Run();

// Exposes Program to WebApplicationFactory<Program> in integration tests.
public partial class Program { }
