using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Dejavu
{
    /// <summary>
    /// This provider writes and reads context from local file system according to file name given from environment variables
    /// </summary>
    public class FileContextProvider : IProvideContext
    {
        private const string CONTEXT_ID_KEY_PATTERN = "dci{0}"; // dejavu-context-id
        private const string CONTEXT_TYPE_RECORDING = "r";
        private const string CONTEXT_TYPE_REPLAYING = "p";

        private StreamWriter m_streamWriter;
        private IDictionary<int, Queue<ContextEntry>> m_contextEntryCache;
        private static object ms_writeLock = new object();


        private readonly IConfiguration m_configuration;
        private readonly ISerializeObject m_objectSerializer;

        public FileContextProvider(
            IConfiguration configuration,
            ISerializeObject objectSerializer
        )
        {
            m_configuration = configuration;
            m_objectSerializer = objectSerializer;
        }

        private string GetContextFile(string contextType)
        {
            var environmentVariableName = string.Format(
                CONTEXT_ID_KEY_PATTERN,
                contextType
            );
            var fileName = m_configuration[environmentVariableName];
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }
            return fileName;
        }

        private IDictionary<int, Queue<ContextEntry>> LoadContextEntryFromFile(string fileName)
        {
            var cache = new Dictionary<int, Queue<ContextEntry>>();
            using (var streamReader = new StreamReader(fileName))
            {
                while (true)
                {
                    var indexValue = streamReader.ReadLine();
                    if (string.IsNullOrEmpty(indexValue) ||
                        !int.TryParse(indexValue, out int threadIndex))
                    {
                        break;
                    }
                    if (!cache.TryGetValue(threadIndex, out Queue<ContextEntry> queue))
                    {
                        queue = new Queue<ContextEntry>();
                        cache[threadIndex] = queue;
                    }
                    var entryContent = streamReader.ReadLine();
                    if (string.IsNullOrEmpty(entryContent))
                    {
                        queue.Enqueue(null);
                    }
                    else
                    {
                        var contextEntry = m_objectSerializer.Deserialize(
                            entryContent,
                            typeof(ContextEntry)
                        );
                        queue.Enqueue((ContextEntry)contextEntry);
                    }
                }
            }
            return cache;
        }

        public string GetRecordID()
        {
            var fileName = GetContextFile(CONTEXT_TYPE_RECORDING);
            if (!string.IsNullOrEmpty(fileName) &&
                m_streamWriter == null)
            {
                m_streamWriter = new StreamWriter(fileName);
            }
            return fileName;
        }
        
        public string GetReplayID()
        {
            var fileName = GetContextFile(CONTEXT_TYPE_REPLAYING);
            if (!string.IsNullOrEmpty(fileName) &&
                m_contextEntryCache == null)
            {
                m_contextEntryCache = LoadContextEntryFromFile(fileName);
            }
            return fileName;
        }
        
        public void InsertEntry(string contextID, int threadIndex, ContextEntry contextEntry)
        {
            if (m_streamWriter == null)
            {
                return;
            }
            var entryContent = m_objectSerializer.Serialize(contextEntry);
            lock (ms_writeLock)
            {
                m_streamWriter.WriteLine(threadIndex);
                m_streamWriter.WriteLine(entryContent);
                m_streamWriter.Flush();
            }
        }

        public ContextEntry GetNextEntry(string contextID, int threadIndex)
        {
            if (m_contextEntryCache == null)
            {
                return null;
            }
            if (!m_contextEntryCache.TryGetValue(
                threadIndex,
                out Queue<ContextEntry> contextEntryQueue
            ))
            {
                return null;
            }
            if (contextEntryQueue.Count == 0)
            {
                return null;
            }
            return contextEntryQueue.Dequeue();
        }
    }
}
