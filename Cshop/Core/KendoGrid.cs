using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using Claw.Core.Enumerations;
using Claw.Modules.Eshop.DAL;
using Kendo.Mvc.UI;
using System.Web.Mvc;
using Kendo.Mvc.UI.Fluent;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Controllers.Admin;

namespace Claw.Modules.Eshop
{
    public class KendoGrid<TModel> where TModel : class
    {
        #region Column
        /// <summary>
        /// define a data column
        /// </summary>
        public interface IColumn
        {
            object HtmlAttributes { get; }
            string Name { get; }
            string Title { get; }
            bool Editable { get; }

            /// <summary>enable sorting</summary>
            bool Sortedable { get; }
            bool SortDesc { get; }

            /// <summary>grid's first load is sorted with this column</summary>
            bool DefaultSort { get; }
            bool InlineEdit { get; }

            /// <summary>name of action that will be invoke when the cell clicked</summary>
            string Action { get; }

            /// <summary>parameter for "Action"</summary>
            string ActField { get; }
            string CssClass { get; }

            /// <summary>client template for the data cell</summary>
            string Template { get; }

            /// <summary>bind to property name of grid data row</summary>
            string DataBind { get; }

            /// <summary>inline edit, [INPUT, TEXTAREA]</summary>
            string InputTag { get; }
            int Width { get; }

            IColumn SetCssClass(string name);
            IColumn SetTemplate(string template);
            IColumn SetDataBind(string dataBind);
            IColumn SetInputTag(string inputTag);
            IColumn SetHtmlAttributes(object value);
            IColumn SetInlineEdit(bool value);
            IColumn SetDefaultSort(bool value, bool desc = false);

            string GetGridTextBox(HtmlHelper htmlHelper, Object htmlAttributes);
            void ConfigInlineEdit(object dsModelDesc);
        }

        public class Column<TVal, TTit> : IColumn
        {
            /// <summary>bind column to which field?</summary>
            public Expression<Func<TModel, TVal>> Expr;
            /// <summary>bind title to another field?</summary>
            public Expression<Func<TModel, TTit>> TitleExpr;
            /// <summary>direct title value, not from model's meta data</summary>
            public string TitleVal;
            public object HtmlAttributes { get; set; }
            public string Action { get; set; }
            public string CssClass { get; set; }
            public string Template { get; set; }
            public string DataBind { get; set; }
            public string InputTag { get; set; }
            public int Width { get; set; }
            public bool Sortedable { get; set; }
            public bool DefaultSort { get; set; }
            public bool SortDesc { get; set; }
            public bool InlineEdit { get; set; }

            public Column()
            {
                InlineEdit = false;
            }

            public string Name
            {
                get { return ExpressionHelper.GetExpressionText(Expr); }
            }

            public string Title
            {
                get
                {
                    if (TitleVal != null) return TitleVal;
                    if (TitleExpr != null) return ModExtensions.GetDisplayName(TitleExpr);
                    return ModExtensions.GetDisplayName(Expr);
                }
            }

            public bool Editable
            {
                get { return !string.IsNullOrEmpty(Action); }
            }

            public virtual string ActField
            {
                get { return "id"; }
            }

            /// <summary>return html code for inline editor element</summary>
            public string GetGridTextBox(HtmlHelper htmlHelper, Object htmlAttributes)
            {
                if (InputTag.EqualsIgCase("TextArea"))
                    return htmlHelper.LocalizeGridTextAreaFor<TModel, TVal>(Expr, htmlAttributes);
                else return htmlHelper.LocalizeGridTextBoxFor<TModel, TVal>(Expr, htmlAttributes);
            }

            public IColumn SetCssClass(string name)
            {
                CssClass = name;
                return this;
            }

            public IColumn SetTemplate(string template)
            {
                Template = template;
                return this;
            }

            public IColumn SetDataBind(string dataBind)
            {
                DataBind = dataBind;
                return this;
            }

            public IColumn SetInputTag(string inputTag)
            {
                InputTag = inputTag;
                return this;
            }

            public IColumn SetHtmlAttributes(object value)
            {
                HtmlAttributes = value;
                return this;
            }

            public IColumn SetInlineEdit(bool value)
            {
                InlineEdit = value;
                return this;
            }

            public IColumn SetDefaultSort(bool value, bool sortDesc = false)
            {
                if (Sortedable)
                {
                    DefaultSort = value;
                    SortDesc = sortDesc;
                }
                return this;
            }

            /// <summary>disable inline edit by default</summary>
            public void ConfigInlineEdit(object dsModelDesc)
            {
                if (!InlineEdit)
                {
                    var ds0 = dsModelDesc as DataSourceModelDescriptorFactory<TModel>;
                    if (ds0 != null) ds0.Field(Expr).Editable(false);
                    else
                    {
                        var ds1 = dsModelDesc as CustomDataSourceModelDescriptorFactory<TModel>;
                        if (ds1 != null) ds1.Field(Expr).Editable(false);
                        else if (Debugger.IsAttached) Debugger.Break();
                    }
                }
            }
        }

        public class Column<TVal, TTit, TAct> : Column<TVal, TTit>
        {
            public Expression<Func<TModel, TAct>> ActExpr;

            public override string ActField
            {
                get { return ((MemberExpression)ActExpr.Body).Member.Name; }
            }
        }

        #endregion
        #region Command

        public interface ICommand
        {
            string Name { get; }
            /// <summary>display text</summary>
            string Text { get; }
            /// <summary>name of action will be invoked on click event</summary>
            string Action { get; }
            /// <summary>parameter for "Action"</summary>
            string ActField { get; }
            string CssClass { get; }
            /// <summary>show confirm dialog box?</summary>
            bool ConfirmDlg { get; }
            /// <summary>"Action" is name of javascript function(uid, gridid)</summary>
            bool JScriptAction { get; }
        }

        public class Command<TAct> : ICommand
        {
            public Expression<Func<TModel, TAct>> ActExpr;
            public string Name { get; set; }
            public string Text { get; set; }
            public string Action { get; set; }
            public string CssClass { get; set; }
            public bool ConfirmDlg { get; set; }
            public bool JScriptAction { get; set; }

