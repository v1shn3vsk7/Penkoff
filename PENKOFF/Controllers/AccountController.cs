﻿using Logic.PENKOFF;
using Microsoft.AspNetCore.Mvc;
using PENKOFF.Models;

namespace PENKOFF.Controllers;

public class AccountController : Controller
{
    private readonly IUserManager _manager;

    public AccountController(IUserManager manager)
    {
        _manager = manager;
    }

    // GET
    public async Task<IActionResult> Account()
    {
        if (HttpContext.Session.GetInt32("Id") == null)
        {
            return View("~/Views/Account/Login.cshtml", new LoginViewModel
            {
                result = ""
            });
        }

        return View();
    }

    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var user = await _manager.FindUser(model.user.Login, model.user.Password);

        if (user is null)
        {
            return View("Login", new LoginViewModel
            {
                result = "Incorrect login or password"
            });
        }

        HttpContext.Session.SetInt32("Id", user.Id);
        return View("Account");
    }

    public IActionResult SignUp()
    {
        return View("SignUp", new SignUpVewModel
        {
            result = ""
        });
    }

    public async Task<IActionResult> Registration(SignUpVewModel model)
    {
        if (model.user.Password != model.ConfirmPassword)
        {
            return View("SignUp", new SignUpVewModel
            {
                result = "Passwords does not match"
            });
        }

        var user = await _manager.FindUser(model.user.Login);

        if (user is not null)
        {
            return View("SignUp", new SignUpVewModel
            {
                result = "Login already exists"
            });
        }

        await _manager.AddUser(model.user);
        HttpContext.Session.SetInt32("Id", _manager.GetUserId(model.user.Login));

        return View("MailVerification", new MailVerificationViewModel());
    }

    public IActionResult MailVerification(MailVerificationViewModel model)
    {
        Random rn = new();
        var verificationCode = rn.Next(100000, 999999);
        HttpContext.Session.SetInt32("verificationCode", verificationCode);
        
        Verification.SendEmail(model.mail, verificationCode);
        HttpContext.Session.SetString("mail", model.mail);
        
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

        await _manager.AddEmailToUser((int)HttpContext.Session.GetInt32("Id"), model.mail);

        return RedirectToAction("Account");
    }
}