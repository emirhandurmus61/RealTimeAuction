using System.Security.Claims;
using AuctionHouse.Core.Constants;
using AuctionHouse.Core.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace AuctionHouse.Web.Services;

/// <summary>
/// Giriş yapan her kullanıcının "Seller" rolüne sahip olmasını garanti eder.
/// Yeni kayıt olan kullanıcılar ilk kimlik doğrulamalarında otomatik olarak
/// Seller rolüne eklenir; böylece açık artırma açma yetkisi rol tabanlı kalır
/// ama herkes pratikte satıcı olabilir. Principal zaten Seller claim'i taşıyorsa
/// veritabanına hiç gidilmez.
/// </summary>
public class AutoSellerClaimsTransformation : IClaimsTransformation
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AutoSellerClaimsTransformation(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Kimliği doğrulanmamış istekleri es geç.
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        // Zaten Seller ise (claim mevcut) hiçbir şey yapma — sıcak yol, DB yok.
        if (principal.IsInRole(Roles.Seller))
            return principal;

        var user = await _userManager.GetUserAsync(principal);
        if (user is null)
            return principal;

        // Veritabanı seviyesinde rolü garanti et.
        if (!await _userManager.IsInRoleAsync(user, Roles.Seller))
            await _userManager.AddToRoleAsync(user, Roles.Seller);

        // Bu istekteki principal'a da Seller claim'ini ekle ki
        // [Authorize(Roles = "Seller")] ilk istekten itibaren çalışsın.
        if (principal.Identity is ClaimsIdentity identity)
            identity.AddClaim(new Claim(identity.RoleClaimType, Roles.Seller));

        return principal;
    }
}
