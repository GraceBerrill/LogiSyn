using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;

namespace SharedLibrary.Model
{
	// =====================================================================
	//  Front-end models + sample data.
	//  Everything in SampleData is placeholder text copied from the Figma
	//  screens - swap these calls for your real data / services later.
	// =====================================================================

	public enum AppRole { Admin, Manager, User }

	/// <summary>One item in the left sidebar.</summary>
	public class NavItem : INotifyPropertyChanged
	{
		private bool _isActive;

		public string Key { get; set; }
		public string Label { get; set; }
		public ImageSource Icon { get; set; }

		public bool IsActive
		{
			get { return _isActive; }
			set
			{
				_isActive = value;
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("IsActive"));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	public static class Palette
	{
		public static Brush From(string hex)
		{
			var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
			brush.Freeze();
			return brush;
		}

		public static readonly Brush ListPending = From("#BDD38478");
		public static readonly Brush ListComplete = From("#BD398158");
		public static readonly Brush DashPending = From("#C6AEA5");
		public static readonly Brush DashComplete = From("#A5C6AC");
		public static readonly Brush UserPending = From("#B22727");
		public static readonly Brush UserComplete = From("#135826");
		public static readonly Brush Black = From("#000000");
	}

	public class OrderRow
	{
		public string Number { get; set; }
		public string Customer { get; set; }
		public DateTime Date { get; set; }
		public string Status { get; set; }              // "Pending" or "Complete"

		public bool IsComplete { get { return Status == "Complete"; } }
		public string DateText { get { return Date.ToString("dd/MM/yy", CultureInfo.InvariantCulture); } }

		public Brush ListStatusBrush { get { return IsComplete ? Palette.ListComplete : Palette.ListPending; } }
		public Brush DashStatusBrush { get { return IsComplete ? Palette.DashComplete : Palette.DashPending; } }
		public string UserStatusText { get { return IsComplete ? "Completed" : "Pending"; } }
		public Brush UserStatusBrush { get { return IsComplete ? Palette.UserComplete : Palette.UserPending; } }
	}

	public class ProductRow
	{
		public string Name { get; set; }
		public string Price { get; set; }
		public string SellBy { get; set; }
		public string BestBefore { get; set; }
		public string Storage { get; set; }
	}

	public class UserRow
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty;
		public string DateAdded { get; set; } = string.Empty;
	}

	// ----- summary paper (Order Sheets / Order Breakdown) -----
	public class SummaryLine
	{
		public string Product { get; set; }
		public string Production { get; set; }
		public string Ingredients { get; set; }
		public string Packaging { get; set; }
	}

	public class SummaryData
	{
		public string Title { get; set; }
		public string DateText { get; set; }
		public bool IsCompleted { get; set; }
		public List<SummaryLine> Lines { get; set; }
	}

	public class RawMaterialData
	{
		public string Title { get; set; }
		public string DateText { get; set; }
		public List<string> Totals { get; set; }
	}

	// ----- production sheet paper -----
	public class SheetIngredient
	{
		public string Name { get; set; }
		public string Amount { get; set; }
		public string Additional { get; set; }
		public string Used { get; set; }
		public Brush UsedBrush { get; set; }
	}

	public class SheetPackaging
	{
		public string Name { get; set; }
		public string Amount { get; set; }
		public string Used { get; set; }
		public Brush UsedBrush { get; set; }
	}

	public class SheetProduct
	{
		public string Name { get; set; }
		public string Amount { get; set; }
		public string Production { get; set; }
		public List<SheetIngredient> Ingredients { get; set; }
		public List<SheetPackaging> Packaging { get; set; }
		public string Notes { get; set; }
	}

	public class SheetData
	{
		public string Title { get; set; }
		public string DateText { get; set; }
		public bool IsCompleted { get; set; }
		public List<SheetProduct> Products { get; set; }
	}

	// ----- product pop-up -----
	public class IngredientLine
	{
		public string Name { get; set; }
		public string Quantity { get; set; }
	}

	public class ProductDetail
	{
		public string Name { get; set; }
		public string DateAdded { get; set; }
		public List<IngredientLine> Ingredients { get; set; }
		public string Method { get; set; }
		public string Storage { get; set; }
	}

	// =====================================================================
	public static class SampleData
	{
		public static string Today()
		{
			return DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
		}

		// Admin "Orders" list and the dashboard's recent orders
		public static List<OrderRow> Orders()
		{
			return new List<OrderRow>
			{
				new OrderRow { Number = "#001", Customer = "Checkers", Date = new DateTime(2026, 5, 9), Status = "Pending" },
				new OrderRow { Number = "#002", Customer = "Spar",     Date = new DateTime(2026, 5, 9), Status = "Complete" }
			};
		}

		// History (completed orders)
		public static List<OrderRow> HistoryOrders()
		{
			return new List<OrderRow>
			{
				new OrderRow { Number = "#001", Customer = "Checkers", Date = new DateTime(2026, 5, 9), Status = "Complete" },
				new OrderRow { Number = "#002", Customer = "Spar",     Date = new DateTime(2026, 5, 9), Status = "Complete" }
			};
		}

		// The "User" role's orders
		public static List<OrderRow> UserOrders()
		{
			return new List<OrderRow>
			{
				new OrderRow { Number = "#001", Customer = "Checkers", Date = new DateTime(2026, 5, 9), Status = "Complete" },
				new OrderRow { Number = "#002", Customer = "Spar",     Date = new DateTime(2026, 5, 9), Status = "Pending" }
			};
		}

