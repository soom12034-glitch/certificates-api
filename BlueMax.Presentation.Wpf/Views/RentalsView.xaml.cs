using System.Windows.Controls;
using System.Windows.Input;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views;

public partial class RentalsView : UserControl
{
    public RentalsView()
    {
        InitializeComponent();
    }

    void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is RentalsViewModel vm && vm.OpenSelectedRentalFileCommand != null)
        {
            vm.OpenSelectedRentalFileCommand.Execute(null);
        }
    }
}
