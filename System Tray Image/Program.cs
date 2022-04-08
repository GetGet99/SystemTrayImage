// To customize application configuration such as set high DPI settings or default font,
// see https://aka.ms/applicationconfiguration.

using SystemTrayImage;
using Microsoft.Win32;
using Properties = SystemTrayImage.Properties;
using AppContext = SystemTrayImage.AppContext;

ApplicationConfiguration.Initialize();

if (!Properties.Settings.Default.FirstRun)
    Application.Run(new AppContext());
else
{
    MessageBox.Show(new Form
    {
        TopMost = true,
        Icon = AppContext.IconImage
    },
    caption: "Warning!",
    text: "This program will run every time you turn on your computer.\n" +
    "If you don't want the program to run on startup, turn it off in Task Manager."
    );
    Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)
    ?.SetValue("FloatingImage", Application.ExecutablePath);
    
    Application.Run(new IntroForm() { Icon = AppContext.IconImage });
}