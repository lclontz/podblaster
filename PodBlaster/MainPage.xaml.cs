using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Xml;
using System.Net;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using System.Windows;
using System.Diagnostics;
using Windows.Web;
using Windows.Web.Http;
using System.Threading;
using Windows.Storage.Streams;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;

namespace PodBlaster
{
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Podcast> podcasts { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();

            StatusCommand.Text = "No Active Downloads";

            ApplicationView.PreferredLaunchViewSize = new Size(320, 1000);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            podcasts = new ObservableCollection<Podcast>();

            string XMLFilePath = Path.Combine(Package.Current.InstalledLocation.Path, "stations.xml");
            XDocument stationsXML = XDocument.Load(XMLFilePath);

            var podData = from query in stationsXML.Descendants("station")
                          select new Podcast
                          {
                              stationName = (string)query.Element("stationName"),
                              stationURL = (string)query.Element("stationURL")
                          };

            PodList.ItemsSource = podData;

        }



        private async void PodList_ItemClick(object sender, ItemClickEventArgs e)
        {

            Windows.Web.Syndication.SyndicationClient client = new Windows.Web.Syndication.SyndicationClient();
            Windows.Web.Syndication.SyndicationFeed feed;

            Podcast poddy = e.ClickedItem as Podcast;

            string feedName = poddy.stationURL;

            Uri rssFeed = new Uri(feedName);
            feed = await client.RetrieveFeedAsync(rssFeed);

            int getNumberOfDownloads = epDownloads.SelectedIndex;

            for (int i = 0; i < (getNumberOfDownloads + 1); i++)
            {
                try {
                    Windows.Web.Syndication.SyndicationItem item = feed.Items[i];
                    Windows.Web.Syndication.SyndicationLink link = feed.Links[0];

                    Windows.Web.Syndication.SyndicationLink rssLink = item.Links.Last();
                    //string rssLinkToUse = rssLink.Uri.AbsoluteUri.ToString();

                    string rssLinkToUse = String.Format("{0}{1}{2}{3}", rssLink.Uri.Scheme,
            "://", rssLink.Uri.Authority, rssLink.Uri.AbsolutePath);


                    await httpDownloadAsync(rssLinkToUse);
                }
                catch (Exception foo)
                {
                    throw foo;
                }
            }

        }

        private async Task httpDownloadAsync(string stationURL)
        {

            Uri uri = new Uri(stationURL);

            StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync("Podblaster Downloads");

            StorageFile destinationFile = await PodBlasterFolder.CreateFileAsync(
                System.IO.Path.GetFileName(uri.ToString()), CreationCollisionOption.ReplaceExisting);

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(uri, destinationFile);

            StatusCommand.Text = "Starting Download of " + System.IO.Path.GetFileName(uri.ToString()) + "...";

            await download.StartAsync();

            StatusCommand.Text = "Downloaded " + System.IO.Path.GetFileName(uri.ToString()) + ".";

        }

        private async void Copy_Files_to_MP3(object sender, RoutedEventArgs e)
        {

            FolderPicker picker = new FolderPicker();

            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

            picker.FileTypeFilter.Add("*");
            StorageFolder podcastFolder = await picker.PickSingleFolderAsync();
            StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync("Podblaster Downloads");
            IReadOnlyList<StorageFile> fileListToCopy = await PodBlasterFolder.GetFilesAsync();

            for (int foo = 0; foo < fileListToCopy.Count(); foo++)
            {

                Debug.WriteLine(fileListToCopy[foo].Path.ToString());
                Debug.WriteLine(podcastFolder.Path.ToString());
                Debug.WriteLine(fileListToCopy[foo].Name.ToString());

                StorageFile destinationFile = await podcastFolder.CreateFileAsync(fileListToCopy[foo].Name.ToString(), CreationCollisionOption.ReplaceExisting);

                await fileListToCopy[foo].CopyAndReplaceAsync(destinationFile);

            }
        }


        private async void Clear_Cache_Button_Click(object sender, RoutedEventArgs e)
        {
           StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync("Podblaster Downloads");
            IReadOnlyList <StorageFile> fileListToDelete = await PodBlasterFolder.GetFilesAsync();
            
            for (int foo=0; foo < fileListToDelete.Count(); foo++ ) { 

            Debug.WriteLine(fileListToDelete[foo].Path.ToString() );

                await fileListToDelete[foo].DeleteAsync();
 

            }

        }

    }
}
