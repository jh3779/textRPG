/*
 * Inventory.cs
 *
 * 📝 역할: include/Inventory.h + src/Inventory.cpp 그대로 포팅.
 * 콘솔 출력(displayItems)은 포팅하지 않음 — UI가 아이템 목록을 순회하며 표시한다.
 * 04_data_model.md: "Player 1 ── 1 Inventory (최대 5칸)" — Game.cpp의 Inventory(5)와 동일하게
 * 기본 용량 5를 기본값으로 둔다(원본 헤더의 기본 인자 20은 실제로 쓰인 적이 없고,
 * Game() 생성자가 항상 Inventory(5)로 생성했으므로 실사용 값을 기본값으로 채택).
 */

using System.Collections.Generic;

namespace TextRPG.GameLogic
{
    public class Inventory
    {
        private readonly List<Item> items = new List<Item>();
        private readonly int capacity;

        public Inventory(int maxCapacity = 5)
        {
            capacity = maxCapacity;
        }

        public bool AddItem(Item item)
        {
            if (IsFull())
            {
                return false;
            }

            items.Add(item);
            return true;
        }

        public bool RemoveItem(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                return false;
            }

            items.RemoveAt(index);
            return true;
        }

        public Item GetItem(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                return null;
            }

            return items[index];
        }

        public int GetItemCount() => items.Count;

        public int GetCapacity() => capacity;

        public bool IsFull() => items.Count >= capacity;

        public void Clear() => items.Clear();

        /// <summary>UI가 순회할 수 있도록 읽기 전용 목록을 노출한다.</summary>
        public IReadOnlyList<Item> Items => items;
    }
}
