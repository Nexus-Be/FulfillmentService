namespace Fulfillment.Application.Exceptions;

using System;

public class CarrierException : Exception
{
    public string? CarrierCode { get; }

    public CarrierException(string message, string? carrierCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        CarrierCode = carrierCode;
    }
}

public class CarrierNotFoundException : CarrierException
{
    public CarrierNotFoundException(string carrierCode)
        : base($"Carrier with code '{carrierCode}' is not supported or not configured.", carrierCode)
    {
    }
}

public class CarrierIntegrationException : CarrierException
{
    public string? ErrorCode { get; }

    public CarrierIntegrationException(string message, string? carrierCode = null, string? errorCode = null, Exception? innerException = null)
        : base(message, carrierCode, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class CarrierTimeoutException : CarrierException
{
    public CarrierTimeoutException(string message, string? carrierCode = null, Exception? innerException = null)
        : base(message, carrierCode, innerException)
    {
    }
}
