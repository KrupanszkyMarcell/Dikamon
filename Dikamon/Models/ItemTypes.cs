using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dikamon.Models
{
    [Table("itemtypes")]
    public class ItemTypes
    {
        [Key]
        public int Id { get; set; }

        [Column("name"), StringLength(30)]
        public string Name { get; set; }

        [Column("name_EN"), StringLength(30)]
        public string Name_EN { get; set; }

        [Column("image")]
        public string Image { get; set; }
    }
}
