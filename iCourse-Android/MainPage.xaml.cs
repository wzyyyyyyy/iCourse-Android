namespace iCourse_Android
{
    public partial class MainPage : ContentPage
    {
        static bool isLogged = false;

        public static Web web { get; private set; }

        public static MainPage Instance { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            Instance = this;
        }

        public void WriteLine(string message)
        {
            DisplayAlert("Error", message, "111111");
        }

        public async Task LoginAsync()
        {
            var name = usernameEntry.Text;
            var pw = passwordEntry.Text;
            web = new Web(name, pw);

            // 登录
            var (code, msg) = await web.LoginAsync();
            var response = web.GetLoginResponse();

            // 登录失败
            if (code != 200)
            {
                WriteLine(msg);
                WriteLine("登录失败，请检查用户名和密码是否正确。");
                isLogged = false;
                return;
            }

            // 登录成功
            WriteLine(msg);
            isLogged = true;

            // 获取学生信息
            var studentName = response["data"]["student"]["XM"].ToString();
            var studentID = response["data"]["student"]["XH"].ToString();
            var collage = response["data"]["student"]["YXMC"].ToString();

            WriteLine($"姓名：{studentName}");
            WriteLine($"学号：{studentID}");
            WriteLine($"学院：{collage}");

            // 显示选课批次
            var batchInfos = web.GetBatchInfo();
            try
            {
                var batchPage = new SelectBatchPage(batchInfos);
            if (batchPage is null)
            {
                WriteLine("fuck");
                return;
            }
                await Navigation.PushAsync(new SelectBatchPage(batchInfos));
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
            }
        }

        private void OnLoginButtonClicked(object sender, EventArgs e)
        {
            if (isLogged)
            {
                DisplayAlert("Error", "请勿重复登录！", "22222");
                return;
            }
            _ = LoginAsync();
        }
    }
}
