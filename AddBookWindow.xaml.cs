using System.Windows;

namespace biblioteka
{
    public partial class AddBookWindow : Window
    {
        public BookData NewBook { get; private set; }

        public AddBookWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text) ||
                string.IsNullOrWhiteSpace(AuthorTextBox.Text) ||
                string.IsNullOrWhiteSpace(YearTextBox.Text))
            {
                MessageBox.Show("Заполните название, автора и год!");
                return;
            }

            if (!int.TryParse(YearTextBox.Text, out int year))
            {
                MessageBox.Show("Год должен быть числом!");
                return;
            }

            NewBook = new BookData
            {
                Title = TitleTextBox.Text,
                Author = AuthorTextBox.Text,
                Year = year,  // SQL использует @Yearr
                Genre = GenreTextBox.Text,
                Description = DescriptionTextBox.Text
            };

            DialogResult = true;
        }
    }

    public class BookData
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public int Year { get; set; }  
        public string Genre { get; set; }
        public string Description { get; set; }
    }
}
