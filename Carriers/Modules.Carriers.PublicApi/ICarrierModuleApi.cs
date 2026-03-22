using Modules.Carriers.PublicApi.Contracts;

namespace Modules.Carriers.PublicApi;

public interface ICarrierModuleApi
{
    Task CreateShipmentAsync(CreateCarrierShipmentRequest request, CancellationToken cancellationToken = default);
}