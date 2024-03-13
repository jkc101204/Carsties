using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services
{
    public class AuctionServiceHttpClient
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration config;

        public AuctionServiceHttpClient(HttpClient httpClient, IConfiguration config)
        {
            this.httpClient = httpClient;
            this.config = config;
        }

        public async Task<List<Item>> GetItemsForSearchDb()
        {
            string lastUpdated = await DB.Find<Item, string>()
                .Sort(x => x.Descending(d => d.UpdatedAt))
                .Project(x => x.UpdatedAt.ToString())
                .ExecuteFirstAsync();

            return await this.httpClient.GetFromJsonAsync<List<Item>>(this.config["AuctionServiceUrl"] + "/api/auctions?date=" + lastUpdated);
        }
    }
}
