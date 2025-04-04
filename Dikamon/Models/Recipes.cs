using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dikamon.Models
{
    [Table("recipes")]
    public class Recipes
    {
        [Key]
        public int Id { get; set; }

        [Column("name"), StringLength(40)]
        public string Name { get; set; }

        [Column("name_EN"), StringLength(40)]
        public string Name_EN { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("description_EN")]
        public string Description_EN { get; set; }

        [Column("type"), StringLength(3)]
        public string Type { get; set; }

        [Column("difficulty")]
        public int Difficulty { get; set; }

        [Column("time")]
        public int Time { get; set; }

        [Column("image")]
        public string? Image { get; set; }


        public ICollection<Contains>? Ingredients { get; set; }
    }
}
