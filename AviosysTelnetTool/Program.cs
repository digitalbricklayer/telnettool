using System;
using System.Diagnostics;
using System.Threading;
using PrimS.Telnet;
using Quartz;
using Quartz.Impl;

namespace AviosysTelnetTool
{
    /// <summary>
    /// Entry point for the Aviosys telnet tool.
    /// </summary>
    class Program
    {
        internal const int ReadTimeoutInMs = 100;
        private readonly ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
        private readonly IScheduler scheduler;

        static void Main(string[] args)
        {
            new Program().Run(args);
        }

        public Program()
        {
            this.scheduler = this.schedulerFactory.GetScheduler();
        }

        internal static Client TelnetClient { get; private set; }

        private void Run(string[] args)
        {
            ScheduleStatusJob();
            ScheduleOnJob();
            ScheduleOffJob();

            using (TelnetClient = new Client("10.0.0.81", 23, new CancellationToken()))
            {
                this.scheduler.Start();
                Debug.Assert(TelnetClient.IsConnected);
                ReadWelcomeMessage();
                LoginToRemote();
                Console.Out.WriteLine("Press [Enter] to terminate.");
                Console.In.ReadLine();
            }

            this.scheduler.Shutdown(waitForJobsToComplete:true);
        }

        private void ScheduleOffJob()
        {
            IJobDetail job = JobBuilder.Create<PowerSwitchOffJob>()
                                       .WithIdentity("Switch Power Off", "Power")
                                       .Build();

            ITrigger trigger = TriggerBuilder.Create()
                                             .WithIdentity("Power Off Trigger", "Power")
                                             .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(18, 00))
                                             .Build();

            this.scheduler.ScheduleJob(job, trigger);
        }

        private void ScheduleOnJob()
        {
            IJobDetail job = JobBuilder.Create<PowerSwitchOnJob>()
                                       .WithIdentity("Switch Power On", "Power")
                                       .Build();

            ITrigger trigger = TriggerBuilder.Create()
                                             .WithIdentity("Power On Trigger", "Power")
                                             .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(08, 00))
                                             .Build();

            this.scheduler.ScheduleJob(job, trigger);
        }

        private void ScheduleStatusJob()
        {
            IJobDetail job = JobBuilder.Create<PowerSwitchStatusJob>()
                                       .WithIdentity("Switch Power Status", "Power")
                                       .Build();

            ITrigger trigger = TriggerBuilder.Create()
                                             .WithIdentity("Power Status Trigger", "Power")
                                             .StartNow()
                                             .WithSimpleSchedule(x => x.WithIntervalInSeconds(100).RepeatForever())
                                             .Build();

            this.scheduler.ScheduleJob(job, trigger);
        }

        private void LoginToRemote()
        {
            // Log into the unit using default user name and password
            TelnetClient.WriteLine("admin:12345678");
            string loginResponse = TelnetClient.TerminatedRead("->", TimeSpan.FromMilliseconds(ReadTimeoutInMs));
            Console.Out.WriteLine(loginResponse);
            Debug.Assert(loginResponse.TrimEnd(' ').EndsWith("->"));
        }

        private void ReadWelcomeMessage()
        {
            string welcomeMessage = TelnetClient.TerminatedRead("->", TimeSpan.FromMilliseconds(ReadTimeoutInMs));
            Console.Out.WriteLine(welcomeMessage);
            Debug.Assert(welcomeMessage.TrimEnd(' ').EndsWith("->"));
        }
    }

    [DisallowConcurrentExecution]
    internal class PowerSwitchStatusJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            Program.TelnetClient.WriteLine("getpower");
            Program.TelnetClient.TerminatedRead("->", TimeSpan.FromMilliseconds(Program.ReadTimeoutInMs));
            Console.Out.Write("*");
        }
    }

    [DisallowConcurrentExecution]
    internal class PowerSwitchOffJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            Program.TelnetClient.WriteLine("setpower=00000000");
            Program.TelnetClient.TerminatedRead("->", TimeSpan.FromMilliseconds(Program.ReadTimeoutInMs));
            Console.Out.Write("-");
        }
    }

    [DisallowConcurrentExecution]
    internal class PowerSwitchOnJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            Program.TelnetClient.WriteLine("setpower=11110000");
            Program.TelnetClient.TerminatedRead("->", TimeSpan.FromMilliseconds(Program.ReadTimeoutInMs));
            Console.Out.Write("+");
        }
    }
}
