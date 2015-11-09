using System.Collections;

namespace SwiftClient.Extensions
{
    internal static class IListExtensions
    {
        public static void MoveFirstToLast(this IList list)
        {
            var count = list.Count;

            if (count < 2) return;

            var item = list[0];
            list.RemoveAt(0);
            list.Insert(count - 1, item);
        }
    }
}
