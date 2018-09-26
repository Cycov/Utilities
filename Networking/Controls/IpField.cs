using System.Windows.Forms;

namespace Utilities.Networking.Controls
{
    public partial class IpField : UserControl
    {
        public string IP
        {
            get
            {
                return string.Format("{0}.{1}.{2}.{3}", textBox1.Text, textBox2.Text, textBox3.Text, textBox4.Text);
            }
        }
        public IpField()
        {
            InitializeComponent();
        }

        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (char.IsLetter(e.KeyChar))
                e.Handled = true;
            if (tb.TextLength == 2 && tb.SelectionLength == 0)
            {
                if (int.Parse(tb.Text + e.KeyChar) > 255)
                    tb.Text = "255";
                SendKeys.Send("{TAB}");
            }
        }

        private void textBox_Enter(object sender, System.EventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void textBox_Enter(object sender, MouseEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }
    }
}
