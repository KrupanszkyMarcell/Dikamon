using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dikamon.Models
{
    [Table("users")]
    public class Users
    {
        [Key]
        public int? Id { get; set; }

        [Column("name"), StringLength(50)]
        public string? Name { get; set; }

        [Column("email"), StringLength(320)]
        public string Email { get; set; }

        [Column("password"), StringLength(100)]
        public string Password { get; set; }

        [Column("role"), StringLength(10)]
        public string? Role { get; set; }

        [Column("token"), StringLength(255)]
        public string? Token { get; set; }
    }
}
