using Sitecore;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.HtmlControls.Data;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace Foundation.ContentEditor.Controls
{
    public class CustomChecklist : Sitecore.Web.UI.HtmlControls.Checklist
    {
        private string _fieldname = string.Empty;
        private string _itemid;
        private string _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sitecore.Shell.Applications.ContentEditor.Checklist" /> class.
        /// </summary>
        public CustomChecklist()
        {
            this.Class = "scContentControlChecklist";
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

        /// <summary>Gets or sets the item language.</summary>
        /// <value>The item language.</value>
        public string ItemLanguage
        {
            get => StringUtil.GetString(this.ViewState[nameof(ItemLanguage)]);
            set
            {
                this.ViewState[nameof(ItemLanguage)] = (object)value;
            }
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

        /// <summary>Gets or sets the source.</summary>
        /// <value>The source.</value>
        /// <contract>
        ///   <requires name="value" condition="not null" />
        ///   <ensures condition="not null" />
        /// </contract>
        public string Source
        {
            get => StringUtil.GetString(this._source);
            set
            {
                this._source = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="T:Sitecore.Shell.Applications.ContentEditor.Checklist" /> tracks the modified.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the <see cref="T:Sitecore.Shell.Applications.ContentEditor.Checklist" /> tracks the  modified; otherwise, <c>false</c>.
        /// </value>
        public bool TrackModified
        {
            get => this.GetViewStateBool(nameof(TrackModified), true);
            set => this.SetViewStateBool(nameof(TrackModified), value, true);
        }

        /// <summary>Handles the message.</summary>
        /// <param name="message">The message.</param>
        /// <contract>
        ///   <requires name="message" condition="not null" />
        /// </contract>
        public override void HandleMessage(Message message)
        {
            base.HandleMessage(message);
            if (!(message["id"] == this.ID))
                return;
            string name = message.Name;
            if (!(name == "checklist:checkall"))
            {
                if (!(name == "checklist:uncheckall"))
                {
                    if (!(name == "checklist:invert"))
                        return;
                    this.Invert();
                }
                else
                    this.UncheckAll();
            }
            else
                this.CheckAll();
        }

        /// <summary>Gets the item value.</summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        /// <contract>
        ///   <requires name="item" condition="not null" />
        ///   <ensures condition="not null" />
        /// </contract>
        protected override string GetItemValue(ChecklistItem item)
        {
            return !(item is DataChecklistItem dataChecklistItem) ? string.Empty : LongID.ToID(dataChecklistItem.ItemID);
        }

        StringBuilder sb = new StringBuilder();
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load"></see> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs"></see> object that contains the event data.</param>
        /// <contract>
        ///   <requires name="e" condition="not null" />
        /// </contract>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                GridPanel gridPanel = new GridPanel { Columns = 4, CellPadding = "2", Fixed = false, CssClass= "scContentButtons" };

                sb.AppendFormat("<table><tr colspan=3><td>DataSource: " + Source.ToString() + "</td></tr><tr><td><a href='#' class='scContentButton' onclick=\"javascript:return scForm.postEvent(this,event,'checklist:checkall(id=" + ID + ")')\">Select all</a></td>");
                
                sb.AppendFormat("<td>{0}</td>", "   |   ");

                sb.AppendFormat("<td><a href='#' class='scContentButton' onclick=\"javascript:return scForm.postEvent(this,event,'checklist:uncheckall(id=" + ID + ")')\">Deselect all</a></td>");

                sb.AppendFormat("<td>{0}</td>", "   |   ");

                sb.AppendFormat("<td><a href='#' class='scContentButton' onclick=\"javascript:return scForm.postEvent(this,event,'checklist:invert(id=" + ID + ")')\">Invert selection</a></td>");

                sb.AppendFormat("</tr></table>");
                LiteralControl topControls = new LiteralControl(sb.ToString());
                gridPanel.Controls.Add(topControls);

                Controls.Add(gridPanel);

                this.RenderOptions();
           }
            else
                this.UpdateValue();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.PreRender"></see> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"></see> object that contains the event data.</param>
        /// <contract>
        ///   <requires name="e" condition="not null" />
        /// </contract>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            this.ServerProperties["Value"] = this.ServerProperties["Value"];
        }

        /// <summary>Sets the Modified flag.</summary>
        protected virtual void SetModified()
        {
            if (!this.TrackModified)
                return;
            Sitecore.Context.ClientPage.Modified = true;
        }

        /// <summary>Renders the options.</summary>
        private void RenderOptions()
        {
            StringList stringList = new StringList((IEnumerable<string>)this.Value.Split('|'));
            Item current = Sitecore.Context.ContentDatabase.GetItem(this.ItemID, Language.Parse(this.ItemLanguage));
            Item[] objArray = (Item[])null;
            using (new LanguageSwitcher(this.ItemLanguage))
                objArray = LookupSources.GetItems(current, this.Source);
            string str = StringUtil.GetString(this.FieldName);
            foreach (Item obj in objArray)
            {
                DataChecklistItem dataChecklistItem = new DataChecklistItem();
                this.Controls.Add((System.Web.UI.Control)dataChecklistItem);
                dataChecklistItem.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("I");
                dataChecklistItem.ItemID = obj.ID.ToString();
                if (str.StartsWith("@", StringComparison.InvariantCulture))
                    dataChecklistItem.Header = obj[str.Substring(1)];
                else if (str.Length > 0)
                    dataChecklistItem.Header = obj[this.FieldName];
                else
                    dataChecklistItem.Header = obj.DisplayName;
                int index = stringList.IndexOf(obj.ID.ToString(), StringComparison.InvariantCulture);
                if (index >= 0)
                {
                    dataChecklistItem.Checked = true;
                    stringList.RemoveAt(index);
                }
                dataChecklistItem.Disabled = this.Disabled;
            }
            foreach (string path in (List<string>)stringList)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    DataChecklistItem dataChecklistItem = new DataChecklistItem();
                    this.Controls.Add((System.Web.UI.Control)dataChecklistItem);
                    dataChecklistItem.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("I");
                    dataChecklistItem.ItemID = path;
                    Item obj = Sitecore.Context.ContentDatabase.GetItem(path);
                    if (obj != null)
                        dataChecklistItem.Header = obj.DisplayName + " " + Translate.Text("[Not in the selection List]");
                    else
                        dataChecklistItem.Header = path + " " + Translate.Text("[Item not found]");
                    dataChecklistItem.Checked = true;
                    dataChecklistItem.Disabled = this.Disabled;
                }
            }
        }

        /// <summary>Updates the value.</summary>
        private void UpdateValue()
        {
            ListString string1 = new ListString();
            foreach (System.Web.UI.Control control in this.Controls)
            {
                if (control is DataChecklistItem dataChecklistItem1 && dataChecklistItem1.Checked)
                    string1.Add(dataChecklistItem1.ItemID);
            }
            ListString string2 = new ListString(this.Value);
            bool ignoreOrdering = true;
            if (ListString.Compare(string1, string2, ignoreOrdering))
                return;
            this.SetModified();
            this.Value = string1.ToString();
        }
    }
}
