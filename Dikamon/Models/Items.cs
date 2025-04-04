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
    [Table("items")]
    public class Items
    {
        [Column("id"), Key]
        public int Id { get; set; }

        [Column("name"), StringLength(50)]
        public string Name { get; set; }

        [Column("name_EN"), StringLength(50)]
        public string Name_EN { get; set; }

        [Column("typeId")]
        public int TypeId { get; set; }

        [Column("unit"), StringLength(10)]
        public string? Unit { get; set; }

        [Column("image")]
        public string? Image { get; set; }

        [JsonIgnore]
        public List<Stores>? Stored { get; set; }

        [JsonIgnore]
        public List<Contains>? Contained { get; set; }
    }
}
