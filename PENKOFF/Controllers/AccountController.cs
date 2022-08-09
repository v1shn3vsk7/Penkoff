﻿using System.Data.Entity;
using System.IdentityModel.Tokens.Jwt;
using Logic.PENKOFF;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PENKOFF.Models;
using Storage.Entities;
using Storage.Enums;

namespace PENKOFF.Controllers;

public class AccountController : Controller
{
    private readonly IUserManager _manager;

    public AccountController(IUserManager manager)
    {
        _manager = manager;
    }

    // GET
    public async Task<IActionResult> Account() => HttpContext.Session.GetInt32("Id") == null ?
        View("~/Views/Account/Login.cshtml", new LoginViewModel()) : View();
    

    /*public AuthenticateResponse Authenticate(LoginViewModel model)
    {
        var user = _manager.GetAll().FirstOrDefault(User => User.Login == model.user.Login && User.Password == Security.HashPassword(model.user.Password));
        
        if (user == null)
        {
            return null;
        }
        
        return new AuthenticateResponse(user, token);
    }*/

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        UserService us = new UserService(_manager);
        var response = await us.Login(model);
        if (response.StatusCode == Enums.StatusCode.OK)
        {
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(response.Data));
            return RedirectToAction("Account");
        }
        ModelState.AddModelError("", response.Description);
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View("Register", new RegisterVewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVewModel model)
    {
        if (model.user.Password != model.ConfirmPassword)
        {
            return View("Register", new RegisterVewModel
            {
                result = "Passwords does not match"
            });
        }

        UserService us = new UserService(_manager);
        var response = await us.Register(model);
        if (response.StatusCode == Enums.StatusCode.OK)
        {
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(response.Data));

            return View("MailVerification", new MailVerificationViewModel());
        }

        return View("Register", new RegisterVewModel()
        {
            result = "Something went wrong"
        });
    }

    public IActionResult MailVerification(MailVerificationViewModel model)
    {
        Random rn = new();
        var verificationCode = rn.Next(100000, 999999);
        HttpContext.Session.SetInt32("verificationCode", verificationCode);

        Verification.SendEmail(model.mail, verificationCode);
        HttpContext.Session.SetString("Email", model.mail);

        return View("MailVerification", new MailVerificationViewModel
        {
            result = "",
            isCodeSent = true,
            mail = model.mail
        });
    }

    public async Task<IActionResult> CheckCode(MailVerificationViewModel model)
    {
        if (model.inputForVerificationCode != HttpContext.Session.GetInt32("verificationCode"))
        {
            return View("MailVerification", new MailVerificationViewModel
            {
                result = "Wrong code. Try again"
            });
        }

        await _manager.AddEmailToUser((int) HttpContext.Session.GetInt32("Id"),
            (string) HttpContext.Session.GetString("Email"));

        HttpContext.Session.Remove("verificationCode");
        HttpContext.Session.Remove("Email");

        return RedirectToAction("Account");
    }
}