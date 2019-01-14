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

namespace PodBlaster
{
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Podcast> podcasts { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();
            podcasts = new ObservableCollection<Podcast>();

            string XMLFilePath = Path.Combine(Package.Current.InstalledLocation.Path, "stations.xml");
            XDocument stationsXML = XDocument.Load(XMLFilePath);

            //string stationName = stationsXML.DocumentElement.SelectSingleNode("station/stationName").InnerText;

            // XmlNodeList stationList = stationsXML.SelectNodes("stations/station");

            var podData = from query in stationsXML.Descendants("station")
                       select new Podcast
                       {
                           stationName = (string)query.Element("stationName"),
                           stationURL = (string)query.Element("stationURL")
                       };

            PodList.ItemsSource = podData;

           // MsgBox.Show("Populated");

            //Console.WriteLine(stationList.Count);


        

        }

       

        private async void PodList_ItemClick(object sender, ItemClickEventArgs e)
        {

            Windows.Web.Syndication.SyndicationClient client = new Windows.Web.Syndication.SyndicationClient();
            Windows.Web.Syndication.SyndicationFeed feed;

            Podcast poddy = e.ClickedItem as Podcast;

            string feedName = poddy.stationURL;
            // XmlDocument rssFeed = new XmlDocument();

            // rssFeed.Load(new StringReader(feedName));

            // XmlNodeList itemNodes = rssFeed.GetElementsByTagName("item");
            // string nodeURL = itemNodes[0].Attributes["url"]?.InnerText;

            Uri rssFeed = new Uri(feedName);
            feed = await client.RetrieveFeedAsync(rssFeed);

            int getNumberOfDownloads = epDownloads.SelectedIndex;

            if (getNumberOfDownloads == 0) { getNumberOfDownloads = 1; }

            for (int i = 0; i < getNumberOfDownloads; i++)
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

            await download.StartAsync();





        }

        private async void Copy_Files_to_MP3(object sender, RoutedEventArgs e)
        {
            //StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync("Podblaster Downloads");
            //IReadOnlyList<StorageFile> fileListToCopy = await PodBlasterFolder.GetFilesAsync();

//            StorageFolder DestinationFolder = await KnownFolders.RemovableDevices

  //          foreach (var item in fileListToCopy)
    //        {
      //          StorageFile sandiskFile = await DestinationFolder.CreateFileAsync(item.ToString());
        //        await item.CopyAsync(DestinationFolder);
        //
          //  }


            // StorageFile destinationFile = await PodBlasterFolder;
        }

        private async void Clear_Cache_Button_Click(object sender, RoutedEventArgs e)
        {
           StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync("Podblaster Downloads");
            IReadOnlyList <StorageFile> fileListToDelete = await PodBlasterFolder.GetFilesAsync();

            

           // StorageFile destinationFile = await PodBlasterFolder;
        }
    }
}
