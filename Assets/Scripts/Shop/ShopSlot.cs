using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Item info")]
    [SerializeField] private string itemKeyName;
    [SerializeField] private ItemDefinition item;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image itemImage;
    [SerializeField] private int price;
    [SerializeField] private Button buyButton;

    [Header("References")]
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ShopInfo shopInfo;

    public void Initialize(string itemKeyName, int price)
    {
        this.itemKeyName = itemKeyName;
        this.item = ItemDatabase.GetItem(itemKeyName);
        itemImage.sprite = item.GetIcon();
        itemNameText.text = item.name;
        this.price = price;
        priceText.text = price.ToString();

        buyButton.onClick.AddListener(OnBuyButtonClicked);
    }

    public void OnBuyButtonClicked()
    {
        shopManager.TryBuyItem(itemKeyName, price);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item != null) 
            shopInfo.ShowItemInfo(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        shopInfo.HideItemInfo();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (item != null)
            shopInfo.FollowMouse();
    }
}
