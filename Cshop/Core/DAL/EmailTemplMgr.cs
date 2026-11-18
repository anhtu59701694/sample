using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.DAL
{
    public class EmailTemplMgr : BaseMgr<EmailTemplModel, EmailTempl>
    {
        static EmailTemplMgr() { Init(typeof(EmailTemplMgr)); }

        #region GetAll

        public static List<EmailTemplModel> GetAll(IDB db, string langCode, int? type, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null)
        {
            var hSql = string.Format(@"
            select templ from {0} as templ join templ.Languages as lang
            where lang.LangCode = '{1}' ", typeof(EmailTempl).FullName, langCode);

            if (!string.IsNullOrEmpty(keyword))
                hSql += string.Format(@" and (templ.Name like '%{0}%' or lang.Name like '%{0}%')", TranslateKWord(keyword));
            
            if (type != null) hSql += " and templ.Type = " + type.Value;
            
            //hSql += string.Format(" and templ.Type not in ({0},{1}) ", (int)EmailTemplateType.AbandonedCheckout, (int)EmailTemplateType.OrderRefund);

            //hSql += string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "templ.") + sortField, GetDir(sortDirection));
            hSql += string.Format(" order by {0} {1}", ("templ.") + sortField, GetDir(sortDirection));

            var ret = GetList<EmailTempl>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<EmailTempl>, List<EmailTemplModel>>(ret);
        }

        public static List<EmailTemplModel> GetAll(IDB db, int? type = null)
        {
            int total;
            return GetAll(db, LanguageMgr.FocusLangCode, type, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Id");
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, EmailTemplModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);

            #region EmailTempl

            if (obj != null)
            {
                obj.Code = model.Code;
                obj.Name = model.Name;
                obj.Type = model.Type;
                obj.AttachFile = model.AttachFile;
            }
            else
                obj = new EmailTempl()
                {
                    Code = model.Code,
                    Name = model.Name,
                    Type = model.Type,
                    AttachFile = model.AttachFile,
                };

            obj.From = model.From;
            obj.ReplyTo = model.ReplyTo;

            #endregion

            #region Languages
            if (model.Languages != null)
            {
                foreach (var item in model.Languages)
                {
                    var lid = item.Id;
                    var ditm = (lid == 0 ? null : obj.Languages.FirstOrDefault(x => x.Id == lid));
                    if (ditm == null)
                        obj.Languages.Add(ditm = new EmailTemplLang()
                        {
                            EmailTempl = obj,
                            LangCode = item.LangCode,
                        });

                    ditm.Subject = item.Subject;
                    ditm.Body = item.Body;
                }

                obj.Languages.RemoveAll(x => model.Languages.All(w => w.Id != x.Id));
            }
            #endregion

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new EmailTemplMgr().OnUpdate(obj); return true;
            }
            else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(EmailTempl obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(EmailTempl obj)
        {
        }
        #endregion
    }
}