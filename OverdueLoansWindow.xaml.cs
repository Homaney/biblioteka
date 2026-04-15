using System;
using System.Windows;
using System.Windows.Controls;
using biblioteka.Services;

namespace biblioteka
{
    public partial class OverdueLoansWindow : Window
    {
        private readonly IssueService _issueService;

        public OverdueLoansWindow()
        {
            InitializeComponent();
            _issueService = new IssueService();
            LoadOverdueLoans();
        }

        private void LoadOverdueLoans()
        {
            try
            {
                var overdue = _issueService.GetOverdueIssues();
                OverdueDataGrid.ItemsSource = overdue;
                UpdateStats(overdue.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки просроченных выдач: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStats(int count)
        {
            if (count == 0)
            {
                StatsText.Text = "✅ Просроченных выдач нет. Отлично!";
                StatsText.Foreground = (System.Windows.Media.SolidColorBrush)FindResource("Success");
            }
            else
            {
                StatsText.Text = $"⚠️ Найдено просроченных выдач: {count}";
                StatsText.Foreground = (System.Windows.Media.SolidColorBrush)FindResource("Danger");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadOverdueLoans();
        }

        private void ReturnBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int issuedId)
            {
                var confirm = MessageBox.Show("Вернуть эту книгу?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    _issueService.ReturnBook(issuedId);
                    MessageBox.Show("Книга возвращена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadOverdueLoans();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при возврате книги: " + ex.Message, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}