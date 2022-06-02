using System;
using Microsoft.Extensions.Hosting;

class  Program
{
    // Main Method
    static public void Main(String[] args)
    {

        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
            });
}

