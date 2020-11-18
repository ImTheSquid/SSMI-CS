using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SSMediaIntegration
{
    class SteelSeries
    {
        private readonly string url;
        private readonly HttpClient client = new HttpClient();

        public SteelSeries(string url)
        {
            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            if (!url.StartsWith("http://"))
            {
                url = "http://" + url;
            }
            this.url = url;
        }

        private Uri UriAppendedWith(string append)
        {
            return new Uri(url + append);
        }

        public async Task<HttpResponseMessage> UpdateEvent(string songTitle, string[] artists, double playbackFraction)
        {
            string artistList = "";
            if (songTitle.Length > 14)
            {
                songTitle = songTitle.Substring(0, 11) + "...";
            }

            if (artists.Length == 1)
            {
                if (artists[0].Length > 14)
                {
                    artistList = artists[0].Substring(0, 11) + "...";
                }
                else
                {
                    artistList = artists[0];
                }
            }

            if (artists.Length > 1)
            {
                if (artistList.Length > 11)
                {
                    artistList = artistList.Substring(0, 8) + "...";
                }
                artistList += " +" + (artists.Length - 1);
            } 

            var data = new
            {
                game = "SSMI",
                @event = "UPDATE",
                data = new
                {
                    value = (int)(playbackFraction * 100),
                    frame = new Dictionary<string, string>()
                    {
                        { "first-line", songTitle },
                        { "second-line", artistList }
                    }
                }
            };
            HttpResponseMessage response = await client.PostAsJsonAsync(UriAppendedWith("game_event"), data);
            return response;
        }

        public async Task<HttpResponseMessage> RemoveEvent()
        {
            var data = new
            {
                game = "SSMI",
                @event = "UPDATE"
            };
            HttpResponseMessage response = await client.PostAsJsonAsync(UriAppendedWith("remove_game_event"), data);
            return response;
        }

        public async Task<HttpResponseMessage> AddMetadata()
        {
            var data = new 
            {
                game = "SSMI",
                game_display_name = "SteelSeries Media Integration",
                developer = "Jack Hogan"
            };
            HttpResponseMessage response = await client.PostAsJsonAsync(UriAppendedWith("game_metadata"), data);
            return response;
        }

        public async Task<HttpResponseMessage> RegisterEvent()
        {
            var data = new Dictionary<string, dynamic>()
            {
                { "game", "SSMI" },
                { "event", "UPDATE" },
                { "min_value", 0 },
                { "max_value", 100 },
                { "value-optional", false },
                { "handlers", new dynamic[1]
                {
                    new Dictionary<string, dynamic>()
                    {
                        { "device-type", "screened-128x40" },
                        { "zone", "one" },
                        { "mode", "screen" },
                        { "datas", new dynamic[1] 
                        {
                            new Dictionary<string, dynamic>()
                            {
                                { "icon-id", 23 },
                                { "lines", new dynamic[3]
                                {
                                    new Dictionary<string, dynamic>() {
                                        { "has-text", true },
                                        { "context-frame-key", "first-line" }
                                    },
                                    new Dictionary<string, dynamic>() {
                                        { "has-text", true },
                                        { "context-frame-key", "second-line" }
                                    },
                                    new Dictionary<string, dynamic>() {
                                        { "has-progress-bar", true }
                                    }
                                } }
                            }
                        } }
                    }
                    
                } }
            };
            HttpResponseMessage response = await client.PostAsJsonAsync(UriAppendedWith("bind_game_event"), data);
            return response;
        }

        public async Task<HttpResponseMessage> RemoveGame()
        {
            var data = new
            {
                game = "SSMI"
            };
            HttpResponseMessage response = await client.PostAsJsonAsync(UriAppendedWith("remove_game"), data);
            return response;
        }

        public async Task Logout()
        {
            await RemoveEvent();
            await RemoveGame();
        }

        public async Task<HttpResponseMessage> Heartbeat()
        {
            var data = new
            {
                game = "SSMI"
            };
            HttpResponseMessage response = await client.PostAsJsonAsync(UriAppendedWith("heartbeat"), data);
            return response;
        }
    }
}