            public Command()
            {
                ConfirmDlg = JScriptAction = false;
            }

            public string ActField
            {
                get { return ((MemberExpression)ActExpr.Body).Member.Name; }
            }
        }

        #endregion

        #region Fields

        protected IColumn[] _columns = new IColumn[0];
        protected List<ICommand> _commands = new List<ICommand>();
        protected object _dsModelDesc;
        protected GridBuilder<TModel> _grid;
        protected HtmlHelper _htmlHelper;
        protected AjaxDataSourceBuilder<TModel> _ajaxDataSource;

        protected string _name;
        protected string _nameSelector;
        protected string _controller;
        protected string _deleteAction;
        protected string _editAction;
        protected string _addAction;
        protected int? _pageSize;
        protected bool _fullMode = true;
        protected string _sortField;
        protected ListSortDirection _sortDir;
        protected int? _finalPgIdx;
        protected int? _finalPgSize;

        public string DlgConfirmTemplate { get; set; }
        public string CheckBoxTemplateHeader { get; set; }
        public string CheckBoxTemplateCell { get; set; }
        public string CheckBoxSelAllId = "SelectAll";
        public string CheckBoxSelAllCss = "select-all-cbox";
        public string InputSearchId = "inputSearch";
        public string FormSearchId = "formSearch";
        public string FilterLanguageId = "filterLanguage";
        public string ButtonAddNewClass = "js-add-new-selector";
        public string ButtonDelBigClass = "delete_big";
        public string ButtonMultiSelActClass = "multi-select-action";
        public string TableCssClass = "table_all table_line table_hover";
        public string CheckBoxRowCssClass = "row-checkbox";
        public string RowEditCssClass = "grid-row-edit";
        public string DlgConfirmTitle = StringTable.Confirmation;
        public string DlgConfirmBtnCssClsCancel = "delete-cancel";
        public string DlgConfirmBtnCssClsOk = "delete-confirm";
        public string AjaxLoadErrorMessage = StringTable.AjaxLoadErrorMessage;
        public string AjaxCommandErrorMessage = StringTable.AjaxCommandErrorMessage;
        public string SearchCountMsg = StringTable.SearchCountMsg;

        public int DlgConfirmWidth = 350;
        public int DlgConfirmHeight = 150;
        /// <summary>
        /// called in OnDataBound
        /// </summary>
        public string CustomScript { get; set; }
        #endregion

        #region KendoGrid

        public KendoGrid(HtmlHelper htmlHelper, GridBuilder<TModel> grid, string name, string controller,
            string deleteAction = "Delete", string editAction = null, string addAction = null, 
            string editEventHandler = null, int? pageSize = null, string detailEventHandler = null,
            Action<KendoGrid<TModel>> firstInit = null)
        {
            if (firstInit != null) firstInit(this);

            _controller = controller;
            _deleteAction = deleteAction;
            _editAction = editAction;
            _addAction = addAction;
            _pageSize = pageSize;
            _htmlHelper = htmlHelper;
            _nameSelector = "#" + name.ToLower();
            _name = string.Format("grid{0}", Guid.NewGuid().ToString().Replace("-", ""));

            (_grid = grid)
                .Name(_name)
                .Selectable(select => select.Mode(GridSelectionMode.Single))
                .Scrollable(scroll => scroll.Enabled(false))
                .Sortable(sort => sort.Enabled(false))
                .Editable(editable => editable.Enabled(false))
                .Events(events =>
                {
                    events.DataBound(EventDataBound);
                    if (!string.IsNullOrEmpty(editEventHandler)) events.Edit(editEventHandler);
                    if (!string.IsNullOrEmpty(detailEventHandler))
                    {
                        events.DetailInit(detailEventHandler);
                        events.DetailCollapse("function(e){ $('." + CheckBoxRowCssClass + "', e.sender.element).prop('checked',false); }");
                    }
                })
                .HtmlAttributes(new { @bactionurl = ActionUrl(), @bactionargs = QParam.Args(true, false) });

            if (Editable)
                _grid.Editable(x => x.Enabled(true).Mode(GridEditMode.InLine));

            CheckBoxTemplateCell = @"<div style='text-align:center;'>
            <input id='" + name + "_rwcbox_#:Id#' class='" + CheckBoxRowCssClass + @" css-checkbox' type='checkbox' />
            <label for='" + name + "_rwcbox_#:Id#' class='css-labelcheck text_hide'></label></div>";

            CheckBoxTemplateHeader = @"<div>
            <input id='" + CheckBoxSelAllId + @"' class='css-checkbox " + CheckBoxSelAllCss + @"' type='checkbox' />
            <label for='" + CheckBoxSelAllId + @"' class='css-labelcheck text_hide' title='Select all'>Any Text</label></div>";

            DlgConfirmTemplate = @"
            <div id='modal'>
                <p class='prompt-mess'>" + StringTable.ConfirmDelete + @"</p>
                <div class='wr_bottom_popup_s'>
                    <section class='wr_btn_pp'>
                        <button class='delete-confirm btn_delete' type='button'>" + StringTable.Delete + @"</button>
                    </section>
                    <section class='wr_btn_pp'>
                        <button class='delete-cancel btn_cancel' type='button'>" + StringTable.Cancel + @"</button>
                    </section>
                </div>
            </div>";

            Paging(1, new int[0]);
        }

        #endregion

        #region Grid
        public GridBuilder<TModel> Grid
        {
            get { return _grid; }
        }
        #endregion
        #region Editable
        public bool Editable
        {
            get { return !string.IsNullOrEmpty(_addAction) || !string.IsNullOrEmpty(_editAction); }
        }
        #endregion
        #region GridId
        public string GridId
        {
            get { return _nameSelector.Substring(1); }
        }
        #endregion

        #region EventDataBound
        /// <summary>name of databound event handler</summary>
        public string EventDataBound
        {
            get { return "OnDataBound"; }
        }

        #endregion
        #region SetEditorFuncName
        /// <summary>name of SetEditor function</summary>
        public string SetEditorFuncName
        {
            get { return "SetEditor"; }
        }

