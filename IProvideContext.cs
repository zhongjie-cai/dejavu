namespace Dejavu
{
    /// <summary>
    /// This interface acts as the storage provider needed for context operations, which is the fundamental part of recording and replaying functionality
    /// </summary>
    public interface IProvideContext
    {
        /// <summary>
        /// This method should return the current recording context ID if a recording session is set; otherwise null should be returned
        /// </summary>
        string GetRecordID();
        
        /// <summary>
        /// This method should return the current replaying context ID if a replaying session is set; otherwise null should be returned
        /// </summary>
        string GetReplayID();
        
        /// <summary>
        /// This method should insert a new context entry into internal storage, which is recorded from a registered interface method invocation
        /// </summary>
        void InsertEntry(string contextID, int threadIndex, ContextEntry contextEntry);

        /// <summary>
        /// This method should retrieve a next context entry from internal storage, which is to be replayed for a registered interface method invocation
        /// </summary>
        ContextEntry GetNextEntry(string contextID, int threadIndex);
    }
}
