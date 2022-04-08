namespace SystemTrayImage;
public partial class IntroForm : Form {
    public IntroForm() {
        InitializeComponent();
        Okay.Click += delegate {
            Properties.Settings.Default.FirstRun = false;
            Properties.Settings.Default.Save();
            Application.Restart();
        };
    }
}