        #endregion

        #region DataSource
        /// <summary>config grid with static datasource</summary>
        /// <param name="modelId">Id field of the model</param>
        /// <param name="items">data list</param>
        public KendoGrid<TModel> DataSource<TId>(Expression<Func<TModel, TId>> modelId, IEnumerable<TModel> items,
            Action<CustomDataSourceBuilder<TModel>> customConf = null)
        {
            _grid.BindTo(items);

            _grid.DataSource(dataSource =>
            {  
               var custDatasource = dataSource.Custom()
                    .ServerPaging(false)
                    .ServerSorting(false).PageSize(PageSizeSetting)
                    .Schema(schema => schema.Model(model =>
                    {
                        _dsModelDesc = model; model.Id(modelId); model.Field(modelId).Editable(false);
                    }));

               //if (Editable)
               //    custDatasource.Transport(transport =>
               //    {
               //        if (!string.IsNullOrEmpty(_editAction))
               //            transport.Update(_editAction, _controller, new { @lang = LanguageMgr.FocusLangCode });
               //        if (!string.IsNullOrEmpty(_addAction))
               //            transport.Create(_addAction, _controller, new { @lang = LanguageMgr.FocusLangCode });
               //    });

               custDatasource.Events(events => events
                   .Error(@"function(e) {console.log('grid Error', e);}")
                   .RequestStart(@"function(e) {console.log('grid RequestStart', e);}")
                   .RequestEnd(@"function(e) {console.log('grid RequestEnd', e);}"));

                if (customConf != null) customConf(custDatasource);
            });

            return this;
        }

