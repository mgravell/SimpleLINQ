namespace SimpleLINQ
{
    /// <summary>
    /// Describes common aggregate operations
    /// </summary>
    public enum Aggregate
    {
        /// <summary>
        /// Returns the first element in a query, or throws if there are no elements
        /// </summary>
        First,
        /// <summary>
        /// Returns the first element in a query, or returns the type's default if there are no elements
        /// </summary>
        FirstOrDefault,
        /// <summary>
        /// Returns the solo element in a query, throws if there are multiple elements, or returns the type's default if there are no elements
        /// </summary>
        Single,
        /// <summary>
        /// Returns the solo element in a query, or throws if there is not exactly one element
        /// </summary>
        SingleOrDefault,
        /// <summary>
        /// Returns the number of elements in a query
        /// </summary>
        Count,
        /// <summary>
        /// Returns the median value of the elements in a query
        /// </summary>
        Average,
        /// <summary>
        /// Returns the least value of the elements in a query
        /// </summary>
        Minimum,
        /// <summary>
        /// Returns the greatest value of the elements in a query
        /// </summary>
        Maximum,
        /// <summary>
        /// Returns the total sum value of the elements in a query
        /// </summary>
        Sum,
        /// <summary>
        /// Indicates whether a query is non-empty
        /// </summary>
        Any,
        /// <summary>
        /// Indicates whether a query is empty
        /// </summary>
        NotAny,
    }
}
