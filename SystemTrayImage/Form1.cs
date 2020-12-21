using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FloatingImage {
    public partial class Form1 : Form {
        public event Action Reset;
        public Form1() {
            TransparencyKey = BackColor = Color.Red;
            InitializeComponent();
            ContextMenuStrip = new ContextMenuStrip();
            var AOT = new ToolStripMenuItem() {
                CheckOnClick = true,
                Checked = true,
                Text = "Always on Top"
            };
            var Re = new ToolStripMenuItem() {
                Text = "Reset Size"
            };
            var Hi = new ToolStripMenuItem() {
                Text = "Hide"
            };
            AOT.CheckedChanged += delegate {
                TopMost = AOT.Checked;
                ShowInTaskbar = !TopMost;
            };
            Re.Click += delegate {
                Reset?.Invoke();
            };
            Hi.Click += delegate {
                Hide();
            };
            VisibleChanged += delegate {
                foreach (Form f in OwnedForms) {
                    f.Visible = Visible;
                }
            };

            ContextMenuStrip.Items.Add(AOT);
            ContextMenuStrip.Items.Add(Re);
            ContextMenuStrip.Items.Add(Hi);


        }
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                // turn on WS_EX_TOOLWINDOW style bit
                cp.ExStyle |= 0x80;
                return cp;
            }
        }
    }
}
