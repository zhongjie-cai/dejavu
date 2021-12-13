using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.ModelBuilder;

namespace Dejavu
{
    /// <summary>
    /// This class configures the interception of given types registered for Windsor.Castle IoC container
    /// </summary>
    public class InterceptorConfiguration : IContributeComponentModelConstruction
    {
        private readonly IDictionary<string, bool> m_interceptingTypes;

        public InterceptorConfiguration()
        {
            m_interceptingTypes = null;
        }

        public InterceptorConfiguration(IEnumerable<Type> interceptingTypes)
        {
            m_interceptingTypes = interceptingTypes.ToDictionary<Type, string, bool>(
                type => type.AssemblyQualifiedName,
                value => true
            );
        }

        /// <summary>
        /// Processes the component model during DI instantiation to inject interceptors according to registrations
        /// </summary>
        public void ProcessModel(IKernel kernel, ComponentModel model)
        {
            foreach (var service in model.Services)
            {
                var matches = m_interceptingTypes == null
                    ? true
                    : m_interceptingTypes.ContainsKey(service.AssemblyQualifiedName);
                if (matches)
                {
                    model.Interceptors.Add(InterceptorReference.ForType<RecordInterceptor>());
                    model.Interceptors.Add(InterceptorReference.ForType<ReplayInterceptor>());
                    return;
                }
            }
        }
    }
}