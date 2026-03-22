using Carter;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Features;
using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Features.Features.UpdateShipmentStatus;

public sealed record UpdateShipmentStatusRequest(ShipmentStatus Status);

public class UpdateShipmentStatusEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		app.MapPost("/api/shipments/{shipmentNumber}/update-status", Handle);
	}

	private static async Task<IResult> Handle(
		[FromRoute] string shipmentNumber,
		[FromBody] UpdateShipmentStatusRequest request,
		IValidator<UpdateShipmentStatusRequest> validator,
		IMediator mediator,
		CancellationToken cancellationToken)
	{
		var validationResult = await validator.ValidateAsync(request, cancellationToken);
		if (!validationResult.IsValid)
		{
			return Results.ValidationProblem(validationResult.ToDictionary());
		}

		var command = new UpdateShipmentStatusCommand(shipmentNumber, request.Status);

		var response = await mediator.Send(command, cancellationToken);
		if (response.IsError)
		{
			return response.Errors.ToProblem();
		}

		return Results.NoContent();
	}
}

internal sealed record UpdateShipmentStatusCommand(string ShipmentNumber, ShipmentStatus Status)
	: IRequest<ErrorOr<Success>>;

internal sealed class UpdateShipmentStatusHandler(
	ShipmentsDbContext context,
	ILogger<UpdateShipmentStatusCommand> logger)
	: IRequestHandler<UpdateShipmentStatusCommand, ErrorOr<Success>>
{
	public async Task<ErrorOr<Success>> Handle(UpdateShipmentStatusCommand request, CancellationToken cancellationToken)
	{
		var shipment = await context.Shipments
			.Where(x => x.Number == request.ShipmentNumber)
			.FirstOrDefaultAsync(cancellationToken: cancellationToken);

		if (shipment is null)
		{
			logger.LogDebug("Shipment with number {ShipmentNumber} not found", request.ShipmentNumber);
			return Error.NotFound("Shipment.NotFound", $"Shipment with number '{request.ShipmentNumber}' not found");
		}

		shipment.Status = request.Status;

		await context.SaveChangesAsync(cancellationToken);

		logger.LogInformation("Updated state of shipment {ShipmentNumber} to {NewState}", request.ShipmentNumber, request.Status);

		return Result.Success;
	}
}
