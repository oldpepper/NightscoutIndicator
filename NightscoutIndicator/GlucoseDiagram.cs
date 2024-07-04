using System.Drawing;
using System.Windows.Forms;
using NightscoutIndicator.Nightscout;
using NightscoutIndicator.Styles;

namespace NightscoutIndicator
{
    public partial class GlucoseDiagram : Form
    {
        public GlucoseDiagram(NightscoutIndicatorSettings settings, List<NightscoutEntry> entries)
        {
            InitializeComponent();

            Width = settings.Width;
            Height = settings.Height;
            PictureBox.Width = settings.Width;
            PictureBox.Height = settings.Height;

            var screenSize = Screen.PrimaryScreen.WorkingArea.Size;
            Location = new Point(screenSize.Width - Width - 16, screenSize.Height - Height - 16);

            PictureBox.Image = new XDripStyle().DrawDiagram(settings, entries);
        }
    }
}
