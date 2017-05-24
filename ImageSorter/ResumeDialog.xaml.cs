using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Utilites;

namespace ImageSorter
{
    /// <summary>
    /// Interaction logic for ResumeDialog.xaml
    /// </summary>
    public partial class ResumeDialog : Window
    {
        public const string NameFormat = "{4}{0}{1}{2}{3}";
        public readonly Color FullColor = new Color()
        {
            A = 255,
            R = 0,
            G = 0,
            B = 0
        };
        public readonly Color DimColor = new Color()
        {
            A = 255,
            R = 185,
            G = 185,
            B = 185
        };

        private string[] files;
        private bool[] resultDark;
        private CategorizationWindow.CategorizationResult[] results;

        public bool CreateNew = false;
        public CategorizationWindow.CategorizationResult Result;

        private ResumeDialog()
        {
            InitializeComponent();
        }

        public ResumeDialog(string[] options) : this()
        {
            files = options;

            results = new CategorizationWindow.CategorizationResult[options.Length];
            resultDark = new bool[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                string file = System.IO.Path.GetFileName(options[i]);

                var fparts = file.Split('.');

                var formatter = new BinaryFormatter();
                var stream = new FileStream(options[i], FileMode.Open, FileAccess.Read, FileShare.Read);
                byte[] bye = new byte[App.ICMP_Head.Length];
                stream.Read(bye, 0, App.ICMP_Head.Length);

                if (!Utils.ULCompare(bye,App.ICMP_Head))
                    continue;

                CategorizationWindow.CategorizationResult res = (CategorizationWindow.CategorizationResult) formatter.Deserialize(stream);
                results[i] = res;
                resultDark[i] = fparts[0] == (Application.Current as App).CurrentHash;
            }
        }

        private void WindowOpen(object sender, RoutedEventArgs e)
        {
            InitRadioButtons();
        }

        private List<RadioButton> radios = new List<RadioButton>();

        private void InitRadioButtons()
        {
            radios.Add(Radio_None);

            RadioButton template = Resources["RadioBase"] as RadioButton;

            for (int i = 0; i < results.Length; i++)
            {
                var result = results[i];
                var dark = resultDark[i];

                RadioButton newbtn = template.ILClone();

                newbtn.Tag = result;

                object[] format = new object[] { result.Position + 1, result.Files.Count, result.CategoryDirectories.Count, result.Completed, result.ExitState, files[i] };

                newbtn.Name = NameFormat.SFormat(format);
                newbtn.Content = ((string)template.Content).SFormat(format);
                newbtn.ToolTip = ((string)template.ToolTip).SFormat(format);

                newbtn.Foreground = new SolidColorBrush(dark ? FullColor : DimColor);

                radios.Add(newbtn);

                RadioPanel.Children.Add(newbtn);
            }
        }

        private bool close = false;

        private void OkClick(object sender, RoutedEventArgs e)
        {
            foreach (var radio in radios)
            {
                if ((bool)radio.IsChecked)
                {
                    object o = radio.Tag;
                    Result = o == null ? default(CategorizationWindow.CategorizationResult) : (CategorizationWindow.CategorizationResult)radio.Tag;
                    CreateNew = o == null;
                    break;
                }
            }
            close = true;
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            close = true;
            Application.Current.Shutdown();
            (Application.Current as App).ExitImmediately = true;
            Close();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!close)
                e.Cancel = true;
        }
    }
}
