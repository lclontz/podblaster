using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Xml;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Diagnostics;
using Windows.Storage;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.Web.Syndication;
using Windows.UI.Xaml.Documents;
using TagLibUWP;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using Windows.Media.Core;

namespace PodBlaster
{
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Podcast> podcasts { get; private set; }
        public ObservableCollection<Podcast_Episode> episodes { get; private set; }

        int currentlyDownloading = 0;

        public MainPage()
        {
            this.InitializeComponent();

            StatusCommand.Text = "No Active Downloads";

            ApplicationView.PreferredLaunchViewSize = new Size(320, 1000);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            podcasts = new ObservableCollection<Podcast>();

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await Write_No_Downloaded_Eps();

            StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync("Podblaster");

            await Show_Downloaded_Episodes(PodBlasterFolder);

            string XMLFilePath = PodBlasterFolder.Path + @"\stations.xml";

            Debug.WriteLine("Hey I'm here...");
            Debug.WriteLine(XMLFilePath);

            StorageFile XMLFile = await PodBlasterFolder.GetFileAsync("stations.xml");
            Stream XMLContent = await XMLFile.OpenStreamForReadAsync();

            string XMLOutput;

            using (StreamReader sr = new StreamReader(XMLContent))
            {
                String line = await sr.ReadToEndAsync();
                Debug.WriteLine(line);
                XMLOutput = line;
            }

            //XDocument stationsXML = XDocument.
            //XDocument.Load(XMLFilePath)
            XDocument stationsXML = XDocument.Parse(XMLOutput);
            
            //var elements = stationsXML.Element("stations").Element("station").Write
                


            var podData = from query in stationsXML.Descendants("station")
                          select new Podcast
                          {
                              stationName = (string)query.Element("stationName"),
                              stationURL = (string)query.Element("stationURL")
                              
                          };

            foreach (var n in podData)
            {
                PodList.Items.Add(n);
            }

           // PodList.CanDragItems = true;
            
           // PodList.Items.OrderByDescending(PodList.Items => )
            
            

            //PodList.ItemsSource = podData;
            

        }

        private async Task Show_Downloaded_Episodes(StorageFolder podBlasterFolder)
        {
            StorageFolder podBlasterDownloads = await KnownFolders.MusicLibrary.GetFolderAsync(@"PodBlaster\Downloads");
            IReadOnlyList<StorageFile> episodesDownloaded = await podBlasterDownloads.GetFilesAsync();

           // episodesDownloaded = new ObservableCollection<Downloaded_Podcast>();
          
            Downloaded_Episodes.Items.Clear();

            for (int foo = 0; foo < episodesDownloaded.Count(); foo++)
            {
                  if (episodesDownloaded[foo].Name.ToString().Substring(0,6) != "_temp_") {
                    //TextBlock TextBox = new TextBlock();

                    ListViewItem downloadedFile = new ListViewItem();
                    downloadedFile.Content = episodesDownloaded[foo].Name.ToString();
                    
                    Downloaded_Episodes.Items.Add(downloadedFile);
               // Downloaded_Episodes.Items.Add(TextBox);
                }
            }

            if (episodesDownloaded.Count() == 0) { await Write_No_Downloaded_Eps(); }
            
        }

        /*
private StorageFolder Get_PodBlaster_Folder(string mode) {

   string filePath;

   if (mode == "downloads") {
       filePath = KnownFolders.MusicLibrary.Path + @"\Podblaster\Downloads";
   } else {
       //string filePathEnv = @"%USERPROFILE%\Music\Podblaster";
       //string filePathstring filePathVariables = Environment.ExpandEnvironmentVariables(filePathEnv);

       filePath = UserDataPaths.GetDefault().Music + @"\Podblaster";
       
       Debug.WriteLine(filePath);

   }

   return filePath;

}
*/

        private async void PodList_ItemClick(object sender, ItemClickEventArgs e)
        {
            
            Windows.Web.Syndication.SyndicationClient client = new Windows.Web.Syndication.SyndicationClient();
            Windows.Web.Syndication.SyndicationFeed feed;

            Podcast poddy = e.ClickedItem as Podcast;

            string feedName = poddy.stationURL;

            Uri rssFeed = new Uri(feedName);
            feed = await client.RetrieveFeedAsync(rssFeed);

            PushFeedToDetails(feed);
            
        }



