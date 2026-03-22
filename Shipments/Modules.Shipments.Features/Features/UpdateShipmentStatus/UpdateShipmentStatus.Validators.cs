using FluentValidation;

namespace Modules.Shipments.Features.Features.UpdateShipmentStatus;

public class UpdateShipmentStatusRequestValidator : AbstractValidator<UpdateShipmentStatusRequest>
{
	public UpdateShipmentStatusRequestValidator()
	{
		RuleFor(x => x.Status).IsInEnum();
	}
}
