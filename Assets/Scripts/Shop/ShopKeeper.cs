using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopKeeper : MonoBehaviour
{
    [Header("Shop keeper")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool playerInRange;
    [SerializeField] private CanvasGroup shopCanvasGroup;
    [SerializeField] private bool isShopOpen = false;

    [Header("Camera")]
    [SerializeField] private Camera shopkeeperCam;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0,0,-1);

    [Header("Shop pages")]
    [SerializeField] private List<ShopItem> shopItemsPage1;
    [SerializeField] private List<ShopItem> shopItemsPage2;
    [SerializeField] private List<ShopItem> shopItemsPage3;


    public static event Action<ShopManager, bool> OnShopStateChanged;

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetButtonDown("Interact") || (isShopOpen && Input.GetButtonDown("Cancel")))
        {
            SetShopOpen(!isShopOpen);
        }
    }

    private void SetShopOpen(bool open)
    {
        isShopOpen = open;
        if (open)
        {
            ShopManager.Instance.CurrentShopKeeper = this;
            if (shopkeeperCam == null) shopkeeperCam = GameObject.Find("ShopkeeperCamera").GetComponent<Camera>();
            shopkeeperCam.transform.position = transform.position + cameraOffset;
            shopkeeperCam.cullingMask = LayerMask.GetMask("Shopkeeper");
            shopkeeperCam.gameObject.SetActive(true);
            OpenShopPage1();
        }
        else
        {
            ShopManager.Instance.CurrentShopKeeper = null;
            shopkeeperCam.gameObject.SetActive(false);
            ShopManager.Instance.ClearShop();
        }

        SetCanvasVisible(open);
        Time.timeScale = open ? 0f : 1f;

        OnShopStateChanged?.Invoke(ShopManager.Instance, open);
    }

    private void SetCanvasVisible(bool visible)
    {
        shopCanvasGroup.alpha = visible ? 1f : 0f;
        shopCanvasGroup.interactable = visible;
        shopCanvasGroup.blocksRaycasts = visible;
    }

    public void OpenShopPage1()
    {
        ShopManager.Instance.ClearShop();
        ShopManager.Instance.PopulateShopItems(shopItemsPage1);
    }

    public void OpenShopPage2()
    {
        ShopManager.Instance.ClearShop();
        ShopManager.Instance.PopulateShopItems(shopItemsPage2);
    }

    public void OpenShopPage3()
    {
        ShopManager.Instance.ClearShop();
        ShopManager.Instance.PopulateShopItems(shopItemsPage3);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            animator.SetBool("playerInRange", true);
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            animator.SetBool("playerInRange", false);
            playerInRange = false;
            if (isShopOpen) isShopOpen = false;
        }
    }
}