		public static List<ProductRow> Products()
		{
			return new List<ProductRow>
			{
				new ProductRow { Name = "Hamburger Rolls", Price = "R24.99", SellBy = "5", BestBefore = "5", Storage = "Freezer" },
				new ProductRow { Name = "Hotdog Rolls",    Price = "R24.99", SellBy = "6", BestBefore = "6", Storage = "Freezer" }
			};
		}

		public static List<UserRow> Users()
		{
			return new List<UserRow>
			{
				new UserRow { Id = "01", Name = "Blessings",        Role = "Admin",   DateAdded = "12/08/2026" },
				new UserRow { Id = "02", Name = "Paul",             Role = "Manager", DateAdded = "12/08/2026" },
				new UserRow { Id = "03", Name = "Bread Station #1", Role = "User",    DateAdded = "12/08/2026" }
			};
		}

		public static SummaryData Summary(bool completed)
		{
			return new SummaryData
			{
				Title = "Spar Order #002",
				DateText = "5 August 2026",
				IsCompleted = completed,
				Lines = new List<SummaryLine>
				{
					new SummaryLine
					{
						Product = "250 Hamburger Rolls", Production = "Production 1",
						Ingredients = "Flour: 6 bags,   Eggs: 27 dozen,   Salt: 3 bags",
						Packaging = "Pans: 2,   Trolleys: 1"
					},
					new SummaryLine
					{
						Product = "100 Rolls", Production = "Production 1",
						Ingredients = "Flour: 6 bags,   Eggs: 27 dozen,   Salt: 3 bags",
						Packaging = "Pans: 2,   Trolleys: 1"
					}
				}
			};
		}

		public static RawMaterialData RawMaterials()
		{
			return new RawMaterialData
			{
				Title = "Spar Order #002",
				DateText = "5 August 2026",
				Totals = new List<string> { "Eggs: 50 dozen", "Flour: 100 bags", "Salt: 50 bags" }
			};
		}

		/// <summary>
		/// The production sheet. filled = true gives the completed version (Used values in
		/// green / red, notes filled in); false gives the blank one a baker fills in.
		/// </summary>
		public static SheetData Sheet(bool filled)
		{
			Brush good = filled ? Palette.UserComplete : Palette.Black;
			Brush bad = filled ? Palette.UserPending : Palette.Black;

			return new SheetData
			{
				Title = "Spar Order #002",
				DateText = "5 August 2026",
				IsCompleted = filled,
				Products = new List<SheetProduct>
				{
					new SheetProduct
					{
						Name = "Hamburger Rolls", Amount = "250", Production = "Production 1",
						Ingredients = new List<SheetIngredient>
						{
							new SheetIngredient { Name = "Flour",  Amount = "5 bags",   Additional = "6 bags",   Used = filled ? "6 bags"   : "", UsedBrush = good },
							new SheetIngredient { Name = "Eggs",   Amount = "20 dozen", Additional = "21 dozen", Used = filled ? "27 dozen" : "", UsedBrush = bad },
							new SheetIngredient { Name = "Salt",   Amount = "4 bags",   Additional = "5 bags",   Used = filled ? "4 bags"   : "", UsedBrush = good },
							new SheetIngredient { Name = "Butter", Amount = "4 bags",   Additional = "5 bags",   Used = filled ? "2 bags"   : "", UsedBrush = good }
						},
						Packaging = new List<SheetPackaging>
						{
							new SheetPackaging { Name = "Pans",     Amount = "4", Used = filled ? "4" : "", UsedBrush = good },
							new SheetPackaging { Name = "Trolleys", Amount = "2", Used = filled ? "2" : "", UsedBrush = good }
						},
						Notes = filled ? "Extra flour was used due to spillage." : ""
					},
					new SheetProduct
					{
						Name = "Croissants", Amount = "110", Production = "Croissant Room",
						Ingredients = new List<SheetIngredient>
						{
							new SheetIngredient { Name = "Flour", Amount = "5 bags",   Additional = "6 bags",   Used = filled ? "6 bags"   : "", UsedBrush = good },
							new SheetIngredient { Name = "Eggs",  Amount = "20 dozen", Additional = "21 dozen", Used = filled ? "27 dozen" : "", UsedBrush = bad },
							new SheetIngredient { Name = "Salt",  Amount = "4 bags",   Additional = "5 bags",   Used = filled ? "4 bags"   : "", UsedBrush = good }
						},
						Packaging = new List<SheetPackaging>
						{
							new SheetPackaging { Name = "Pans",     Amount = "4", Used = filled ? "4" : "", UsedBrush = good },
							new SheetPackaging { Name = "Trolleys", Amount = "2", Used = filled ? "2" : "", UsedBrush = good }
						},
						Notes = ""
					}
				}
			};
		}

		public static ProductDetail DetailFor(ProductRow row)
		{
			return new ProductDetail
			{
				Name = row.Name,
				DateAdded = "10 August 2026",
				Ingredients = new List<IngredientLine>
				{
					new IngredientLine { Name = "Eggs",  Quantity = "5" },
					new IngredientLine { Name = "Flour", Quantity = "5 Cups" },
					new IngredientLine { Name = "Salt",  Quantity = "1 Cup" }
				},
				Method = "Mix eggs, flour and salt until combine. Place on baking tray and put into oven at 180 degrees for 20 minutes.",
				Storage = row.Storage + " Room 1"
			};
		}
	}
}