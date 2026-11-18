using System;
using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    [Flags]
    public enum EmailTemplateType
    {
        [Display(Name = "User account activation")]
        UserAccActivated = 1,
        [Display(Name = "User account welcome")]
        UserAccWelcome = 2,
        [Display(Name = "User password reset")]
        UserPassReset = 3,
        [Display(Name = "New order confirmation")]
        OrderConfirm = 4,
        [Display(Name = "Order cancelled")]
        OrderCancel = 5,
        [Display(Name = "Order sent notification")]
        OrderSent = 6,
        [Display(Name = "Refund notification")]
        OrderRefund = 7,
        [Display(Name = "Abandoned checkout notification")]
        AbandonedCheckout = 8,
        [Display(Name = "Order change status")]
        OrderChangeStatus = 9, 
        [Display(Name = "Recurring order confirm")]
        OrderRecurConfirm = 10,
        [Display(Name = "New pos invoice order confirmation")]
        OrderConfirmPosInv = 11,
        [Display(Name = "New user account unregistered")]
        NewUserUnregistered = 12,
        ProductWaiting = 13,
        ProductEmailAsk = 14,
        GiftcardOrdered = 15,
        RecurOrderFailed = 16,
        Custom0 = 17,
        Custom1 = 18,
        OrderOffer = 19,
        NotifyMe = 20,
        [Display(Name = "New Pending User account")]
        NewUserPendingAccount = 21,
        Custom2 = 22,
        Custom3 = 23,
        Custom4 = 24,
        [Display(Name = "One time login")]
        OneTimeLogin = 25,
        Custom5 = 26,
        Custom6 = 27,
        Custom7 = 28,
        Custom8 = 29,  
        Custom9 = 30,
        [Display(Name = " Product A-assortment")]
        ProductAassortment = 31,
    }
}