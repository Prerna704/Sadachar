using Microsoft.AspNetCore.Mvc;
using SakshamGovernance.Core.Entities;
using SakshamGovernance.Core.Enums;
using SakshamGovernance.Core.Interfaces;
using SakshamGovernance.Web.DTOs;

namespace SakshamGovernance.Web.Controllers;

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

        var newTicket = new GrievanceTicket
        {
            StudentId = request.StudentId,
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
    [HttpGet("grievances")]
    [ProducesResponseType(typeof(IEnumerable<GrievanceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllGrievances(CancellationToken cancellationToken)
    {
        var tickets = await _grievanceRepository.GetAllTicketsAsync(cancellationToken);
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

    /// <summary>
    /// Manually triggers the statutory 72-Hour SLA escalation service scan across all active tickets.
    /// </summary>
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

