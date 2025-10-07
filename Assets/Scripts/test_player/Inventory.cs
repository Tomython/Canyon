using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // Структура предмета в инвентаре
    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;
        public int quantity;

        public InventoryItem(string name, int qty)
        {
            itemName = name;
            quantity = qty;
        }
    }

    public List<InventoryItem> items = new List<InventoryItem>();
    public int maxSlots = 20;

    // Добавить предмет в инвентарь
    public bool AddItem(string itemName, int amount)
    {
        // Ищем предмет в списке
        InventoryItem existingItem = items.Find(i => i.itemName == itemName);

        if (existingItem != null)
        {
            existingItem.quantity += amount;
            return true;
        }
        else
        {
            if (items.Count < maxSlots)
            {
                items.Add(new InventoryItem(itemName, amount));
                return true;
            }
            else
            {
                Debug.Log("Инвентарь заполнен");
                return false;
            }
        }
    }

    // Удалить предмет из инвентаря
    public bool RemoveItem(string itemName, int amount)
    {
        InventoryItem existingItem = items.Find(i => i.itemName == itemName);

        if (existingItem != null && existingItem.quantity >= amount)
        {
            existingItem.quantity -= amount;
            if (existingItem.quantity <= 0)
            {
                items.Remove(existingItem);
            }
            return true;
        }
        else
        {
            Debug.Log("Недостаточно предметов для удаления");
            return false;
        }
    }

    // Проверить количество предметов
    public int GetItemQuantity(string itemName)
    {
        InventoryItem existingItem = items.Find(i => i.itemName == itemName);
        return existingItem != null ? existingItem.quantity : 0;
    }
}
