namespace UGTS.Log4Net.Extensions
{
    /// <summary>
    /// The waiting strategy to use when running tasks
    /// </summary>
    public enum WaitType
    {
        /// <summary>
        /// Never wait for the task to complete - run asynchronously
        /// </summary>
        Never,
        /// <summary>
        /// Always wait for the task to complete - run synchronously
        /// </summary>
        Always
    }
}