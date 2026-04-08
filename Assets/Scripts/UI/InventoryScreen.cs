using Unity.Entities;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    public class InventoryScreen : UIScreen
    {
        const int k_InitialSlotCount = 16;

        const string k_SlotUssClassName = "inventory-slot";
        Button m_BackButton;
        Label m_EnergyLabel;
        readonly InventorySlot[] m_Slots = new InventorySlot[k_InitialSlotCount];
        VisualElement m_SlotsContainer;

        public static InventoryScreen Instantiate(VisualElement parentElement) {
            var screen = CreateInstance<InventoryScreen>();
            screen.RootElement = parentElement;

            screen.m_EnergyLabel = screen.RootElement.Q<Label>("inventory__energy-label");
            screen.m_BackButton = screen.RootElement.Q<Button>("inventory__back-button");
            screen.m_SlotsContainer = screen.RootElement.Q<VisualElement>("inventory__slots-container");

            screen.InitializeSlots();

            screen.m_BackButton.clicked += screen.OnBackClicked;

            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        public void UpdateInventory(DynamicBuffer<InventoryItem> itemsBuf, int energyCount,
            CollectablesData collectablesData) {
            for (var i = 0; i < itemsBuf.Length && i < m_Slots.Length; i++) {
                var type = itemsBuf[i].Type;
                var sprite = collectablesData.Collectables[(int)type].Icon;
                m_Slots[i].SetItem(sprite);
            }

            m_EnergyLabel.text = energyCount.ToString();
        }

        public void OnBackClicked() {
            var entity = entityCommandBuffer.CreateEntity();
            entityCommandBuffer.AddComponent<BackClickedEvent>(entity);
            entityCommandBuffer.AddComponent<Event>(entity);
        }

        void InitializeSlots() {
            for (var i = 0; i < k_InitialSlotCount; i++) {
                var slot = new InventorySlot();
                slot.AddToClassList(k_SlotUssClassName);
                m_SlotsContainer.Add(slot);
                m_Slots[i] = slot;
            }
        }

        public struct BackClickedEvent : IComponentData
        { }
    }
}