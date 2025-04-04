using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dikamon.Models
{
    [Table("stores")]
    public class Stores
    {
        [Key, JsonIgnore]
        public int? Id { get; set; }

        [Column("userId")]
        public int UserId { get; set; }

        [Column("itemId")]
        public int ItemId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        public Items? StoredItem { get; set; }
    }
}
