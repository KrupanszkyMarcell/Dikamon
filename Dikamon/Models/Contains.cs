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
    [Table("contains")]
    public class Contains
    {
        [Key, JsonIgnore]
        public int? Id { get; set; }

        [Column("recipeId")]
        public int RecipeId { get; set; }

        [Column("itemId")]
        public int ItemId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }


        public Items? Item { get; set; }

        [JsonIgnore]
        public Recipes? Recipe { get; set; }
    }
}
