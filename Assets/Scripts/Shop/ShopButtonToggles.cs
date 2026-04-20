using UnityEngine;
using UnityEngine.UI;

public class ShopButtonToggles : MonoBehaviour
{
    [SerializeField] private Button shopButtonPage1;
    [SerializeField] private Button shopButtonPage2;
    [SerializeField] private Button shopButtonPage3;

    private void Start()
    {
        shopButtonPage1.onClick.AddListener(OpenShopPage1);
        shopButtonPage2.onClick.AddListener(OpenShopPage2);
        shopButtonPage3.onClick.AddListener(OpenShopPage3);
    }

    public void OpenShopPage1()
    {
        if (ShopManager.Instance.CurrentShopKeeper != null)
            ShopManager.Instance.CurrentShopKeeper.OpenShopPage1();
    }

    public void OpenShopPage2()
    {
        if (ShopManager.Instance.CurrentShopKeeper != null)
            ShopManager.Instance.CurrentShopKeeper.OpenShopPage2();
    }

    public void OpenShopPage3()
    {
        if (ShopManager.Instance.CurrentShopKeeper != null)
            ShopManager.Instance.CurrentShopKeeper.OpenShopPage3();
    }
}
