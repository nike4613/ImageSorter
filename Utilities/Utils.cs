using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Utilites
{
    public static class Utils
    {
        public static string SFormat(this string s, params object[] args)
        {
            return String.Format(s, args);
        }

        public static string UrlDecode(this string s)
        {
            return HttpUtility.UrlDecode(s);
        }

        public static uint SetBits(this long n)
        {
            uint count = 0;
            while (n != 0)
            {
                n &= (n - 1);
                count++;
            }
            return count;
        }

        public static string GetRelativePath(string fullPath, string basePath)
        {
            // Require trailing backslash for path
            if (!basePath.EndsWith("\\"))
                basePath += "\\";

            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

            // Uri's use forward slashes so convert back to backward slashes
            return relativeUri.ToString().Replace("/", "\\");

        }

        public static Process ProcessCommandLine(string exec, params string[] args)
        {
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = exec;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = string.Join(" ",args);

            Process exeProcess = new Process();
            exeProcess.StartInfo = startInfo;
            return exeProcess;
        }
    }

}