        private void PushFeedToDetails(SyndicationFeed feed)
        {

            Background_Episodes_Image.Opacity = 0;

            // Show Image
            Uri getImageFromFeed = feed.ImageUri;
            //ImageSource feedImage = getImageFromFeed.ToString();

           // Debug.WriteLine(getImageFromFeed.ToString());

            try { 

            var bgimage = new BitmapImage(getImageFromFeed);
  
            Background_Episodes_Image.Source = bgimage;
                Podcast_PosterImage.Source = bgimage;

                SolidColorBrush blackBrush = new SolidColorBrush(Windows.UI.Colors.Black);
                
                PosterBorder.BorderBrush = blackBrush;
            } catch (Exception e)
            {
                Background_Episodes_Image.Source = null;
                Podcast_PosterImage.Source = null;
                SolidColorBrush whiteBrush= new SolidColorBrush(Windows.UI.Colors.White);

                PosterBorder.BorderBrush = whiteBrush;
            }

            episodes = new ObservableCollection<Podcast_Episode>();

            int NumberOfEpisodes = feed.Items.Count;

            PodcastHeader.Text = feed.Title.Text;

            Episode_List.Items.Clear();
            
            
            for (int i = 0; i < (NumberOfEpisodes); i++)
            {
                SyndicationItem item = feed.Items[i];
                SyndicationLink link = feed.Links[0];
                //  Debug.WriteLine(item.Summary.Text);
                //  Debug.WriteLine(item.PublishedDate.ToString());

                TextBlock TextBox = new TextBlock();
                TextBox.Text = item.PublishedDate.ToLocalTime().Year.ToString() + "-" +
                    item.PublishedDate.ToLocalTime().Month.ToString() + "-" +
                    item.PublishedDate.ToLocalTime().Day.ToString() + ": " + item.Title.Text;
                    
                    
                //Uri currentAddUri = new Uri(item.ItemUri.AbsoluteUri);
                // currentUris.Add(currentAddUri);



                Hyperlink hyperlink = new Hyperlink();

                Windows.Web.Syndication.SyndicationLink rssLink = item.Links.Last();
                //string rssLinkToUse = rssLink.Uri.AbsoluteUri.ToString();

                string rssLinkToUse = String.Format("{0}{1}{2}{3}", rssLink.Uri.Scheme,
                "://", rssLink.Uri.Authority, rssLink.Uri.AbsolutePath);

                

                hyperlink.NavigateUri = new Uri(rssLinkToUse);

                TextBox.Inlines.Add(hyperlink);
                
                Episode_List.Items.Add(TextBox);

            }



        }

        

        private async Task HttpDownloadAsync(string stationURL, string episodeNameString, string showName)
        {

            Debug.WriteLine("Made it to Download");

            Uri uri = new Uri(stationURL);

            Debug.WriteLine("StationURL is " + stationURL);
            
            StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync(@"PodBlaster\Downloads");

            Debug.WriteLine("StorageFolder allocated");

            StorageFile destinationFile = await PodBlasterFolder.CreateFileAsync(
                "_temp_" + System.IO.Path.GetFileName(uri.ToString()), CreationCollisionOption.ReplaceExisting);

            
            Debug.WriteLine("DestinationFile created");

            BackgroundDownloader downloader = new BackgroundDownloader();

            Debug.WriteLine("BackgroundDownloader created");
            DownloadOperation download = downloader.CreateDownload(uri, destinationFile);

            Debug.WriteLine("DownloadOperation created");

            await Add_Another_Download(System.IO.Path.GetFileName(uri.ToString()));
           
            Debug.WriteLine("Status Command Sent");

            await download.StartAsync();

            await CompleteDownloading(System.IO.Path.GetFileName(uri.ToString()));

            //destinationFile.RenameAsync()

            await destinationFile.RenameAsync(showName + " - " + episodeNameString + ".mp3", NameCollisionOption.ReplaceExisting);

            await FixUp_ID3_Tags(destinationFile, showName, episodeNameString);

            await Show_Downloaded_Episodes(PodBlasterFolder);
           
        }

