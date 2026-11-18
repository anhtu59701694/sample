using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Claw.Core.Enumerations;
using Kendo.Mvc.UI;
using System.Web.Mvc;
using Kendo.Mvc.UI.Fluent;
using Claw.Core.Extensions;

namespace Claw.Modules.Eshop
{
    public class KendoTree<TModel> where TModel : class
    {
        #region DataField

        public interface IDataField
        {
            string Name { get; }
            Type Type { get; }
        }

        public class DataField<TVal> : IDataField
        {
            private readonly Expression<Func<TModel, TVal>> _expr;

            public DataField(Expression<Func<TModel, TVal>> expr)
            {
                _expr = expr;
            }

            public string Name
            {
                get { return ((MemberExpression) _expr.Body).Member.Name; }
            }

            public Type Type
            {
                get { return _expr.ReturnType; }
            }
        }

        #endregion

        #region Fields

        protected TreeViewBuilder _tree;
        protected HtmlHelper _htmlHelper;

        protected string _name;
        protected string _controller;

        #endregion

        #region KendoTree

        public KendoTree(HtmlHelper htmlHelper, TreeViewBuilder tree, string name, string controller)
        {
            _controller = controller;
            _htmlHelper = htmlHelper;

            (_tree = tree)
                .Name(_name = name.ToLower())
                .Events(events => events
                    .Collapse(@"function(e){ return;
                        var nodeid = $(e.node).data('uid');
                        if(this.catenoExpItems) this.catenoExpItems = this.catenoExpItems.filter(function(item) { return nodeid != item; });
                    }")
                    .Expand(@"function(e){ return;
                        var nodeid = $(e.node).data('uid'); var kendoTree = this;
                        if(this.catenoExpItems) {
                            var parent = $(e.node).closest('ul').closest('.k-item'); var ancestors = [];
                            while(parent.length > 0){
                                ancestors.push(parent.data('uid')); parent = parent.closest('ul').closest('.k-item');
                            }
                            this.catenoExpItems.forEach(function(itm) {
                                if(ancestors.indexOf(itm) == -1) kendoTree.collapse(kendoTree.findByUid(itm));
                            });
                        }
                        else this.catenoExpItems = [];
 
                        this.catenoExpItems.push(nodeid);
                    }"));
        }

        #endregion

        #region Tree
        /// <summary>kendo TreeViewBuilder</summary>
        public TreeViewBuilder Tree
        {
            get { return _tree; }
        }

        #endregion

        #region DataSource
        /// <summary>config ajax datasource</summary>
        /// <param name="modelId">Id field</param>
        /// <param name="actionList">name of action that return data list</param>
        /// <param name="fFields">the function that return field info array</param>
        /// <param name="dataHandler">name of javascript function that return args for "actionList"</param>
        public KendoTree<TModel> DataSource<TId>(Expression<Func<TModel, TId>> modelId,
            Expression<Func<TModel, bool>> hasChildren, string actionList,
            Func<KendoTree<TModel>, IDataField[]> fFields = null, string dataHandler = null)
        {
            _tree.DataSource(dataSource => dataSource
                .Read(crud =>
                {
                    crud.Action(actionList, _controller);
                    if (!string.IsNullOrEmpty(dataHandler)) crud.Data(dataHandler);
                })
                .Model(model =>
                {
                    model.Id(((MemberExpression) modelId.Body).Member.Name);
                    if (hasChildren != null) 
                        model.HasChildren(((MemberExpression) hasChildren.Body).Member.Name);
                    if (fFields != null)
                        foreach (var field in fFields(this))
                            model.Field(field.Name, field.Type);
                }));

            return this;
        }

        #endregion

        #region DataTextField
        /// <summary>declare the field that return text value</summary>
        public KendoTree<TModel> DataTextField<TId>(Expression<Func<TModel, TId>> field)
        {
            _tree.DataTextField(((MemberExpression) field.Body).Member.Name);

            return this;
        }

        #endregion

        #region DataUrlField
        /// <summary>declare the field that return url value</summary>
        public KendoTree<TModel> DataUrlField<TId>(Expression<Func<TModel, TId>> field)
        {
            _tree.DataUrlField(((MemberExpression) field.Body).Member.Name);

            return this;
        }

        #endregion

        #region Checkboxes
        /// <summary>show checkboxes</summary>
        /// <param name="checkChildren">auto check children if parent is checked</param>
        /// <param name="template">client template</param>
        public KendoTree<TModel> Checkboxes(bool enabled = true, bool checkChildren = false, string template = null)
        {
            _tree.Checkboxes(cbox =>
            {
                cbox.Enabled(enabled).CheckChildren(checkChildren);
                if (template != null) cbox.Template(template);
            });

            return this;
        }

        #endregion

        #region OnDataBound
        /// <summary>name of javascript function that handling databound event</summary>
        public KendoTree<TModel> OnDataBound(string handler)
        {
            _tree.Events(events => events.DataBound(handler));

            return this;
        }

        #endregion

        #region OnSelect
        /// <summary>name of javascript function that handling select event</summary>
        public KendoTree<TModel> OnSelect(string handler)
        {
            _tree.Events(events => events.Select(handler));

            return this;
        }

        #endregion

        #region Field
        /// <summary>return field definition from expression</summary>
        public IDataField Field<TVal>(Expression<Func<TModel, TVal>> expr)
        {
            return new DataField<TVal>(expr);
        }

        #endregion

        #region Config
        /// <summary>More customs for the tree view</summary>
        public KendoTree<TModel> Config(Action<KendoTree<TModel>> fConf)
        {
            fConf(this);

            return this;
        }
        #endregion

        #region HtmlAttributes
        public KendoTree<TModel> HtmlAttributes(object attributes)
        {
            _tree.HtmlAttributes(attributes);

            return this;
        }
        #endregion

        #region ToMvcHtmlString
        /// <summary>return html and javascript code of the tree view</summary>
        public MvcHtmlString ToMvcHtmlString()
        {
            return MvcHtmlString.Create(_tree.ToHtmlString());
        }

        #endregion
    }
}
