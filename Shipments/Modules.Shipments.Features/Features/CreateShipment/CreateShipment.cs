using Bogus;
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
using Modules.Carriers.PublicApi;
using Modules.Carriers.PublicApi.Contracts;
using Modules.Common.Features;
using Modules.Shipments.Domain.Entities;
using Modules.Shipments.Features.Features.Shared.Requests;
using Modules.Shipments.Features.Features.Shared.Responses;
using Modules.Shipments.Infrastructure.Database;
using Modules.Stocks.PublicApi;
using Modules.Stocks.PublicApi.Contracts;
using Address = Modules.Shipments.Domain.ValueObjects.Address;

namespace Modules.Shipments.Features.Features.CreateShipment;

public sealed record CreateShipmentRequest(
    string OrderId,
    Address Address,
    string Carrier,
    string ReceiverEmail,
    List<ShipmentItemRequest> Items);

public class CreateShipmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/shipments", Handle);
    }

    private static async Task<IResult> Handle(
        [FromBody] CreateShipmentRequest request,
        IValidator<CreateShipmentRequest> validator,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var command = request.MapToCommand();

        var response = await mediator.Send(command, cancellationToken);
        if (response.IsError)
        {
            return response.Errors.ToProblem();
        }

        return Results.Ok(response.Value);
    }
}

internal sealed record CreateShipmentCommand(
    string OrderId,
    Address Address,
    string Carrier,
    string ReceiverEmail,
    List<ShipmentItemRequest> Items)
    : IRequest<ErrorOr<ShipmentResponse>>;

internal sealed class CreateShipmentCommandHandler(
    ShipmentsDbContext context,
    IStockModuleApi stockApi,
    ICarrierModuleApi carrierApi,
    ILogger<CreateShipmentCommandHandler> logger)
    : IRequestHandler<CreateShipmentCommand, ErrorOr<ShipmentResponse>>
{
    public async Task<ErrorOr<ShipmentResponse>> Handle(
        CreateShipmentCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentExists = await context.Shipments.AnyAsync(x => x.OrderId == request.OrderId, cancellationToken);
        if (shipmentExists)
        {
            logger.LogInformation("Shipment for order '{OrderId}' already exists", request.OrderId);
            return Error.Conflict($"Shipment for order '{request.OrderId}' already exists");
        }

        var stockRequest = CreateStockRequest(request);
        
        var stockResponse = await stockApi.CheckStockAsync(stockRequest, cancellationToken);
        if (!stockResponse.IsSuccess)
        {
            logger.LogInformation("Stock check failed: {ErrorMessage}", stockResponse.ErrorMessage);
            return Error.Validation("ProductsNotAvailableInStock", stockResponse.ErrorMessage ?? "Products not available in stock");
        }

        var shipmentNumber = new Faker().Commerce.Ean8();
        var shipment = request.MapToShipment(shipmentNumber);

        await context.Shipments.AddAsync(shipment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created shipment: {@Shipment}", shipment);

        var carrierRequest = CreateCarrierRequest(request);

        try
        {
            await carrierApi.CreateShipmentAsync(carrierRequest, cancellationToken);
            
            var updateRequest = CreateUpdateStockRequest(shipment);

            var response = await stockApi.UpdateStockAsync(updateRequest, cancellationToken);
            if (!response.IsSuccess)
            {
                return Error.Failure("StockUpdateFailed", "Failed to update stock");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create carrier shipment for order {OrderId}", request.OrderId);
            return Error.Failure("CarrierShipmentCreationFailed", "Failed to create carrier shipment");
        }

        return shipment.MapToResponse();
    }

    private static UpdateStockRequest CreateUpdateStockRequest(Shipment shipment)
    {
        return new UpdateStockRequest(
            Products: shipment.Items.Select(x => new ProductStock(x.Product, x.Quantity)).ToList(),
            Operation: StockOperation.Decrease
        );
    }

    private static CheckStockRequest CreateStockRequest(CreateShipmentCommand request)
    {
        return new CheckStockRequest(
            request.Items
                .Select(x => new ProductStock(x.Product, x.Quantity))
                .ToList()
        );
    }

    private static CreateCarrierShipmentRequest CreateCarrierRequest(CreateShipmentCommand request)
    {
        return new CreateCarrierShipmentRequest(
            request.OrderId,
            new Carriers.PublicApi.Contracts.Address(
                request.Address.Street,
                request.Address.City,
                request.Address.Zip
            ),
            request.Carrier,
            request.ReceiverEmail,
            request.Items
                .Select(x => new CarrierShipmentItem(x.Product, x.Quantity))
                .ToList()
        );
    }
}
