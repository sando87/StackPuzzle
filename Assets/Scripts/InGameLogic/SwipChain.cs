using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using JoyPop;

public class SwipChain : MonoBehaviour
{
    public Product ProductA { get; private set; } = null;
    public Product ProductB { get; private set; } = null;

    public void InitChain(Product productA, Product productB)
    {
        productA.Chain = this;
        productB.Chain = this;
        ProductA = productA;
        ProductB = productB;
    }

    public void DestroyChain()
    {
        ProductA.Chain = null;
        ProductB.Chain = null;
        Destroy(gameObject);
    }

    public void DoEffectChainLocked(Product toProduct)
    {

    }
}

