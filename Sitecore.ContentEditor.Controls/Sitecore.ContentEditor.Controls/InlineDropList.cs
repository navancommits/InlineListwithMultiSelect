using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web.UI;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Resources;
using Sitecore.SecurityModel;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using ListItem = Sitecore.Web.UI.HtmlControls.ListItem;
using Sitecore.Globalization;
using Sitecore.Collections;
using System.Collections;
using Sitecore.Web.UI.HtmlControls.Data;
using Sitecore.Shell;
using Sitecore;
using System.Web;

namespace Foundation.ContentEditor.Controls
{
	/// <summary>
	/// This field behaves like a Droplist, except that the source of the field is dynamic and 
	/// allows authors to add items to the source without "leaving" the item they're currently editing.
	///
	/// Dynamically added items are based on a pre-defined template, Authors can also edit or delete any item in the combo.
	/// </summary>
	public class InlineDropList : Sitecore.Web.UI.HtmlControls.Control
	{
        private string _fieldname;
        private string _source;
        private string _itemid;
        private bool _hasPostData;
        StringBuilder sb = new StringBuilder();
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sitecore.Shell.Applications.ContentEditor.LookupEx" /> class.
        /// </summary>
        public InlineDropList()
        {
            this._fieldname = string.Empty;
            this._source = string.Empty;
            base.Class = "scContentControl scCombobox";
            base.Background = "white";
            this.Activation = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is read only.
        /// </summary>
        /// <value><c>true</c> if  this instance is read only; otherwise, <c>false</c>.</value>
        public virtual bool ReadOnly
        {
            get => this.GetViewStateBool(nameof(ReadOnly));
            set
            {
                this.SetViewStateBool(nameof(ReadOnly), value);
                if (value)
                {
                    this.Attributes["readonly"] = "readonly";
                    this.Disabled = true;
                }
                else
                    this.Attributes.Remove("readonly");
            }
        }


        public string SourceItemTemplateID
        {
            get { return Sitecore.StringUtil.GetString(ServerProperties["SourceItemTemplateID"]); }
            set { ServerProperties["SourceItemTemplateID"] = value; }
        }

        public string UniqueDataSourceBasePath
        {
            get { return Sitecore.StringUtil.GetString(ServerProperties["UniqueDataSourceBasePath"]); }
            set { ServerProperties["UniqueDataSourceBasePath"] = value; }
        }

        public string StandardDataSource
        {
            get { return Sitecore.StringUtil.GetString(ServerProperties["StandardDataSource"]); }
            set { ServerProperties["StandardDataSource"] = value; }
        }

        public string DataSourcePath
        {
            get { return Sitecore.StringUtil.GetString(ServerProperties["DataSourcePath"]); }
            set { ServerProperties["DataSourcePath"] = value; }
        }

        /// <summary>Gets or sets the name of the field.</summary>
        /// <value>The name of the field.</value>
        /// <contract>
        ///   <requires name="value" condition="not null" />
        ///   <ensures condition="nullable" />
        /// </contract>
        public string FieldName
        {
            get => this._fieldname;
            set
            {
                this._fieldname = value;
            }
        }

        /// <summary>Gets or sets the item language.</summary>
        /// <value>The item language.</value>
        public string ItemLanguage
        {
            get => this.GetViewStateString(nameof(ItemLanguage));
            set
            {
                this.SetViewStateString(nameof(ItemLanguage), value);
            }
        }

        /// <summary>Gets or sets the item ID.</summary>
        /// <value>The item ID.</value>
        /// <contract>
        ///   <requires name="value" condition="not null" />
        ///   <ensures condition="nullable" />
        /// </contract>
        public string ItemID
        {
            get => this._itemid;
            set
            {
                this._itemid = value;
            }
        }

        /// <summary>
		/// The FieldID property is set by the Content Editor via reflection
		/// </summary>
		public string FieldID { get; set; }

        public string Rendered { get; set; }

        /// <summary>Gets or sets the source.</summary>
        /// <value>The source.</value>
        /// <contract>
        ///   <requires name="value" condition="not null" />
        ///   <ensures condition="nullable" />
        /// </contract>
        public string Source
        {
            get => this._source;
            set
            {
                this._source = value;
            }
        }

        /// <summary>
		/// The ItemVersion property is set by the Content Editor via reflection
		/// </summary>
		public string ItemVersion { get; set; }

        /// <summary>
		/// Set the properties used by this field
		/// </summary>
		protected void SetProperties()
        {
            NameValueCollection sourceParameters = Sitecore.Web.WebUtil.ParseUrlParameters(Source.ToLower());
            UniqueDataSourceBasePath = sourceParameters["uniquedatasourcebasepath"];
            StandardDataSource = sourceParameters["standarddatasource"];
            SourceItemTemplateID = sourceParameters["sourceitemtemplateid"];

            DataSourcePath = GetDataSourcePath();
        }
        /// <summary>
		/// Get the source path where items for this field will be added.
		/// The item source path starts with the BaseSourcePath property and then uses the current Item ID and the current Field ID as 
		/// the final parts of the path. This way the field has it's own dynamic source.
		/// The item source path will look like this:
		/// [BaseSourcePath]/[ItemID.ToShortID()]/[FieldID.ToShortID()]
		/// </summary>
		/// <returns></returns>
		private string GetDataSourcePath()
        {
            Item item = Sitecore.Context.ContentDatabase.GetItem(ItemID, Language.Parse(ItemLanguage), Sitecore.Data.Version.Parse(ItemVersion));
            if (item == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(UniqueDataSourceBasePath))
                return UniqueDataSourceBasePath;

            if (!string.IsNullOrEmpty(StandardDataSource))
                return GetStandardDataSourcePath(item);

            return string.Empty;
        }

        private string GetStandardDataSourcePath(Item currentItem)
        {
            Item sourceItem = currentItem.Database.GetItem(StandardDataSource);
            if (sourceItem == null)
                return string.Empty;

            return sourceItem.Paths.FullPath;
        }

        private string GetUniqueDataSourcePath(Item currentItem)
        {
            //If the current item doesn't exist or it's a standard values item, then don't try to create the corresponding item folders
            //If we don't allow use of the field in Standard Values, should probably disable the editing controls when in a Standard Values item view
            if (currentItem == null || TemplateManager.IsStandardValuesHolder(currentItem))
                return string.Empty;

            Field f = currentItem.Fields[Sitecore.Data.ID.Parse(FieldID)];
            if (f == null)
                return string.Empty;

            Item baseSourceItem = Sitecore.Context.ContentDatabase.GetItem(UniqueDataSourceBasePath);
            if (baseSourceItem == null)
                return string.Empty;

            Sitecore.Data.TemplateID folderTemplateID = new Sitecore.Data.TemplateID(Sitecore.TemplateIDs.Folder);

            string itemLevelName = currentItem.ID.ToShortID().ToString();
            Item itemLevelSource = baseSourceItem.Axes.GetChild(itemLevelName) ?? baseSourceItem.Add(itemLevelName, folderTemplateID);
            if (itemLevelSource.DisplayName != currentItem.Name)
            {
                using (new SecurityDisabler())
                {
                    itemLevelSource.Editing.BeginEdit();
                    itemLevelSource.Appearance.DisplayName = currentItem.Name;
                    itemLevelSource.Editing.EndEdit();
                }
            }

            string fieldLevelName = f.ID.ToShortID().ToString();
            Item fieldLevelSource = itemLevelSource.Axes.GetChild(fieldLevelName) ?? itemLevelSource.Add(fieldLevelName, folderTemplateID);
            if (fieldLevelSource.DisplayName != f.Name)
            {
                using (new SecurityDisabler())
                {
                    fieldLevelSource.Editing.BeginEdit();
                    fieldLevelSource.Appearance.DisplayName = f.Name;
                    fieldLevelSource.Editing.EndEdit();
                }
            }

            return string.Format("{0}/{1}/{2}", UniqueDataSourceBasePath, itemLevelName, fieldLevelName);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load"></see> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs"></see> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                SetProperties();
                GridPanel gridPanel = new GridPanel { Columns = 4, CellPadding = "2", Fixed = false };

                GetControlAttributes();
                foreach (string key in Attributes.Keys)
                {
                    gridPanel.Attributes.Add(key, Attributes[key]);
                }

                sb.Append("<table>");
                sb.Append("<tr>");

                //Add button
                string clientEventAdd = Sitecore.Context.ClientPage.GetClientEvent(ID + ".Add");
                sb.AppendFormat("<td><a href=\"#\" class=\"scContentButton\" onclick=\"{0}\">Add Item</a></td>", clientEventAdd);
                ImageBuilder imageAdd = new ImageBuilder { Src = "Applications/16x16/add.png", ID = ID + "_add", OnClick = clientEventAdd };
                sb.AppendFormat("<td>{0}</td>", imageAdd);

                //spacer
                sb.AppendFormat("<td>{0}</td>", Images.GetSpacer(16, 1));

                //Edit button
                string clientEventEdit = Sitecore.Context.ClientPage.GetClientEvent(ID + ".Edit");
                sb.AppendFormat("<td><a href=\"#\" class=\"scContentButton\" onclick=\"{0}\">Edit Item</a></td>", clientEventEdit);
                ImageBuilder imageEdit = new ImageBuilder { Src = "Applications/16x16/edit.png", ID = ID + "_edit", OnClick = clientEventEdit };
                sb.AppendFormat("<td>{0}</td>", imageEdit);

                //spacer
                sb.AppendFormat("<td>{0}</td>", Images.GetSpacer(16, 1));

                //Delete button
                string clientEventRemove = Sitecore.Context.ClientPage.GetClientEvent(ID + ".Remove");
                sb.AppendFormat("<td><a href=\"#\" class=\"scContentButton\" onclick=\"{0}\">Remove Item</a></td>", clientEventRemove);
                ImageBuilder imageRemove = new ImageBuilder { Src = "Applications/16x16/delete.png", ID = ID + "_delete", OnClick = clientEventRemove };
                sb.AppendFormat("<td>{0}</td>", imageRemove);

                sb.Append("</tr>");
                sb.Append("</table>");
            }
            base.OnLoad(e);
            if (this._hasPostData)
            return;
            this.LoadPostData(string.Empty);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.PreRender"></see> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"></see> object that contains the event data.</param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            this.ServerProperties["Value"] = this.ServerProperties["Value"];
        }

