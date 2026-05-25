using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    private Dictionary<string, int> items = new Dictionary<string, int>();

    public void AddItem(string itemName)
    {
        if (items.ContainsKey(itemName))
        {
            items[itemName]++;
        }
        else
        {
            items[itemName] = 1;
        }
        Debug.Log($"Added {itemName}. Total: {items[itemName]}");
    }

    public void RemoveItem(string itemName)
    {
        if (items.ContainsKey(itemName))
        {
            items[itemName]--;
            if (items[itemName] <= 0)
            {
                items.Remove(itemName);
            }
        }
        Debug.Log($"Removed {itemName}. Total: {items.GetValueOrDefault(itemName, 0)}");
    }

    public int GetItemCount(string itemName)
    {
        return items.GetValueOrDefault(itemName, 0);
    }

    public void PrintInventory()
    {
        Debug.Log("Inventory:");
        foreach (var item in items)
        {
            Debug.Log($"{item.Key}: {item.Value}");
        }
    }
}
