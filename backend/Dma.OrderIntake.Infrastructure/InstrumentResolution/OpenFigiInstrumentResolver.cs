using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dma.OrderIntake.Application.Abstractions;
using Dma.OrderIntake.Contracts;
using Dma.OrderIntake.Domain;
using Microsoft.Extensions.Logging;

namespace Dma.OrderIntake.Infrastructure.InstrumentResolution;

// A genuinely working call to the real, public OpenFIGI mapping API — unlike
// Bloomberg EMSX, OpenFIGI isn't proprietary/unavailable, so this isn't a
// skeleton. Reuses Domain.Isin/Mic for validation, same as MockInstrumentResolver
// — the resolve flow (validate, check digit, normalize MIC, search, never
// auto-pick when ambiguous) doesn't change just because the backing data does.
//
// KNOWN LIMITATIONS (OpenFIGI's free mapping endpoint doesn't return these):
// - Currency isn't returned at all. Bloomberg's ~1100 exchCode values have no
//   public, authoritative exchCode -> currency mapping, so ExchangeCurrencies
//   below is a small, deliberately incomplete table covering major primary
//   listing venues only — each entry verified live against a real security on
//   that exchange (see git history / PR notes), not guessed from memory.
//   Anything not in the table stays "UNKNOWN" rather than risk a wrong
//   currency in a financial application; a real integration needs a licensed
//   reference-data source (e.g. Bloomberg's own SYMBOLOGY/exchange metadata)
//   for full coverage — small/secondary/OTC venues are common and this table
//   will not have them (e.g. exchCode "GI": foreign stocks get unrelated
//   local tickers there — a pattern typical of secondary/retail OTC venues —
//   and there's no reliable free way to confirm which currency it settles in).
// - "exchCode" is Bloomberg's own exchange code, not an ISO 10383 MIC. A real
//   integration needs an exchCode -> MIC lookup table; until then it's passed
//   through as-is (visibly not a MIC) instead of silently mislabeled.
public class OpenFigiInstrumentResolver(HttpClient httpClient, ILogger<OpenFigiInstrumentResolver> logger) : IInstrumentResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // exchCode -> ISO 4217 currency, for major markets only. Each mapping was
    // verified against a real, well-known security's primary listing (Apple,
    // BAE Systems, SAP, BNP Paribas, ASML, Roche, Toyota, RBC, HSBC, ...),
    // not inferred from the code's name.
    private static readonly IReadOnlyDictionary<string, string> ExchangeCurrencies = new Dictionary<string, string>
    {
        ["UN"] = "USD", // New York Stock Exchange
        ["UW"] = "USD", // Nasdaq
        ["LN"] = "GBP", // London Stock Exchange
        ["GY"] = "EUR", // Xetra (Germany)
        ["FP"] = "EUR", // Euronext Paris
        ["NA"] = "EUR", // Euronext Amsterdam
        ["SE"] = "CHF", // SIX Swiss Exchange
        ["JT"] = "JPY", // Tokyo Stock Exchange
        ["AT"] = "AUD", // Australian Securities Exchange
        ["CT"] = "CAD", // Toronto Stock Exchange
        ["HK"] = "HKD", // Hong Kong Stock Exchange
    };

    public async Task<InstrumentResolutionResult> ResolveAsync(InstrumentResolutionRequest request, CancellationToken cancellationToken)
    {
        if (!Isin.TryParse(request.Isin, out var isin, out var isinError))
        {
            return new InstrumentResolutionResult(InstrumentResolutionStatus.Invalid, isinError, []);
        }

        Mic? mic = null;
        if (!string.IsNullOrWhiteSpace(request.Mic))
        {
            if (!Mic.TryParse(request.Mic, out var parsedMic, out var micError))
            {
                return new InstrumentResolutionResult(InstrumentResolutionStatus.Invalid, micError, []);
            }

            mic = parsedMic;
        }

        var job = new OpenFigiMappingJob("ID_ISIN", isin.Value, mic?.Value);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("mapping", new[] { job }, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "OpenFIGI request failed for ISIN {Isin}.", isin.Value);
            return new InstrumentResolutionResult(InstrumentResolutionStatus.NotFound, "Could not reach OpenFIGI.", []);
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("OpenFIGI returned {StatusCode} for ISIN {Isin}.", response.StatusCode, isin.Value);
            return new InstrumentResolutionResult(InstrumentResolutionStatus.NotFound, $"OpenFIGI returned HTTP {(int)response.StatusCode}.", []);
        }

        var results = await response.Content.ReadFromJsonAsync<OpenFigiMappingResult[]>(JsonOptions, cancellationToken);
        var jobResult = results?.FirstOrDefault();

        if (jobResult?.Data is null || jobResult.Data.Length == 0)
        {
            var reason = jobResult?.Error ?? jobResult?.Warning ?? "No instrument found for the given ISIN/MIC.";
            return new InstrumentResolutionResult(InstrumentResolutionStatus.NotFound, reason, []);
        }

        var matches = jobResult.Data
            .Select(d => new InstrumentMatch(
                DeterministicInstrumentId(d.Figi),
                isin.Value,
                d.ExchCode ?? mic?.Value ?? "UNKNOWN",
                d.Name ?? d.SecurityDescription ?? "Unknown",
                CurrencyFor(d.ExchCode), // see KNOWN LIMITATIONS above
                d.SecurityType ?? "Unknown",
                d.Ticker ?? "",
                d.Figi))
            .ToList();

        return matches.Count == 1
            ? new InstrumentResolutionResult(InstrumentResolutionStatus.Resolved, null, matches)
            : new InstrumentResolutionResult(InstrumentResolutionStatus.MultipleMatches, "Multiple listings matched — an exchange must be chosen explicitly.", matches);
    }

    private static string CurrencyFor(string? exchCode) =>
        exchCode is not null && ExchangeCurrencies.TryGetValue(exchCode, out var currency) ? currency : "UNKNOWN";

    // OpenFIGI doesn't hand out a stable internal id, and Order.InstrumentId is
    // a Guid — derive one deterministically from the FIGI (not for security,
    // just so the same instrument always maps to the same id across calls).
    private static Guid DeterministicInstrumentId(string figi)
    {
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(figi));
        return new Guid(hash);
    }
}

internal record OpenFigiMappingJob(
    [property: JsonPropertyName("idType")] string IdType,
    [property: JsonPropertyName("idValue")] string IdValue,
    // OpenFIGI rejects an explicit "micCode": null ("micCode must be a valid
    // string") — it has to be omitted entirely to mean "search every market".
    [property: JsonPropertyName("micCode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MicCode);

internal record OpenFigiMappingResult(
    [property: JsonPropertyName("data")] OpenFigiSecurity[]? Data,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("warning")] string? Warning);

internal record OpenFigiSecurity(
    [property: JsonPropertyName("figi")] string Figi,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("ticker")] string? Ticker,
    [property: JsonPropertyName("exchCode")] string? ExchCode,
    [property: JsonPropertyName("securityType")] string? SecurityType,
    [property: JsonPropertyName("securityDescription")] string? SecurityDescription);
