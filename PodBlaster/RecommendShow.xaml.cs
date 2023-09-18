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
using System.Diagnostics;
using Azure.AI.OpenAI;
using Azure;
using System.Xml.Linq;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;


// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PodBlaster
{
    public sealed partial class RecommendShow : ContentDialog
    {

        JObject jsonContentAI = new JObject();

        public RecommendShow(Podcast sourceRec)
        {

            
            this.InitializeComponent();

            

            getAIRecommendation(sourceRec);

        }

        private async Task Show_Error_Dialog(string errorMessage)
        {

            var messageDialog = new MessageDialog(errorMessage);
            messageDialog.Commands.Add(new UICommand("Close"));
            messageDialog.DefaultCommandIndex = 0;
            messageDialog.CancelCommandIndex = 0;

            await messageDialog.ShowAsync();
        }

        public async void getAIRecommendation(Podcast originalPodcast)
        {
            OpenAIClient client = new OpenAIClient(
new Uri(),
new AzureKeyCredential());

            Response<ChatCompletions> responseWithoutStream = await client.GetChatCompletionsAsync(
    "gpt-35-turbo",
    new ChatCompletionsOptions()
    {
        Messages =
        {
        new ChatMessage(ChatRole.System, @"You are a search engine for podcast RSS links. You will give current and accurate recommendations for podcast RSS feeds based on the show name provided. Always include a RSS link to the podcast and a short description of the content. Exclude any sexually-explicit podcasts. Your reply should only be JSON with no introductory text, and if you're not sure you should take your best guess."),
new ChatMessage(ChatRole.User, @"What is the RSS feed link for a similar podcast for someone who likes a podcast called '" + originalPodcast.stationName + @"'. Respond in JSON format like:
 { ""name"": ""the official name of the podcast""
""rss:"" ""rss feed""
""description:"" ""podcast description"""),


        },
        Temperature = (float)0.7,
        MaxTokens = 800,
        NucleusSamplingFactor = (float)0.95,
        FrequencyPenalty = 0,
        PresencePenalty = 0,
    });
            Debug.WriteLine("New feedname is: " + originalPodcast.stationName);
            Debug.WriteLine(responseWithoutStream.Value.Choices[0].Message.Content);

            string json = responseWithoutStream.Value.Choices[0].Message.Content;
          //  try
          //  {

                
                jsonContentAI = JObject.Parse(json);

                string  showName = (string)jsonContentAI["name"];
            string showRSS = (string)jsonContentAI["rss"];
            string showDescription = (string)jsonContentAI["description"];

           
           

            ShowRecommendationText.Text = showName + ": " + showDescription;
            IsPrimaryButtonEnabled = true;

            

            //  Debug.WriteLine("The results variable is " + showName + " and " + showRSS);

            
        }

        public async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            //var jsonContentAI = (sender as FrameworkElement).Tag as JObject

            Debug.WriteLine("The JSON Content in the button is " + jsonContentAI.ToString());

            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

            roamingSettings.Values["newFeedName"] = (string)jsonContentAI["name"];
            roamingSettings.Values["NewRSSName"] = (string)jsonContentAI["rss"];

            Windows.Web.Syndication.SyndicationClient localClient = new Windows.Web.Syndication.SyndicationClient();
            Windows.Web.Syndication.SyndicationFeed feed;

            string feedName = (string)jsonContentAI["rss"];

            Uri rssFeed = new Uri(feedName);
            try
            {
            feed = await localClient.RetrieveFeedAsync(rssFeed);
            //      Uri getImageFromFeed = feed.ImageUri;
            //      var bgimage = new BitmapImage(getImageFromFeed);

            //      ShowIcon.Source = bgimage;

            }
            catch
            {
            await Show_Error_Dialog("There's something wrong with that feed link. " +
            "The AI data is probably out-of-date.");

            }


            
             
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

            roamingSettings.Values["newFeedName"] = "";
            roamingSettings.Values["NewRSSName"] = "";

        }
    }
}
