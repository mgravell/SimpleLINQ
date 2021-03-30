namespace SimpleLINQ.Internal
{
    internal static class InternalQueryExtensions
    {
        public static Query RemoveDistinctIfNoSkip(this Query query)
            => query.Distinct && query.Skip == 0 ? query.ApplyDistinct(false) : query;
    }
}
