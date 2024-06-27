using System.Windows;
using ChatClient.MVVM.ViewModel;

namespace ChatClient.MVVM.View
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}