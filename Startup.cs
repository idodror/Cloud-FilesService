using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FilesService.Helpers;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection.ServiceCollection;
using RawRabbit.Enrichers.GlobalExecutionId;
using RawRabbit.Enrichers.HttpContext;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Operations.MessageSequence;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Instantiation;
using RawRabbit.Enrichers.MessageContext.Context;

namespace FilesService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
            .AddRawRabbit(new RawRabbitOptions {
                ClientConfiguration = 
                    RawRabbit.Common
                    .ConnectionStringParser.Parse("pcwvalie:SMGHv_ilwrd9K1D9YlhZqSV-CvdIjjgt@golden-kangaroo.rmq.cloudamqp.com/pcwvalie"),
                Plugins = p => p.UseGlobalExecutionId().UseMessageContext<MessageContext>()
            })
            .AddSingleton<IRedisConnectionFactory,RedisConnectionFactory>()
            .AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
