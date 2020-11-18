using Newtonsoft.Json;
using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using SpotifyAPI.Web;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SSMediaIntegration
{
    public partial class SSMIService : ServiceBase
    {
        const string clientId = "dca51e3f460548018a6f0b305ee3724e";

        string dataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\SSMI";
        System.Timers.Timer callTimer = new System.Timers.Timer();
        SpotifyClient spotifyClient = null;
        SteelSeries steelSeries = null;
        EventLog eventLog = new EventLog();

        bool ssLoggedIn = false;
        uint deadTimer = 0;

        public class CoreProps
        {
            public string address { set; get; }
            public string encrypted_address { set; get; }
        }

        public class RefreshToken
        {
            public string refresh_token { set; get; }
        }
        
        public SSMIService()
        {
            InitializeComponent();

            if (!EventLog.SourceExists("Application"))
            {
                EventLog.CreateEventSource("SSMI", "Default");
            }
            eventLog.Source = "SSMI";
            eventLog.Log = "Default";
        }

        protected override void OnStart(string[] args)
        {
            WriteLog("SSMI Startup");
            // Set data path if needed and initialize log, timer
            ServiceStatus status = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(ServiceHandle, ref status);

            callTimer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            callTimer.Interval = 1000;
            callTimer.Enabled = true;

            // Connect with SteelSeries
            Login();
        }

        protected override void OnStop()
        {
            ServiceStatus status = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_STOP_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(ServiceHandle, ref status);

            if (steelSeries != null && ssLoggedIn)
            {
                steelSeries.Logout().Wait();
            }
            WriteLog("SSMI Shutdown");

            status = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_STOPPED
            };
            SetServiceStatus(ServiceHandle, ref status);
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            if (spotifyClient != null)
            {
                Task<CurrentlyPlayingContext> playback = null;
                try
                {
                    playback = spotifyClient.Player.GetCurrentPlayback();
                    playback.Wait();
                }
                catch (Exception exception)
                {
                    WriteLog($"Exception caught: {exception.ToString()}");
                    return;
                }

                if (!playback.Result.IsPlaying || playback.Result.Device.Type.ToLower() != "computer")
                {
                    deadTimer++;
                }
                else if (playback.Result.IsPlaying && playback.Result.Device.Type.ToLower() == "computer")
                {
                    deadTimer = 0;
                }

                if (deadTimer >= 5)
                {
                    ssLoggedIn = false;
                    steelSeries.RemoveEvent().Wait();
                }
                else if (deadTimer == 0 && !ssLoggedIn)
                {
                    ssLoggedIn = true;
                    steelSeries.RegisterEvent().Wait();
                }

                if (deadTimer > 0 || !ssLoggedIn)
                {
                    steelSeries.Heartbeat().Wait();
                    return;
                }

                if (playback.Result.Item is FullTrack track) {
                    steelSeries.UpdateEvent(track.Name, GetArtistNames(track.Artists).ToArray(), (double)playback.Result.ProgressMs / track.DurationMs).Wait();
                } else if (playback.Result.Item is FullEpisode episode)
                {
                    steelSeries.UpdateEvent(episode.Name, new string[0], (double)playback.Result.ProgressMs / episode.DurationMs).Wait();
                }
            }
        }

        private List<string> GetArtistNames(List<SimpleArtist> artists)
        {
            var artistsOut = new List<string>();

            artists.ForEach((artist) =>
            {
                artistsOut.Add(artist.Name);
            });

            return artistsOut;
        }

        private void WriteLog(string msg)
        {
            string path = dataPath + "\\logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filePath = dataPath + "\\logs\\SSMI_Log_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            DateTime time = DateTime.Now.ToLocalTime();
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine($"[{time.ToShortDateString()} {time.ToShortTimeString()}] {msg}");
            }

            // Log to EventLog
            eventLog.WriteEntry(msg);
        }

        // Utilities to read OAuth creds to mitigate repeat logins
        private PKCETokenResponse ReadOAuthCreds()
        {
            if (File.Exists(dataPath + "\\oauth.json"))
            {
                using (StreamReader sr = File.OpenText(dataPath + "\\oauth.json"))
                {
                    return JsonConvert.DeserializeObject<PKCETokenResponse>(sr.ReadToEnd());
                }
            } else
            {
                return null;
            }
        }

        private void WriteOAuthCreds(PKCETokenResponse data)
        {
            File.WriteAllText(dataPath + "\\oauth.json", JsonConvert.SerializeObject(data));
        }

        private void Login()
        {
            WriteLog("Attempting to find coreProps.json");
            int tries = 0;
            string coreProps = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\SteelSeries\\SteelSeries Engine 3\\coreProps.json";
            while (tries < 10 && !File.Exists(coreProps))
            {
                Thread.Sleep(5000);
                ++tries;
            }

            if (tries == 10)
            {
                WriteLog($"Error: Couldn't find coreProps.json at path {coreProps}");
                Stop();
                return;
            }

            CoreProps output = new CoreProps();
            // Read coreProps.json
            using (StreamReader sr = File.OpenText(coreProps))
            {
                output = JsonConvert.DeserializeObject<CoreProps>(sr.ReadToEnd());
            }

            // Initialize HTTP
            string formatted = $"http://{output.address}/";
            WriteLog($"coreProps.json found with address {formatted}");
            steelSeries = new SteelSeries(formatted);

            // SteelSeries Engine has been identified and connection has been initialized. Time to move on to Spotify

            var creds = ReadOAuthCreds();
            // If creds are already present
            if (creds != null)
            {
                var newResponse = new OAuthClient().RequestToken(
                    new PKCETokenRefreshRequest(clientId, creds.RefreshToken)
                );
                newResponse.Wait();
                WriteOAuthCreds(newResponse.Result);
                spotifyClient = new SpotifyClient(newResponse.Result.AccessToken);
            }
            else
            {
                WriteLog("Failed to load Spotify credentials, please use the helper program (SSMIHelper.exe)");
                Stop();
                return;
            }

            WriteLog("Loading completed, registering event");
            var task = steelSeries.AddMetadata();
            task.Wait();
            WriteLog($"Metadata: {task.Result.StatusCode}");
            task = steelSeries.RegisterEvent();
            task.Wait();
            WriteLog($"Event: {task.Result.StatusCode}");
            ssLoggedIn = true;
            ServiceStatus status = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_RUNNING
            };
            SetServiceStatus(ServiceHandle, ref status);
        }

        // Service stuff
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
