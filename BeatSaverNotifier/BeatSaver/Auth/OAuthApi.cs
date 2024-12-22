using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BeatSaverNotifier.Configuration;
using Newtonsoft.Json.Linq;
using UnityEngine.Experimental.Rendering;

namespace BeatSaverNotifier.BeatSaver.Auth
{
    public class OAuthApi
    {
        private readonly HttpClient _httpClient;

        private const string scope = "search";
        private const string clientId = "nNcZZiLg4egyqpgutJkw";
        private const string clientSecret = "0193baef-a69c-7569-ba03-52730b0d9fd4";
        private string lastState = String.Empty;

        public event Action onAccessCodeAquired;
        public event Action onAccessTokenAquired;

        public OAuthApi()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(
                $"BeatSaverNotifier/{Plugin.Instance.metaData.HVersion}");
        }

        private string getNewState()
        {
            var state = Guid.NewGuid().ToString();
            lastState = state;
            return state;
        }

        public void startNewOAuthFlow()
        {
            PluginConfig.Instance.refreshToken = "";
            
            System.Diagnostics.Process.Start($"https://beatsaver.com/oauth2/authorize?" +
                                             $"state={getNewState()}" +
                                             $"&client_id={clientId}" +
                                             $"&scope={scope}" +
                                             $"&response_type=code" +
                                             $"&redirect_uri={CallbackListener.callbackUri}");
        }

        private async Task<string> getNewToken()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://beatsaver.com/api/oauth2/token", UriKind.Absolute),
                Content = new FormUrlEncodedContent([
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", !string.IsNullOrEmpty(PluginConfig.Instance.refreshToken) 
                        ? PluginConfig.Instance.refreshToken 
                        : throw new Exception("No refresh token! (you may need to sign in with BeatSaver again)")),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret)
                ])
            };
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception($"Failed to refresh token with status code {(int) response.StatusCode}");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            PluginConfig.Instance.refreshToken = JObject.Parse(responseContent)["refresh_token"]?.Value<string>();
            return JObject.Parse(responseContent)["access_token"]?.Value<string>();
        }

        public async Task<HttpRequestMessage> addAuthenticationToRequest(HttpRequestMessage request)
        {
            var token = await getNewToken();
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        internal async Task<string> exchangeCodeForToken(string code, string state)
        {
            onAccessCodeAquired?.Invoke();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://beatsaver.com/api/oauth2/token", UriKind.Absolute),
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("redirect_uri", CallbackListener.callbackUri),
                    new KeyValuePair<string, string>("scope", scope),
                })
            };
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception($"Failed to exchange code for token: failed with status code {(int)response.StatusCode}");
            if (state != lastState) throw new Exception("Invalid state returned from BeatSaver!");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var parsedJson = JObject.Parse(responseContent);
            
            PluginConfig.Instance.refreshToken = parsedJson["refresh_token"]?.Value<string>();
            PluginConfig.Instance.isSignedIn = true;
            onAccessTokenAquired?.Invoke();
            return parsedJson["refresh_token"]?.Value<string>();
        }
    }
}