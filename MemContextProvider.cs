using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Dejavu
{
    /// <summary>
    /// This provider writes and reads context from runtime memory and allows dumping of data manually
    /// </summary>
    public class MemContextProvider : IProvideContext
    {
        private IDictionary<int, Queue<ContextEntry>> m_contextEntryCache;
        private static object ms_writeLock = new object();

        private string m_recordID;
        private string m_replayID;


        private readonly ISerializeObject m_objectSerializer;

        public MemContextProvider(
            ISerializeObject objectSerializer
        )
        {
            m_objectSerializer = objectSerializer;
        }

        private IDictionary<int, Queue<ContextEntry>> LoadContextEntry(StreamReader streamReader)
        {
            var cache = new Dictionary<int, Queue<ContextEntry>>();
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
            return cache;
        }

        private void SaveContextEntry(StreamWriter streamWriter, IDictionary<int, Queue<ContextEntry>> cache)
        {
            foreach (var cacheItem in cache)
            {
                var threadIndex = cacheItem.Key;
                var queue = cacheItem.Value;
                while (queue.Count > 0)
                {
                    var contextEntry = queue.Dequeue();
                    var entryContent = m_objectSerializer.Serialize(contextEntry);
                    streamWriter.WriteLine(threadIndex);
                    streamWriter.WriteLine(entryContent);
                }
            }
            streamWriter.Flush();
        }

        public bool StartRecording(string recordID)
        {
            if (!string.IsNullOrEmpty(m_recordID))
            {
                return false;
            }
            m_recordID = recordID;
            m_contextEntryCache = new Dictionary<int, Queue<ContextEntry>>();
            return true;
        }

        public bool StopRecording(string recordID, StreamWriter streamWriter)
        {
            if (m_recordID != recordID)
            {
                return false;
            }
            lock (ms_writeLock)
            {
                SaveContextEntry(
                    streamWriter,
                    m_contextEntryCache
                );
                m_recordID = string.Empty;
                m_contextEntryCache = null;
                return true;
            }
        }

        public string GetRecordID()
        {
            return m_recordID;
        }

        public bool StartReplaying(string replayID, StreamReader streamReader)
        {
            if (!string.IsNullOrEmpty(m_replayID))
            {
                return false;
            }
            m_replayID = replayID;
            m_contextEntryCache = LoadContextEntry(streamReader);
            return true;
        }

        public bool StopReplaying(string replayID)
        {
            if (m_replayID != replayID)
            {
                return false;
            }
            m_replayID = string.Empty;
            m_contextEntryCache = null;
            return true;
        }
        
        public string GetReplayID()
        {
            return m_replayID;
        }
        
        public void InsertEntry(string contextID, int threadIndex, ContextEntry contextEntry)
        {
            if (m_contextEntryCache == null)
            {
                return;
            }
            lock (ms_writeLock)
            {
                if (!m_contextEntryCache.TryGetValue(threadIndex, out Queue<ContextEntry> queue))
                {
                    queue = new Queue<ContextEntry>();
                    m_contextEntryCache[threadIndex] = queue;
                }
                queue.Enqueue(contextEntry);
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
