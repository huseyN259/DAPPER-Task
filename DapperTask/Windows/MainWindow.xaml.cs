using Dapper;
using DapperTask.Models;
using DapperTask.Windows;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Z.Dapper.Plus;

namespace DapperTask;

public partial class MainWindow : Window, INotifyPropertyChanged
{
	public SqlConnection? connection = null;
	public IConfigurationRoot? configuration = null;

	public event PropertyChangedEventHandler? PropertyChanged;
	private void OnPropertyChanged([CallerMemberName] String propertyName = "")
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public MainWindow()
	{
		InitializeComponent();

		DataContext = this;
		Configuration();
	}

	private List<Product>? _products;
	public List<Product>? Products
	{
		get => _products;
		set
		{
			_products = value;
			OnPropertyChanged();
		}
	}

	private bool _checkDb;
	public bool CheckDb
	{
		get => _checkDb;
		set
		{
			_checkDb = value;
			OnPropertyChanged();
		}
	}

	private async void Configuration()
	{
		configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();
		var conStr = configuration.GetConnectionString("ConnectionString");
		connection = new SqlConnection(conStr);

		var checkDatabaseExist = "DECLARE @isDatabaseExist bit = 0 " +
			"IF EXISTS(SELECT * FROM sys.databases WHERE NAME = 'OnlineStore') " +
			"SET @isDatabaseExist = 1 " +
			"SELECT @isDatabaseExist";

		CheckDb = await connection.ExecuteScalarAsync<bool>(checkDatabaseExist);
		if (CheckDb)
		{
			DapperPlusManager.Entity<Product>().Table("Products");
			var begin = conStr.IndexOf(';') + 1;
			connection.ConnectionString = conStr.Insert(begin, "Database = OnlineStore;");
			CheckDb = true;
		}
	}

	private async void btnSearch_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(txtSearch.Text))
		{
			MessageBox.Show("Search Box cannot be empty!", "Online Shop", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}

		var getDataCommand = $"SELECT * FROM Products WHERE Name LIKE '%{txtSearch.Text}%'";
		var collection = await connection.QueryAsync<Product>(getDataCommand);
		Products = collection.ToList();
	}

	private async void btnDelete_Click(object sender, RoutedEventArgs e)
	{
		var collection = DbInfo.SelectedItems.Cast<Product>().ToList();

		foreach (var item in collection)
			Products.Remove(item);

		connection.BulkDelete(collection);

		var getDataCommand = "SELECT * FROM Products";
		var tempCollection = await connection.QueryAsync<Product>(getDataCommand);
		Products = tempCollection.ToList();
	}

	private void btnAdd_Click(object sender, RoutedEventArgs e)
	{
		AddWindow addWindow = new();
		addWindow.ShowDialog();

		if (addWindow.DialogResult == true)
		{
			var product = addWindow.Product;
			var addCommand = "INSERT INTO Products VALUES(@name,@country,@cost)";
			connection.Execute(addCommand, new { product.Name, product.Country, product.Cost });
		}
	}

	private void DbInfo_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		if (DbInfo.SelectedItem is Product product)
		{
			UpdateWindow updateWindow = new(product);
			updateWindow.ShowDialog();

			if (updateWindow.DialogResult == true)
			{
				var updateCommand = "UPDATE Products SET Name = @name, Country = @country, Cost = @cost WHERE Id = @id";
				connection.Execute(updateCommand, new { product.Name, product.Country, product.Cost, product.Id });
			}
		}
	}

	private async void btnGetData_Click(object sender, RoutedEventArgs e)
	{
		var getDataCommand = "SELECT * FROM Products";
		var collection = await connection.QueryAsync<Product>(getDataCommand);
		txtSearch.Text = string.Empty;
		Products = collection.ToList();
	}

	private async void btnDatabaseCreate_Click(object sender, RoutedEventArgs e)
	{
		if (CheckDb)
		{
			MessageBox.Show("Database already exists.", "Online Shop", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}

		var createDb = "IF NOT EXISTS(SELECT * FROM sys.databases WHERE NAME = 'OnlineStore') " +
			"CREATE DATABASE OnlineStore";
		var createTb = @"IF EXISTS(SELECT * FROM sys.databases WHERE NAME = 'OnlineStore')
BEGIN
USE OnlineStore
IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Products')
    BEGIN
    CREATE TABLE Products (
        Id int PRIMARY KEY IDENTITY (1, 1),
        Name nvarchar(40) NOT NULL,
        Country nvarchar(40) NULL,
        Cost money NOT NULL,
    );
    IF EXISTS(SELECT * FROM sys.databases WHERE NAME = 'Northwind')
    BEGIN
        INSERT INTO OnlineStore.dbo.Products(Name,Country,Cost)
        SELECT [ProductName] AS Name
        ,[UnitPrice] AS Price
        ,[UnitsInStock] AS Count
        FROM [Northwind].[dbo].[Products]
    END
    END
END";

		await connection.ExecuteAsync(createDb);
		await connection.ExecuteAsync(createTb);
		var conStr = connection.ConnectionString;
		var startIndex = conStr.IndexOf(';') + 1;
		connection.ConnectionString = conStr.Insert(startIndex, "Database = OnlineStore;");
		CheckDb = true;

		MessageBox.Show("Database created.", "Online Shop", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}