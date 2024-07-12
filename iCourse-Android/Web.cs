﻿using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace iCourse_Android
{
    public class Web(string username, string password)
    {
        private Http client;

        private string aesKey;
        private readonly string username = username;
        private string password = password;
        private string uuid;
        private string token;
        private JObject loginResponse;
        private BatchInfo batch;

        private async Task InitializeClientAsync()
        {
            await RetrieveAESKeyAsync();
            byte[] encryptedBytes = EncryptWithAES(password, aesKey);
            password = Convert.ToBase64String(encryptedBytes);
        }

        private async Task<string> FetchCaptchaAsync()
        {
            client.SetOrigin("https://icourses.jlu.edu.cn");
            client.SetReferer("https://icourses.jlu.edu.cn/xsxk/profile/index.html");

            const string captchaEndpoint = "xsxk/auth/captcha";
            var result = await client.HttpPostAsync(captchaEndpoint, null);
            var json = JObject.Parse(result);
            uuid = json["data"]["uuid"].ToString();
            var captchaImage = json["data"]["captcha"].ToString();
            return captchaImage.Substring(captchaImage.IndexOf(",") + 1);
        }

        private async Task<(int code, string msg)> AttemptLoginAsync(string captcha)
        {
            var response = await PostLoginAsync(captcha);
            var json = JObject.Parse(response);
            loginResponse = json;

            int code = json["code"].ToObject<int>();
            string msg = json["msg"].ToString();

            if (code == 200 && json.ContainsKey("data"))
            {
                token = json["data"]["token"].ToString();
            }

            return (code, msg);
        }

        public async Task<(int code, string msg)> LoginAsync()
        {
            client = new Http(TimeSpan.FromSeconds(5));
            await InitializeClientAsync();
            string captchaImage = await FetchCaptchaAsync();

            var captcha = await DisplayCaptchaAsync(captchaImage);

            var (code, msg) = await AttemptLoginAsync(captcha);

            if (msg == "验证码错误")
            {
                MainPage.Instance.WriteLine(msg);
                (code, msg) = await LoginAsync();
            }

            return (code, msg);
        }

        private byte[] EncryptWithAES(string plainText, string aesKey)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(aesKey);
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = keyBytes;
                aesAlg.Mode = CipherMode.ECB;
                aesAlg.Padding = PaddingMode.PKCS7;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return encryptedBytes;
            }
        }

        private async Task RetrieveAESKeyAsync()
        {
            var response = await client.HttpGetAsync("");
            var match = Regex.Match(response, "loginVue\\.loginForm\\.aesKey\\s*=\\s*\"([^\"]+)\"");
            if (match.Success)
            {
                aesKey = match.Groups[1].Value;
            }
            else
            {
                throw new InvalidOperationException("AES key not found in content.");
            }
        }

        private async Task<string> PostLoginAsync(string captcha)
        {
            var response = await client.HttpPostAsync("xsxk/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"loginname", username},
                {"password", password},
                {"captcha", captcha},
                {"uuid", uuid}
            }));
            return response;
        }

        private async Task<string> DisplayCaptchaAsync(string captchaImage)
        {
            var captchaPage = new CaptchaPage(captchaImage);
            await Application.Current.MainPage.Navigation.PushAsync(captchaPage);
            string userInput = await captchaPage.GetCaptchaInputAsync();
            await Application.Current.MainPage.Navigation.PopAsync();
            return userInput;
        }

        public JObject GetLoginResponse()
        {
            return loginResponse;
        }

        public List<BatchInfo> GetBatchInfo()
        {
            var batchInfos = new List<BatchInfo>();
            loginResponse["data"]["student"]["electiveBatchList"].ToList().ForEach(batch =>
            {
                var batchInfo = new BatchInfo
                {
                    batchId = batch["code"].ToString(),
                    batchName = batch["name"].ToString(),
                    beginTime = batch["beginTime"].ToString(),
                    endTime = batch["endTime"].ToString(),
                    tacticName = batch["tacticName"].ToString(),
                    noSelectReason = batch["noSelectReason"].ToString(),
                    typeName = batch["typeName"].ToString(),
                    canSelect = batch["canSelect"].ToString() != "0"
                };
                batchInfos.Add(batchInfo);
            });
            return batchInfos;
        }

        public async Task SetBatchIDAsync(BatchInfo batch)
        {
            this.batch = batch;
            client.SetOrigin("https://icourses.jlu.edu.cn");
            client.SetReferer("https://icourses.jlu.edu.cn/xsxk/profile/index.html");
            client.AddHeader("Authorization", token);
            var response = await client.HttpPostAsync("xsxk/elective/user", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"batchId", batch.batchId}
            }));
            var json = JObject.Parse(response);
            if (json["code"].ToObject<int>() == 200)
            {
                MainPage.Instance.WriteLine("选课批次设置成功");
                MainPage.Instance.WriteLine("已选批次:" + batch.batchName);
            }
            else
            {
                MainPage.Instance.WriteLine(json["msg"].ToString());
            }

            client.SetReferer("https://icourses.jlu.edu.cn/xsxk/profile/index.html");

            await client.HttpGetAsync("xsxk/elective/grablessons?batchId=" + batch.batchId);
        }

        public async Task<List<Course>> GetFavoriteCoursesAsync()
        {
            List<Course> coursesList = new List<Course>();

            client.SetReferer("https://icourses.jlu.edu.cn/xsxk/elective/grablessons?batchId=" + batch.batchId);
            var response = await client.HttpPostAsync("xsxk/sc/clazz/list", null);
            var json = JObject.Parse(response);

            if (json["code"].ToObject<int>() == 200)
            {
                var courses = json["data"];
                foreach (var course in courses)
                {
                    var courseInfo = new Course
                    {
                        courseName = course["KCM"].ToString(),
                        courseID = course["JXBID"].ToString(),
                        secretVal = course["secretVal"].ToString(),
                        clazzType = course["teachingClassType"].ToString()
                    };
                    coursesList.Add(courseInfo);
                }
            }
            else
            {
                MainPage.Instance.WriteLine(json["msg"].ToString());
            }

            MainPage.Instance.WriteLine("收藏中的课程:\n" + string.Join("\n", coursesList.Select(c => c.courseName)));
            return coursesList;
        }

        public async Task<(bool isSuccess, string? msg)> SelectCourseAsync(Course course)
        {
            while (true)
            {
                var response = await client.HttpPostAsync("xsxk/sc/clazz/addxk", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"clazzId", course.courseID},
                    {"secretVal", course.secretVal},
                    {"clazzType", course.clazzType }
                }));

                var json = JObject.Parse(response);

                var code = json["code"].ToObject<int>();

                if (code == 200)
                {
                    MainPage.Instance.WriteLine("已选课程:" + course.courseName);
                    return (true, null);
                }
                else
                {
                    var msg = json["msg"].ToString();
                    if (msg == "该课程已在选课结果中")
                    {
                        MainPage.Instance.WriteLine(course.courseName + " : " + msg);
                        MainPage.Instance.WriteLine(course.courseName + " : 已放弃,尝试选下一门课程");
                        return (true, null);
                    }

                    if (msg == "课容量已满")
                    {
                        MainPage.Instance.WriteLine(course.courseName + " : " + msg);
                        MainPage.Instance.WriteLine(course.courseName + " : 已放弃,尝试选下一门课程");
                        return (false, msg);
                    }

                    MainPage.Instance.WriteLine(course.courseName + " : 选课失败,原因：" + msg);
                    MainPage.Instance.WriteLine(course.courseName + " : 重新尝试...");
                    await Task.Delay(200 + new Random().Next(0, 200));
                }
            }
        }

        public void KeepOnline()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var response = await client.HttpPostAsync("xsxk/sc/clazz/list", null);
                    if (response.StartsWith('<'))
                    {
                        //MessageBox.Show("检测到掉线，请重启本软件！！！");
                        return;
                    }
                    await Task.Delay(3000);
                }
            });
        }
    }
}
