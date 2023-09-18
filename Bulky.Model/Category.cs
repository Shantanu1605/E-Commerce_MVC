using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bulky.Model
{
    public class Category
    {
        // In order to define a key as primary key we need to us data annotations
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(30)]
        [DisplayName("Category Name")]
        public string Name { get; set; }
        [DisplayName("Display Order")]
        [Range(1, 100, ErrorMessage = "Display Order must be between 1-100")]
        public int DisplayOrder { get; set; }
    }
}
