using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Utilities;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using Utilites;

namespace ImageSorter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private CategorizationWindow.CategorizationResult Result;
        private CategorizationWindow win;
        private DefaultArray<string> Args;

        private bool resume = false;
        private CategorizationWindow.CategorizationResult Resume;

        public static readonly string ICMP_Loc = ".";
        public static readonly string ICMP_Ext = ".icmp";

        public string CurrentHash;

        public bool ExitImmediately = false;

        private void Start(object sender, StartupEventArgs e)
        {
            Args = e.Args;

            SHA256 hasher = SHA256.Create(); 
            CurrentHash = BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(
                (String.Join(" ", Args)) // Hashed string
                    .ToArray()))).Replace("-", "").ToLower();

            Directory.CreateDirectory(ICMP_Loc);

            var icmpf = Directory.EnumerateFiles(ICMP_Loc, "*" + ICMP_Ext, SearchOption.TopDirectoryOnly).ToArray();

            if (icmpf.Length > 0)
            {
                var resdiag = new ResumeDialog(icmpf);

                resdiag.ShowDialog();

                resume = !resdiag.CreateNew;
                Resume = resdiag.Result;
            }

            if (ExitImmediately)
            {
                Shutdown();
                return;
            }

            RunCateg();
        }

        private void RunCateg()
        {
            string fdir = Args[0, "."];
            string catdir = Args[1, "."];
            string namem = Args[2, "names.json"];

            var images = GetImagesIn(fdir);
            var dirs = GetDirsIn(catdir);
            var namemap = ParseNameMap(namem, out Dictionary<string, string> shortcuts);

            if (!resume)
            {
                win = new CategorizationWindow(images, dirs, namemap, shortcuts);
            }
            else
            {
                win = new CategorizationWindow(images, dirs, namemap, shortcuts, Resume);
            }

            win.JobCompleted += Win_JobCompleted;

            win.Closed += Win_Closed;

            win.ShowDialog();

            WriteFSCateg();

            Shutdown();
        }

        private void WriteFSCateg()
        {
            if (Result.Completed == 0 || Result.ExitState != ExitState.Done) return;

            var files = Result.Files;
            var categdirs = Result.CategoryDirectories;
            var categd = Result.Categorized;

            for (int i = 0; i < files.Count; i++)
            {
                int[] chng = categd[i].ToArray();

                string filesrc = files[i];

                foreach(var j in chng)
                {
                    string filedst = Path.Combine(categdirs[j], Path.GetFileName(filesrc));

                    bool b = Utils.CreateHardLink(Path.GetFullPath(filedst), Path.GetFullPath(filesrc));
                }
            }
        }

        public static readonly byte[] ICMP_Head = Encoding.UTF8.GetBytes("/CMP");

        private void Win_Closed(object sender, EventArgs e)
        {
            if (Result.Completed == 0) return;

            string filenex = CurrentHash + "." + Result.Completed.ToString("X") + "." + Result.Position.ToString("X") + "." + Result.ExitState.ToString();

            string filename = filenex + ICMP_Ext;

            var formatter = new BinaryFormatter();
            var stream = new FileStream(Path.Combine(ICMP_Loc, filename), FileMode.Create, FileAccess.Write, FileShare.None);
            stream.Write(ICMP_Head, 0, ICMP_Head.Length);
            formatter.Serialize(stream, Result);
            stream.Close();
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
                string shc = kv.Value.Substring(kv.Value.IndexOf('&')+1,1).ToUpper();
                shortc[kv.Key] = shc;
            }

            shortcuts = shortc;
            return namemap;
        }

        private List<string> GetDirsIn(string v)
        {
            var va = Directory.EnumerateDirectories(v, "*", SearchOption.TopDirectoryOnly).ToList();
            return va;
        }

        public static List<string> ImageExtensions = new List<string>();
        public static void LoadImageExtensions()
        {
            if (ImageExtensions.Count != 0) return;
            foreach (var kv in MimeTypeMap.List.MappingList.Mappings)
            {
                foreach (var mime in kv.Value)
                {
                    if (mime.Contains("image"))
                    {
                        ImageExtensions.Add(kv.Key);
                        break;
                    }
                }
            }
        }

        private List<string> GetImagesIn(string v)
        {
            LoadImageExtensions();
            var files = Directory.EnumerateFiles(v, "*", SearchOption.TopDirectoryOnly).ToList();

            var outf = new List<string>();
            foreach (var f in files)
            {
                if (ImageExtensions.Contains(Path.GetExtension(f)))
                    outf.Add(f);
            }

            return outf;
        }

        private void Win_JobCompleted(CategorizationWindow win)
        {
            Result = win.Result;
        }
    }
}
