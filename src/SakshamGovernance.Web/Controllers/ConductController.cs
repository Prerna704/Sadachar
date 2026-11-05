using Microsoft.AspNetCore.Mvc;
using SakshamGovernance.Core.Entities;
using SakshamGovernance.Core.Interfaces;
using SakshamGovernance.Web.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace SakshamGovernance.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConductController : ControllerBase
{
    private readonly IConductRepository _conductRepository;

    public ConductController(IConductRepository conductRepository)
    {
        _conductRepository = conductRepository;
    }

    /// <summary>
    /// Logs an AI-detected ableist language or harassment incident adhering to DPDP Act 2023 pseudonymization.
    /// </summary>
    [HttpPost("log-incident")]
    [ProducesResponseType(typeof(ConductIncidentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LogIncident(
        [FromBody] ConductIncidentReportDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // DPDP Act 2023: Pseudonymize student/actor identity with SHA-256 and salt
        string pseudonymizedHash = ComputePseudonymizedHash(request.RawUserId);

        // Sanitize snippet to prevent storing plain-text PII (Phone, Email, Full Names)
        string sanitizedSnippet = SanitizePii(request.FlaggedContent);

        var conductLog = new ConductLog
        {
            PseudonymizedUserHash = pseudonymizedHash,
            IncidentType = request.IncidentType,
            SanitizedContextSnippet = sanitizedSnippet,
            AiConfidenceScore = request.AiConfidenceScore,
            FlaggedAtUtc = DateTime.UtcNow,
            DataRetentionExpiryUtc = DateTime.UtcNow.AddDays(90), // 90-day storage limitation
            IsReviewedByOmbudsman = false
        };

        var savedIncident = await _conductRepository.LogIncidentAsync(conductLog, cancellationToken);

        var responseDto = new ConductIncidentResponseDto(
            savedIncident.IncidentId,
            savedIncident.PseudonymizedUserHash,
            savedIncident.IncidentType,
            savedIncident.SanitizedContextSnippet,
            savedIncident.AiConfidenceScore,
            savedIncident.FlaggedAtUtc,
            savedIncident.DataRetentionExpiryUtc,
            savedIncident.IsReviewedByOmbudsman
        );

        return StatusCode(StatusCodes.Status201Created, responseDto);
    }

    /// <summary>
    /// Returns all flagged conduct incidents for ombudsman audit without exposing personal identifying data.
    /// </summary>
    [HttpGet("incidents")]
    [ProducesResponseType(typeof(IEnumerable<ConductIncidentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncidents(CancellationToken cancellationToken)
    {
        var incidents = await _conductRepository.GetActiveIncidentsAsync(cancellationToken);
        var response = incidents.Select(i => new ConductIncidentResponseDto(
            i.IncidentId,
            i.PseudonymizedUserHash,
            i.IncidentType,
            i.SanitizedContextSnippet,
            i.AiConfidenceScore,
            i.FlaggedAtUtc,
            i.DataRetentionExpiryUtc,
            i.IsReviewedByOmbudsman
        ));

        return Ok(response);
    }

    /// <summary>
    /// Enforces DPDP Act 2023 Storage Limitation: purges incidents exceeding the data retention window.
    /// </summary>
    [HttpPost("purge-expired")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PurgeExpiredIncidents(CancellationToken cancellationToken)
    {
        int purgedCount = await _conductRepository.PurgeExpiredIncidentsAsync(DateTime.UtcNow, cancellationToken);
        return Ok(new
        {
            Message = "DPDP Act 2023 Storage Limitation purge completed successfully.",
            IncidentsPurged = purgedCount,
            TimestampUtc = DateTime.UtcNow
        });
    }

    private static string ComputePseudonymizedHash(string rawUserId)
    {
        const string salt = "Saksham_Gov_DPDP_Salt_2026";
        byte[] inputBytes = Encoding.UTF8.GetBytes(rawUserId + salt);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string SanitizePii(string rawContent)
    {
        // Redacts sensitive identifiable structures (e.g. email, phone numbers)
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            rawContent,
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
            "[REDACTED_EMAIL]");

        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"\b\d{10}\b",
            "[REDACTED_PHONE]");

        return sanitized.Length > 500 ? sanitized[..500] : sanitized;
    }
}

