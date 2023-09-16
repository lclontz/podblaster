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
    public sealed partial class AddNewShowAI : ContentDialog
    {
        public AddNewShowAI()
        {
            this.InitializeComponent();
        }

        private async Task Show_Error_Dialog(string errorMessage)
        {

            var messageDialog = new MessageDialog(errorMessage);
            messageDialog.Commands.Add(new UICommand("Close"));
            messageDialog.DefaultCommandIndex = 0;
            messageDialog.CancelCommandIndex = 0;

            await messageDialog.ShowAsync();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

            roamingSettings.Values["newFeedName"] = "";
            roamingSettings.Values["NewRSSName"] = "";

        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["newFeedName"] = NewFeedName.Text;
            roamingSettings.Values["NewRSSName"] = NewRSSName.Text;

            //Debug.WriteLine(NewFeedName.Text);

        }

        private async void ContentBtn_Click_AI(object sender, RoutedEventArgs args)
        {
            Debug.WriteLine("New feedname is: " + NewFeedName.Text);

            FeedDescription.Text = "";
            ShowIcon.Source = null;


            OpenAIClient client = new OpenAIClient(
new Uri(OPEN_AI_ENDPOINT),
new AzureKeyCredential(AZURE_AI_CREDENTIAL))

            Response<ChatCompletions> responseWithoutStream = await client.GetChatCompletionsAsync(
    "gpt-35-turbo",
    new ChatCompletionsOptions()
    {
        Messages =
        {
        new ChatMessage(ChatRole.System, @"You are a search engine for podcast RSS links. You will give current and accurate recommendations for podcast RSS feeds based on the show name provided. Always include a RSS link to the podcast and a short description of the content. Exclude any sexually-explicit podcasts. Your reply should only be JSON, and if you're not sure you should take your best guess."),
new ChatMessage(ChatRole.User, @"What is the RSS feed link for a podcast fitting the general description of '" + NewFeedName.Text + @"'. Respond in JSON format like:
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
            Debug.WriteLine("New feedname is: " + NewFeedName.Text);
            Debug.WriteLine(responseWithoutStream.Value.Choices[0].Message.Content);

            string json = responseWithoutStream.Value.Choices[0].Message.Content;
            try
            {
                JObject jsonContentAI = JObject.Parse(json);

                string showName = (string)jsonContentAI["name"];
                string showRSS = (string)jsonContentAI["rss"];
                string showDescription = (string)jsonContentAI["description"];

                Debug.WriteLine("The results variable is " + showName + " and " + showRSS);

                Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                NewFeedName.Text = showName;
                NewRSSName.Text = showRSS;
                FeedDescription.Text = showName + ":\n" + showDescription;


                Windows.Web.Syndication.SyndicationClient localClient = new Windows.Web.Syndication.SyndicationClient();
                Windows.Web.Syndication.SyndicationFeed feed;

                string feedName = showRSS;

                Uri rssFeed = new Uri(feedName);
                try
                {
                    feed = await localClient.RetrieveFeedAsync(rssFeed);
                    Uri getImageFromFeed = feed.ImageUri;
                    var bgimage = new BitmapImage(getImageFromFeed);

                    ShowIcon.Source = bgimage;

                }
                catch
                {
                    await Show_Error_Dialog("There's something wrong with that feed link. " +
                        "The AI data is probably out-of-date.");
                    FeedDescription.Text = "";
                    ShowIcon.Source = null;

                }

            }
            catch
            {
                await Show_Error_Dialog("Looks like the feed file is in error. The message received is: " + responseWithoutStream.Value.Choices[0].Message.Content);
                FeedDescription.Text = "";
                ShowIcon.Source = null;
            }

        }

        private void Button_Test_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(NewRSSName.Text);
        }
    }
}
