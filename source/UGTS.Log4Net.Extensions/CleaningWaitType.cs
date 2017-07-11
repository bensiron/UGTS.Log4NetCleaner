namespace UGTS.Log4Net.Extensions
{
    /// <summary>
    /// The waiting strategy to use for the log directory cleaning task
    /// </summary>
    public enum CleaningWaitType
    {
        /// <summary>
        /// Never wait for the task to complete - run asynchronously
        /// </summary>
        Never,
        /// <summary>
        /// Only wait for the task to complete if it was run on the first logging call for the process
        /// </summary>
        FirstTimeOnly,
        /// <summary>
        /// Always wait for the task to complete - run synchronously
        /// </summary>
        Always
    }
}