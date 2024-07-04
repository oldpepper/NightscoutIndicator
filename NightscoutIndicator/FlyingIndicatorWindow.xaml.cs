using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NightscoutIndicator
{
    /// <summary>
    /// Interaction logic for FlyingIndicator.xaml
    /// </summary>
    public partial class FlyingIndicatorWindow : Window
    {
        public FlyingIndicatorWindow()
        {
            InitializeComponent();

            Deactivated += OnDeactivated;
            Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            Deactivated -= OnDeactivated;
            Closing -= OnClosing;
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            {
                this.Topmost = false;
                this.Topmost = true;
            }
            ;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}
