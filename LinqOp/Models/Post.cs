using System.ComponentModel.DataAnnotations;

namespace LinqOp.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? ContentHtml { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
