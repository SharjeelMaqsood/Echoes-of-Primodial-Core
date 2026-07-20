using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for Inventory.cs (Assets/Scripts 1/Inventroy/Inventory.cs).
/// Covers adding items, stacking, capacity limits, using consumables and equipment.
/// </summary>
public class InventoryTests
{
    private GameObject inventoryGO;
    private Inventory inventory;
    private InventoryUI inventoryUI;

    [SetUp]
    public void SetUp()
    {
        inventoryGO = new GameObject("InventoryUnderTest");
        inventory = inventoryGO.AddComponent<Inventory>();

        // InventoryUI.RefreshUI() safely no-ops when slotPrefab/slotParent are
        // unassigned, so we only need the component to exist (Inventory calls
        // inventoryUI.RefreshUI() after every mutation).
        var uiGO = new GameObject("InventoryUIUnderTest");
        inventoryUI = uiGO.AddComponent<InventoryUI>();
        inventory.inventoryUI = inventoryUI;

        var handGO = new GameObject("Hand");
        inventory.handTransform = handGO.transform;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(inventoryGO);
        Object.DestroyImmediate(inventoryUI.gameObject);
        Object.DestroyImmediate(inventory.handTransform.gameObject);
    }

    private ItemData CreateItem(string name, ItemType type, bool stackable = false,
        int maxStack = 10, int value = 0)
    {
        var item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName = name;
        item.itemType = type;
        item.isStackable = stackable;
        item.maxStack = maxStack;
        item.value = value;
        return item;
    }

    [Test]
    public void AddItem_NewItem_AddsSlotAndReturnsTrue()
    {
        var potion = CreateItem("Potion", ItemType.Consumable);

        bool added = inventory.AddItem(potion);

        Assert.IsTrue(added);
        Assert.AreEqual(1, inventory.slots.Count);
        Assert.AreEqual(potion, inventory.slots[0].item);
        Assert.AreEqual(1, inventory.slots[0].quantity);
    }

    [Test]
    public void AddItem_NullItem_ReturnsFalseAndDoesNotAddSlot()
    {
        bool added = inventory.AddItem(null);

        Assert.IsFalse(added);
        Assert.AreEqual(0, inventory.slots.Count);
    }

    [Test]
    public void AddItem_StackableItemAlreadyInInventory_IncreasesQuantityInsteadOfNewSlot()
    {
        var arrows = CreateItem("Arrows", ItemType.Misc, stackable: true, maxStack: 20);
        inventory.AddItem(arrows, 5);

        bool added = inventory.AddItem(arrows, 3);

        Assert.IsTrue(added);
        Assert.AreEqual(1, inventory.slots.Count, "Stacking should not create a second slot");
        Assert.AreEqual(8, inventory.slots[0].quantity);
    }

    [Test]
    public void AddItem_StackableItemAtMaxStack_DoesNotOverfillExistingSlot()
    {
        var arrows = CreateItem("Arrows", ItemType.Misc, stackable: true, maxStack: 5);
        inventory.AddItem(arrows, 5); // slot is now full

        inventory.AddItem(arrows, 1);

        // Existing full slot must never exceed maxStack.
        Assert.AreEqual(5, inventory.slots[0].quantity);
    }

    [Test]
    public void AddItem_WhenInventoryAtMaxSlots_ReturnsFalse()
    {
        // maxSlots defaults to 10 (private field) - fill it with 10 distinct,
        // non-stackable items so every AddItem call creates a new slot.
        for (int i = 0; i < 10; i++)
        {
            var item = CreateItem($"Item{i}", ItemType.Misc);
            Assert.IsTrue(inventory.AddItem(item), $"Slot {i} should have been added");
        }

        var overflowItem = CreateItem("Overflow", ItemType.Misc);
        bool added = inventory.AddItem(overflowItem);

        Assert.IsFalse(added);
        Assert.AreEqual(10, inventory.slots.Count);
    }

    [Test]
    public void UseItem_Equipment_DoesNotDecreaseOrRemoveSlot()
    {
        var sword = CreateItem("Sword", ItemType.Equipment);
        inventory.AddItem(sword);
        var slot = inventory.slots[0];

        inventory.UseItem(slot);

        Assert.AreEqual(1, inventory.slots.Count, "Equipment should stay in the inventory after use");
        Assert.AreEqual(1, slot.quantity);
    }

    [Test]
    public void UseItem_ConsumableWithMultipleCharges_DecrementsQuantityButKeepsSlot()
    {
        var potion = CreateItem("Potion", ItemType.Consumable, stackable: true, maxStack: 10, value: 10);
        inventory.AddItem(potion, 3);
        var slot = inventory.slots[0];

        inventory.UseItem(slot);

        Assert.AreEqual(1, inventory.slots.Count);
        Assert.AreEqual(2, slot.quantity);
    }

    [Test]
    public void UseItem_LastConsumableCharge_RemovesSlotFromInventory()
    {
        var potion = CreateItem("Potion", ItemType.Consumable, value: 10);
        inventory.AddItem(potion, 1);
        var slot = inventory.slots[0];

        inventory.UseItem(slot);

        Assert.AreEqual(0, inventory.slots.Count, "Slot should be removed once quantity hits zero");
    }

    [Test]
    public void UseItem_ConsumableWithoutHealthReference_DoesNotThrow()
    {
        // Inventory.health is intentionally left null here to verify the
        // guard clause in UseConsumable() prevents a NullReferenceException.
        var potion = CreateItem("Potion", ItemType.Consumable, value: 25);
        inventory.AddItem(potion);
        var slot = inventory.slots[0];

        Assert.DoesNotThrow(() => inventory.UseItem(slot));
    }

    [UnityTest]
    public IEnumerator UseItem_Consumable_HealsAssignedPlayerHealth()
    {
        var playerGO = new GameObject("Player");
        var health = playerGO.AddComponent<Health>();
        var deathUIGO = new GameObject("DeathUI");
        deathUIGO.transform.SetParent(playerGO.transform);
        deathUIGO.AddComponent<CanvasGroup>();
        health.deathUI = deathUIGO;

        yield return null; // let Health.Start() run so currentHealth is initialized

        health.ChangeHealth(50); // simulate prior damage, currentHealth = 50
        inventory.health = health;

        var potion = CreateItem("Potion", ItemType.Consumable, value: 20);
        inventory.AddItem(potion);
        inventory.UseItem(inventory.slots[0]);

        Assert.AreEqual(70, health.currentHealth);

        Object.DestroyImmediate(playerGO);
    }

    [Test]
    public void UseItem_NullSlot_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => inventory.UseItem(null));
    }
}
