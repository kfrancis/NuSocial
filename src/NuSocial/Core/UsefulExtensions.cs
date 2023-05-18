namespace NuSocial.Core
{
    public static class ObservableCollectionExtensions
    {
        public static void RemoveLastN<T>(this ObservableCollection<T> collection, int n)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (n < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n), "n must be non-negative");
            }

            int removeCount = Math.Min(n, collection.Count);

            for (int i = 0; i < removeCount; i++)
            {
                collection.RemoveAt(collection.Count - 1);
            }
        }
    }
}
