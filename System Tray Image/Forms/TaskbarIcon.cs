namespace SystemTrayImage.Forms;

class TaskbarIcon
{
    class TaskbarForm : Form
    {
        public TaskbarForm()
        {
            TopMost = true;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            ShowInTaskbar = false;
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= (int)PInvoke.User32.WindowStylesEx.WS_EX_TOOLWINDOW;
                return cp;
            }
        }
    }
    public Form f = new()
    {
        TopMost = true,
        FormBorderStyle = FormBorderStyle.None,
        BackColor = Color.Magenta,
        TransparencyKey = Color.Magenta,
        ShowInTaskbar = false
    };
    public event Action? TaskbarClick;
    public bool Visible { get => f.ShowInTaskbar; set => f.ShowInTaskbar = value; }
    public Icon Icon { get => f.Icon; set => f.Icon = value; }
    public TaskbarIcon()
    {
        f.Show();
        f.GotFocus += (o, e) => TaskbarClick?.Invoke();
    }
}
class TaskbarIconWithForm : TaskbarIcon
{
    public TaskbarIconWithForm(Form target) => TaskbarClick += () => target.Activate();
}
