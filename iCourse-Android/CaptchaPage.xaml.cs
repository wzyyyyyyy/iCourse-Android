namespace iCourse_Android;

public partial class CaptchaPage : ContentPage
{
    private TaskCompletionSource<string> _tcs;

	public CaptchaPage(string base64Image)
	{
		InitializeComponent();
        byte[] imageBytes = Convert.FromBase64String(base64Image);
        captchaImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        _tcs?.SetResult(captchaInput.Text);
    }

    public Task<string> GetCaptchaInputAsync()
    {
        _tcs = new TaskCompletionSource<string>();
        return _tcs.Task;
    }
}