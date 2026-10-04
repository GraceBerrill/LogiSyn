using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
// Provide UI type aliases so the library can be multi-targeted.
#if WINDOWS
using UiBrush = System.Windows.Media.Brush;
using UiImageSource = System.Windows.Media.ImageSource;
using System.Windows.Media;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using UiBrush = System.Windows.Media.Brush;
using UiImageSource = System.Windows.Media.ImageSource;
using System.Windows.Media;

#if !WINDOWS
using UiBrush = System.Object;
using UiImageSource = System.Object;
#endif

namespace SharedLibrary.Model
{

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

		public string? Key { get; set; }
		public string? Label { get; set; }
		public UiImageSource? Icon { get; set; }

		public bool IsActive
		{
			get { return _isActive; }
			set
			{
				_isActive = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsActive"));
			}
		}

	public event PropertyChangedEventHandler? PropertyChanged;
	}

	public static class Palette
	{
		public static UiBrush From(string hex)
		{
#if WINDOWS
			var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
			brush.Freeze();
			return brush;
#else
			return null!;
#endif
		}

		public static readonly UiBrush ListPending = From("#BDD38478");
		public static readonly UiBrush ListComplete = From("#BD398158");
		public static readonly UiBrush DashPending = From("#C6AEA5");
		public static readonly UiBrush DashComplete = From("#A5C6AC");
		public static readonly UiBrush UserPending = From("#B22727");
		public static readonly UiBrush UserComplete = From("#135826");
		public static readonly UiBrush Black = From("#000000");
	}

	public class OrderRow
	{
		public string? Number { get; set; }
		public string? Customer { get; set; }
		public DateTime Date { get; set; }
		public string? Status { get; set; }              // "Pending" or "Complete"

		public bool IsComplete { get { return Status == "Complete"; } }
		public string DateText { get { return Date.ToString("dd/MM/yy", CultureInfo.InvariantCulture); } }

		public UiBrush ListStatusBrush { get { return IsComplete ? Palette.ListComplete : Palette.ListPending; } }
		public UiBrush DashStatusBrush { get { return IsComplete ? Palette.DashComplete : Palette.DashPending; } }
		public string UserStatusText { get { return IsComplete ? "Completed" : "Pending"; } }
		public UiBrush UserStatusBrush { get { return IsComplete ? Palette.UserComplete : Palette.UserPending; } }
	}

	public class ProductRow
	{
	public class ProductRow
	{
		public string? Name { get; set; }
		public string? Price { get; set; }
		public string? SellBy { get; set; }
		public string? BestBefore { get; set; }
		public string? Storage { get; set; }
	}

	public class UserRow
	{


public class UserRow
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonIgnore]
        public string SqlId { get; set; } = string.Empty; 

        [BsonElement("Username")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("Password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("Role")]
        public string Role { get; set; } = string.Empty;

        [BsonElement("DateAdded")]
        public string DateAdded { get; set; } = string.Empty;
    }


    // ----- summary paper (Order Sheets / Order Breakdown) -----
    public class SummaryLine
	{
		public string? Product { get; set; }
		public string? Production { get; set; }
		public string? Ingredients { get; set; }
		public string? Packaging { get; set; }
	}

	public class SummaryData
	{
		public string? Title { get; set; }
		public string? DateText { get; set; }
		public bool IsCompleted { get; set; }
		public List<SummaryLine> Lines { get; set; } = new List<SummaryLine>();
	}

	public class RawMaterialData
	{
		public string? Title { get; set; }
		public string? DateText { get; set; }
		public List<string> Totals { get; set; } = new List<string>();
	}

	// ----- production sheet paper -----
	public class SheetIngredient
	{
		public string? Name { get; set; }
		public string? Amount { get; set; }
		public string? Additional { get; set; }
		public string? Used { get; set; }
		public UiBrush? UsedBrush { get; set; }
	}

	public class SheetPackaging
	{
		public string? Name { get; set; }
		public string? Amount { get; set; }
		public string? Used { get; set; }
		public UiBrush? UsedBrush { get; set; }
	}

	public class SheetProduct
	{
		public string? Name { get; set; }
		public string? Amount { get; set; }
		public string? Production { get; set; }
		public List<SheetIngredient> Ingredients { get; set; } = new List<SheetIngredient>();
		public List<SheetPackaging> Packaging { get; set; } = new List<SheetPackaging>();
		public string? Notes { get; set; }
	}

	public class SheetData
	{
		public string? Title { get; set; }
		public string? DateText { get; set; }
		public bool IsCompleted { get; set; }
		public List<SheetProduct> Products { get; set; } = new List<SheetProduct>();
	}

	// ----- product pop-up -----
	public class IngredientLine
	{
		public string? Name { get; set; }
		public string? Quantity { get; set; }
	}

	public class ProductDetail
	{
		public string? Name { get; set; }
		public string? DateAdded { get; set; }
		public List<IngredientLine> Ingredients { get; set; } = new List<IngredientLine>();
		public string? Method { get; set; }
		public string? Storage { get; set; }
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
				new OrderRow { Number = "#001", Customer = "Checkers", Date = DateTime.Now, Status = "Pending" },
				new OrderRow { Number = "#002", Customer = "Spar", Date = DateTime.Now, Status = "Complete" }
			};
		}

		// History (completed orders)
		public static List<OrderRow> HistoryOrders()
		{
			return new List<OrderRow>
			{
				new OrderRow { Number = "#001", Customer = "Checkers", Date = DateTime.Now, Status = "Complete" },
				new OrderRow { Number = "#002", Customer = "Spar", Date = DateTime.Now, Status = "Complete" }
			};
		}

		// The "User" role's orders
		public static List<OrderRow> UserOrders()
		{
			return new List<OrderRow>
			{
				new OrderRow { Number = "#001", Customer = "Checkers", Date = DateTime.Now, Status = "Complete" },
				new OrderRow { Number = "#002", Customer = "Spar", Date = DateTime.Now, Status = "Pending" }
			};
		}

		public static List<ProductRow> Products()
		{
			return new List<ProductRow>
			{
				new ProductRow { Name = "Product A", Price = "24.99", SellBy = "5", BestBefore = "5", Storage = "Cool" },
				new ProductRow { Name = "Product B", Price = "19.99", SellBy = "6", BestBefore = "6", Storage = "Cool" }
			};
		}

		public static List<UserRow> Users()
		{
			return new List<UserRow>
			{
				new UserRow { Id = "01", Name = "Admin User",        Role = "Admin",   DateAdded = DateTime.Now.ToString("dd/MM/yyyy") },
				new UserRow { Id = "02", Name = "Manager User",      Role = "Manager", DateAdded = DateTime.Now.ToString("dd/MM/yyyy") },
				new UserRow { Id = "03", Name = "Standard User",     Role = "User",    DateAdded = DateTime.Now.ToString("dd/MM/yyyy") }
			};
		}

		public static SummaryData Summary(bool completed)
		{
			return new SummaryData
			{
			Title = "Spar Order #002",
			DateText = Today(),
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
			DateText = Today(),
				Totals = new List<string> { "Eggs: 50 dozen", "Flour: 100 bags", "Salt: 50 bags" }
			};
		}

		/// <summary>
		/// The production sheet. filled = true gives the completed version (Used values in
		/// green / red, notes filled in); false gives the blank one a baker fills in.
		/// </summary>
		public static SheetData Sheet(bool filled)
		{
			UiBrush good = filled ? Palette.UserComplete : Palette.Black;
			UiBrush bad = filled ? Palette.UserPending : Palette.Black;

			return new SheetData
			{
			Title = "Spar Order #002",
			DateText = Today(),
				IsCompleted = filled,
				Products = new List<SheetProduct>
				{
					new SheetProduct
					{
					Name = "Product A", Amount = "250", Production = "Production 1",
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
					Name = "Product B", Amount = "110", Production = "Production 2",
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