        /// <summary>
		/// This event responds to the client 'Add Item' button. It basically just kicks off the AddItem pipeline/method.
		/// </summary>
		protected void Add()
        {
            if (Disabled)
                return;

            Sitecore.Context.ClientPage.Start(this, "AddItem");
        }

        /// <summary>
		/// This event responds to the client 'Remove Item' button and kicks off the RemoveItem pipeline/method 
		/// </summary>
		protected void Remove()
        {
            if (Disabled)
                return;

            //run the RemoveItem pipeline/method
            Sitecore.Context.ClientPage.Start(this, "RemoveItem");
        }

        protected void Edit()
        {
            if (Disabled)
                return;

            //run the EditItem pipeline/method, passing in the selected item's ID
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("itemid", this.GetViewStateString("Value"));
            Sitecore.Context.ClientPage.Start(this, "EditItem", nvc);
        }

        /// <summary>Renders the control.</summary>
        /// <param name="output">The output.</param>
        protected override void DoRender(HtmlTextWriter output)
        {
            if (string.IsNullOrWhiteSpace(this.ItemID)) ItemID= this.GetViewStateString("CurrentItemID");
            Item[] items = this.GetItems(Sitecore.Context.ContentDatabase.GetItem(ItemID, Language.Parse(this.ItemLanguage)));
            output.Write(sb.ToString());
            output.Write("<select" + this.GetControlAttributes() + ">");
            output.Write("<option value=\"\"></option>");
            bool flag1 = false;
            foreach (Item obj in items)
            {
                string itemHeader = this.GetItemHeader(obj);
                bool flag2 = this.IsSelected(obj);
                if (flag2)
                    flag1 = true;
                output.Write("<option value=\"" + this.GetItemValue(obj) + "\"" + (flag2 ? " selected=\"selected\"" : string.Empty) + ">" + itemHeader + "</option>");
            }
            bool flag3 = !string.IsNullOrEmpty(this.Value) && !flag1;
            if (flag3)
            {
                output.Write("<optgroup label=\"" + Translate.Text("Value not in the selection list.") + "\">");
                string str = HttpUtility.HtmlEncode(this.Value);
                output.Write("<option value=\"" + str + "\" selected=\"selected\">" + str + "</option>");
                output.Write("</optgroup>");
            }
            output.Write("</select>");
            if (!string.IsNullOrWhiteSpace(DataSourcePath)) output.Write("DataSource: " + DataSourcePath);
            if (!flag3)
                return;
            output.Write("<div style=\"color:#999999;padding:2px 0px 0px 0px\">{0}</div>", (object)Translate.Text("The field contains a value that is not in the selection list."));
        }

