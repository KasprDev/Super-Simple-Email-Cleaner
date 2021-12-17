using HtmlAgilityPack;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Email_Cleaner
{
    public partial class MainWindow : Window
    {
        private ImapClient _client = new ImapClient();
        private List<UnsubscribeEmail> _emails = new List<UnsubscribeEmail>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void VerifyButton_Click(object sender, RoutedEventArgs e)
        {
            VerifyButton.IsEnabled = false;

            await _client.ConnectAsync(IMAPServer.Text, Convert.ToInt32(IMAPPort.Text));

            if (!_client.IsConnected)
            {
                MessageBox.Show("Unable to establish a connection with the specified IMAP server.");
                VerifyButton.IsEnabled = true;
                return;
            }

            _client.AuthenticationMechanisms.Remove("XOAUTH2");

            await _client.AuthenticateAsync(IMAPUsername.Text, IMAPPassword.Text);

            if (!_client.IsAuthenticated)
            {
                MessageBox.Show("Unable to be authenticated by the specified IMAP server.");
                VerifyButton.IsEnabled = true;
                return;
            }

            BeginButton.IsEnabled = true;
        }

        public async void MenuItemDelete_Click(object sender, EventArgs e)
        {
            if (UnsubscribeLinksList.SelectedIndex == -1)
                return;

            await _client.Inbox.AddFlagsAsync(_emails[UnsubscribeLinksList.SelectedIndex].Uid, MessageFlags.Deleted, true);

            int index = UnsubscribeLinksList.SelectedIndex;
            UnsubscribeLinksList.Items.RemoveAt(index);
        }

        public void MenuItemOpen_Click(object sender, EventArgs e)
        {
            if (UnsubscribeLinksList.SelectedIndex == -1)
                return;

            int index = UnsubscribeLinksList.SelectedIndex;
            string? item = UnsubscribeLinksList.Items[index].ToString();

            Process.Start(new ProcessStartInfo(item.Split(' ')[1])
            {
                UseShellExecute = true
            });
        }

        private async void BeginButton_Click(object sender, RoutedEventArgs e)
        {
            BeginButton.IsEnabled = false;
            BeginButton.Content = "Running..";

            OpenAll.IsEnabled = false;
            DeleteEmails.IsEnabled = false;

            UnsubscribeLinksList.Items.Clear();

            List<string> whitelist = new List<string>();

            if (File.Exists("whitelist.txt"))
            {
                whitelist = File.ReadAllLines("whitelist.txt").ToList();
            }

            var inbox = _client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var results = inbox.Search(SearchOptions.All, SearchQuery.Not(SearchQuery.Deleted));

            foreach (var email in results.UniqueIds)
            {
                var message = await inbox.GetMessageAsync(email);

                if (message.HtmlBody != null)
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(message.HtmlBody);

                    HtmlNodeCollection links = doc.DocumentNode.SelectNodes("//a/@href");

                    if (links == null)
                        continue;

                    foreach (var link in links)
                    {
                        if (link.InnerText.ToLower().Contains("unsubscribe"))
                        {
                            string hrefValue = link.GetAttributeValue("href", string.Empty);
                            string emailSender = message.From.OfType<MailboxAddress>().Single().Address;

                            if (whitelist.Contains(emailSender))
                                continue;

                            if (_emails.Any(x => x.Sender == emailSender))
                                continue;

                            _emails.Add(new UnsubscribeEmail()
                            {
                                Sender = emailSender,
                                Uid = email,
                                Link = hrefValue
                            });

                            UnsubscribeLinksList.Items.Add($"({emailSender}): {hrefValue}");
                        }
                    }
                }
            }

            OpenAll.IsEnabled = true;
            DeleteEmails.IsEnabled = true;

            BeginButton.IsEnabled = true;
            BeginButton.Content = "Begin";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new Whitelist().ShowDialog();
        }

        private void OpenAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var link in _emails)
            {
                Process.Start(new ProcessStartInfo(link.Link)
                {
                    UseShellExecute = true
                });
            }
        }

        private async void DeleteEmails_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete all found emails?", "Confirm Email Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                foreach (var link in _emails)
                {
                    await _client.Inbox.AddFlagsAsync(link.Uid, MessageFlags.Deleted, true);
                }

                UnsubscribeLinksList.Items.Clear();
            }
        }
    }

    class UnsubscribeEmail
    {
        public UniqueId Uid;
        public string Sender;
        public string Link;
    }
}
