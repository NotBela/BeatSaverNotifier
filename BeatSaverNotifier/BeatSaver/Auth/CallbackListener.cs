using System;
using System.Net;
using System.Threading.Tasks;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.BeatSaver.Auth
{
    public class CallbackListener : IInitializable, IDisposable
    {
        [Inject] private readonly SiraLog _logger = null;
        [Inject] private readonly OAuthApi _oAuthApi = null;
        
        private readonly HttpListener _listener = new HttpListener
        {
            Prefixes = { callbackUri }
        };
        public const string callbackUri = "http://localhost:20198/";

        private bool listen;

        private void start()
        {
            listen = true;
            this._listener.Start();

            _ = Task.Run(async () =>
            {
                while (listen)
                {
                    try
                    {
                        var context = await this._listener.GetContextAsync();

                        var queries = context.Request.QueryString;
                        if (queries["code"] == null) throw new Exception("Code not in queries");
                        if (queries["state"] == null) throw new Exception("State not in queries");

                        await _oAuthApi.exchangeCodeForToken(queries["code"], queries["state"]);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                    }
                }
            });
        }
        
        private void stop() => listen = false;
        
        public void Initialize()
        {
            this.start();
        }

        public void Dispose()
        {
            this.stop();
        }
    }
}