using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace ImageSorter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Start(object sender, StartupEventArgs e)
        {
            string fdir = ".";
            string catdir = ".";
            string namem = "names.json";

            fdir = e.Args[0];
            catdir = e.Args[1];
            namem = e.Args[2];

            List<string> files = GetFilesIn(fdir);
            List<string> categds = GetDirsIn(catdir);
            Dictionary<string, string> namemap = ParseNameMap(namem, out Dictionary<string,string> shortcuts);

            var win = new CategorizationWindow(new List<string>(), new List<string>(), new Dictionary<string, string>(), new Dictionary<string, string>());

            win.JobCompleted += Win_JobCompleted;

            win.Show();
        }

        private Dictionary<string, string> ParseNameMap(string v, out Dictionary<string, string> shortcuts)
        {
            string json = File.ReadAllText(v);

            Dictionary<string, string> decjson = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            Dictionary<string, string> namemap = new Dictionary<string, string>();
            Dictionary<string, string> shortc = new Dictionary<string, string>();

            foreach (var kv in decjson)
            {
                namemap[kv.Key] = kv.Value.Replace("&", "");
                string shc = kv.Value.Substring(kv.Value.IndexOf('&')+1,1);
                shortc[kv.Key] = shc;
            }

            shortcuts = shortc;
            return namemap;
        }

        private List<string> GetDirsIn(string v)
        {
            return Directory.EnumerateDirectories(v, "*", SearchOption.TopDirectoryOnly).ToList();
        }

        private List<string> GetFilesIn(string v)
        {
            return Directory.EnumerateFiles(v, "*", SearchOption.TopDirectoryOnly).ToList();
        }

        private void Win_JobCompleted(CategorizationWindow win)
        {
            var res = win.Result;
        }
    }
}
