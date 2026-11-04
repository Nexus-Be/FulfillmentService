namespace Fulfillment.Application.Services.Shipping;

using System;
using System.Collections.Generic;
using System.Linq;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Interfaces.Shipping;

public class CarrierResolver : ICarrierResolver
{
    private readonly IReadOnlyDictionary<string, ICarrierGateway> _gateways;
    private readonly string _defaultCarrierCode;

    public CarrierResolver(IEnumerable<ICarrierGateway> gateways, string defaultCarrierCode)
    {
        ArgumentNullException.ThrowIfNull(gateways);

        if (string.IsNullOrWhiteSpace(defaultCarrierCode))
        {
            throw new ArgumentException("Default carrier code cannot be null or whitespace.", nameof(defaultCarrierCode));
        }

        _gateways = gateways.ToDictionary(g => g.Code, g => g, StringComparer.OrdinalIgnoreCase);
        _defaultCarrierCode = defaultCarrierCode.Trim();
    }

    public ICarrierGateway Get(string? carrierCode = null)
    {
        var targetCode = string.IsNullOrWhiteSpace(carrierCode) ? _defaultCarrierCode : carrierCode.Trim();

        if (!_gateways.TryGetValue(targetCode, out var gateway))
        {
            throw new CarrierNotFoundException(targetCode);
        }

        return gateway;
    }
}
