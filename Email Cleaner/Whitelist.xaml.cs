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
using System.Windows.Shapes;

namespace Email_Cleaner
{
    /// <summary>
    /// Interaction logic for Whitelist.xaml
    /// </summary>
    public partial class Whitelist : Window
    {
        public Whitelist()
        {
            InitializeComponent();
        }

        public void MenuItemDelete_Click(object sender, EventArgs e)
        {
            if (WhitelistList.SelectedIndex == -1)
                return;

            var list = File.ReadAllLines("whitelist.txt").ToList();

            list.Remove(list.First(x => x == WhitelistList.Items[WhitelistList.SelectedIndex].ToString()));

            File.WriteAllLines("whitelist.txt", list);

            int index = WhitelistList.SelectedIndex;
            WhitelistList.Items.RemoveAt(index);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("whitelist.txt"))
            {
                string[] list = File.ReadAllLines("whitelist.txt");

                foreach (string item in list)
                {
                    WhitelistList.Items.Add(item);
                }
            }
        }

        private void AddWhitelist_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SenderEmail.Text))
                return;

            if (!SenderEmail.Text.Contains("@"))
            {
                MessageBox.Show("Please enter a valid email address.");
                return;
            }

            File.AppendAllText("whitelist.txt", SenderEmail.Text);

            WhitelistList.Items.Add(SenderEmail.Text);
        }
    }
}
