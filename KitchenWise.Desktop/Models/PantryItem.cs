using System;
using System.ComponentModel;

namespace KitchenWise.Desktop.Models
{
    /// <summary>
    /// Represents a single item in the user's pantry
    /// </summary>
    public class PantryItem : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;
        private double _quantity;
        private string _unit = "piece";
        private DateTime _addedDate;
        private DateTime? _expiryDate;
        private string _category = "Other";
        private bool _isSelected;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Name
        {
            get => _name ?? string.Empty;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public double Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(DisplayQuantity));
            }
        }

        public string Unit
        {
            get => _unit ?? "piece";
            set
            {
                _unit = value;
                OnPropertyChanged(nameof(Unit));
                OnPropertyChanged(nameof(DisplayQuantity));
            }
        }

        public DateTime AddedDate
        {
            get => _addedDate;
            set
            {
                _addedDate = value;
                OnPropertyChanged(nameof(AddedDate));
                OnPropertyChanged(nameof(DisplayAddedDate));
            }
        }

        public DateTime? ExpiryDate
        {
            get => _expiryDate;
            set
            {
                _expiryDate = value;
                OnPropertyChanged(nameof(ExpiryDate));
                OnPropertyChanged(nameof(DisplayExpiryDate));
                OnPropertyChanged(nameof(IsExpiringSoon));
                OnPropertyChanged(nameof(IsExpired));
            }
        }

        public string Category
        {
            get => _category ?? "Other";
            set
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // Computed Properties for Display
        public string DisplayQuantity => $"{Quantity:F1} {Unit}";

        public string DisplayAddedDate => AddedDate.ToString("MMM dd, yyyy");

        public string DisplayExpiryDate => ExpiryDate?.ToString("MMM dd, yyyy") ?? "No expiry";

        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.Now.Date;

        public bool IsExpiringSoon => ExpiryDate.HasValue &&
                                     ExpiryDate.Value.Date >= DateTime.Now.Date &&
                                     ExpiryDate.Value.Date <= DateTime.Now.Date.AddDays(7);

        public string ExpiryStatus
        {
            get
            {
                if (IsExpired) return "Expired";
                if (IsExpiringSoon) return "Expiring Soon";
                return "Fresh";
            }
        }

        // Constructor
        public PantryItem()
        {
            Id = 0;
            Name = string.Empty;
            Quantity = 1.0;
            Unit = "piece";
            AddedDate = DateTime.Now;
            ExpiryDate = null;
            Category = "Other";
            IsSelected = false;
        }

        public PantryItem(string name, double quantity = 1.0, string unit = "piece", string category = "Other")
        {
            Id = 0;
            Name = name;
            Quantity = quantity;
            Unit = unit;
            AddedDate = DateTime.Now;
            ExpiryDate = null;
            Category = category;
            IsSelected = false;
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Helper Methods
        public bool MatchesSearchTerm(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return true;

            var term = searchTerm.ToLowerInvariant();
            return Name.ToLowerInvariant().Contains(term) ||
                   Category.ToLowerInvariant().Contains(term) ||
                   Unit.ToLowerInvariant().Contains(term);
        }

        public void UpdateQuantity(double newQuantity)
        {
            if (newQuantity >= 0)
            {
                Quantity = newQuantity;
            }
        }

        public void ConsumeQuantity(double consumedAmount)
        {
            if (consumedAmount > 0 && consumedAmount <= Quantity)
            {
                Quantity -= consumedAmount;
            }
        }

        public PantryItem Clone()
        {
            return new PantryItem
            {
                Id = this.Id,
                Name = this.Name,
                Quantity = this.Quantity,
                Unit = this.Unit,
                AddedDate = this.AddedDate,
                ExpiryDate = this.ExpiryDate,
                Category = this.Category,
                IsSelected = this.IsSelected
            };
        }

        public override string ToString()
        {
            return $"{Name} ({DisplayQuantity}) - {Category}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is PantryItem other)
            {
                return Id == other.Id && Name == other.Name;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }

    /// <summary>
    /// Common pantry item categories
    /// </summary>
    public static class PantryCategories
    {
        public const string Vegetables = "Vegetables";
        public const string Fruits = "Fruits";
        public const string Meat = "Meat";
        public const string Dairy = "Dairy";
        public const string Grains = "Grains";
        public const string Spices = "Spices";
        public const string Canned = "Canned";
        public const string Frozen = "Frozen";
        public const string Beverages = "Beverages";
        public const string Snacks = "Snacks";
        public const string Other = "Other";

        public static readonly string[] AllCategories =
        {
            Vegetables, Fruits, Meat, Dairy, Grains,
            Spices, Canned, Frozen, Beverages, Snacks, Other
        };
    }

    /// <summary>
    /// Common measurement units
    /// </summary>
    public static class MeasurementUnits
    {
        // Count
        public const string Piece = "piece";
        public const string Item = "item";

        // Weight
        public const string Gram = "g";
        public const string Kilogram = "kg";
        public const string Pound = "lb";
        public const string Ounce = "oz";

        // Volume
        public const string Milliliter = "ml";
        public const string Liter = "L";
        public const string Cup = "cup";
        public const string Tablespoon = "tbsp";
        public const string Teaspoon = "tsp";

        public static readonly string[] AllUnits =
        {
            Piece, Item, Gram, Kilogram, Pound, Ounce,
            Milliliter, Liter, Cup, Tablespoon, Teaspoon
        };
    }
}