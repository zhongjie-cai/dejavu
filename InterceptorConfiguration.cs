using System;
using System.Collections.Generic;
using System.Reflection;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Castle.MicroKernel.ModelBuilder;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Microsoft.Extensions.Logging;

namespace Dejavu
{
    /// <summary>
    /// This class configures the interception of given types registered for Windsor.Castle IoC container
    /// </summary>
    public class InterceptorConfiguration : IContributeComponentModelConstruction
    {
        private readonly IDictionary<string, bool> m_interceptingAssemblies = new Dictionary<string, bool>();
        private readonly IDictionary<string, bool> m_interceptingTypes = new Dictionary<string, bool>();

        internal InterceptorConfiguration(
            Assembly callingAssembly,
            IEnumerable<Assembly> interceptingAssemblies,
            IEnumerable<Type> interceptingTypes
        )
        {
            if (interceptingAssemblies != null)
            {
                foreach (var interceptingAssembly in interceptingAssemblies)
                {
                    m_interceptingAssemblies[interceptingAssembly.FullName] = true;
                }
            }
            if (interceptingTypes != null)
            {
                foreach (var interceptingType in interceptingTypes)
                {
                    m_interceptingTypes[interceptingType.AssemblyQualifiedName] = true;
                }
            }
            if (m_interceptingAssemblies.Count == 0 && m_interceptingTypes.Count == 0)
            {
                m_interceptingAssemblies[callingAssembly.FullName] = true;
            }
        }

        /// <summary>
        /// This method configures the given container, registering with interceptors, context provider and object serializer as given
        /// </summary>
        /// <typeparam name="TContextProvider">The implementation of <see cref="IProvideContext"/></typeparam>
        /// <typeparam name="TObjectSerializer">The implementation of <see cref="ISerializeObject"/></typeparam>
        /// <param name="container">The Windsor.Castle container to be configured</param>
        /// <param name="interceptingAssemblies">A list of assemblies to be intercepted for record and replay</param>
        /// <param name="interceptingTypes">A list of types to be intercepted for record and replay</param>
        public static IWindsorContainer ConfigureFor<TContextProvider, TObjectSerializer>(
            IWindsorContainer container,
            IEnumerable<Assembly> interceptingAssemblies = null,
            IEnumerable<Type> interceptingTypes = null
        )
            where TContextProvider : IProvideContext
            where TObjectSerializer : ISerializeObject
        {
            container.Register(Component.For<IInterceptor>().ImplementedBy<RecordInterceptor>().LifestyleSingleton());
            container.Register(Component.For<IInterceptor>().ImplementedBy<ReplayInterceptor>().LifestyleSingleton());
            container.Register(Component.For<IProvideContext>().ImplementedBy<TContextProvider>().LifestyleSingleton());
            container.Register(Component.For<ISerializeObject>().ImplementedBy<TObjectSerializer>().LifestyleSingleton());
            var callingAssembly = Assembly.GetCallingAssembly();
            var contributor = new InterceptorConfiguration(
                callingAssembly,
                interceptingAssemblies,
                interceptingTypes
            );
            container.Kernel.ComponentModelBuilder.AddContributor(
                contributor
            );
            return container;
        }

        /// <summary>
        /// This method configures the given container, registering with interceptors, context provider and object serializer as given
        /// </summary>
        /// <param name="container">The Windsor.Castle container to be configured</param>
        /// <param name="contextProvider">An instance of IProvideContext implementation to be registered</param>
        /// <param name="objectSerializer">An instance of ISerializeObject implementation to be registered</param>
        /// <param name="interceptingAssemblies">A list of assemblies to be intercepted for record and replay</param>
        /// <param name="interceptingTypes">A list of types to be intercepted for record and replay</param>
        public static IWindsorContainer ConfigureFor(
            IWindsorContainer container,
            IProvideContext contextProvider,
            ISerializeObject objectSerializer,
            IEnumerable<Assembly> interceptingAssemblies = null,
            IEnumerable<Type> interceptingTypes = null
        )
        {
            container.Register(Component.For<IInterceptor>().ImplementedBy<RecordInterceptor>().LifestyleSingleton());
            container.Register(Component.For<IInterceptor>().ImplementedBy<ReplayInterceptor>().LifestyleSingleton());
            container.Register(Component.For<IProvideContext>().Instance(contextProvider).LifestyleSingleton());
            container.Register(Component.For<ISerializeObject>().Instance(objectSerializer).LifestyleSingleton());
            var callingAssembly = Assembly.GetCallingAssembly();
            var contributor = new InterceptorConfiguration(
                callingAssembly,
                interceptingAssemblies,
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
                if (!ShouldIntercept(service, m_interceptingAssemblies, m_interceptingTypes))
                {
                    continue;
                }
                model.Interceptors.Add(InterceptorReference.ForType<RecordInterceptor>());
                model.Interceptors.Add(InterceptorReference.ForType<ReplayInterceptor>());
                return;
            }
        }

        private static bool ShouldIntercept(
            Type service,
            IDictionary<string, bool> interceptingAssemblies,
            IDictionary<string, bool> interceptingTypes
        )
        {
            if (interceptingAssemblies.Count > 0)
            {
                if (interceptingAssemblies.ContainsKey(service.Assembly.FullName))
                {
                    return true;
                }
            }
            if (interceptingTypes.Count > 0)
            {
                if (interceptingTypes.ContainsKey(service.AssemblyQualifiedName))
                {
                    return true;
                }
            }
            return false;
        }
    }
}