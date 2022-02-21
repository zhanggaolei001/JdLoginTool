using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JdLoginTool.Wpf
{
    /// <summary>
    /// InputWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InputWindow : Window
    {
        public InputWindow()
        {
            InitializeComponent();
        }

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        public string Remarkers
        {
            get { return this.TextBox.Text; }
            set { this.TextBox.Text = value; }
        }
    }
}
