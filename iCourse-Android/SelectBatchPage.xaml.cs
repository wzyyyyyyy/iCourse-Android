namespace iCourse_Android;

public partial class SelectBatchPage : ContentPage
{
    public SelectBatchPage(List<BatchInfo> batchList)
    {
        InitializeComponent();
        objectListView.ItemsSource = batchList;
        BindingContext = this;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = e.CurrentSelection.FirstOrDefault() as BatchInfo;
        if (selectedItem != null)
        {
            batchCodeLabel.Text = selectedItem.batchId;
            batchNameLabel.Text = selectedItem.batchName;
            beginTimeLabel.Text = selectedItem.beginTime;
            endTimeLabel.Text = selectedItem.endTime;
            tacticNameLabel.Text = selectedItem.tacticName;
            noSelectReasonLabel.Text = selectedItem.noSelectReason;
            typeNameLabel.Text = selectedItem.typeName;
            canSelectLabel.Text = selectedItem.canSelect ? "��" : "��";
        }
    }

    private async void OnConfirmButtonClicked(object sender, EventArgs e)
    {
        var selectedItem = objectListView.SelectedItem as BatchInfo;
        if (selectedItem == null)
        {
            await DisplayAlert("����", "��ѡ��һ������", "ȷ��");
            return;
        }
        //await MainWindow.Instance.StartSelectClass(selectedItem);
        await Navigation.PopAsync();
    }
}
