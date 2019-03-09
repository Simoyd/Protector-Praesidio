using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProtectorPraesidio
{
    class BoundlessTradeService
    {
        public JObject PriceCheck(ushort item, int? volume = null)
        {
            return PriceCheck(item.ToString(), volume);
        }

        public JObject PriceCheck(string item, int? volume = null)
        {
            return PriceCheckAsync(item, volume).Result;
        }

        public async Task<JObject> PriceCheckAsync(ushort item, int? volume = null)
        {
            return await PriceCheckAsync(item, volume);
        }

        public async Task<JObject> PriceCheckAsync(string item, int? volume = null)
        {
            item = new string(item.ToLower().Where(cur => char.IsLetterOrDigit(cur) || cur == ' ').ToArray()).Replace(" ", "%20");

            HttpClient client = new HttpClient();
            var result = await client.GetAsync($"https://boundlesstrade.net/api/v1/item/{item}/price-check{(volume.HasValue ? $"?volume={volume.Value}" : "")}");

            string resultContent = await result.Content.ReadAsStringAsync();

            return JObject.Parse(resultContent);
        }
    }
}
