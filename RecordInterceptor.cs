using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Dejavu
{
    /// <summary>
    /// This class processes the interceptions of all registered interface methods for recording of context entries
    /// </summary>
    public class RecordInterceptor : IInterceptor
    {
        private readonly IProvideContext m_contextProvider;
        private readonly ISerializeObject m_objectSerializer;
        private readonly ILogger m_logger;

        private readonly IDictionary<int, int> m_threadIDMap = new Dictionary<int, int>();

        public RecordInterceptor(
            IProvideContext contextProvider,
            ISerializeObject objectSerializer,
            ILogger logger
        )
        {
            m_contextProvider = contextProvider;
            m_objectSerializer = objectSerializer;
            m_logger = logger;
        }

        private int GetThreadIndex()
        {
            var threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (!m_threadIDMap.TryGetValue(threadID, out int threadIndex))
            {
                threadIndex = m_threadIDMap.Count;
                m_threadIDMap[threadID] = threadIndex;
            }
            return threadIndex;
        }

        /// <summary>
        /// This method intercepts an invocation and processes the recording of a context entry if necessary
        /// </summary>
        /// <param name="invocation">The intercepted invocation of a certain class and method.</param>
        public void Intercept(IInvocation invocation)
        {
            var contextID = m_contextProvider.GetRecordID();
            if (string.IsNullOrEmpty(contextID))
            {
                invocation.Proceed();
                return;
            }
            var threadIndex = GetThreadIndex();
            var inputParameters = new List<string>();
            for (int parameterIndex = 0; parameterIndex < invocation.Arguments.Length; parameterIndex++)
            {
                var inputParameter = m_objectSerializer.Serialize(
                    invocation.Arguments[parameterIndex]
                );
                inputParameters.Add(inputParameter);
            }
            var contextEntry = new ContextEntry
            {
                ClassName = invocation.TargetType.AssemblyQualifiedName,
                MethodName = invocation.Method.Name,
                InputParameters = inputParameters.ToArray(),
                ReturnValue = string.Empty,
                ErrorType = string.Empty,
            };
            m_contextProvider.InsertEntry(contextID, threadIndex, contextEntry); // records the entering
            m_logger.LogTrace(
                "Successfully recorded {class}.{method} entering with {count} parameters",
                contextEntry.ClassName,
                contextEntry.MethodName,
                invocation.Arguments.Length
            );
            contextEntry = new ContextEntry
            {
                ClassName = invocation.TargetType.AssemblyQualifiedName,
                MethodName = invocation.Method.Name,
                InputParameters = null,
                ReturnValue = string.Empty,
                ErrorType = string.Empty,
            };
            try
            {
                invocation.Proceed();
                var returnValue = m_objectSerializer.Serialize(invocation.ReturnValue);
                contextEntry.ReturnValue = returnValue;
            }
            catch (Exception exception)
            {
                var returnValue = m_objectSerializer.Serialize(exception);
                var errorType = exception.GetType().AssemblyQualifiedName;
                contextEntry.ReturnValue = returnValue;
                contextEntry.ErrorType = errorType;
            }
            m_contextProvider.InsertEntry(contextID, threadIndex, contextEntry); // records the exiting
            m_logger.LogTrace(
                "Successfully recorded {class}.{method} existing with {action}",
                contextEntry.ClassName,
                contextEntry.MethodName,
                contextEntry.ErrorType == string.Empty ? "value returned" : "exception thrown"
            );
        }
    }
}
