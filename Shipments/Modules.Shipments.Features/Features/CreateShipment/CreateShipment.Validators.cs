using FluentValidation;
using Modules.Shipments.Domain.ValueObjects;

namespace Modules.Shipments.Features.Features.CreateShipment;

public class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    public CreateShipmentRequestValidator()
    {
        RuleFor(shipment => shipment.OrderId).NotEmpty();
        RuleFor(shipment => shipment.Carrier).NotEmpty();
        RuleFor(shipment => shipment.ReceiverEmail).NotEmpty();
        RuleFor(shipment => shipment.Items).NotEmpty();

        RuleFor(shipment => shipment.Address)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Address must not be null")
            .SetValidator(new AddressValidator());
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(address => address.Street).NotEmpty();
        RuleFor(address => address.City).NotEmpty();
        RuleFor(address => address.Zip).NotEmpty();
    }
}
