using System;
using System.Windows;
using System.Windows.Controls;
using biblioteka.Services;

namespace biblioteka
{
    public partial class WriteOffHistoryWindow : Window
    {
        private readonly WriteOffService _writeOffService;

        public WriteOffHistoryWindow()
        {
            InitializeComponent();
            _writeOffService = new WriteOffService();
            LoadActs();
        }

        private void LoadActs()
        {
            try
            {
                var acts = _writeOffService.GetAllActsWithCount();
                ActsDataGrid.ItemsSource = acts;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки актов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditAct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int actId)
            {
                var editWindow = new EditWriteOffWindow(actId);
                editWindow.Owner = this;
                if (editWindow.ShowDialog() == true)
                    LoadActs();
            }
        }
    }
}