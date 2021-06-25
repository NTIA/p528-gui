using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Windows;

namespace p528_gui.Windows
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            tb_AppVersion.Text = $"{version.Major}.{version.Minor}.{version.Build}";

            var dll = FileVersionInfo.GetVersionInfo("p528_x86.dll");
            tb_DllVersionText.Text = $"P.528-{dll.FileMajorPart} DLL Version:";
            tb_DllVersion.Text = $"{dll.FileMajorPart}.{dll.FileMinorPart}.{dll.FileBuildPart}";

            if (CheckForUpdate(out int major, out int minor, out int build) &&
                (major > version.Major ||
                 major == version.Major && minor > version.Minor ||
                 major == version.Major && minor == version.Minor && build > version.Build))
                    tb_NewVersion.Visibility = Visibility.Visible;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private bool CheckForUpdate(out int major, out int minor, out int build)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident / 6.0)");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                HttpResponseMessage response = client.GetAsync(@"https://api.github.com/repos/ntia/p528-gui/releases/latest").GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                // parse version number
                var index_start = responseBody.IndexOf("tag_name");
                var index_end = responseBody.IndexOf(",", index_start);
                var version = responseBody.Substring(index_start, index_end - index_start).Replace(@"""", "").Split(':')[1];

                major = Convert.ToInt32(version.Split('.')[0].Replace("v", ""));
                minor = Convert.ToInt32(version.Split('.')[1]);
                build = Convert.ToInt32(version.Split('.')[2]);

                return true;
            }
            catch
            {
                // do nothing is something fails
            }

            major = -1;
            minor = -1;
            build = -1;

            return false;
        }
    }
}
