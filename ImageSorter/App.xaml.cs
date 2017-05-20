using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ImageSorter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Start(object sender, StartupEventArgs e)
        {
            var win = new CategorizationWindow(new List<string>(), new List<string>(), new Dictionary<string, string>(), new Dictionary<string, string>());

            win.JobCompleted += Win_JobCompleted;

            win.Show();
        }

        private void Win_JobCompleted(CategorizationWindow win)
        {
            var res = win.Result;
        }
    }
}
