using System;
using System.Collections.Generic;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Castle.MicroKernel.ModelBuilder;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace Dejavu
{
    /// <summary>
    /// This class configures the interception of given types registered for Windsor.Castle IoC container
    /// </summary>
    public class InterceptorConfiguration : IContributeComponentModelConstruction
    {
        private readonly IDictionary<string, bool> m_interceptingTypes = new Dictionary<string, bool>();

        internal InterceptorConfiguration(IEnumerable<Type> interceptingTypes)
        {
            foreach (var interceptingType in interceptingTypes)
            {
                m_interceptingTypes[interceptingType.AssemblyQualifiedName] = true;
            }
        }

        /// <summary>
        /// This method configures the given container, registering with interceptors, context provider and object serializer as given
        /// </summary>
        /// <typeparam name="TContextProvider">The implementation of <see cref="IProvideContext"/></typeparam>
        /// <typeparam name="TObjectSerializer">The implementation of <see cref="ISerializeObject"/></typeparam>
        /// <param name="container">The Windsor.Castle container to be configured</param>
        /// <param name="interceptingTypes">A list of types to be intercepted for record and replay; if nothing is provided, then all types are intercepted</param>
        public static IWindsorContainer ConfigureFor<TContextProvider, TObjectSerializer>(
            IWindsorContainer container,
            params Type[] interceptingTypes
        )
            where TContextProvider : IProvideContext
            where TObjectSerializer : ISerializeObject
        {
            container.Register(Component.For<IInterceptor>().ImplementedBy<RecordInterceptor>().LifestyleSingleton());
            container.Register(Component.For<IInterceptor>().ImplementedBy<ReplayInterceptor>().LifestyleSingleton());
            container.Register(Component.For<IProvideContext>().ImplementedBy<TContextProvider>().LifestyleSingleton());
            container.Register(Component.For<ISerializeObject>().ImplementedBy<TObjectSerializer>().LifestyleSingleton());
            var contributor = new InterceptorConfiguration(
                interceptingTypes
            );
            container.Kernel.ComponentModelBuilder.AddContributor(
                contributor
            );
            return container;
        }

        /// <summary>
        /// Processes the component model during DI instantiation to inject interceptors according to registrations
        /// </summary>
        public void ProcessModel(IKernel kernel, ComponentModel model)
        {
            foreach (var service in model.Services)
            {
                var matches = m_interceptingTypes.Count == 0
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