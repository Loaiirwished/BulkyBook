using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BulkyBook.Web.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [DisplayName("Display Order")]
        [Range(1,100,ErrorMessage ="must be from 1 to 100")]
        public int DisplayOrder { get; set; }
        public DateTime? CreateAt { get; set; } = DateTime.Now;

    }
}
