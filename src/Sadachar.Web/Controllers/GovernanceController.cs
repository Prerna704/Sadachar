using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sadachar.Core.Entities;
using Sadachar.Core.Enums;
using Sadachar.Core.Interfaces;
using Sadachar.Web.DTOs;

namespace Sadachar.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GovernanceController : ControllerBase
{
    private readonly IGrievanceRepository _grievanceRepository;
    private readonly ISlaEscalationService _slaEscalationService;

    public GovernanceController(
        IGrievanceRepository grievanceRepository,
        ISlaEscalationService slaEscalationService)
    {
        _grievanceRepository = grievanceRepository;
        _slaEscalationService = slaEscalationService;
    }

    /// <summary>
    /// Logs a new grievance ticket for a specially-abled student, initializing a strict 72-Hour SLA clock.
    /// </summary>
    [HttpPost("grievance")]
    [Authorize(Roles = "Student,Educator")]
    [ProducesResponseType(typeof(GrievanceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGrievanceTicket(
        [FromBody] GrievanceCreateDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized();
        }

        var newTicket = new GrievanceTicket
        {
            StudentId = studentId,
            IssueDescription = request.IssueDescription,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow
            // SLAExpiryDateTime automatically assigned as CreatedAt + 72 Hours in constructor
        };

        var createdTicket = await _grievanceRepository.CreateTicketAsync(newTicket, cancellationToken);

        var responseDto = new GrievanceResponseDto(
            createdTicket.TicketId,
            createdTicket.StudentId,
            createdTicket.IssueDescription,
            createdTicket.Status,
            createdTicket.CreatedAt,
            createdTicket.SLAExpiryDateTime,
            DateTime.UtcNow > createdTicket.SLAExpiryDateTime,
            createdTicket.EscalatedAt
        );

        return CreatedAtAction(nameof(GetTicketById), new { ticketId = createdTicket.TicketId }, responseDto);
    }

    /// <summary>
    /// Fetches a grievance ticket by ID and automatically validates / escalates if the 72-Hour SLA has breached.
    /// </summary>
    [HttpGet("grievance/{ticketId:guid}")]
    [ProducesResponseType(typeof(GrievanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketById(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _grievanceRepository.GetTicketByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            return NotFound(new { Message = $"Grievance ticket with ID '{ticketId}' not found." });
        }

        if (!User.IsInRole(nameof(UserRole.Admin))
            && !User.IsInRole(nameof(UserRole.Auditor))
            && ticket.StudentId != User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            return Forbid();
        }

        // Real-time SLA breach validation upon lookup
        if ((ticket.Status == TicketStatus.Open || ticket.Status == TicketStatus.InProgress) 
            && DateTime.UtcNow > ticket.SLAExpiryDateTime)
        {
            ticket.Status = TicketStatus.Escalated;
            ticket.EscalatedAt = DateTime.UtcNow;
            await _grievanceRepository.UpdateTicketAsync(ticket, cancellationToken);
        }

        var responseDto = new GrievanceResponseDto(
            ticket.TicketId,
            ticket.StudentId,
            ticket.IssueDescription,
            ticket.Status,
            ticket.CreatedAt,
            ticket.SLAExpiryDateTime,
            DateTime.UtcNow > ticket.SLAExpiryDateTime,
            ticket.EscalatedAt
        );

        return Ok(responseDto);
    }

    /// <summary>
    /// Returns all registered grievance tickets and their current SLA status.
    /// </summary>
    [Authorize(Roles = "Student,Educator,Admin,Auditor")]
    [HttpGet("grievances")]
    [ProducesResponseType(typeof(IEnumerable<GrievanceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllGrievances(CancellationToken cancellationToken)
    {
        var tickets = await _grievanceRepository.GetAllTicketsAsync(cancellationToken);
        if (User.IsInRole(nameof(UserRole.Student)) || User.IsInRole(nameof(UserRole.Educator)))
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            tickets = tickets.Where(ticket => ticket.StudentId == studentId).ToList();
        }

        var currentUtc = DateTime.UtcNow;

        var response = tickets.Select(ticket => new GrievanceResponseDto(
            ticket.TicketId,
            ticket.StudentId,
            ticket.IssueDescription,
            ticket.Status,
            ticket.CreatedAt,
            ticket.SLAExpiryDateTime,
            currentUtc > ticket.SLAExpiryDateTime,
            ticket.EscalatedAt
        ));

        return Ok(response);
    }

    public record GrievanceStatusUpdate(TicketStatus Status, string? ResolutionNotes);

    [Authorize(Roles = "Admin")]
    [HttpPut("grievance/{ticketId:guid}/status")]
    public async Task<IActionResult> UpdateGrievanceStatus(Guid ticketId, [FromBody] GrievanceStatusUpdate request, CancellationToken cancellationToken)
    {
        if (request.Status is not (TicketStatus.InProgress or TicketStatus.Resolved) || request.ResolutionNotes?.Length > 1000) return BadRequest();
        var ticket = await _grievanceRepository.GetTicketByIdAsync(ticketId, cancellationToken);
        if (ticket is null) return NotFound();
        ticket.Status = request.Status;
        if (request.Status == TicketStatus.Resolved)
        {
            ticket.ResolvedAt = DateTime.UtcNow;
            ticket.ResolutionNotes = request.ResolutionNotes?.Trim();
        }
        await _grievanceRepository.UpdateTicketAsync(ticket, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Manually triggers the statutory 72-Hour SLA escalation service scan across all active tickets.
    /// </summary>
    [Authorize(Roles = "Admin,Auditor")]
    [HttpPost("grievance/trigger-sla-check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerSlaCheck(CancellationToken cancellationToken)
    {
        int escalatedCount = await _slaEscalationService.EvaluateAndEscalateOverdueTicketsAsync(cancellationToken);
        return Ok(new
        {
            Message = "Statutory 72-Hour SLA verification executed successfully.",
            TicketsEscalated = escalatedCount,
            ProcessedAtUtc = DateTime.UtcNow
        });
    }
}
