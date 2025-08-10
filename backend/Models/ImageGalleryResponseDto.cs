using System.Collections.Generic;

namespace AiRoleplayChat.Backend.Models
{
    public class ImageGalleryResponseDto
    {
        public IEnumerable<ImageItemDto> Items { get; set; } = new List<ImageItemDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}