        private async Task CompleteDownloading(string fileNameDownloading)
        {
            currentlyDownloading--;
            if (currentlyDownloading == 0)
            {
                StatusCommand.Text = "All downloads complete.";
            } else {
                if (currentlyDownloading == 1) { 
                StatusCommand.Text = currentlyDownloading + " file currently downloading. ";
                } else
                {
                    StatusCommand.Text = currentlyDownloading + " files currently downloading. ";
                }
                //Now downloading " + fileNameDownloading + "...";
            }
        }

        private async Task Add_Another_Download(string fileNameDownloading)
        {
            currentlyDownloading++;
            if (currentlyDownloading == 1)
            {
                StatusCommand.Text = currentlyDownloading + " file currently downloading. ";
            }
            else
            {
                StatusCommand.Text = currentlyDownloading + " files currently downloading. ";
            }
            //Now downloading " + fileNameDownloading + "...";


            //BackgroundDownloader downloadingInfo = new BackgroundDownloader();

            //await BackgroundDownloader.GetCurrentDownloadsAsync();
        }

        private async Task FixUp_ID3_Tags(StorageFile destinationFile, string showName, string episodeNameString)
        {
            var gimmeTheDebug = await Task.Run(() => TagManager.ReadFile(destinationFile));
            var tag = gimmeTheDebug.Tag;

            tag.Title = episodeNameString;
            tag.Album = showName;

            await Task.Run(() => TagManager.WriteFile(destinationFile, tag));

            gimmeTheDebug = await Task.Run(() => TagManager.ReadFile(destinationFile));

            string title = tag.Title;
            Debug.WriteLine(title.ToString());

            string album = tag.Album;
            Debug.WriteLine(album.ToString());
        }

        private async void Copy_Files_to_MP3(object sender, RoutedEventArgs e)
        {

            FolderPicker picker = new FolderPicker();

            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            
            picker.FileTypeFilter.Add("*");
            StorageFolder podcastFolder = await picker.PickSingleFolderAsync();
            StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync(@"Podblaster\Downloads");
            IReadOnlyList<StorageFile> fileListToCopy = await PodBlasterFolder.GetFilesAsync();

            StatusCommand.Text = "File copy started. Don't disconnect your player!";

            for (int foo = 0; foo < fileListToCopy.Count(); foo++)
            {

                Debug.WriteLine(fileListToCopy[foo].Path.ToString());
                Debug.WriteLine(podcastFolder.Path.ToString());
                Debug.WriteLine(fileListToCopy[foo].Name.ToString());

                StorageFile destinationFile = await podcastFolder.CreateFileAsync(fileListToCopy[foo].Name.ToString(), CreationCollisionOption.ReplaceExisting);

                await fileListToCopy[foo].CopyAndReplaceAsync(destinationFile);

            }
            StatusCommand.Text = "Podcasts copied. You may now disconnect";
        }


        private async void Clear_Cache_Button_Click(object sender, RoutedEventArgs e)
        {
           StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync(@"Podblaster\Downloads");
            IReadOnlyList <StorageFile> fileListToDelete = await PodBlasterFolder.GetFilesAsync();
            
            for (int foo=0; foo < fileListToDelete.Count(); foo++ ) { 

            Debug.WriteLine(fileListToDelete[foo].Path.ToString() );

                await fileListToDelete[foo].DeleteAsync();
 

            }

            await Show_Downloaded_Episodes(PodBlasterFolder);

        }

        private async void Episode_List_ItemClick(object sender, ItemClickEventArgs e)
        {

            TextBlock selectedTextBlock = e.ClickedItem as TextBlock;
            Inline thisLink = selectedTextBlock.Inlines.ElementAt(1);
            Hyperlink thisLinkURL = (Hyperlink)thisLink;
            Debug.WriteLine(thisLinkURL.NavigateUri.ToString());
            Debug.WriteLine("Hey, what about this?");
            Debug.WriteLine(selectedTextBlock.Text.ToString());

            string nameOfShow = selectedTextBlock.Text.ToString();

            foreach (var c in Path.GetInvalidFileNameChars()) { nameOfShow = nameOfShow.Replace(c, '-'); }

            await HttpDownloadAsync(thisLinkURL.NavigateUri.ToString(), nameOfShow, PodcastHeader.Text.ToString());
          

        }

