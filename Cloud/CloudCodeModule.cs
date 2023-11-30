using System.Net;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.Economy.Model;

public class CloudCodeModule
{
    [CloudCodeFunction("MakeVirtualPurchase")]
    public async Task<VirtualPurchaseResult> MakeVirtualPurchase(IExecutionContext context, IGameApiClient gameApiClient,
        string virtualPurchaseId)
    {
        var virtualPurchaseResult = new VirtualPurchaseResult();
        
        try
        {
            var virtualPurchaseResponse = await TryMakeVirtualPurchaseAsync(context, gameApiClient, virtualPurchaseId);
            var resourceId = virtualPurchaseResponse.Data.Rewards.Inventory[0].Id;
            
            var purchaseResult = await TryGetResource(resourceId);
        
            if (string.IsNullOrEmpty(purchaseResult))
            {
                virtualPurchaseResult.VirtualPurchaseStatusCode = HttpStatusCode.NoContent;
        
                var purchaseCost = virtualPurchaseResponse.Data.Costs.Currency[0].Amount;
                var purchaseItemId = virtualPurchaseResponse.Data.Rewards.Inventory[^1].PlayersInventoryItemIds[0];
        
                await IncrementPlayerCurrencyBalanceAsync(context, gameApiClient, purchaseCost);
                await RemovePlayerInventoryItemAsync(context, gameApiClient, purchaseItemId);
            
                return virtualPurchaseResult;
            }

            ObtainResource(context.PlayerId ,purchaseResult);
            
            var updatePurchaseInstanceResponse = await UpdatePurchaseInstanceData(context, gameApiClient, virtualPurchaseResponse, purchaseResult);
        
            virtualPurchaseResult.VirtualPurchaseStatusCode = virtualPurchaseResponse.StatusCode;
            virtualPurchaseResult.Result = purchaseResult;
            
            return virtualPurchaseResult;
        }
        catch (ApiException e)
        {
            virtualPurchaseResult.VirtualPurchaseStatusCode = e.Response.StatusCode;
        
            return virtualPurchaseResult;
        }
    }

    private async Task<Task<int>> ObtainResource(string playerId, string purchaseResult)
    {
        var client = new PostgreClientConnection();
        
        return client.Execute($"UPDATE resources SET used=true, player_id='{playerId}' WHERE resource='{purchaseResult}'");
    }


    [CloudCodeFunction("UpdatePlayerBalance")]
    public async Task<long> UpdateBalance(IExecutionContext context, IGameApiClient gameApiClient, double revenue)
    {
        var response = await IncrementPlayerCurrencyBalanceAsync(context,gameApiClient, Convert.ToInt64(revenue * 10000));

        return response.Data.Balance;
    }

    private Task<ApiResponse<CurrencyBalanceResponse>> IncrementPlayerCurrencyBalanceAsync(IExecutionContext context, IGameApiClient gameApiClient, long incrementAmount)
    {
        return gameApiClient.EconomyCurrencies.IncrementPlayerCurrencyBalanceAsync(context, context.AccessToken,
            context.ProjectId, context.PlayerId, "BALANCE", new CurrencyModifyBalanceRequest("BALANCE", incrementAmount));
    }

    private async Task<string> TryGetResource(string resourceId)
    {
        var client = new PostgreClientConnection();

        var result =
            await client.Query(
                $"SELECT resource FROM resources WHERE used=false and resource_id='{resourceId}' LIMIT 1");
        return result;
    }

    private Task<ApiResponse<PlayerPurchaseVirtualResponse>> TryMakeVirtualPurchaseAsync(IExecutionContext context, IGameApiClient gameApiClient, string virtualPurchaseId)
    {
        return gameApiClient.EconomyPurchases
            .MakeVirtualPurchaseAsync(context, context.AccessToken, context.ProjectId, context.PlayerId,
                new PlayerPurchaseVirtualRequest(virtualPurchaseId));
    }

    private Task<Task<ApiResponse>> RemovePlayerInventoryItemAsync(IExecutionContext context, IGameApiClient gameApiClient, string playersInventoryItemId)
    {
        return Task.FromResult(gameApiClient.EconomyInventory.DeleteInventoryItemAsync(context, context.AccessToken, context.ProjectId,
            context.PlayerId, playersInventoryItemId));
    }

    private Task<ApiResponse<InventoryResponse>> UpdatePurchaseInstanceData(IExecutionContext context,
        IGameApiClient gameApiClient, ApiResponse<PlayerPurchaseVirtualResponse> response, string result)
    {
        
        var purchaseItemInstanceData = new PurchaseItemInstanceData(key: result);
        var inventoryRequestUpdate = new InventoryRequestUpdate(purchaseItemInstanceData);
        
        return gameApiClient.EconomyInventory.UpdateInventoryItemAsync(context,
            context.AccessToken, context.ProjectId,
            context.PlayerId, response.Data.Rewards.Inventory[0].PlayersInventoryItemIds[^1],
            inventoryRequestUpdate);
    }
}

public struct VirtualPurchaseResult
{
    public HttpStatusCode VirtualPurchaseStatusCode;
    public string? Result;
}

public struct PurchaseItemInstanceData
{
    public string Key;

    public PurchaseItemInstanceData(string key)
    {
        Key = key;
    }
}
