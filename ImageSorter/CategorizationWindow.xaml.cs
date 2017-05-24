using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Utilites;
using WpfAnimatedGif;

namespace ImageSorter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CategorizationWindow : Window
    {
        private string curFile;
        private int pos = -1;
        private List<string> files;
        private List<string> categorydirs;
        private Dictionary<string, string> namemap = new Dictionary<string, string>();
        private Dictionary<string, string> shortcuts = new Dictionary<string, string>();

        private string TitleFormat;

        #region private List<List<int>> Categs;
        private List<List<int>> _categorizations = null;
        private List<List<int>> Categs
        {
            get
            {
                if (_categorizations == null) {
                    _categorizations = new List<List<int>>(files.Count);

                    foreach (var v in files)
                    {
                        _categorizations.Add(new List<int>());
                    }
                }

                return _categorizations;
            }
        }

        #endregion // l[x] where x is file index, result is categ indexes

        #region Initializers

        public CategorizationWindow(List<string> fileList, List<string> catdirs)
        {
            InitializeComponent();

            JobCompleted += (CategorizationWindow win) => { };

            files = fileList;
            categorydirs = catdirs;
        }

        public CategorizationWindow(List<string> fileList, List<string> catdirs, Dictionary<string,string> nameMap) 
            : this(fileList, catdirs)
        {
            namemap = nameMap;
        }

        public CategorizationWindow(List<string> fileList, List<string> catdirs, Dictionary<string, string> nameMap, Dictionary<string, string> shortc) 
            : this(fileList, catdirs, nameMap)
        {
            shortcuts = shortc;
        }

        public CategorizationWindow(List<string> fileList, List<string> catdirs, Dictionary<string, string> nameMap, Dictionary<string, string> shortc, CategorizationResult res)
            : this(fileList, catdirs, nameMap, shortc)
        {
            Resume(res);
        }
        #endregion

        private ExitState exitType = ExitState.UserClose;
        public ExitState ExitType {
            get => exitType;
            set => exitType = value;
        }

        private void GoPrevImage()
        {
            SaveCheckboxState();

            if (pos > 0)
            {
                curFile = files[--pos];
                InitImagePage();
            }
        }

        private void GoNextImage()
        {
            LoadNextImage();
        }

        private void LoadNextImage()
        {
            SaveCheckboxState();

            if (files.Count > pos+1)
            {
                curFile = files[++pos];
                InitImagePage();
            }
            else
            {
                if (HasCategorizedAll())
                {
                    MessageBox.Show("You're done!", "", MessageBoxButton.OK, MessageBoxImage.None);

                    ExitType = ExitState.Done;
                    Close();
                }
            }
        }

        private bool chbxSaveFirs = true;

        private void SaveCheckboxState()
        {
            if (pos < 0) return;

            if (!chbxSaveFirs)
            {
                Categs[pos].Clear();
            }

            chbxSaveFirs = false;

            for (int i = 0; i < checkboxes.Count; i++)
            {
                if ((bool) checkboxes[i].IsChecked)
                    Categs[pos].Add(i);
            }
        }

        private void InitImagePage()
        {
            #region Nav button format
            Button_Prev.Visibility = Visibility.Visible;
            Button_Next.Content = Resources["NextText"];

            if (pos == 0) {
                Button_Prev.Visibility = Visibility.Hidden;
            } else
            if (pos == files.Count - 1)
            {
                Button_Next.Content = Resources["DoneText"];
            }
            #endregion

            if (new FileInfo(curFile).Length > 10 * 1024 * 1024)
                GC.Collect();

            CategImage.Visibility = Visibility.Visible;
            CategImage.Source = new BitmapImage(new Uri(Path.GetFullPath(curFile)));
            CategImageAnim.Source = new BitmapImage();
            CategImageAnim.Visibility = Visibility.Collapsed;

            if (MimeTypeMap.List.MimeTypeMap.GetMimeType(Path.GetExtension(curFile)).Contains("image/gif"))
            {
                CategImage.Visibility = Visibility.Collapsed;
                ImageBehavior.SetAnimatedSource(CategImageAnim, new BitmapImage(new Uri(Path.GetFullPath(curFile))));
                CategImageAnim.Visibility = Visibility.Visible;
            }

            if (System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 > 1L * 1024 * 1024 * 1024)
                GC.Collect();

            #region Set checkbox values
            foreach (CheckBox cb in checkboxes)
            {
                cb.IsChecked = false;
            }

            foreach (int idx in Categs[pos])
            {
                checkboxes[idx].IsChecked = true;
            }
            #endregion

            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
            TaskbarItemInfo.ProgressValue = pos*1d / files.Count;

            if (TaskbarItemInfo.ProgressValue == 0) TaskbarItemInfo.ProgressValue = .05d;

             Title = TitleFormat.SFormat(pos+1, files.Count);
        }

        private void WindowOpened(object sender, RoutedEventArgs e)
        {
            if (closewhenopen)
            {
                Close();
                return;
            }

            TitleFormat = Title;

            InitCheckboxes();

            LoadNextImage();
        }

        private List<CheckBox> checkboxes = new List<CheckBox>();
        private Dictionary<string, int> checkkeybs = new Dictionary<string, int>();

        private void InitCheckboxes()
        {
            CheckBox cb_template = (CheckBox) Resources["FolderCheck"];

            for (int i = 0; i < categorydirs.Count; i++)
            {
                CheckBox newcb = Utils.ILClone(cb_template);//.ILClone();
                string cdir = categorydirs[i];

                string sname = Path.GetFileName(cdir);
                string name = sname;
                if (namemap.ContainsKey(sname))
                    name = namemap[sname];

                newcb.Content = cb_template.Content.ToString().SFormat(name, sname);
                newcb.ToolTip = cb_template.ToolTip.ToString().SFormat(name, sname);

                if (shortcuts.ContainsKey(sname))
                    checkkeybs[shortcuts[sname]] = i;

                checkboxes.Add(newcb);

                Buttons.Children.Add(newcb);
            }
        }

        [Serializable]
        public struct CategorizationResult
        {
            public List<string> Files;
            public List<string> CategoryDirectories;
            public List<List<int>> Categorized;
            public int Position;
            public ExitState ExitState;
            public int Completed;
        }

        public CategorizationResult Result;

        public delegate void WindowComplete(CategorizationWindow window);
        public event WindowComplete JobCompleted;

        public new void Close()
        {
            base.Close();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveCheckboxState();

            if (ExitType == ExitState.Done)
            {
                // Finish Close
                FinishClose();
            }
            if (ExitType == ExitState.UserClose)
            {
                if (!HasCategorizedAll())
                {
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;

                    var result = MessageBox.Show(this, "You have not categorized all images! Are you sure you want to quit?", "", 
                        MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                    if (result == MessageBoxResult.Yes)
                    {
                        ExitType = ExitState.Incomplete;
                        // Finish close
                        FinishClose();
                    }
                    else
                    {
                        e.Cancel = true;
                        
                        TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
                    }
                }
                else
                {
                    ExitType = ExitState.Done;
                    FinishClose();
                }
            }
        }

        private bool closewhenopen = false;

        private void Resume(CategorizationResult res)
        {
            var filec = files;
            files = res.Files;
            files.AddRange(filec.Except(files));

            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    MessageBox.Show(this, "One or more files referenced by this ICMP no longer exist, therefore you cannot change data in this file.", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    closewhenopen = true;
                    var proc = Utils.ProcessCommandLine(Assembly.GetEntryAssembly().Location, Environment.GetCommandLineArgs());
                    proc.Start();
                    Application.Current.Shutdown();
                    return;
                }
            }

            var catc = categorydirs;
            categorydirs = res.CategoryDirectories;
            categorydirs.AddRange(catc.Except(categorydirs));

            pos = res.Position-1;

            var nel = Categs;
            for (int i = 0; i < res.Categorized.Count; i++)
            {
                Categs[i] = res.Categorized[i];
            }

            if (res.ExitState != ExitState.Incomplete)
            {
                var result = MessageBox.Show(this, "Would you like to modify the categorizations?", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.Yes)
                {

                }
                else
                {
                    pos = res.Position;
                    ExitType = ExitState.Done;
                    closewhenopen = true;
                }
            }
        }

        private void FinishClose()
        {
            Result = new CategorizationResult()
            {
                Files = files,
                CategoryDirectories = categorydirs,
                Categorized = Categs,
                Position = pos,
                ExitState = ExitType,
                Completed = NumberDone()
            };

            JobCompleted.Invoke(this);
        }

        private int NumberDone()
        {
            if (closewhenopen)
            {
                return Categs.Count;
            }
            int i = 0;
            foreach (List<int> l in Categs)
            {
                if (l.Count != 0) i++;
            }
            return i;
        }

        private bool HasCategorizedAll()
        {
            foreach (List<int> l in Categs)
            {
                if (l.Count == 0) return false;
            }

            return true;
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right) GoNextImage();
            if (e.Key == Key.Left) GoPrevImage();

            if (checkkeybs.ContainsKey(e.Key.ToString().ToUpper()))
            {
                checkboxes[checkkeybs[e.Key.ToString()]].IsChecked = !checkboxes[checkkeybs[e.Key.ToString()]].IsChecked;
            }
        }

        private void GoPrevImage(object sender, RoutedEventArgs e)
        {
            GoPrevImage();
        }

        private void GoNextImage(object sender, RoutedEventArgs e)
        {
            GoNextImage();
        }
    }
}
