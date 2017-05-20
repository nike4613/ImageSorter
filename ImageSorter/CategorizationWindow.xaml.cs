using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private CategorizationWindow()
        {
            InitializeComponent();

            files = new List<string>();
        }

        public CategorizationWindow(List<string> fileList, List<string> catdirs) : this()
        {
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
        #endregion

        private ExitState exitType = ExitState.UserClose;

        private void GoPrevImage()
        {
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
            if (files.Count > pos+1)
            {
                curFile = files[++pos];
                InitImagePage();
            }
            else
            {
                MessageBox.Show("You're done!", "", MessageBoxButton.OK, MessageBoxImage.None);

                exitType = ExitState.Done;
                Close();
            }
        }

        private void InitImagePage()
        {
            #region Nav button format
            Button_Prev.Visibility = Visibility.Visible;
            Button_Next.Content = new DynamicResourceExtension("NextText"); //Resources["NextText"];

            if (pos == 0) {
                Button_Prev.Visibility = Visibility.Hidden;
            } else
            if (pos == files.Count - 1)
            {
                Button_Next.Content = new DynamicResourceExtension("DoneText");
            }
            #endregion

            CategImage.Source = new BitmapImage(new Uri(Path.GetFullPath(curFile)));

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
        }

        private void WindowOpened(object sender, EventArgs e)
        {
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
                CheckBox newcb = new CheckBox();
                newcb.IsTabStop = cb_template.IsTabStop;

                string cdir = categorydirs[i];

                string sname = Path.GetDirectoryName(cdir);
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

        public struct CategorizationResult
        {
            public List<string> Files;
            public List<string> CategoryDirectories;
            public List<List<int>> Categorized;
            public int Position;
            public ExitState ExitState;
        }

        public CategorizationResult Result;

        public delegate void WindowComplete(CategorizationWindow window);
        public event WindowComplete JobCompleted;

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (exitType == ExitState.Done)
            {
                // Finish Close
                FinishClose();
            }
            if (exitType == ExitState.UserClose)
            {
                if (!HasCategorizedAll())
                {
                    var result = MessageBox.Show(this, "You have not categorized all images! Are you sure you want to quit?", "", 
                        MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                    if (result == MessageBoxResult.Yes)
                    {
                        exitType = ExitState.Incomplete;
                        // Finish close
                        FinishClose();
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    exitType = ExitState.Done;
                    FinishClose();
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
                ExitState = exitType
            };

            JobCompleted.Invoke(this);
        }

        private bool HasCategorizedAll()
        {
            foreach (List<int> l in Categs)
            {
                if (l.Count == 0) return false;
            }

            return true;
        }

        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right) GoNextImage();
            if (e.Key == Key.Left) GoPrevImage();

            if (checkkeybs.ContainsKey(e.Key.ToString()))
            {
                checkboxes[checkkeybs[e.Key.ToString()]].IsChecked = !checkboxes[checkkeybs[e.Key.ToString()]].IsChecked;
            }
        }
    }
}
