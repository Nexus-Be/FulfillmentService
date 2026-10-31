namespace Fulfillment.Application.Services.Inventory;

using Fulfillment.Application.Interfaces;

public sealed partial class InventoryAppService : IInventoryAppService
{
    private readonly IFulfillmentDbContext _dbContext;

    public InventoryAppService(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}

