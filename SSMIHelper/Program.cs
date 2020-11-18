using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSMIHelper
{
    class Program
    {
        static string clientId = "dca51e3f460548018a6f0b305ee3724e";

        static string dataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\SSMI";

        static void Main(string[] args)
        {
            Console.WriteLine("===SSMI Helper Program===");
            GetSpotifyCredentials().Wait();
        }

        private static async Task GetSpotifyCredentials()
        {
            string code = null;
            Console.WriteLine("Begin Spotify code aquisition");

            // Temporarily hosts a web server to get redirect information
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8888/callback/");
            listener.Start();
            IAsyncResult result = listener.BeginGetContext(new AsyncCallback((callback) =>
            {
                HttpListener listen = (HttpListener)callback.AsyncState;
                HttpListenerContext context = listen.EndGetContext(callback);
                HttpListenerRequest request = context.Request;
                code = request.QueryString["code"];

                HttpListenerResponse response = context.Response;
                string responseString = "<HTML><BODY><H1>Authentication successful. You may now close this page.</H1></BODY></HTML>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // Close the output stream
                output.Close();
            }), listener);

            // Generates a secure random verifier of length 100 and its challenge
            var (verifier, challenge) = PKCEUtil.GenerateCodes();

            var loginRequest = new LoginRequest(
              new Uri("http://localhost:8888/callback"),
              clientId,
              LoginRequest.ResponseType.Code
            )
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
                Scope = new[] { Scopes.UserReadCurrentlyPlaying, Scopes.UserReadPlaybackState }
            };
            var uri = loginRequest.ToUri();
            Console.WriteLine("Attempting to start browser...");
            Process.Start(uri.ToString());

            // Wait 5 minutes or until code value is fulfilled
            for (int i = 0; i < 300; i++)
            {
                if (code != null)
                {
                    break;
                }
                Console.WriteLine($"Waiting for user to connect account... ({i}/300 sec)");
                Thread.Sleep(1000);
            }

            if (code == null)
            {
                Console.WriteLine("Failed to fetch code! Exiting...");
                Environment.Exit(1);
            }
            Console.WriteLine("Code aquisition successful");
            listener.Stop();

            // Log in to Spotify
            // Note that we use the verifier calculated above!
            var initialResponse = await new OAuthClient().RequestToken(
              new PKCETokenRequest(clientId, code, new Uri("http://localhost:8888/callback"), verifier)
            );

            WriteOAuthCreds(initialResponse);
            Console.WriteLine("File written");
        }

        private static void WriteOAuthCreds(PKCETokenResponse data)
        {
            File.WriteAllText(dataPath + "\\oauth.json", JsonConvert.SerializeObject(data));
        }
    }
}
