namespace Fulfillment.Application.Interfaces.Shipping;

public interface ICarrierResolver
{
    ICarrierGateway Get(string? carrierCode = null);
}

