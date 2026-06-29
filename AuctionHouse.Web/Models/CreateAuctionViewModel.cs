using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AuctionHouse.Web.Models;

/// <summary>Açık artırma oluşturma formu için view model.</summary>
public class CreateAuctionViewModel
{
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Başlık 3-200 karakter olmalıdır.")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Kategori")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Açılış fiyatı zorunludur.")]
    [Range(0.01, 100_000_000, ErrorMessage = "Açılış fiyatı 0'dan büyük olmalıdır.")]
    [Display(Name = "Açılış Fiyatı (₺)")]
    public decimal StartPrice { get; set; }

    [Required(ErrorMessage = "Minimum artış zorunludur.")]
    [Range(0.01, 1_000_000, ErrorMessage = "Minimum artış 0'dan büyük olmalıdır.")]
    [Display(Name = "Minimum Artış (₺)")]
    public decimal MinIncrement { get; set; } = 10m;

    [Required(ErrorMessage = "Başlangıç zamanı zorunludur.")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Başlangıç Zamanı")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Bitiş zamanı zorunludur.")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Bitiş Zamanı")]
    public DateTime EndTime { get; set; }

    [Display(Name = "Ürün Fotoğrafları")]
    public List<IFormFile>? Images { get; set; }

    public List<SelectListItem> Categories { get; set; } = new();
}
