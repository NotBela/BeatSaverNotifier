using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.BeatSaver.Auth
{
    public class CallbackListener : IInitializable, IDisposable
    {
        [Inject] private readonly SiraLog _logger = null;
        [Inject] private readonly OAuthApi _oAuthApi = null;
        
        private HttpListener _listener = new HttpListener
        {
            Prefixes = { callbackUri }
        };
        public const string callbackUri = "http://localhost:20198/";

        private bool listen;
        
        private readonly byte[] responseBuffer = Encoding.UTF8.GetBytes("<p>Authenticated with BeatSaver successfully! You can now close this tab.</p>");

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

                        var responsePage = context.Response;
                        responsePage.ContentType = "text/html";
                        responsePage.StatusCode = 200;
                        responsePage.ContentLength64 = responseBuffer.Length;

                        await responsePage.OutputStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    }
                    catch (ObjectDisposedException)
                    {
                        _listener = new HttpListener()
                        {
                            Prefixes = { callbackUri }
                        };
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                    }
                }
            });
        }

        private void stop()
        {
            listen = false;
            this._listener.Stop();
        }
        
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