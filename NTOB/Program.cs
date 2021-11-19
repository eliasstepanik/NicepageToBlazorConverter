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
using Microsoft.VisualBasic.FileIO;
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
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                //.AddJsonFile(AppDomain.CurrentDomain.BaseDirectory + "\\appsettings.json", optional: true, reloadOnChange: true)
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
                    "--inplace",
                    "Turn In Place on or off"),
                new Option<bool>(
                    "--debug",
                    () => false,
                    "Turn Debug on or off")
            };
            
            rootCommand.Description = "My sample app";
            
            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<string, string, bool,bool>((inputPath, outputPath, debug,inplace) =>
            {
                if(debug)
                    Log.Logger.Debug("Debug is on");


                if (inputPath == null)
                    throw new ArgumentNullException(nameof(inputPath));
                if(debug)
                    Log.Logger.Debug("Input Path was set to '" + inputPath + "'");
                
                if (inplace)
                {
                    string tempPath = inputPath;
                    
                    outputPath = Path.Combine(inputPath, tempPath.Replace(Path.GetDirectoryName(tempPath) + Path.DirectorySeparatorChar, ""));
                    if(debug)
                        Log.Logger.Debug("Output Path was set to '" + outputPath + "'");
                }
                    
                if(outputPath == null)
                    throw new ArgumentNullException(nameof(outputPath));

                ConvertToBlazor(inputPath,outputPath,debug);

            });

            // Parse the incoming args and invoke the handler
            return await rootCommand.InvokeAsync(args);
            
        }
        
        
        public static void ConvertToBlazor(string inputPath, string outputPath, bool debug)
        {
            
            BlazorPage blazorPage= new BlazorPage("TestApp", outputPath);
            blazorPage.CreatePage();
            if(debug)
                Log.Logger.Debug("Blazor Page Generated!");
            
            File.Delete(Path.Combine(inputPath, "index.html"));
            if(debug)
                Log.Logger.Debug("index.html deleted!");
            
            
            File.Delete(Path.Combine(outputPath,"Pages", "index.razor"));
            if(debug)
                Log.Logger.Debug("index.razor deleted!");

            Directory.CreateDirectory(Path.Combine(outputPath, "wwwroot", "nicepage"));
            if(debug)
                Log.Logger.Debug("Folder '" + Path.Combine(outputPath, "wwwroot", "nicepage") + "' created!");
            
            Directory.CreateDirectory(Path.Combine(outputPath, "wwwroot", "nicepage", "css"));
            if(debug)
                Log.Logger.Debug("Folder '" + Path.Combine(outputPath, "wwwroot", "nicepage", "css")+ "' created!");
            
            Directory.CreateDirectory(Path.Combine(outputPath, "wwwroot", "nicepage", "js"));
            if(debug)
                Log.Logger.Debug("Folder '" + Path.Combine(outputPath, "wwwroot", "nicepage", "js")+ "' created!");
            
            File.Copy(Path.Combine(inputPath, "nicepage.css"), Path.Combine(outputPath, "wwwroot", "nicepage", "css", "nicepage.css"));
            if(debug)
                Log.Logger.Debug("Copyed '" + Path.Combine(inputPath, "nicepage.css")+ "' to '" + Path.Combine(outputPath, "wwwroot", "nicepage", "css", "nicepage.css") + "'.");
            
            File.Copy(Path.Combine(inputPath, "nicepage.js"), Path.Combine(outputPath, "wwwroot", "nicepage", "js", "nicepage.js"));
            if(debug)
                Log.Logger.Debug("Copyed '" + Path.Combine(inputPath, "nicepage.js")+ "' to '" + Path.Combine(outputPath, "wwwroot", "nicepage", "css", "nicepage.css") + "'.");

            File.Copy(Path.Combine(inputPath, "jquery.js"), Path.Combine(outputPath, "wwwroot", "nicepage", "js", "jquery.js"));
            if(debug)
                Log.Logger.Debug("Copyed '" + Path.Combine(inputPath, "jquery.js")+ "' to '" + Path.Combine(outputPath, "wwwroot", "nicepage", "js", "jquery.js") + "'.");

            //Directory.Move(Path.Combine(inputPath, "images"), Path.Combine(outputPath, "wwwroot", "nicepage", "images"));
            FileSystem.CopyDirectory(Path.Combine(inputPath, "images"), Path.Combine(outputPath, "wwwroot", "nicepage", "images"));
            if(debug)
                Log.Logger.Debug("Copyed '" + Path.Combine(inputPath, "images")+ "' to '" + Path.Combine(outputPath, "wwwroot", "nicepage", "images") + "'.");

            
            Directory.CreateDirectory(Path.Combine(outputPath, "Pages", "nicepage"));
            if(debug)
                Log.Logger.Debug("Folder '" + Path.Combine(outputPath, "Pages", "nicepage") + "' created!");
            
            var files = Directory.GetFiles(inputPath).ToList();

            int i = 0;

            foreach (var file in files)
            {

                string str = File.ReadAllText(file);
                str = str.Replace("images/","nicepage/images/");
                File.WriteAllText(file, str);
                if(debug)
                    Log.Logger.Debug("Replaced 'images/' to 'nicepage/images/' in '" + file + "'.");
                
                FileInfo fileInfo = new FileInfo(file);
                string fileContont = File.ReadAllText(file);

                if(fileInfo.Extension == ".html")
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.Load(file);

                    var body = doc.DocumentNode.SelectSingleNode("//body");
                    var header = doc.DocumentNode.SelectSingleNode("//header");
                    var head = doc.DocumentNode.SelectSingleNode("//head");
                    if (i == 0)
                    {
                        i++;
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
                        if(debug)
                            Log.Logger.Debug("Created '" + Path.Combine(outputPath, "Shared", "MainLayout.razor") + "'.");

                        var content = File.ReadAllLines(Path.Combine(outputPath, "Pages", "_Layout.cshtml")).ToList();
                        content.Insert(12, "<link href='nicepage/css/nicepage.css' rel='stylesheet' media='screen'/>");
                        File.WriteAllLines(Path.Combine(outputPath, "Pages", "_Layout.cshtml"), content);
                        if(debug)
                            Log.Logger.Debug("Added 'nicepage/css/nicepage.css' to '" + Path.Combine(outputPath, "Pages", "_Layout.cshtml") + "'.");
                        
                        StringBuilder fText2 = new StringBuilder();

                        fText2.AppendLine("@page " + '"' + "/" + '"');
                        fText2.AppendLine(body.InnerHtml);
                    
                        File.WriteAllText(file, fText2.ToString());
                        
                    
                        File.Copy(file, Path.Combine(outputPath, "Pages", "Index.razor"));
                        if(debug)
                            Log.Logger.Debug("Created '" + Path.Combine(outputPath, "Pages", "Index.razor") + "'.");
                        
                        File.Copy(file.Replace(".html", ".css"), Path.Combine(outputPath, "Pages", "Index.razor.css"));
                        if(debug)
                            Log.Logger.Debug("Created '" + Path.Combine(outputPath, "Pages", "Index.razor.css") + "'.");
                        
                    }

                    body.SelectSingleNode("//header").Remove();

                    StringBuilder fText = new StringBuilder();

                    fText.AppendLine("@page " + '"' + "/" + Path.GetFileName(file).Replace(".html","") + '"');
                    fText.AppendLine(body.InnerHtml);
                    
                    File.WriteAllText(file, fText.ToString());

                    var fileName = Path.GetFileName(file);
                    var newFileName = fileName.Replace(".html", ".razor");
                    
                    File.Copy(file, Path.Combine(outputPath, "Pages", "nicepage", newFileName));
                    if(debug)
                        Log.Logger.Debug("Created '" + Path.Combine(outputPath, "Pages", "nicepage", newFileName) + "'.");
                }
                else if (fileInfo.Extension == ".css")
                {
                    var fileName = Path.GetFileName(file);
                    
                    if(fileName.Equals("nicepage.css"))
                        continue;
                    
                    var newFileName = fileName.Replace(".css", ".razor.css");

                    File.Copy(file, Path.Combine(outputPath, "Pages", "nicepage", newFileName));
                    if(debug)
                        Log.Logger.Debug("Created '" + Path.Combine(outputPath, "Pages", "nicepage", newFileName) + "'.");
                }




            }


        }
        
    }
}