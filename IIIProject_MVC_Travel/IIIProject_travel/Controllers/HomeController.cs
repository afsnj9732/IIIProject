﻿using IIIProject_travel.Models;
using IIIProject_travel.Services;
using IIIProject_travel.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.Design;

namespace IIIProject_travel.Controllers
{
    public class HomeController : Controller
    {
        //宣告service物件
        private readonly MembersService membersService = new MembersService();
        //宣告寄信用service物件
        private readonly MailService mailService = new MailService();

        // GET: Home
        [AllowAnonymous]        //不須做登入驗證即可進入
        public ActionResult Home(int? id)
        {
           
            //if (id == 0)
            //{
            //    Session.Remove("member");
            //}
            //else {
            //    string displayImg = (string)Session["member"];
            //    dbJoutaEntities db = new dbJoutaEntities();
            //    tMember t = db.tMember.FirstOrDefault(k => k.f會員編號 == id);
            //    Session["member"] = t;
            //    string v =  t.f會員大頭貼.ToString();
            //    ViewData["Img"] = v;
            //}
            return View();
        }

        [Authorize]     //通過驗證才可進入頁面
        public ActionResult QuickMatch()
        {
            return View();
        }

        public ActionResult Register()
        {
            //判斷使用者是否已經過登入驗證
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Home","Home");
            //若無登入驗證，則導向註冊頁面
            return View();
        }

        [HttpPost]
        public ActionResult Register(CRegisterModel p)
        {
            //判斷資料是否通過驗證
            if (ModelState.IsValid)
            {
                if (p.MembersImg != null)
                {
                    if (membersService.CheckImg(p.MembersImg.ContentType))
                    {
                        string fileName = Path.GetFileName(p.MembersImg.FileName);
                        string url = Path.Combine(Server.MapPath($@"~/Upload/Members/"), fileName);
                        //將檔案存進伺服器
                        p.MembersImg.SaveAs(url);
                        //設定路徑
                        p.newMember.txtFiles = fileName;
                        //將頁面資料中的密碼填入
                        p.newMember.txtPassword = p.txtPassword;
                        //取得信箱驗證碼
                        string AuthCode = mailService.getValidationCode();
                        //填入驗證碼
                        p.newMember.fActivationCode = AuthCode;
                        //呼叫service註冊新會員
                        membersService.Register(p.newMember);
                        string tempMail = System.IO.File.ReadAllText(
                            Server.MapPath("~/Views/Shared/RegisterEmailTemplate.html"));

                        //宣告Email驗證用Url
                        UriBuilder validateUrl = new UriBuilder(Request.Url)
                        {
                            Path = Url.Action("emailValidation", "Home", new
                            {
                                email = p.newMember.txtEmail,
                                authCode = AuthCode
                            })
                        };
                        //將資料寫入信中
                        string MailBody = mailService.getRegisterMailBody(tempMail, p.newMember.txtNickname, validateUrl.ToString().Replace("%3F", "?"));
                        //寄信
                        mailService.sendRegisterMail(MailBody, p.newMember.txtEmail);
                        //以tempData儲存註冊訊息
                        TempData["RegisterState"] = "註冊成功，請去收取驗證信";
                        return RedirectToAction("RegisterResult");
                    }
                    else
                    {
                        ModelState.AddModelError("MembersImg", "所上傳檔案不是圖片");
                    }
                }
                else
                {
                    ModelState.AddModelError("MembersImg", "請選擇上傳檔案");
                    return View(p);
                }
            }
            //未經驗證清空密碼相關欄位
            p.txtPassword = null;
            p.txtPassword_confirm = null;
            //資料回填至view中
            return View(p);
        }

        //註冊結果顯示頁面
        public ActionResult RegisterResult()
        {
            return View();
        }

        //判斷信箱是否被註冊過
        public JsonResult accountCheck(CRegisterModel p)
        {
            //呼叫service來判斷，並回傳結果
            return Json(membersService.accountCheck(p.newMember.txtEmail), JsonRequestBehavior.AllowGet);
        
        }

        //接收驗證信連結傳進來
        public ActionResult emailValidation(string email, string AuthCode)
        {
            //用ViewData儲存，使用Service進行信箱驗證後的結果訊息
            ViewData["emailValidation"] = membersService.emailValidation(email,AuthCode);
            return View();
        }

        //修改密碼
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(CChangePassword p)
        {
            if (ModelState.IsValid)
            {
                ViewData["ChangeState"] = membersService.ChangePassword(User.Identity.Name,p.txtPassword,p.txtNewPassword);
            }
            return View();
        }

        [AllowAnonymous]
        public ActionResult About()
        {
            return View();
        }

    }
}