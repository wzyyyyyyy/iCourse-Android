﻿using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace iCourse_Android
{
    class Http : HttpClient
    {
        public Http(TimeSpan timeout)
            : base(
                  new HttpClientHandler { 
                      UseCookies = true, 
                      ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            })
        {
            Timeout = timeout;
            BaseAddress = new Uri("https://icourses.jlu.edu.cn");

            DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            DefaultRequestHeaders.Add("Host", "icourses.jlu.edu.cn");
            DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
        }

        public void SetOrigin(string origin)
        {
            DefaultRequestHeaders.Remove("Origin");
            DefaultRequestHeaders.Add("Origin", origin);
        }

        public void SetReferer(string referer)
        {
            DefaultRequestHeaders.Remove("Referer");
            DefaultRequestHeaders.Add("Referer", referer);
        }

        public void AddHeader(string key, string value)
        {
            DefaultRequestHeaders.Remove(key);
            DefaultRequestHeaders.Add(key, value);
        }

        public void AddCookie(string cookie)
        {
            DefaultRequestHeaders.Add("Cookie", cookie);
        }

        private async Task<string> SendWithRetryAsync(Func<Task<HttpResponseMessage>> sendRequest)
        {
            int retryAttempt = 0;
            while (true)
            {
                try
                {
                    using var response = await sendRequest();
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
                catch (TaskCanceledException ex)
                {
                    retryAttempt++;
                    var waitTime = TimeSpan.FromMilliseconds(Math.Pow(2, Math.Min(retryAttempt, 8))); // 上限为256ms
                    MainPage.Instance.WriteLine($"重试第 {retryAttempt} 次，等待时间 {waitTime.TotalSeconds} 秒");
                    await Task.Delay(waitTime);
                }
                catch (Exception ex)
                {
                    retryAttempt++;
                    var waitTime = TimeSpan.FromMilliseconds(Math.Pow(2, Math.Min(retryAttempt, 8))); // 上限为256ms
                    MainPage.Instance.WriteLine($"发生异常：{ex}，等待 {waitTime.TotalSeconds} 秒后重试。");
                    await Task.Delay(waitTime);
                }
            }
        }

        public async Task<string> HttpGetAsync(string url)
        {
            return await SendWithRetryAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                return await SendAsync(request);
            });
        }

        public async Task<string> HttpPostAsync(string url, HttpContent? content)
        {
            return await SendWithRetryAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                return await SendAsync(request);
            });
        }
    }
}
