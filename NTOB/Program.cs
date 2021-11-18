using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NTOB.logic;
using Serilog;
using Serilog.Extensions.Logging;

namespace NTOB
{
    class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(AppDomain.CurrentDomain.BaseDirectory + "\\appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();          
            
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(config =>
                    {
                        config.ClearProviders();
                        config.AddProvider(new SerilogLoggerProvider(Log.Logger));
                        var minimumLevel = Configuration.GetSection("Serilog:MinimumLevel")?.Value;
                        if (!string.IsNullOrEmpty(minimumLevel))
                        {
                            config.SetMinimumLevel(Enum.Parse<LogLevel>(minimumLevel));                            
                        }  
                    });
                    
                });
            
            
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--input-path",
                    "This is the Path where the Nicepage generated Project is located."),
                new Option<string>(
                    "--output-path",
                    "This is the Path where the Blazor Project should be generated."),
                new Option<bool>(
                    "--debug",
                    "Turn Debug on or off")
            };
            
            rootCommand.Description = "My sample app";
            
            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<string, string, bool>((inputPath, outputPath, debug) =>
            {
                if(debug)
                    Log.Logger.Debug("Debug is on");


                if (inputPath == null)
                    throw new ArgumentNullException(nameof(inputPath));
                    
                if(outputPath == null)
                    throw new ArgumentNullException(nameof(outputPath));


                ConvertToBlazor(inputPath,outputPath);

            });

            // Parse the incoming args and invoke the handler
            return await rootCommand.InvokeAsync(args);
            
        }
        
        
        public static void ConvertToBlazor(string inputPath, string outputPath)
        {
            
            BlazorPage blazorPage= new BlazorPage("TestApp", outputPath);
            blazorPage.CreatePage();
            
            File.Delete(Path.Combine(inputPath, "index.html"));

            Directory.CreateDirectory(Path.Combine(outputPath, "wwwroot", "nicepage"));
            Directory.CreateDirectory(Path.Combine(outputPath, "wwwroot", "nicepage", "css"));
            Directory.CreateDirectory(Path.Combine(outputPath, "wwwroot", "nicepage", "js"));
            
            File.Move(Path.Combine(inputPath, "nicepage.css"), Path.Combine(outputPath, "wwwroot", "nicepage", "css", "nicepage.css"));
            File.Move(Path.Combine(inputPath, "nicepage.js"), Path.Combine(outputPath, "wwwroot", "nicepage", "js", "nicepage.js"));
            File.Move(Path.Combine(inputPath, "jquery.js"), Path.Combine(outputPath, "wwwroot", "nicepage", "js", "jquery.js"));
            
            Directory.Move(Path.Combine(inputPath, "images"), Path.Combine(outputPath, "wwwroot", "nicepage", "images"));
            
            
            Directory.CreateDirectory(Path.Combine(outputPath, "Pages", "nicepage"));

            var files = Directory.GetFiles(inputPath).ToList();

            
            foreach (var file in files)
            {

                string str = File.ReadAllText(file);
                str = str.Replace("images/","nicepage/images/");
                File.WriteAllText(file, str);
                
                int i = files.IndexOf(file);
                FileInfo fileInfo = new FileInfo(file);
                string fileContont = File.ReadAllText(file);
                
                if(fileInfo.Extension == ".html")
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.Load(file);

                    var body = doc.DocumentNode.SelectSingleNode("//body");
                    var header = doc.DocumentNode.SelectSingleNode("//header");
                    var head = doc.DocumentNode.SelectSingleNode("//head");
                    if (i == 1)
                    {
                        StringBuilder sb = new StringBuilder();
                        StringBuilder bodyClasses = new StringBuilder();
                        foreach (var classes in body.GetClasses())
                        {
                            bodyClasses.Append($"{classes} ");
                        }

                        sb.AppendLine("@inherits LayoutComponentBase");
                        sb.AppendLine($"<div class='{bodyClasses.ToString()}'>");
                        sb.Append(header.OuterHtml);
                        sb.AppendLine("@Body");
                        sb.AppendLine("</div>");
                        
                        
                        File.WriteAllText (Path.Combine(outputPath, "Shared", "MainLayout.razor"), sb.ToString());

                        var content = File.ReadAllLines(Path.Combine(outputPath, "Pages", "_Layout.cshtml")).ToList();
                        content.Insert(12, "<link href='nicepage/css/nicepage.css' rel='stylesheet' media='screen'/>");
                        File.WriteAllLines(Path.Combine(outputPath, "Pages", "_Layout.cshtml"), content); 
                    }

                    body.SelectSingleNode("//header").Remove();

                    StringBuilder fText = new StringBuilder();

                    fText.AppendLine("@page " + '"' + "/" + Path.GetFileName(file).Replace(".html","") + '"');
                    fText.AppendLine(body.InnerHtml);
                    
                    File.WriteAllText(file, fText.ToString());
                    
                    var fileName = Path.GetFileName(file);
                    var newFileName = fileName.Replace(".html", ".razor");
                    
                    File.Move(file, Path.Combine(outputPath, "Pages", "nicepage", newFileName));
                }
                else if (fileInfo.Extension == ".css")
                {
                    var fileName = Path.GetFileName(file);
                    var newFileName = fileName.Replace(".css", ".razor.css");

                    File.Move(file, Path.Combine(outputPath, "Pages", "nicepage", newFileName));
                }




            }


        }
        
    }
}