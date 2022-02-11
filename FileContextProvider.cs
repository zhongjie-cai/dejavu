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

        private static object ms_writeLock = new object();
        private static IDictionary<string, int> ms_entryIndex = new Dictionary<string, int>();

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
            return GetContextFile(CONTEXT_TYPE_RECORDING);
        }
        
        public string GetReplayID()
        {
            return GetContextFile(CONTEXT_TYPE_REPLAYING);
        }

        private static string GetNextEntryFileName(string contextID, int threadIndex)
        {
            lock (ms_writeLock)
            {
                var filePrefix = $"{contextID}_{threadIndex}";
                if (!ms_entryIndex.TryGetValue(filePrefix, out int entryIndex))
                {
                    entryIndex = 0;
                }
                var fileName = $"{filePrefix}_{entryIndex}";
                ms_entryIndex[filePrefix] = entryIndex + 1;
                return fileName;
            }
        }
        
        public void InsertEntry(string contextID, int threadIndex, ContextEntry contextEntry)
        {
            var entryContent = m_objectSerializer.Serialize(contextEntry);
            var fileName = GetNextEntryFileName(contextID, threadIndex);
            using (var streamWriter = new StreamWriter(fileName))
            {
                streamWriter.WriteLine(entryContent);
                streamWriter.Flush();
            }
        }

        public ContextEntry GetNextEntry(string contextID, int threadIndex)
        {
            var fileName = GetNextEntryFileName(contextID, threadIndex);
            using (var streamReader = new StreamReader(fileName))
            {
                var entryContent = streamReader.ReadToEnd();
                var contextEntry = m_objectSerializer.Deserialize(
                    entryContent,
                    typeof(ContextEntry)
                );
                return (ContextEntry)contextEntry;
            }
        }
    }
}
