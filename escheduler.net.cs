dotnet add package Quartz
dotnet add package Quartz.Plugins.TimeZoneConverter
dotnet add package Quartz.Serialization.Json
dotnet add package Quartz
dotnet add package Microsoft.Extensions.Hosting

======================================================================================
using System;
using System.Collections.Generic;
using System.Data.SqlClient; // Install System.Data.SqlClient NuGet package if not already installed
using System.Net.Mail;
using System.Text;

public class MailService
{
    public void SendEmail()
    {
        string subject = "Feeder Status Update";
        string toEmail = "recipient@example.com"; // Replace with recipient email
        string fromEmail = "sender@example.com"; // Replace with sender email
        string smtpHost = "smtp.example.com"; // Replace with your SMTP host
        int smtpPort = 587; // Replace with your SMTP port
        string smtpUsername = "smtp_username"; // Replace with your SMTP username
        string smtpPassword = "smtp_password"; // Replace with your SMTP password

        // Fetch data from the database
        var feederData = GetFeederDataFromDatabase();

        // Create the email content with the table
        StringBuilder emailBody = new StringBuilder();
        emailBody.Append("<html><body>");
        emailBody.Append("<h2>Feeder Numbers and Status</h2>");
        emailBody.Append("<table border='1' style='border-collapse: collapse; width: 50%; text-align: left;'>");
        emailBody.Append("<thead><tr style='background-color: #4CAF50; color: white;'>");
        emailBody.Append("<th>Location</th>");
        emailBody.Append("<th>Feeder Numbers</th>");
        emailBody.Append("<th>Status</th>");
        emailBody.Append("</tr></thead>");
        emailBody.Append("<tbody>");

        // Generate table rows dynamically with styling
        bool isAlternate = false; // To alternate row colors
        foreach (var feeder in feederData)
        {
            string rowColor = isAlternate ? "#f2f2f2" : "#ffffff"; // Light gray or white for alternating rows
            string statusColor = feeder.Status == "Active" ? "green" : "red"; // Green for Active, Red for Inactive

            emailBody.Append($"<tr style='background-color: {rowColor};'>");
            emailBody.Append($"<td>{feeder.Location}</td>");
            emailBody.Append($"<td>{feeder.FeederNumbers}</td>");
            emailBody.Append($"<td style='color: {statusColor}; font-weight: bold;'>{feeder.Status}</td>");
            emailBody.Append("</tr>");

            isAlternate = !isAlternate; // Toggle row color
        }

        emailBody.Append("</tbody>");
        emailBody.Append("</table>");
        emailBody.Append("</body></html>");

        // Create the MailMessage
        MailMessage mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail),
            Subject = subject,
            Body = emailBody.ToString(),
            IsBodyHtml = true
        };

        mailMessage.To.Add(toEmail);

        // Configure the SMTP client
        SmtpClient smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = true
        };

        // Send the email
        try
        {
            smtpClient.Send(mailMessage);
            Console.WriteLine("Email sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
        }
    }

    // Fetch data from the database
    private List<FeederInfo> GetFeederDataFromDatabase()
    {
        var feederData = new List<FeederInfo>();

        // Replace with your actual connection string
        string connectionString = "Server=your_server;Database=your_database;User Id=your_user;Password=your_password;";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Location, FeederNumbers, Status FROM FeederTable"; // Replace with your table and column names

            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    feederData.Add(new FeederInfo
                    {
                        Location = reader["Location"].ToString(),
                        FeederNumbers = reader["FeederNumbers"].ToString(),
                        Status = reader["Status"].ToString()
                    });
                }
            }
        }

        return feederData;
    }
}

// Class to hold feeder information
public class FeederInfo
{
    public string Location { get; set; }
    public string FeederNumbers { get; set; }
    public string Status { get; set; }
}
=======================================================================================================



using Quartz;
using Quartz.Impl;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public class QuartzHostedService : IHostedService
{
    private readonly IScheduler _scheduler;

    public QuartzHostedService(IScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Load job configuration from DB and schedule jobs
        var jobs = GetJobConfigurationsFromDb(); // Implement database fetching

        foreach (var jobConfig in jobs)
        {
            var jobDetail = JobBuilder.Create<EmailJob>()
                                      .WithIdentity(jobConfig.JobName, jobConfig.JobGroup)
                                      .Build();

            var trigger = TriggerBuilder.Create()
                                        .WithCronSchedule(jobConfig.CronExpression)
                                        .Build();

            await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _scheduler.Shutdown(cancellationToken);
    }

    private List<JobConfig> GetJobConfigurationsFromDb()
    {
        // Fetch job configurations from DB (e.g., cron expressions)
        return new List<JobConfig>
        {
            new JobConfig { JobName = "Job1", JobGroup = "EmailGroup", CronExpression = "0 0 10 * * ?" }
        };
    }
}

public class JobConfig
{
    public string JobName { get; set; }
    public string JobGroup { get; set; }
    public string CronExpression { get; set; }
}

public class EmailJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // Your email sending logic here
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Add Quartz services
                services.AddQuartz(q =>
                {
                    q.UseMicrosoftDependencyInjectionScopedJobFactory(); // Use DI for Quartz jobs
                });
                
                // Add the hosted service to manage job scheduling
                services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

                // Add the Quartz hosted service that runs on app startup
                services.AddHostedService<QuartzHostedService>();
            });
}