        /// <summary>
        /// Method for adding an item to the field source.
        /// </summary>
        /// <param name="args"></param>
        protected void AddItem(ClientPipelineArgs args)
        {
            // Get the source item for the field.
            if (string.IsNullOrEmpty(DataSourcePath)) 
            {
                SheerResponse.Alert("No field source is specified, I have no place to put the item you want to add", new string[0]);
                return;
            }

            Item fieldSource = Sitecore.Context.ContentDatabase.GetItem(DataSourcePath, LanguageManager.DefaultLanguage);
            if (fieldSource == null)
            {
                SheerResponse.Alert("Field source not found", new string[0]);
                return;
            }

            if (!fieldSource.Access.CanCreate())
            {
                SheerResponse.Alert("You do not have permission to create items here.", new string[0]);
                return;
            }

            TemplateItem template = Sitecore.Context.ContentDatabase.GetTemplate(SourceItemTemplateID);
            if (template == null)
            {
                SheerResponse.Alert("Template not found", new string[0]);
                return;
            }

            //on initial run, prompt the user for the name of the new item
            if (!args.IsPostBack)
            {
                SheerResponse.Input("Enter the name of the new item:", template.DisplayName, Settings.ItemNameValidation, "'$Input' is not a valid name.", Settings.MaxItemNameLength);
                args.WaitForPostBack();
            }
            else
            {
                if (args.HasResult)
                {
                    //Disable site notifications before creating the new item, otherwise the content editor will
                    //try to redirect the user to the new item after it has been created.
                    Sitecore.Client.Site.Notifications.Disabled = true;
                    Item newItem = fieldSource.Add(args.Result, template);
                    Sitecore.Client.Site.Notifications.Disabled = false;

                    string fieldId = GetViewStateString("ID");
                    Sitecore.Web.UI.HtmlControls.Control listbox = FindControl(fieldId) as Sitecore.Web.UI.HtmlControls.Control;
                    if (listbox == null)
                        return;

                    //add a new item to the source listbox
                    ListItem listItem = new ListItem { ID = GetUniqueID("I"), Header = newItem.Name, Value = newItem.ID.ToString() };
                    Sitecore.Context.ClientPage.AddControl(listbox, listItem);

                    //run the EditItem pipeline/method so the user can populate the new item's fields
                    NameValueCollection nvc = new NameValueCollection();
                    nvc.Add("itemid", newItem.ID.ToString());
                    Sitecore.Context.ClientPage.Start(this, "EditItem", nvc);

                    Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox);

                    SetModified();
                }
            }
        }

