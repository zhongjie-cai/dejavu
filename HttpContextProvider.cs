using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Dejavu
{
    /// <summary>
    /// This provider writes and reads context from a current HTTP session
    /// </summary>
    public class HttpContextProvider : IProvideContext
    {
        private const string CONTEXT_ID_HEADER_PATTERN = "dci{0}"; // dejavu-context-id
        private const string CONTEXT_TYPE_RECORDING = "r";
        private const string CONTEXT_TYPE_REPLAYING = "p";
        private const string CONTEXT_ENTRY_INDEX_KEY = "dcei"; // dejavu-context-entry-index
        private const string CONTEXT_ENTRY_CONTENT_KEY_PATTERN = "dcec-{0}-{1}"; // dejavu-context-entry-content

        private readonly IHttpContextAccessor m_httpContextAccessor;
        private readonly ISerializeObject m_objectSerializer;

        public HttpContextProvider(
            IHttpContextAccessor httpContextAccessor,
            ISerializeObject objectSerializer
        )
        {
            m_httpContextAccessor = httpContextAccessor;
            m_objectSerializer = objectSerializer;
        }

        private string GetContextID(string contextType)
        {
            var headerName = string.Format(
                CONTEXT_ID_HEADER_PATTERN,
                contextType
            );
            var headerValueFound = m_httpContextAccessor
                .HttpContext
                .Request
                .Headers
                .TryGetValue(
                    headerName,
                    out StringValues contextIDValue
                );
            if (!headerValueFound || contextIDValue.Count == 0)
            {
                return string.Empty;
            }
            return contextIDValue[0];
        }

        public string GetRecordID()
        {
            return GetContextID(CONTEXT_TYPE_RECORDING);
        }

        public string GetReplayID()
        {
            return GetContextID(CONTEXT_TYPE_REPLAYING);
        }

        public void InsertEntry(string contextID, int threadIndex, ContextEntry contextEntry)
        {
            var entryIndex = GetNextEntryIndex();
            var entryContentKey = GetEntryContentKey(threadIndex, entryIndex);
            var entryContent = m_objectSerializer.Serialize(contextEntry);
            m_httpContextAccessor
                .HttpContext
                .Response
                .Headers[entryContentKey] = entryContent;
        }

        public ContextEntry GetNextEntry(string contextID, int threadIndex)
        {
            var entryIndex = GetNextEntryIndex();
            var entryContentKey = GetEntryContentKey(threadIndex, entryIndex);
            var entryContent = (string)m_httpContextAccessor
                .HttpContext
                .Request
                .Headers[entryContentKey];
            if (string.IsNullOrEmpty(entryContent))
            {
                return null;
            }
            var contextEntry = m_objectSerializer.Deserialize(
                entryContent,
                typeof(ContextEntry)
            );
            return (ContextEntry)contextEntry;
        }

        private int GetNextEntryIndex()
        {
            int entryIndex = 0;
            var entryIndexFound = m_httpContextAccessor
                .HttpContext
                .Items
                .TryGetValue(
                    CONTEXT_ENTRY_INDEX_KEY,
                    out object entryIndexValue
                );
            if (entryIndexFound)
            {
                entryIndex = (int)entryIndexValue + 1;
            }
            m_httpContextAccessor
                .HttpContext
                .Items[CONTEXT_ENTRY_INDEX_KEY] = entryIndex;
            return entryIndex;
        }

        private string GetEntryContentKey(int threadIndex, int entryIndex)
        {
            return string.Format(
                CONTEXT_ENTRY_CONTENT_KEY_PATTERN,
                threadIndex,
                entryIndex
            );
        }
    }
}
