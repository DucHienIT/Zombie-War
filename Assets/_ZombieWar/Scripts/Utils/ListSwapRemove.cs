using System.Collections.Generic;

namespace ZombieWar.Utils
{
    public static class ListSwapRemove
    {
        // O(1) removal for lists where order does not matter: the last element takes the
        // removed slot instead of shifting everything after it.
        public static void RemoveAtSwap<T>(this List<T> list, int index)
        {
            int last = list.Count - 1;
            list[index] = list[last];
            list.RemoveAt(last);
        }
    }
}