        /// <summary>
		/// Refreshes the Listboxes used in the field
		/// </summary>
		private void Refresh()
        {
            string fieldId = GetViewStateString("ID");
            Sitecore.Web.UI.HtmlControls.Control listbox = FindControl(fieldId) as Sitecore.Web.UI.HtmlControls.Control;
            if (listbox == null)
                return;

            Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox);
        }


        protected void EditItem(ClientPipelineArgs args)
        {
            //on first run, extract the ID of the item to edit
            //then launch the Content Editor in a modal dialog
            if (!args.IsPostBack)
            {
                string idParam = args.Parameters["itemid"];

                UrlString str = new UrlString("/sitecore/shell/Applications/Content Manager/default.aspx");
                str["fo"] = idParam;
                str["mo"] = "popup";
                str["wb"] = "0";
                str["pager"] = "0";
                Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(str.ToString(), "630", "560");

                args.WaitForPostBack();
            }
            else
            {
                Refresh();
            }
        }

        /// <summary>
		/// This method removes the currently selected item from it's respective Listbox and 
		/// also moves the item to the Recycle Bin (or deletes the item if the Recycle Bin is not active)
		/// </summary>
		/// <param name="args"></param>
		protected void RemoveItem(ClientPipelineArgs args)
        {
            Item item = Sitecore.Context.ContentDatabase.GetItem(this.GetViewStateString("Value"));
            if (item == null || !item.Access.CanDelete())
            {
                SheerResponse.Alert("You do not have permission to delete this item.", new string[0]);
                return;
            }

            if (!args.IsPostBack)
            {
                SheerResponse.Confirm("Are you sure you want to delete this item?");
                args.WaitForPostBack();
            }
            else
            {
                if (args.HasResult && args.Result.ToLower() == "yes")
                {
                    item.Recycle();

                    string fieldId = GetViewStateString("ID");
                    Sitecore.Web.UI.HtmlControls.Control listbox = FindControl(fieldId) as Sitecore.Web.UI.HtmlControls.Control;
                    if (listbox == null)
                        return;


                    listbox.Controls.Remove(listbox);
                    Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox);
                    SetModified();
                }
            }
        }

        /// <summary>Gets the items.</summary>
        /// <param name="current">The current.</param>
        /// <returns>The items.</returns>
        protected virtual Item[] GetItems(Item current)
        {
            using (new LanguageSwitcher(this.ItemLanguage))
                return LookupSources.GetItems(current, DataSourcePath);
        }
        
        /// <summary>Gets the item header.</summary>
        /// <param name="item">The item.</param>
        /// <returns>The item header.</returns>
        /// <contract>
        ///   <requires name="item" condition="not null" />
        ///   <ensures condition="not null" />
        /// </contract>
        protected virtual string GetItemHeader(Item item)
        {
            if (!UserOptions.View.UseDisplayName)
                return item.Name;
            string str = StringUtil.GetString(this.FieldName);
            return !str.StartsWith("@", StringComparison.InvariantCulture) ? (str.Length <= 0 ? item.DisplayName : item[this.FieldName]) : item[str.Substring(1)];
        }

        /// <summary>Gets the item value.</summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        /// <contract>
        ///   <requires name="item" condition="not null" />
        ///   <ensures condition="not null" />
        /// </contract>
        protected virtual string GetItemValue(Item item)
        {
            return item.ID.ToString();
        }

        /// <summary>Determines whether the specified item is selected.</summary>
        /// <param name="item">The item.</param>
        /// <returns>
        /// 	<c>true</c> if the specified item is selected; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool IsSelected(Item item)
        {
            return this.Value == item.ID.ToString() || this.Value == item.Paths.LongID;
        }

        /// <summary>Loads the post data.</summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <contract>
        ///   <requires name="value" condition="none" />
        /// </contract>
        protected override bool LoadPostData(string value)
        {
            this._hasPostData = true;
            if (value == null)
                return false;
            if (this.GetViewStateString("Value") != value)
                SetModified();
            this.SetViewStateString("Value", value);
            this.SetViewStateString("ID", ID);
            if (!string.IsNullOrWhiteSpace(this.ItemID)) this.SetViewStateString("CurrentItemID", this.ItemID);
            return true;
        }

        /// <summary>Sets the modified.</summary>
        private static void SetModified() => Sitecore.Context.ClientPage.Modified = true;
    }
}