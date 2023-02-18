using DapperTask.Models;
using System.Text;
using System.Windows;

namespace DapperTask.Windows;

public partial class AddWindow : Window
{
	public Product Product { get; set; }

	public AddWindow()
	{
		InitializeComponent();

		DataContext = this;
		Product = new();
	}

	private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
	{
		var builder = new StringBuilder();

		if (string.IsNullOrWhiteSpace(Product.Name))
			builder.Append($"{nameof(Product.Name)} cannot be empty.\n");

		if (Product.Cost <= 0)
			builder.Append($"{nameof(Product.Cost)} cannot be negative or equal to 0!\n");

		if (builder.Length > 0)
		{
			MessageBox.Show(builder.ToString());
			return;
		}

		DialogResult = true;
	}

	private void ButtonCancel_Click(object sender, RoutedEventArgs e) 
		=> DialogResult = false;
}