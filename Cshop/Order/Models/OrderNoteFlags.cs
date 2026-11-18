using System;

namespace Claw.Modules.Eshop.Models
{
    [Flags]
    public enum OrderNoteFlags { None = 0, SentEmail = 1, SentSms = 2 }
}