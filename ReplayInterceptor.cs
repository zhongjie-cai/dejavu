using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Dejavu
{
    /// <summary>
    /// This class processes the overrides of all registered interface methods for replaying of context entries
    /// </summary>
    public class ReplayInterceptor : IInterceptor
    {
        private readonly IProvideContext m_contextProvider;
        private readonly ISerializeObject m_objectSerializer;
        private readonly ILogger m_logger;

        private readonly IDictionary<int, int> m_threadIDMap = new Dictionary<int, int>();

        public ReplayInterceptor(
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
        /// This method intercepts an invocation and processes the replaying of a context entry if necessary
        /// </summary>
        /// <param name="invocation">The intercepted invocation of a certain class and method.</param>
        public void Intercept(IInvocation invocation)
        {
            var contextID = m_contextProvider.GetReplayID();
            if (string.IsNullOrEmpty(contextID))
            {
                invocation.Proceed();
                return;
            }
            var threadIndex = GetThreadIndex();
            var contextEntry = m_contextProvider.GetNextEntry(contextID, threadIndex);
            var success = ProcessEntryOverride(
                invocation,
                contextEntry
            );
            invocation.Proceed();
            if (!success)
            {
                return;
            }
            contextEntry = m_contextProvider.GetNextEntry(contextID, threadIndex);
            ProcessReturnOverride(
                invocation,
                contextEntry
            );
        }

        private bool ProcessEntryOverride(
            IInvocation invocation,
            ContextEntry contextEntry
        )
        {
            if (contextEntry == null)
            {
                // something went wrong here, so skip this override and continue
                m_logger.LogWarning(
                    "Failed to override {class}.{method} due to context entry missing",
                    invocation.TargetType.AssemblyQualifiedName,
                    invocation.Method.Name
                );
                return false;
            }
            var className = invocation.TargetType.AssemblyQualifiedName;
            if (!string.Equals(contextEntry.ClassName, className))
            {
                // something went wrong here, so skip this override and continue
                m_logger.LogWarning(
                    "Failed to override {class}.{method} due to class name mismatch: actual class name is {actual}",
                    contextEntry.ClassName,
                    contextEntry.MethodName,
                    className
                );
                return false;
            }
            var methodName = invocation.Method.Name;
            if (!string.Equals(contextEntry.MethodName, methodName))
            {
                // something went wrong here, so skip this override and continue
                m_logger.LogWarning(
                    "Failed to override {class}.{method} due to method name mismatch: actual method name is {actual}",
                    contextEntry.ClassName,
                    contextEntry.MethodName,
                    methodName
                );
                return false;
            }
            if (contextEntry.InputParameters.Length != invocation.Arguments.Length)
            {
                // something went wrong here, so skip this override and continue
                m_logger.LogWarning(
                    "Failed to override {class}.{method} due to argument count mismatch: expect {expect} but got {actual}",
                    contextEntry.ClassName,
                    contextEntry.MethodName,
                    invocation.Arguments.Length,
                    contextEntry.InputParameters.Length
                );
                return false;
            }
            for (int parameterIndex = 0; parameterIndex < invocation.Arguments.Length; parameterIndex++)
            {
                try
                {
                    var argument = invocation.GetArgumentValue(parameterIndex);
                    var argumentType = argument.GetType();
                    var inputParameter = m_objectSerializer.Deserialize(
                        contextEntry.InputParameters[parameterIndex],
                        argumentType
                    );
                    invocation.SetArgumentValue(
                        parameterIndex,
                        inputParameter
                    );
                }
                catch (Exception exception)
                {
                    // something went wrong here, so skip this override and continue
                    m_logger.LogWarning(
                        exception,
                        "Failed to override {class}.{method} due to argument override failure at index {index}",
                        contextEntry.ClassName,
                        contextEntry.MethodName,
                        parameterIndex
                    );
                    return false;
                }
            }
            m_logger.LogTrace(
                "Successfully overrided {class}.{method} entering with {count} parameters",
                contextEntry.ClassName,
                contextEntry.MethodName,
                contextEntry.InputParameters.Length
            );
            return true;
        }
        
        private bool ProcessReturnOverride(
            IInvocation invocation,
            ContextEntry contextEntry
        )
        {
            if (contextEntry == null)
            {
                // something went wrong here, so skip this override and continue
                m_logger.LogWarning(
                    "Failed to override {class}.{method} due to context entry missing",
                    invocation.TargetType.AssemblyQualifiedName,
                    invocation.Method.Name
                );
                return false;
            }
            var className = invocation.TargetType.AssemblyQualifiedName;
            if (!string.Equals(contextEntry.ClassName, className))
            {
                // something went wrong here, so skip this override and continue
                m_logger.LogWarning(
                    "Failed to override {class}.{method} due to class name mismatch: actual class name is {actual}",
                    contextEntry.ClassName,
                    contextEntry.MethodName,
                    className
                );
                return false;
            }
            var methodName = invocation.Method.Name;
            if (!string.Equals(contextEntry.MethodName, methodName))
            {
                // something went wrong here, so skip this override and continue
                m_logger.LogWarning(
                    "Failed to override {class}.{method} due to method name mismatch: actual method name is {actual}",
                    contextEntry.ClassName,
                    contextEntry.MethodName,
                    methodName
                );
                return false;
            }
            if (contextEntry.ErrorType == string.Empty)
            {
                var returnType = invocation.ReturnValue.GetType();
                invocation.ReturnValue = m_objectSerializer.Deserialize(
                    contextEntry.ReturnValue,
                    returnType
                );
                m_logger.LogTrace(
                    "Successfully replayed {class}.{method} exiting with value returned",
                    contextEntry.ClassName,
                    contextEntry.MethodName
                );
            }
            else
            {
                var errorType = Type.GetType(
                    contextEntry.ErrorType,
                    true,
                    false
                );
                var exception = m_objectSerializer.Deserialize(
                    contextEntry.ReturnValue,
                    errorType
                );
                m_logger.LogTrace(
                    "Successfully replayed {class}.{method} entering with exception thrown",
                    contextEntry.ClassName,
                    contextEntry.MethodName
                );
                throw (Exception)exception;
            }
            return true;
        }
    }
}
