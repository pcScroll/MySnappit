using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

public class QuartzHostedService : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobFactory _jobFactory;
    private IScheduler _scheduler;

    public QuartzHostedService(ISchedulerFactory schedulerFactory, IJobFactory jobFactory)
    {
        _schedulerFactory = schedulerFactory;
        _jobFactory = jobFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Initialize and start the scheduler
        _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        _scheduler.JobFactory = _jobFactory;

        // Configure jobs and triggers
        var job = JobBuilder.Create<MyJob>()
            .WithIdentity("MyJob")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("MyJobTrigger")
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(10)
                .RepeatForever())
            .Build();

        await _scheduler.ScheduleJob(job, trigger, cancellationToken);
        await _scheduler.Start(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
        }
    }
}



using Quartz;
using System;
using System.Threading.Tasks;

public class MyJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine($"Job executed at {DateTime.Now}");
        return Task.CompletedTask;
    }
}


using Quartz;
using Quartz.Spi;
using System;

public class SimpleJobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SimpleJobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return (IJob)_serviceProvider.GetService(bundle.JobDetail.JobType);
    }

    public void ReturnJob(IJob job)
    {
        // Optional: Cleanup if necessary
    }
}




using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Spi;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    // Add Quartz services
    services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
    services.AddSingleton<IJobFactory, SimpleJobFactory>();
    services.AddSingleton<MyJob>();

    // Register the hosted service
    services.AddHostedService<QuartzHostedService>();
});

var app = builder.Build();
await app.RunAsync();