        /// <summary>config grid with ajax datasource</summary>
        /// <param name="modelId">Id field of the model</param>
        /// <param name="actionList">name of action that return data list</param>
        /// <param name="cond">if true then run "customConf"</param>
        /// <param name="dataHandler">name of javascript function that return args for "actionList"</param>
        /// <param name="usePageSiz">set page size by query string param "PageSiz"</param>
        public KendoGrid<TModel> DataSource<TId>(Expression<Func<TModel, TId>> modelId,
            string actionList = "List", Func<bool> cond = null,
            Action<AjaxDataSourceBuilder<TModel>> customConf = null,
            string dataHandler = null, bool usePageSiz = true)
        {
            QParam.GetGridSort<TModel>(out _sortField, out _sortDir);

            _grid.DataSource(dataSource =>
            {
                var inputForm = QParam.IsInputForm;
                int? pgIdx = _finalPgIdx = inputForm ? 1 : (QParam.GetPageIdx() ?? 1), pgSize = _finalPgSize = inputForm || !usePageSiz ? PageSizeSetting : (QParam.GetPageSize() ?? PageSizeSetting);
                var ajaxDataSource = dataSource.Ajax()
                    .Model(model =>
                    {
                        _dsModelDesc = model; model.Id(modelId); model.Field(modelId).Editable(false);
                    })
                    .PageSize(pgSize ?? 0);

                ajaxDataSource.Read(read => read.Action(actionList, _controller)
                    .Data(@"function(e) {
                        var grid = $('#" + _name + @"').data('kendoGrid'), ds = grid.dataSource, sortField, kword = $('#" + InputSearchId + @"');
                        if(e.sort && e.sort[0]) sortField = e.sort[0].field + '-' + e.sort[0].dir;
                        e.page = ds.page();
                        ds.clawArgs = $.extend(
                            { keyword: (kword.is(':visible') ? kword.val() : ''), lang: '" + LanguageMgr.FocusLangCode + @"', 'grid-sort': sortField, pageidx: e.page, pagesiz: e.pageSize }, 
                            " + (string.IsNullOrEmpty(dataHandler) ? "{}" : dataHandler + "(grid,e)") + @");
                        return ds.clawArgs;
                    }"));

                if (_sortField != null) ajaxDataSource.Sort(sortCnf => sortCnf.Add(_sortField).Order(_sortDir));

                if (!string.IsNullOrEmpty(_editAction))
                    ajaxDataSource.Update(_editAction, _controller, new { @lang = LanguageMgr.FocusLangCode });
                if (!string.IsNullOrEmpty(_addAction)) 
                    ajaxDataSource.Create(_addAction, _controller, new { @lang = LanguageMgr.FocusLangCode });

                ajaxDataSource.Events(events => events
                    .Error(@"function(e) {
                        var errMsg = '" + AjaxLoadErrorMessage + @"', grid = $('#" + _name + @"').data('kendoGrid'); 
                        if(e.status == 'customerror' && e.errors && Object.keys(e.errors).length != 0){
                            var arrMsg = e.errors[Object.keys(e.errors)[0]].errors;
                            var dataRow = grid.clawSelectId != null ? $(grid.dataSource.get(grid.clawSelectId)) : null;
                            var uid = $(dataRow).attr('uid'), rowId = $(dataRow).attr('id');
                            errMsg = arrMsg && arrMsg.length > 0 ? arrMsg[0] : e.errorThrown; console.log('eror row uid = ', uid);
                            if(uid) setTimeout(function(){
                                var row = grid.element.find('tr[data-uid=' + uid + ']'); console.log('eror row = ', row);
                                if(row.length != 0){ grid.select(row); grid.editRow(row); row.find('input[data-val=true]').first().focus(); }
                                else console.log('Error not found uid=',uid, e);
                            }, 1000);
                        }
                        grid.clawReqStartTime = 0;
                        grid.showMessage(errMsg, '" + ActionResultStatus.Fail + @"'); 
                    }")
                    .RequestStart(@"function(e) { 
                        var lstPg = '" + (inputForm ? "0" : "1") + @"' == '1';
                        if(!e.sender.clawQParamInit) { 
                            if(" + pgIdx + @" > 1) { e.sender._skip = " + (pgIdx - 1) * pgSize + @"; e.sender._page = e.sender.page(); }
                            if(lstPg) $('#" + InputSearchId + @"').val('" + QParam.GetKWord().Replace("\\", "\\\\").Replace("'", "\\'") + @"');
                            e.sender.clawQParamInit = true;
                        }
                        var grid = $('#" + _name + @"').data('kendoGrid');
                        grid.clawSelectId = $(grid.dataItem(grid.select())).attr('id'); 
                        grid.clawReqStartTime = Date.now();
                        var deep = grid.element.attr('data-deep');
                        if(grid.clawSkipHideMsg) grid.clawSkipHideMsg = null; 
                        else if(lstPg && (!deep || deep == '0')) admincommon.hideMessage();

                    }")
                    .RequestEnd(@"function(e) {
                        var kword = $('#" + InputSearchId + @"'), keyword = (kword.is(':visible') ? kword.val() : ''), grid = $('#" + _name + @"').data('kendoGrid');
                        if((e.type == 'update' || e.type == 'create') && e.response && !e.response.Errors) {
                            grid.showMessage(e.type == 'update' ? '" + StringTable.UpdateSuccess + @"':'" + StringTable.AddNewSuccess + @"', '" + ActionResultStatus.Success + @"'); 
                        }
                        else if(e.type == 'read' && keyword && keyword.length > 0 && e.response && !(grid.clawSkipShowResultCountMsg || false)) {
                            grid.showMessage('" + SearchCountMsg + @"'.replace('{0}',e.response.Total), '" + ActionResultStatus.Success + @"');
                        }
                        grid.clawSkipShowResultCountMsg = null;   
                        if(!e.response || !e.response.Errors) grid.clawReqStartTime = 0;else console.log('RequestEnd skip',e.response);
                        var deep = grid.element.attr('data-deep');
                        if(!deep || deep == '0')                        
                            $('." + ButtonAddNewClass + @", #" + FilterLanguageId + @"').attr('actionargs', $.param(e.sender.clawArgs || {}, true));
                    }"));

                if (cond != null && customConf != null && cond()) customConf(ajaxDataSource);
                _ajaxDataSource = ajaxDataSource;
            });

            return this;
        }

        #endregion

        #region BuildShare
        /// <summary>return shared javascript code</summary>
        /// <param name="includeTag">include script tag in result code?</param>
        private StringBuilder BuildShare(bool includeTag = true)
        {
            Quote();

            var ret = new StringBuilder(includeTag ? "<script type='text/javascript'>" : "");

            #region AjaxAction
            
            ret.Append(@"
            function gridAjaxAction(gridSelector,dialog,actionUrl,arrayids){
               var parentid = $(gridSelector).attr('data-parentid');  
               var grid = $(parentid ? '#' + parentid : gridSelector).data('kendoGrid'); 
               $.ajax({
			        url: urlInjectFocusLang(actionUrl),
			        type: 'POST',
			        contentType: 'application/json; charset=utf-8',
			        data: JSON.stringify(arrayids),
			        beforeSend: function() { dialog.close(); admincommon.hideMessage(); admincommon.showMask(); },
			        success: function(result) {
                        grid.clawSkipShowResultCountMsg = true; grid.dataSource.read();
			            if (result.Message != null) grid.showMessage(result.Message, result.Status);
			        },
			        error: function (e) { grid.showMessage('" + AjaxCommandErrorMessage + @"', '" + ActionResultStatus.Fail + @"'); },
			        complete: function () { admincommon.hideMask(); }
			    });
            }");

            #endregion

            #region DoCmdAction
            ret.Append(@"
            function gridCmdAction(actionUrl, arrayids,ajaxHandler){
                var dialog = new ClawDialog('modal-mask');
                var title = " + '"' + DlgConfirmTitle + '"' + @";
                var content = " + '"' + Regex.Replace(DlgConfirmTemplate, @"\s{2,}", " ") + '"' + @";
                var kendoWindow = dialog.showDialog(title, content, " + DlgConfirmWidth + @", " + DlgConfirmHeight + @");
                kendoWindow.find('." + DlgConfirmBtnCssClsCancel + @"').click(function(){dialog.close();});
                kendoWindow.find('." + DlgConfirmBtnCssClsOk + @"').click(function(){ ajaxHandler(dialog); });
            }");
            #endregion

            #region DoCmdAction
            ret.Append(@"
            function gridDoCmdAction(grid, actionUrl, arrayids){
                gridCmdAction(actionUrl, arrayids,function (dialog){ gridAjaxAction(grid,dialog,actionUrl,arrayids);});
            }
            "); 
            #endregion

            if (includeTag) ret.Append(@"</script>");

            return ret;
        }

        #endregion
        #region BuildEditable
        /// <summary>return javascript code for inline edit</summary>
        /// <param name="includeTag">include script tag in result code?</param>
        private StringBuilder BuildEditable(bool includeTag = true)
        {
            var ret = new StringBuilder(includeTag ? "<script type='text/javascript'>" : "");

            if (Editable)
            {
                ret.Append(@" var " + _name + "EditorTempl = {};\r\n");

                foreach (var col in _columns)
                {
                    ret.Append(_name + "EditorTempl['" + col.Name + "'] = '" +
                        col.GetGridTextBox(_htmlHelper, 
                        new
                        {
                            @class = "text-box single-line",
                            @data_bind = string.IsNullOrEmpty(col.DataBind) ? null : "value:" + col.Name + "." + col.DataBind 
                        }).Replace("'", "\'") + "'; ");
                }

                ret.Append(@"
                function " + SetEditorFuncName + @"(fieldName){
                    var grid = $('#" + _name + @"').data('kendoGrid'), editorTempl = " + _name + @"EditorTempl;
                    grid.columns.forEach(function(col){
                        if(col.editor && col.field == fieldName) col.editor = editorTempl[fieldName]; 
                    });
                }");

                ret.Append("\r\n");

                foreach (var col in _columns)
                    ret.Append(SetEditorFuncName + "('" + col.Name + "'); ");
            }

            if (includeTag) ret.Append(@"</script>");

            return ret;
        }
        
        #endregion
        #region BuildScript
        /// <summary>return javascript code that handling grid events</summary>
        /// <param name="includeTag">include script tag in result code?</param>
        private StringBuilder BuildScript(bool includeTag = true)
        {
            var ret = new StringBuilder(includeTag ? "<script type='text/javascript'>" : "");
            var fmode = includeTag ? (_fullMode ? "1" : "0") : "fullMode";

            #region SelectAllCheckbox

            ret.Append(@"function gridInitSelAllCheckbox(gridSelector, fullmode){
                $(gridSelector + ' ." + CheckBoxSelAllCss + @"').unbind('click').click(function () {
                    var isChecked = $(this).is(':checked'), count = 0;
                    
                    $('." + CheckBoxRowCssClass + @":visible').each(function () {
                        if ($(this).attr('disabled') != 'disabled') {
                            $(this).prop('checked', isChecked); count++;
                            if (isChecked) $(this).parent().parent().addClass('active');
                            else $(this).parent().parent().removeClass('active');
                        }
                    });

                    var multiSelectAction = $('." + ButtonDelBigClass + ",." + ButtonMultiSelActClass + @"');
                    if (isChecked && count != 0) multiSelectAction.fadeIn(700);
                    else multiSelectAction.fadeOut(700);
                });
            }" + "\r\n");

            #endregion

            #region DeleteBigBtn & SearchBtn

            ret.Append(@"function gridInitDeleteBigSearchBtn(gridSelector, fullmode){
                if(fullmode == 1) {
                    $('." + ButtonDelBigClass + @"').click(function () {
                        var arrIds = []; var grid = $(gridSelector); var gridData = grid.data('kendoGrid');
                        var rows = gridData.tbody.find('input:checked." + CheckBoxRowCssClass + @"').closest('tr');
                        rows.each(function() { 
                            var childGrid = $(this).closest('.k-grid').data('kendoGrid');
                            var datarw = childGrid.dataItem($(this));
                            arrIds.push({id: datarw.id, grid: childGrid.element.attr('id')}); 
                        }); 
                        
                        if (arrIds.length > 0) gridDoCmdAction(grid, grid.attr('bactionurl') + '/" + _deleteAction +
                           @"', arrIds);
                    });

                    $('#" + FormSearchId + @"').submit(function () {
                        $(gridSelector).data('kendoGrid').dataSource.page(1);
                        return false;
                    });
                }
            }" + "\r\n");

            #endregion

            #region RowEditEvent

            ret.Append(@"function gridInitRowEditEvent(grid, holder){
                $('a." + RowEditCssClass + @"', holder || grid.element).unbind('mousedown').mousedown(function(e) {
                    var tdNode = $(this).is('td') ? $(this) : $(this).closest('td'), newWin = $(this).attr('target') == '_blank';
                    var rwdata = grid.dataItem($(this).closest('tr')), clawArgs = grid.dataSource.clawArgs || {};
                    var actionUrl = grid.element.attr('bactionurl'), actionArgs = grid.element.attr('bactionargs');
                    var url = actionUrl + '/' + tdNode.attr('action') + '/' + $(rwdata).attr(tdNode.attr('actfield'));
                    clawArgs['grid'] = '~'; 
                    if(newWin) url = urlMergeParams(url, $.extend(urlToJSON(actionArgs), {recontroller: $(this).attr('recontroller')}));
                    else url += actionArgs; 
                    if($(this).hasClass('deleted')) $(this).attr('href', null);                   
                    else $(this).attr('href', newWin ? url : urlMergeParams(url, urlToJSON('?' + $.param(clawArgs), false))); 
                });
            }" + "\r\n");

            #endregion

            #region OnDataBound

            ret.Append(@"function " + GridId + "OnDataBound(grid, gridSelector, fullmode) { ");

            #region Paging

            ret.Append(@"  
                if(grid.pager) {     
                    var pSizes = grid.pager.options.pageSizes;   
                    var ePager = grid.pager.element, deep = grid.element.attr('data-deep') || '0';
                    if (grid.dataSource.total() <= (pSizes != null && pSizes.length > 0 ? pSizes[0] : 10)) {
                        if(deep == '0') ePager.show(); else ePager.hide();
                        $('.k-link,.k-pager-numbers,.k-pager-sizes', ePager).hide();
                        $('.k-pager-info', ePager).css('float','left');
                    } else {
                        ePager.show().find('.k-pager-info').css('float','');
                        var links = $('.k-pager-numbers,.k-link,.k-pager-sizes', ePager);
                        var sizes = $('.k-pager-sizes', ePager);
                        if (grid.pager.totalPages() <= 1) { links.hide(); sizes.css('padding-left', 0); }
                        else { links.show(); sizes.css('padding-left', 17); }
                    }
                    if (grid.pager.page() > grid.pager.totalPages()) {
                        grid.pager.page(grid.pager.totalPages());
                    }
                }
            ");

            #endregion

            #region CssClass

            ret.Append(@"
            $(gridSelector + ' .k-selectable').addClass('" + TableCssClass + "');");

            foreach (var cmd in _commands)
                if (!string.IsNullOrEmpty(cmd.CssClass))
                    ret.Append(@"
                    $(gridSelector + ' .k-grid-" + cmd.Name + "').addClass('" + cmd.CssClass + "');");


            #endregion

            #region ResetCheckbox

            ret.Append(@"
                $(gridSelector + ' ." + CheckBoxSelAllCss + @"').prop('checked', false);
                $(gridSelector + ' tr ." + CheckBoxRowCssClass + @"').prop('checked', false);

                if (fullmode == 1)
                    $('." + ButtonDelBigClass + ",." + ButtonMultiSelActClass + @"').fadeOut(700);");

            #endregion

            #region RowCheckbox

            ret.Append(@"
                $(gridSelector + ' tr ." + CheckBoxRowCssClass + @"').unbind('click').click(function () {
                    if (this.checked) $(this).parent().parent().addClass('active');
                    else $(this).parent().parent().removeClass('active');
                               
                    var checkCount = $('input:checked." + CheckBoxRowCssClass + @":visible').length;
                    var multiSelectAction = $('." + ButtonDelBigClass + ",." + ButtonMultiSelActClass + @"');
                    if (checkCount > 0) multiSelectAction.fadeIn(700);
                    else multiSelectAction.fadeOut(700);
                           
                    var isCheck = $('." + CheckBoxRowCssClass + @":visible').length == checkCount;
                    var selAll = $('." + CheckBoxSelAllCss + @":visible');
                    if (selAll.checked != isCheck) selAll.prop('checked', isCheck);
                });");

            #endregion

            #region RowEdit

            ret.Append(@"
                gridInitRowEditEvent(grid);
                grid.clawRowEditEventHandler = gridInitRowEditEvent;
            ");

            #endregion

            #region EditCmd

            ret.Append(@"
                $('a.k-grid-Edit.edit_fr[jscriptaction=0],a.btn_edit.k-grid-Edit,a.btn_detail.k-grid-View', grid.element).unbind('mousedown').mousedown(function(e) {
                    var node = $(e.currentTarget), rwdata = grid.dataItem(node.closest('tr')), clawArgs = grid.dataSource.clawArgs || {};
                    var actionUrl = grid.element.attr('bactionurl'), actionArgs = grid.element.attr('bactionargs');
                    var url = actionUrl + '/' + node.attr('action') + '/' +  $(rwdata).attr(node.attr('actfield')) + actionArgs;
                    clawArgs['grid'] = '~'; $(this).attr('href', urlMergeParams(url, urlToJSON('?' + $.param(clawArgs), false))); 
                });
            "); 

            #endregion

            #region Offline
            ret.Append(@"
                var transport = grid.dataSource.transport, preOptions = grid.clawPreOptions;
                if (transport && transport.options && transport.options.clawOffline && preOptions && preOptions.transport) {
                    transport.options.read.url = preOptions.transport.read.url;
                    transport.options.data = null;  
                    transport.options.clawOffline = false;
                    grid.options.autoBind = true;
                }
            "); 
            #endregion

            ret.Append("\r\n\t}\r\n");
            
            #endregion

            ret.Append(@"
            function " + EventDataBound + @"() {
                " + GridId + "OnDataBound(this, '#" + _name + "', " + fmode + @"); ");

            if (!string.IsNullOrEmpty(CustomScript))
            {
                ret.Append("\r\n\r\n\t\t");
                ret.Append(CustomScript);
                ret.Append("(this);\r\n");
            }

            ret.Append(@"
            }

            gridInitSelAllCheckbox('#" + _name + "', " + fmode + @"); 
            gridInitDeleteBigSearchBtn('#" + _name + "', " + fmode + @"); 
            ");

            if (includeTag) ret.Append(@"</script>");

            return ret;
        }
        #endregion

        #region Sortable
        /// <summary>enable column sorting for whole grid</summary>
        public KendoGrid<TModel> Sortable(bool enable, GridSortMode mode = GridSortMode.SingleColumn)
        {
            _grid.Sortable(sortconf => sortconf.Enabled(enable).SortMode(mode));

            return this;
        }

        #endregion
        #region Config
        /// <summary>more custom config for the grid</summary>
        public KendoGrid<TModel> Config(Action<KendoGrid<TModel>> fConf)
        {
            fConf(this);

            return this;
        }
        #endregion
        #region Paging
        /// <summary>config paging params for the grid</summary>
        /// <param name="buttonCount">kendo arg</param>
        /// <param name="pageSizes">list of allowed page sizes</param>
        /// <param name="itemPhrase">localized name of item</param>
        public KendoGrid<TModel> Paging(int buttonCount, int[] pageSizes, string itemPhrase = null)
        {
            var pSizes = new List<int>(pageSizes);
            pSizes.Add(PageSizeSetting);

            _grid.Pageable(x => x.ButtonCount(buttonCount)
                .Numeric(true)
                .Messages(mbuilder => mbuilder
                    .ItemsPerPage(StringTable.GridPagingItemsPPage(itemPhrase))
                    .Display(StringTable.GridPagingDisplay(itemPhrase, PageSizeSetting))
                    .First(StringTable.GridPagingFirst)
                    .Previous(StringTable.GridPagingPrevious)
                    .Last(StringTable.GridPagingLast)
                    .Next(StringTable.GridPagingNext)
                    .Refresh(StringTable.GridPagingRefresh))
                .PageSizes(pSizes.Distinct().OrderBy(y => y).ToArray()));

            return this;
        }
        #endregion
        #region Columns
        /// <summary>define data columns for the grid</summary>
        /// <param name="fColumns">the function that return column defining array</param>
        public KendoGrid<TModel> Columns(Func<KendoGrid<TModel>, IColumn[]> fColumns)
        {
            _columns = fColumns(this);
            _grid.Columns(conf =>
            {
                foreach (var col in _columns)
                {
                    var hasTempl = col.Template != null;
                    var colBuilder = conf.Bound(col.Name)
                        .Title(col.Title)
                        .Sortable(col.Sortedable);

                    if (hasTempl) colBuilder.ClientTemplate(col.Template);
                    else
                        if (col.Editable)
                            colBuilder.ClientTemplate(string.Format("<a class='{0}' title=''>#:{1}#</a>", RowEditCssClass, col.Name));

                    if (col.HtmlAttributes != null)
                        colBuilder.HtmlAttributes(col.HtmlAttributes);
                    
                    if (col.Editable)
                        colBuilder.HtmlAttributes(new
                        {
                            @class = (col.CssClass == null ? "" : col.CssClass + " "),
                            @action = col.Action,
                            @actfield = col.ActField
                        });
                    else
                        if (col.CssClass != null) colBuilder.HtmlAttributes(new { @class = col.CssClass });

                    if (col.Width != 0) colBuilder.Width(col.Width);

                    if (Editable) col.ConfigInlineEdit(_dsModelDesc);
                }
            });

            return this;
        }

        #endregion
        #region ColumnCheckBox
        
        public KendoGrid<TModel> ColumnCheckBox<TId>(bool enable, Expression<Func<TModel, TId>> modelId, int width = 20, string prefix = null)
        {
            return enable ? ColumnCheckBox(ExpressionHelper.GetExpressionText(modelId), prefix, width) : this;
        }

        public KendoGrid<TModel> ColumnCheckBox<TId>(Expression<Func<TModel, TId>> modelId, int width = 20, string prefix = null)
        {
            return ColumnCheckBox(ExpressionHelper.GetExpressionText(modelId), prefix, width);
        }

        /// <summary>enable checkbox column and bind the column to a field</summary>
        /// <param name="prefix">define prefix for attributes [Id,Name] of html elements</param>
        public KendoGrid<TModel> ColumnCheckBox(string fieldName, string prefix = null, int width = 20)
        {
            string templCell = CheckBoxTemplateCell;

            if (fieldName != "Id" || !string.IsNullOrEmpty(prefix))
            {
                if (!string.IsNullOrEmpty(prefix)) templCell = templCell.Replace("_#:Id#", prefix + "_#:Id#");
                templCell = templCell.Replace("_#:Id#", "_#:" + fieldName + "#");
            }

            _grid.Columns(
                columns => columns
                    .Bound(fieldName)
                    .Sortable(false)
                    .Width(width)
                    .ClientTemplate(templCell)
                    .HeaderTemplate(CheckBoxTemplateHeader)
                );

            return this;
        }

        #endregion
        #region ColumnCommand
        /// <summary>define command column for the grid</summary>
        /// <param name="fCommands">the function that return commands defining array</param>
        public KendoGrid<TModel> ColumnCommand(Func<KendoGrid<TModel>, ICommand[]> fCommands, int width = 120, string title = "Actions",
            string cssClass = "claw-action")
        {
            var commands = fCommands(this);

            if (commands.Length == 0) return this;

            _commands.AddRange(commands);

            _grid.Columns(columns => columns
                .Command(actions =>
                {
                    foreach (var cmd in commands)
                    {
                        var cmdBuilder = actions
                            .Custom(cmd.Name)
                            .Text(cmd.Text ?? cmd.Name)
                            .HtmlAttributes(new
                            {
                                @href = "javascript:void(0);",
                                @title = (cmd.Text ?? cmd.Name).GlobalLocalize(),
                                @action = cmd.Action,
                                @actfield = cmd.ActField,
                                @jscriptaction = cmd.JScriptAction ? 1 : 0
                            });

                        if (cmd.ConfirmDlg)
                            cmdBuilder.Click(@"function(e){  
                                e.preventDefault(); e.stopPropagation();
                                var grid = $(e.delegateTarget), node = $(e.currentTarget);
                                var rwdata = this.dataItem(node.closest('tr'));  
                                var actionUrl = grid.attr('bactionurl') + '/' + node.attr('action') + grid.attr('bactionargs');
                                var arrayids = [{ id:$(rwdata).attr(node.attr('actfield')), grid:grid.attr('id') }];
                                gridDoCmdAction(grid, actionUrl, arrayids);
                            }");
                        else
                            cmdBuilder.Click(@"function(e){ 
                                e.preventDefault(); e.stopPropagation();
                                var grid = $(e.delegateTarget), node = $(e.currentTarget);
                                var rwdata = this.dataItem(node.closest('tr')), clawArgs = this.dataSource.clawArgs || {};
                                if(node.attr('jscriptaction') == '1') eval(node.attr('action') + '(\'' + rwdata.uid + '\',\'' + grid.attr('id') + '\')');
                                else {
                                    var url = grid.attr('bactionurl') + '/' + node.attr('action') + '/' +  $(rwdata).attr(node.attr('actfield')) + grid.attr('bactionargs');
                                    $(location).attr('href', urlMergeParams(url, urlToJSON('?' + $.param(clawArgs), false)));
                                }
                            }");
                    }
                })
                .HtmlAttributes(new { @class = cssClass })
                .Title(title.GlobalLocalize())
                .Width(width)
                );

            return this;
        }

        #endregion

        #region Col
        /// <summary>create a data column define</summary>
        /// <param name="expr">bind the column to field?</param>
        /// <param name="titleExpr">bind column title to another field</param>
        public IColumn Col<TVal, TTit>(Expression<Func<TModel, TVal>> expr, Expression<Func<TModel, TTit>> titleExpr, bool sortable = false, int width = 0)
        {
            return new Column<TVal, TTit>()
            {
                Expr = expr,
                TitleExpr = titleExpr,
                Action = string.Empty,
                Width = width,
                Sortedable = sortable
            };
        }

        public IColumn Col<TVal>(Expression<Func<TModel, TVal>> expr, string titleVal, bool sortable, int width = 0)
        {
            return new Column<TVal, object>()
            {
                Expr = expr,
                TitleVal = titleVal,
                Action = string.Empty,
                Width = width,
                Sortedable = sortable
            };
        }

        public IColumn Col<TVal>(Expression<Func<TModel, TVal>> expr, bool sortable = false, int width = 0)
        {
            return new Column<TVal, string>()
            {
                Expr = expr,
                Action = string.Empty,
                Width = width,
                Sortedable = sortable
            };
        }

        /// <summary>create a data column define</summary>
        /// <param name="expr">bind the column to field?</param>
        /// <param name="titleExpr">bind column title to another field</param>
        /// <param name="action">name of action that will be invoke when the data cell clicked</param>
        /// <param name="actExpr">which field to be arg for "action"</param>
        public IColumn Col<TVal, TTit, TAct>(Expression<Func<TModel, TVal>> expr,
            Expression<Func<TModel, TTit>> titleExpr, string action, Expression<Func<TModel, TAct>> actExpr, bool sortable = false, int width = 0)
        {
            if (action.EqualsIgCase("Edit")) action = "Input";

            return new Column<TVal, TTit, TAct>()
            {
                Expr = expr,
                TitleExpr = titleExpr,
                Action = action,
                ActExpr = actExpr,
                Width = width,
                Sortedable = sortable
            };
        }

        public IColumn Col<TVal, TAct>(Expression<Func<TModel, TVal>> expr, string action, Expression<Func<TModel, TAct>> actExpr, bool sortable = false, int width = 0)
        {
            if (action.EqualsIgCase("Edit")) action = "Input";

            return new Column<TVal, string, TAct>()
            {
                Expr = expr,
                Action = action,
                ActExpr = actExpr,
                Width = width,
                Sortedable = sortable
            };
        }

        #endregion
        #region Cmd
        /// <summary>create a command define</summary>
        /// <param name="actExpr">which field to be arg for "action"</param>
        /// <param name="action">name of action that will be invoke when the command cell clicked</param>
        /// <param name="confirmDlg">show the confirm dialog box</param>
        /// <param name="text">display text</param>
        /// <param name="jscriptAction">"action" is a name of a javascript function (uid, gridid) ?</param>
        public ICommand Cmd<TVal>(Expression<Func<TModel, TVal>> actExpr, string action, string name, string cssClass,
            bool confirmDlg = false, string text = null, bool jscriptAction = false)
        {
            return new Command<TVal>()
            {
                ActExpr = actExpr,
                Action = action,
                Name = name,
                CssClass = cssClass,
                Text = text,
                ConfirmDlg = confirmDlg,
                JScriptAction = jscriptAction && !confirmDlg
            };
        }

        #endregion

        #region PageSizeSetting
        public int PageSizeSetting
        {
            get { return Math.Max(1, _pageSize ?? BaseController.GetSettings().Display.Admin.PageSize); }
        }
        #endregion

        #region GetEagerData

        public DataSourceResult GetEagerData(object dataHanlder)
        {
            if (dataHanlder == null) return null;

            return ((GridEagerLoadDataHandler) dataHanlder)(_finalPgIdx ?? 1, _finalPgSize ?? 10, _sortField ?? "Id",
                _sortDir == ListSortDirection.Ascending ? SortDirection.Ascending : SortDirection.Descending);
        }

        #endregion

        #region Quote
        void Quote()
        {
            DlgConfirmTemplate = DlgConfirmTemplate.Replace("\"", "'");
            DlgConfirmTitle = DlgConfirmTitle.Replace("\"", "'");

            AjaxLoadErrorMessage = AjaxLoadErrorMessage.Replace("'", "&#39;");
            AjaxCommandErrorMessage = AjaxCommandErrorMessage.Replace("'", "&#39;");
        }
        #endregion
        #region ToString
        /// <summary>return html & javascript code of the grid</summary>
        public string ToString(bool buildShareCode = true)
        {
            #region DefaultSort
            if (_sortField.IsNullOrEmpty() && !QParam.GetGridSort().EqualsIgCase("none") && _ajaxDataSource != null)
            {
                var defSortCol = _columns.FirstOrDefault(x => x.Sortedable && x.DefaultSort);
                if (defSortCol != null)
                {
                    _sortField = defSortCol.Name;
                    _sortDir = defSortCol.SortDesc ? ListSortDirection.Descending : ListSortDirection.Ascending;
                    _ajaxDataSource.Sort(sortCnf => sortCnf.Add(_sortField).Order(_sortDir));
                }

            } 
            #endregion

            const string scriptTag = "<script>";
            const string jQStart = "jQuery(function(){";
            const string jQEnd = "});";
            const string kendoCode = ".kendoGrid(";
            var gridCode = _grid.ToString();
            var iB = gridCode.IndexOf("<script>",StringComparison.OrdinalIgnoreCase);
            var iE = gridCode.IndexOf("</script>", iB, StringComparison.OrdinalIgnoreCase);

            var ret = gridCode.Substring(0, iB);
            iB += scriptTag.Length;
            iB = gridCode.IndexOf(jQStart, iB, StringComparison.OrdinalIgnoreCase);
            iB += jQStart.Length;
            iB = gridCode.IndexOf(kendoCode, iB, StringComparison.OrdinalIgnoreCase);
            iB += kendoCode.Length;

            iE = gridCode.LastIndexOf(jQEnd, iE, StringComparison.OrdinalIgnoreCase);
            iE -= jQEnd.Length - 1;

            ret = Regex.Replace(ret, @"([?]|&amp;)grid-sort=[^=&]*(&amp;|$)", "$1", RegexOptions.IgnoreCase);
            ret = ret.Replace(_name + "-sort=", "grid-sort=");

            ret += "\r\n <script> \r\n " + (buildShareCode ? BuildShare(false).ToString() : "") +
                   " \r\n function " + GridId + "Init(nameSelector, fullMode, initHandler) { \r\n" +
                   BuildScript(false) + ";\r\nvar gridParams = " + gridCode.Substring(iB, iE - iB) +
                   ";\r\n var initArgResult = initHandler ? initHandler(gridParams) :null;\r\n jQuery(nameSelector).kendoGrid(gridParams);" + BuildEditable(false) +
                   "var grid = $('#" + _name + @"').data('kendoGrid'); grid.clawPreOptions = initArgResult; " +
                   @"grid.showMessage = function(message, status) {     
                        admincommon.hideMessage(); admincommon.showMessageDefault(message, status);
                   };
                   if (initArgResult) setTimeout(function(){ grid.dataSource.transport.options.read.data(initArgResult.dataEvent); }, 100);
                   if (!grid.options.autoBind){ 
                        if (initArgResult && initArgResult.beforeLoad) initArgResult.beforeLoad();
                        grid.dataSource.read();
                    } " + "\r\n }";

            ret = ret.Replace("'#" + _name + "'", "nameSelector");
            ret = ret.Replace("\"#" + _name + "\"", "nameSelector");

            return ret + "\r\n $('#" + _name + "').attr('id','" + GridId + "'); \r\n </script>";
        }
        #endregion
        #region ActionUrl
        /// <summary>create url for a action name</summary>
        private string ActionUrl(string action = "")
        {
            UrlHelper uhelper = new UrlHelper(_htmlHelper.ViewContext.RequestContext, _htmlHelper.RouteCollection);
            return uhelper.Action(action, _controller);
        }

        #endregion
        #region ToMvcHtmlString

        public MvcHtmlString ToMvcHtmlString(bool buildShareCode = true, bool fullMode = true)
        {
            _fullMode = fullMode;

            var ret = MvcHtmlString.Create(ToString(buildShareCode));

            _fullMode = true;

            return ret;
        }

        #endregion
    }
}
