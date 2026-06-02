using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InstantBot.AdminPanel.Controllers;

public class AuthController(IConfiguration config) : Controller
{
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        if (username != config["Admin:Username"] || password != config["Admin:Password"])
        {
            ViewBag.Error = "Неверные данные";
            return View();
        }

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Admin:JwtSecret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.Name, username)],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        Response.Cookies.Append("admin_token", new JwtSecurityTokenHandler().WriteToken(token),
            new CookieOptions { HttpOnly = true, Expires = DateTimeOffset.UtcNow.AddHours(8) });

        return RedirectToAction("Index", "Dashboard");
    }

    public IActionResult Logout()
    {
        Response.Cookies.Delete("admin_token");
        return RedirectToAction("Login");
    }
}
