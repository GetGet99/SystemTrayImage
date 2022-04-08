using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PInvoke;
namespace SystemTrayImage;

public class ThemeContextMenu : ContextMenuStrip
{
    public ThemeContextMenu()
    {
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = SystemColors.GrayText;
        RenderMode = ToolStripRenderMode.System;
        //var menuinfo = User32.MENUINFO.Create();
        //menuinfo.fMask = User32.MenuInfoMask.MIM_BACKGROUND | User32.MenuInfoMask.MIM_APPLYTOSUBMENUS;
        //menuinfo.hbrBack = CreateSolidBrush((uint)ColorTranslator.ToWin32(Color.Black));
        //SetMenuInfo(handle, ref menuinfo);
    }
}
public class ThemeMenuItem : ToolStripMenuItem
{
    public ThemeMenuItem()
    {
        Init();
    }
    public ThemeMenuItem(string text, Image? image, EventHandler onClick) : base(text, image, onClick)
    {
        Init();
    }
    void Init()
    {
        BackColor = Color.Transparent;
        ForeColor = Color.White;
    }
}
public class ThemeSeparator : ToolStripSeparator
{
    public ThemeSeparator()
    {
        BackColor = Color.Transparent;
        ForeColor = Color.White;
    }
}