        private void Background_Episodes_Image_ImageOpened(object sender, RoutedEventArgs e)
        {
            var opacityAnimation = new DoubleAnimation
            {
                To = .1,
                Duration = TimeSpan.FromSeconds(1)
            };

            Storyboard.SetTarget(opacityAnimation, (DependencyObject)sender);
            Storyboard.SetTargetProperty(opacityAnimation, "Background_Episodes_Image.Opacity");

            var storyboard = new Storyboard();
            storyboard.Children.Add(opacityAnimation);
            storyboard.Begin();

        }

        private async void Downloaded_Episodes_ItemClick(object sender, ItemClickEventArgs e)
        {
            TextBlock selectedLocalEpisode = e.ClickedItem as TextBlock;
            string playFileName = selectedLocalEpisode.Text.ToString();
            Debug.WriteLine("Here's the filename: ");
            Debug.WriteLine(playFileName);

            StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync(@"Podblaster\Downloads");
            StorageFile playFile = await PodBlasterFolder.GetFileAsync(playFileName);
            Uri playFileUri = new Uri(playFile.Path.ToString());
            Debug.WriteLine("Here's the URI:");
            Debug.WriteLine(playFileUri.ToString());
            // IMediaPlaybackSource mediaPlaybackSource = 

            MediaSource playSource = MediaSource.CreateFromStorageFile(playFile);

            Podcast_Player_Element.Source = playSource;

        }

        private async void Downloaded_Episodes_ButtonClick(object sender, SelectionChangedEventArgs e)
        {
            //TextBlock selectedLocalEpisode = e.ClickedItem as TextBlock;
            //var selectedLocalItem = (sender as ListView).SelectedValue.ToString();

            var selectedEpisode = e.AddedItems?.FirstOrDefault();
            // edit: also get container
            var container = ((ListViewItem)(Downloaded_Episodes.ContainerFromItem(selectedEpisode)));

           // int selectedListItem = Downloaded_Episodes.SelectedIndex;
            ListViewItem selectedListItem = (ListViewItem)Downloaded_Episodes.SelectedItem;
            //ListViewItem thisOne = selectedListItem.Content.ToString();

            Debug.WriteLine("The index is " + selectedListItem.Content.ToString() );
            string playFileName = selectedListItem.Content.ToString();

            
            Debug.WriteLine("Here's the filename: ");
            Debug.WriteLine(playFileName);

            StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync(@"Podblaster\Downloads");
            StorageFile playFile = await PodBlasterFolder.GetFileAsync(playFileName);
            Uri playFileUri = new Uri(playFile.Path.ToString());
            Debug.WriteLine("Here's the URI:");
            Debug.WriteLine(playFileUri.ToString());
            // IMediaPlaybackSource mediaPlaybackSource = 

            MediaSource playSource = MediaSource.CreateFromStorageFile(playFile);

            Podcast_Player_Element.Source = playSource;

        }

        private async Task  Write_No_Downloaded_Eps()
        {

            TextBlock TextBox = new TextBlock();
            TextBox.Text = "No Currently Downloaded Episodes";
            
            Downloaded_Episodes.Items.Add(TextBox);

        }

        private async void Add_New_Show(object sender, RoutedEventArgs e)
        {
            // Instantiate window

            string newFeedNameVar;

            AddNewShow newShow = new AddNewShow();
            ContentDialogResult addNewResult = await newShow.ShowAsync();

            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            Debug.WriteLine(roamingSettings.Values["newFeedName"]);
            Debug.WriteLine(roamingSettings.Values["newRSSName"]);

            Podcast createPodcast = new Podcast();
            createPodcast.stationName = roamingSettings.Values["newFeedName"].ToString();
            createPodcast.stationURL = roamingSettings.Values["newRSSName"].ToString();

            PodList.Items.Add(createPodcast);

            //podcasts.Add(createPodcast);

            await Dump_Out_XML();

            Debug.WriteLine(podcasts.Count());


        }

