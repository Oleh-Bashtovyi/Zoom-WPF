using System.Windows;
using System.Windows.Controls;
using Zoom_UI.MVVM.Models;
using Zoom_UI.MVVM.ViewModels;

namespace Zoom_UI.MVVM.Views;

public partial class MessageView : UserControl
{
    public MessageView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if(DataContext is MessageModel mes && mes.Content is ChatFileItem chatItem)
        {
            chatItem.OnLoaded();
        }
    }
}
