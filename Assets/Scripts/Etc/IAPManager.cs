using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

[Serializable]
public enum IAPProductType
{
    // enum 이름이 스토어에 등록된 인앱결제 아이템 id와 동일해야 함
    joypop_product_dia1,
    joypop_product_dia2,
    ProductID_C,
    ProductID_D,
}

[Serializable]
public class IAPProduct
{
    public IAPProductType ID;
    public Action<bool> EventResult;
}

public class IAPManager : MonoBehaviour
{
    private static IAPManager mInst = null;
    public static IAPManager Inst { get { if (mInst == null) mInst = FindObjectOfType<IAPManager>(); return mInst; } }

    [SerializeField] IAPProduct[] RegistorProducts = null;

    StoreController m_StoreController; // The Unity Purchasing system.
    Dictionary<string, IAPProduct> mProducts = new Dictionary<string, IAPProduct>();

    public bool IsConnected { get; private set; } = false;

    void Awake()
    {
        IsConnected = false;
        m_StoreController = UnityIAPServices.StoreController();

        m_StoreController.OnPurchasePending += OnPurchasePending;
        m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
        m_StoreController.OnStoreDisconnected += OnStoreDisconnected;
        m_StoreController.OnProductsFetchFailed += OnProductsFetchedFailed;
        m_StoreController.OnProductsFetched += OnProductsFetched;

        TryConnectIAP();

        StartCoroutine(CoTryReconnect());
    }

    async void TryConnectIAP()
    {
        try
        {
            await m_StoreController.Connect();
            InitProducts();

            IsConnected = true;
        }
        catch (Exception e)
        {
            IsConnected = false;
        }
    }

    IEnumerator CoTryReconnect()
    {
        while (true)
        {
            yield return new WaitForSeconds(5);

            if (!IsConnected && Application.internetReachability != NetworkReachability.NotReachable)
            {
                TryConnectIAP();
            }
        }

    }

    void InitProducts()
    {
        List<ProductDefinition> initialProductsToFetch = new List<ProductDefinition>();
        foreach (IAPProduct pro in RegistorProducts)
        {
            string proID = pro.ID.ToString();
            mProducts[proID] = pro;
            initialProductsToFetch.Add(new ProductDefinition(proID, ProductType.Consumable));
        }

        m_StoreController.FetchProducts(initialProductsToFetch);
    }

    public void BuyProduct(IAPProductType productType, Action<bool> eventResult)
    {
        LOG.trace($"Purchase request - Product: {productType.ToString()}");

        string proID = productType.ToString();
        mProducts[proID].EventResult = eventResult;
        m_StoreController.PurchaseProduct(proID);
    }

    void OnPurchaseFailed(FailedOrder order)
    {
        var product = GetFirstProductInOrder(order);
        if (product == null)
        {
            LOG.trace("Could not find product in failed order.");
            return;
        }

        LOG.trace($"Purchase failed - Product: '{product?.definition.id}'," +
                    $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                    + $"Purchase Failure Details: {order.Details}");
        
        string proID = product.definition.id;
        if (mProducts.ContainsKey(proID))
        {
            mProducts[proID].EventResult?.Invoke(false);
            mProducts[proID].EventResult = null;
        }
    }

    void OnPurchasePending(PendingOrder order)
    {
        var product = GetFirstProductInOrder(order);
        if (product is null)
        {
            LOG.trace("Could not find product in order.");
            return;
        }
        
        LOG.trace($"Purchase complete - Product: {product.definition.id}");
        
        m_StoreController.ConfirmPurchase(order);

        //Add the purchased product to the players inventory
        string proID = product.definition.id;
        if (mProducts.ContainsKey(proID))
        {
            mProducts[proID].EventResult?.Invoke(true);
            mProducts[proID].EventResult = null;
        }
    }

    void OnPurchaseConfirmed(Order order)
    {
        switch (order)
        {
            case ConfirmedOrder confirmedOrder:
                OnPurchaseConfirmed(confirmedOrder);
                break;
            case FailedOrder failedOrder:
                OnPurchaseConfirmationFailed(failedOrder);
                break;
            default:
                LOG.trace("Unknown OnPurchaseConfirmed result.");
                break;
        }
    }

    void OnPurchaseConfirmed(ConfirmedOrder order)
    {
        var product = GetFirstProductInOrder(order);
        if (product == null)
        {
            LOG.trace("Could not find product in purchase confirmation.");
        }
        else
        {
            LOG.trace($"Purchase confirmed- Product: {product?.definition.id}");
        }
    }

    void OnPurchaseConfirmationFailed(FailedOrder order)
    {
        var product = GetFirstProductInOrder(order);
        if (product == null)
        {
            LOG.trace("Could not find product in failed confirmation.");
        }
        else
        {
            LOG.trace($"Confirmation failed - Product: '{product?.definition.id}'," +
                        $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                        + $"Confirmation Failure Details: {order.Details}");
        }
    }

    Product GetFirstProductInOrder(Order order)
    {
        return order.CartOrdered.Items().First()?.Product;
    }

    // Calling StoreController.Connect without a listener on the StoreController.OnStoreDisconnected event will result in warnings.
    void OnStoreDisconnected(StoreConnectionFailureDescription description)
    {
        // LOG.trace($"Store disconnected details: {description.message}");
        IsConnected = false;
    }

    // Calling StoreController.Connect without listeners on StoreController.OnProductsFetched and StoreController.OnProductsFetchedFailed will result in warnings.
    void OnProductsFetched(List<Product> products)
    {
        // LOG.trace($"Products fetched successfully for {products.Count} products.");
    }

    void OnProductsFetchedFailed(ProductFetchFailed failure)
    {
        // LOG.trace($"Products fetch failed for {failure.FailedFetchProducts.Count} products: {failure.FailureReason}");
    }
}
