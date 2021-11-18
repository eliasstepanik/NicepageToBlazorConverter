using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NTOB.logic
{
    public class BlazorPage
    {
        public string Title { get; set; }
        public string Path { get; set; }
        
        
        
        public BlazorPage(string title, string path)
        {
            Title = title;
            Path = path;
        }

        public void CreatePage(bool https = false, AuthType authType = AuthType.Individual, bool debug = false)
        {


            string command = "dotnet new blazorserver" +
                             " -o " + '"' +Path + '"' +
                             " -n " + Title +
                             (https ? " " : " --no-https ") +
                             "-au " + authType.ToString();
            
                             
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine(command);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            if (debug)
            {
                Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            }
            
            
        }
    }
    
    public enum AuthType
    {
        None,
        Individual,
        IndividualB2C,
        SingleOrg,
        MultiOrg,
        Windows
    }
}