        private async Task Dump_Out_XML()
        {
            Debug.WriteLine("Debugging Dump...");

            XmlDocument outputXML = new XmlDocument();
            

            XmlElement rootElement = outputXML.CreateElement(string.Empty, "stations", string.Empty);
            outputXML.AppendChild(rootElement);
            
            XmlSignificantWhitespace sigws = outputXML.CreateSignificantWhitespace("\n\t");
            rootElement.InsertAfter(sigws, rootElement.FirstChild);

            foreach (Podcast thisOne in PodList.Items) 
            {
                XmlElement stationElement = outputXML.CreateElement(string.Empty, "station", string.Empty);
                
                XmlElement stationNameElement = outputXML.CreateElement(string.Empty, "stationName", string.Empty);
                  
                XmlText text1 = outputXML.CreateTextNode(thisOne.stationName);
                stationNameElement.AppendChild(text1);
                stationElement.AppendChild(stationNameElement);
                stationElement.InsertAfter(sigws, stationNameElement);

                XmlElement stationURLElement = outputXML.CreateElement(string.Empty, "stationURL", string.Empty);

                XmlText text2 = outputXML.CreateTextNode(thisOne.stationURL);
                stationURLElement.AppendChild(text2);
                stationElement.AppendChild(stationURLElement);
                stationElement.InsertAfter(sigws, stationURLElement);


                rootElement.AppendChild(stationElement);

                
                
                

                Debug.WriteLine(thisOne.stationName);
                Debug.WriteLine(thisOne.stationURL);
            }

            

            Debug.WriteLine(outputXML.InnerXml.ToString());

            StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync("Podblaster");
          
            string XMLFilePath = PodBlasterFolder.Path + @"\stations.xml";

            StorageFile XMLFile = await PodBlasterFolder.GetFileAsync("stations.xml");

            await FileIO.WriteTextAsync(XMLFile, outputXML.InnerXml.ToString());

            

        }

        private async void DebugMe_Click(object sender, RoutedEventArgs e)
        {

            await Dump_Out_XML();
            /*
            StorageFolder PodBlasterFolder = await KnownFolders.MusicLibrary.GetFolderAsync(@"Podblaster\Downloads");
            StorageFile playFile = await PodBlasterFolder.GetFileAsync("npr_politics.mp3");

            var gimmeTheDebug = await Task.Run(() => TagManager.ReadFile(playFile));
            var tag = gimmeTheDebug.Tag;

            tag.Title = "NPR Politics - Lee";
            tag.Album = "Hey Look, An Episode";

            await Task.Run(() => TagManager.WriteFile(playFile, tag));

            gimmeTheDebug = await Task.Run(() => TagManager.ReadFile(playFile));

            string title = tag.Title;
            Debug.WriteLine(title.ToString());
            
            string album = tag.Album;
            Debug.WriteLine(album.ToString());
            */
        }

        private async void Delete_Show(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem);

            var listItem = item.DataContext as Podcast;

            PodList.Items.Remove(listItem);
            Clear_Middle();
            Debug.WriteLine(listItem.ToString());

            await Dump_Out_XML();
        }

        private void TextBlock_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Debug.WriteLine("FOO");

            var thisTextBlock = (TextBlock)sender;


            Debug.WriteLine(thisTextBlock.Text);

            var senderBlock = (TextBlock)sender;

            DeleteShowFlyout.ShowAt(senderBlock);

           // Debug.WriteLine(senderBlock.)
        }

        private async void Clear_Middle()
        {
            Episode_List.Items.Clear();
            PodcastHeader.Text = "";
            Background_Episodes_Image.Source = null;
        }

        private void Delete_Episode(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem);

            //var listItem = item.DataContext as Podcast;

           // PodList.Items.Remove(listItem);
           // Clear_Middle();
           // Debug.WriteLine(listItem.ToString());
            
        }

        private void Downloaded_Episodes_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem);
            var senderList = (ListView)sender;
            var senderBlock = (ListViewItem)senderList.SelectedItem;

            var originalSource = (FrameworkElement)e.OriginalSource;

            Debug.WriteLine(originalSource.ToString());
            DeleteDownloadedFlyout.ShowAt(originalSource);
        }
    }
}
