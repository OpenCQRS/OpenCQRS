﻿using System;
using System.Linq;
using Kledex.Configuration;
using Kledex.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Kledex.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Kledex. 
        /// Pass any of the types that are contained in the assemblies where your messages and handlers are.
        /// One for each assembly.
        /// e.g. typeOf(CreatePost) where CreatePost is one of your commands.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="types">The types.</param>
        public static IKledexServiceBuilder AddKledex(this IServiceCollection services, params Type[] types)
        {
            return AddKledex(services, opt => {}, types);
        }

        /// <summary>
        /// Adds Kledex. 
        /// Pass any of the types that are contained in the assemblies where your messages and handlers are.
        /// One for each assembly.
        /// e.g. typeOf(CreatePost) where CreatePost is one of your commands.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="setupAction">The options.</param>
        /// <param name="types">The types.</param>
        public static IKledexServiceBuilder AddKledex(this IServiceCollection services, Action<MainOptions> setupAction, params Type[] types)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var typeList = types.ToList();
            typeList.Add(typeof(IDispatcher));

            services.Scan(s => s
                .FromAssembliesOf(typeList)
                .AddClasses()
                .AsImplementedInterfaces());

            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));

            services.AddAutoMapper(typeList);

            services.Configure(setupAction);

            return new KledexServiceBuilder(services);
        }
    }
}
