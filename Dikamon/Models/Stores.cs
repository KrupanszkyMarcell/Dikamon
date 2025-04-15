using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dikamon.Models
{
    [Table("stores")]
    public class Stores : INotifyPropertyChanged
    {
        [Key, JsonIgnore]
        public int? Id { get; set; }

        [Column("userId")]
        public int UserId { get; set; }

        [Column("itemId")]
        public int ItemId { get; set; }

        private int _quantity;
        [Column("quantity")]
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                }
            }
        }

        private Items? _storedItem;
        public Items? StoredItem
        {
            get => _storedItem;
            set
            {
                if (_storedItem != value)
                {
                    _storedItem = value;
                    OnPropertyChanged();
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}