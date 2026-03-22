using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Shipments.Features.Features.CreateShipment;
using Modules.Shipments.Features.Features.Shared.Responses;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Features.Features.GetShipmentByNumber;

public class GetShipmentByNumberEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		app.MapGet("/api/shipments/{shipmentNumber}", Handle);
	}

	private static async Task<IResult> Handle(
		[FromRoute] string shipmentNumber,
		IMediator mediator,
		CancellationToken cancellationToken)
	{
		var response = await mediator.Send(new GetShipmentByNumberQuery(shipmentNumber), cancellationToken);
		return response is not null ? Results.Ok(response) : Results.NotFound($"Shipment with number '{shipmentNumber}' not found");
	}
}

internal sealed record GetShipmentByNumberQuery(string ShipmentNumber)
	: IRequest<ShipmentResponse?>;

internal sealed class GetShipmentByNumberQueryHandler(
	ShipmentsDbContext dbContext,
	ILogger<GetShipmentByNumberQueryHandler> logger)
	: IRequestHandler<GetShipmentByNumberQuery, ShipmentResponse?>
{
	public async Task<ShipmentResponse?> Handle(GetShipmentByNumberQuery request, CancellationToken cancellationToken)
	{
		var shipment = await dbContext.Shipments
			.Include(x => x.Items)
			.FirstOrDefaultAsync(x => x.Number == request.ShipmentNumber, cancellationToken);

		if (shipment is null)
		{
			logger.LogDebug("Shipment with number {ShipmentNumber} not found", request.ShipmentNumber);
			return null;
		}

		var response = shipment.MapToResponse();
		return response;
	}
}
