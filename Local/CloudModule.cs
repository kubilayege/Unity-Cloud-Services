using System;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using Unity.Services.CloudCode;
using Unity.Services.Economy;
using UnityEngine;
using VContainer;

public sealed class CloudModule : BaseModule
{
    public override int StartOrder => 10;

    public override UniTask StartAsync()
    {
        return default;
    }

    [Inject] private AppChannelGroup _channelGroup;
    [Inject] private ShopModule shopModule;
    [Inject] private EconomyModule economyModule;

    public async UniTask<bool> MakeVirtualPurchase(string purchaseId)
    {
        Debug.Log("VAR " + purchaseId);
        var args = new Dictionary<string, object>
        {
            {"virtualPurchaseId", purchaseId}
        };
        var message = await CloudCodeService.Instance.CallModuleEndpointAsync<VirtualPurchaseResult>(CloudLibrary.Module, CloudLibrary.MakeVirtualPurchase, args);

        if (message.VirtualPurchaseStatusCode == HttpStatusCode.NoContent)
        {
            Debug.Log("No key left");
            return false;
        }

        if (message.VirtualPurchaseStatusCode == HttpStatusCode.UnprocessableEntity)
        {
            Debug.Log("Not enough Balance");
            return false;
        }

        economyModule.UpdateBalance();
        shopModule.PlayerInventoryUpdated();

        return true;
        // TODO: update player balance
    }

    public async UniTask UpdateBalance(double revenue)
    {
        var args = new Dictionary<string, object>
        {
            {"revenue", revenue}
        };
        var message = await CloudCodeService.Instance.CallModuleEndpointAsync<long>(CloudLibrary.Module, CloudLibrary.UpdateBalance, args);
        
        economyModule.UpdateBalance();
    }
}

public static class CloudLibrary
{
    public const string Module = "Adshop";
    public const string MakeVirtualPurchase = "MakeVirtualPurchase";
    public const string UpdateBalance = "UpdatePlayerBalance";
}

[Preserve]
[Serializable]
public struct VirtualPurchaseResult
{
    public HttpStatusCode VirtualPurchaseStatusCode;
    public string? Result;
}
