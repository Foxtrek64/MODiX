using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using Modix.Services.CommandHelp;
using Modix.Services.Utilities;
using Newtonsoft.Json;
using Serilog;

namespace Modix.Modules
{
    [Name("Fun")]
    [Summary("A bunch of miscellaneous, fun commands.")]
    [HelpTags("jumbo")]
    public class FunModule : ModuleBase
    {
        public FunModule(IHttpClientFactory httpClientFactory)
        {
            HttpClientFactory = httpClientFactory;
        }

        [Command("jumbo"), Summary("Jumbofy an emoji.")]
        public async Task JumboAsync(
            [Summary("The emoji to jumbofy.")]
                string emoji)
        {
            var emojiUrl = EmojiUtilities.GetUrl(emoji);

            try
            {
                var client = HttpClientFactory.CreateClient();
                var req = await client.GetStreamAsync(emojiUrl);

                await Context.Channel.SendFileAsync(req, Path.GetFileName(emojiUrl), Context.User.Mention);
                
                try
                {
                    await Context.Message.DeleteAsync();
                }
                catch (HttpRequestException)
                {
                    Log.Information("Couldn't delete message after jumbofying.");
                }
            }
            catch (HttpRequestException)
            {
                await ReplyAsync($"Sorry {Context.User.Mention}, I don't recognize that emoji.");
            }
        }

        [Command("xkcd"), Summary("Posts a random xkcd article")]
        public async Task PostRandomXkcd()
        {
            var client = HttpClientFactory.CreateClient();
            var request = await client.GetStringAsync("https://xkcd.com/info.0.json");

            var article = JsonConvert.DeserializeObject<XkcdArticle>(request);

            int latest = article?.Id ?? 2100;

            await PostXkcd(latest);
        }

        [Command("xkcd"), Summary("Posts the specified xkcd article")]
        public async Task PostXkcd([Summary("The zero-based Id of the article you're trying to get.")] int id)
        {
            await ReplyAsync($"https://xkcd.com/{id}/");
        }

        protected IHttpClientFactory HttpClientFactory { get; }
    }

    [JsonObject]
    internal class XkcdArticle
    {
        public int Month { get; set; }
        [JsonProperty("num")]
        public int Id { get; set; }
        public string Link { get; set; }
        public int Year { get; set; }
        public string News { get; set; }
        [JsonProperty("safe_title")]
        public string SafeTitle { get; set; }
        public string Transcript { get; set; }
        [JsonProperty("alt")]
        public string Description { get; set; }
        [JsonProperty("img")]
        public string ImageUrl { get; set; }
        public string Title { get; set; }
        public int Day { get; set; }
    }
}
