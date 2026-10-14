using System.Text.Json.Serialization;
using GlAccounts.Api.Features.GlAccounts;
using GlAccounts.Api.Features.GlAccounts.CreateAccount;
using GlAccounts.Api.Features.GlAccounts.DeleteAccount;
using GlAccounts.Api.Features.GlAccounts.GetAccount;
using GlAccounts.Api.Features.GlAccounts.ListAccountGroups;
using GlAccounts.Api.Features.GlAccounts.ManageAttributes;
using GlAccounts.Api.Features.GlAccounts.PeriodSummary;
using GlAccounts.Api.Features.GlAccounts.Templates;
using GlAccounts.Api.Features.GlAccounts.Transactions;
using GlAccounts.Api.Features.GlAccounts.UpdateAccount;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddGlAccountsFeature();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapCreateAccountEndpoint();
app.MapSearchAccountsEndpoint();
app.MapGetAccountEndpoint();
app.MapUpdateAccountEndpoint();
app.MapDeleteAccountEndpoint();
app.MapPeriodSummaryEndpoint();
app.MapTransactionsEndpoints();
app.MapManageAttributesEndpoints();
app.MapListAccountGroupsEndpoints();

app.Run();

// Makes the entry point visible to WebApplicationFactory<Program> in the integration tests.
public partial class Program;
