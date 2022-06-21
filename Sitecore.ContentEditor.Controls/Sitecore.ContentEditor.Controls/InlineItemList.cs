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

namespace Foundation.ContentEditor.Controls
{
	/// <summary>
	/// This field behaves like a Multilist, except that the source of the field is dynamic and 
	/// allows authors to add items to the source without "leaving" the item they're currently editing.
	///
	/// Dynamically added items are based on a pre-defined template and available in the "left" source Listbox
	/// of the Multilist controls. Authors can also edit or delete any item in the Listboxes.
	/// </summary>
	public class InlineItemList : Sitecore.Web.UI.HtmlControls.Control, IContentField
	{
		//fields
		private Listbox _listboxSource;
		private Listbox _listboxSelected;
		private const string SelectedListboxIDSuffix = "_selected";
		private const string SourceListboxIDSuffix = "_source";

		public InlineItemList()
		{
			base.Class = "scContentControl";
			base.Background = "white";
			Activation = true;
		}

		#region Properties

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

		/// <summary>
		/// The Source property is set by the Content Editor via reflection
		/// </summary>
		public string Source { get; set; }

		/// <summary>
		/// The ItemID proprety is set by the Content Editor via reflection
		/// </summary> 
		public string ItemID { get; set; }

		/// <summary>
		/// The FieldID property is set by the Content Editor via reflection
		/// </summary>
		public string FieldID { get; set; }

		/// <summary>
		/// The ItemVersion property is set by the Content Editor via reflection
		/// </summary>
		public string ItemVersion { get; set; }

		/// <summary>
		/// The ItemLanguage property is set by the Content Editor via reflection
		/// </summary>
		public string ItemLanguage { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// standard web control OnLoad method 
		/// </summary>
		/// <param name="args"></param>
		protected override void OnLoad(EventArgs args)
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

				SetViewStateString("ID", ID);

				//<Row>
				//<Column 1-4>
				StringBuilder sb = new StringBuilder();
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

				//spacer
				sb.AppendFormat("<td>{0}</td>", Images.GetSpacer(16, 1));

				//SelectAll button
				string clientEventSelectAll = Sitecore.Context.ClientPage.GetClientEvent(ID + ".SelectAll");
				sb.AppendFormat("<td><a href='#' class='scContentButton' onclick=\"{0}\">Select all</a></td>", clientEventSelectAll);
				ImageBuilder imageSelectAll = new ImageBuilder { Src = "Applications/16x16/checkbox.png", ID = ID + "_selectall", OnClick = clientEventSelectAll };
				sb.AppendFormat("<td>{0}</td>", imageSelectAll);

				//spacer
				sb.AppendFormat("<td>{0}</td>", Images.GetSpacer(16, 1));

				//UnSelectAll button
				string clientEventDeselectAll = Sitecore.Context.ClientPage.GetClientEvent(ID + ".DeselectAll");
				sb.AppendFormat("<td><a href='#' class='scContentButton' onclick=\"{0}\">Deselect all</a></td>", clientEventDeselectAll);
				ImageBuilder imageDeselectAll = new ImageBuilder { Src = "Applications/16x16/selection.png", ID = ID + "_deselectall", OnClick = clientEventDeselectAll };
				sb.AppendFormat("<td>{0}</td>", imageDeselectAll);

				sb.Append("</tr>");
				sb.Append("</table>");

				LiteralControl topControls = new LiteralControl(sb.ToString());
				gridPanel.Controls.Add(topControls);
				gridPanel.SetExtensibleProperty(topControls, "ColSpan", "4");
				//</Column 1-4>

				//</Row>

				//<Row>
				//<Column 1>
				Literal literal = new Literal("All");
				literal.Class = "scContentControlMultilistCaption";
				gridPanel.Controls.Add(literal);
				gridPanel.SetExtensibleProperty(literal, "Width", "50%");
				//</Column 1>

				//<Column 2>
				literal = new Literal(Images.GetSpacer(30, 1));
				gridPanel.Controls.Add(literal);
				gridPanel.SetExtensibleProperty(literal, "Width", "30");
				//</Column 2>

				//<Column 3>
				literal = new Literal("Selected");
				literal.Class = "scContentControlMultilistCaption";
				gridPanel.Controls.Add(literal);
				gridPanel.SetExtensibleProperty(literal, "Width", "50%");
				//</Column 3>

				//<Column 4>
				literal = new Literal(Images.GetSpacer(30, 1));
				gridPanel.Controls.Add(literal);
				gridPanel.SetExtensibleProperty(literal, "Width", "30");
				//</Column 4>
				//</Row>

				//<Row>
				//<Column 1>

				Listbox listbox1 = new Listbox
				{
					ID = ID + SourceListboxIDSuffix,
					DblClick = ID + ".Right",
					Size = "10",
					Change = ID + ".Click(\\\"_source\\\")" //parameters to methods must be in the form ("paramValue"), but need to escape the quotes for the JS call to work
				};

				listbox1.Attributes["class"] = "scContentControlMultilistbox";
				listbox1.Multiple = true;
				_listboxSource = listbox1;
				gridPanel.Controls.Add(listbox1);

				//</Column 1>

				//<Column 2>
				ImageBuilder builderRight = new ImageBuilder
				{
					Src = "Applications/16x16/nav_right_blue.png",
					ID = ID + "_right",
					Margin = "2",
					OnClick = Sitecore.Context.ClientPage.GetClientEvent(ID + ".Right")
				};
				ImageBuilder builderLeft = new ImageBuilder
				{
					Src = "Applications/16x16/nav_left_blue.png",
					ID = ID + "_left",
					Margin = "2",
					OnClick = Sitecore.Context.ClientPage.GetClientEvent(ID + ".Left")
				};

				LiteralControl middleControls = new LiteralControl(builderRight + "<br />" + builderLeft);
				gridPanel.Controls.Add(middleControls);
				gridPanel.SetExtensibleProperty(middleControls, "Align", "center");
				gridPanel.SetExtensibleProperty(middleControls, "VAlign", "top");
				//</Column 2>

				//<Column 3>
				//create a Listbox control, style it and add it to the GridPanel column
				Listbox listbox2 = new Listbox
				{
					ID = ID + SelectedListboxIDSuffix,
					DblClick = ID + ".Left",
					Size = "10",
					Change = ID + ".Click(\\\"_selected\\\")" //parameters to methods must be in the form ("paramValue"), but need to escape the quotes for the JS call to work
				};

				listbox2.Attributes["class"] = "scContentControlMultilistbox";
				listbox2.Multiple = true;
				_listboxSelected = listbox2;
				gridPanel.Controls.Add(listbox2);
				//</Column 3>

				//<Column 4>
				//create the action buttons that appear to the right of the Listbox
				//these action buttons specify events to be used for each action
				ImageBuilder builderUp = new ImageBuilder
				{
					Src = "Applications/16x16/nav_up_blue.png",
					ID = ID + "_up",
					Margin = "2",
					OnClick = Sitecore.Context.ClientPage.GetClientEvent(ID + ".Up")
				};

				ImageBuilder builderDown = new ImageBuilder
				{
					Src = "Applications/16x16/nav_down_blue.png",
					ID = ID + "_down",
					Margin = "2",
					OnClick = Sitecore.Context.ClientPage.GetClientEvent(ID + ".Down")
				};

				//combine the image button html into one literal and add it to the GridPanel column
				LiteralControl rightControls = new LiteralControl(builderUp + "<br/>" + builderDown);
				gridPanel.Controls.Add(rightControls);
				gridPanel.SetExtensibleProperty(rightControls, "Align", "center");
				gridPanel.SetExtensibleProperty(rightControls, "VAlign", "top");
				//<Column 4>
				//</Row>

				if (!string.IsNullOrWhiteSpace(DataSourcePath))
				{
					LiteralControl sourceControl = new LiteralControl("DataSource: " + DataSourcePath);
					gridPanel.Controls.Add(sourceControl);
				}

				LiteralControl bottomSpacer = new LiteralControl(Images.GetSpacer(16, 16));
				gridPanel.Controls.Add(bottomSpacer);
				gridPanel.SetExtensibleProperty(bottomSpacer, "ColSpan", "4");

				//add the GridPanel to the base control collection
				Controls.Add(gridPanel);

				//restore the value of the control if any value exists
				RestoreState();
			}

			base.OnLoad(args);
		}

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
				return GetUniqueDataSourcePath(item);

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
		/// IContentField method
		/// </summary>
		/// <returns></returns>
		public string GetValue()
		{			
			string fieldId = GetViewStateString("ID");
			Listbox lb = FindControl(fieldId + SelectedListboxIDSuffix) as Listbox;
			if (lb == null)
				return string.Empty;

			//get the array of items in the Listbox
			ListItem[] items = lb.Items;
			if (items.Length <= 0)
				return string.Empty;

			ListString ls = new ListString('|');
			foreach (ListItem li in items)
			{
				ls.Add(li.Value);
			}

			return ls.ToString();
		}

		/// <summary>
		/// IContentField method
		/// </summary>
		/// <param name="value"></param>
		public void SetValue(string value)
		{
			Value = value;
		}

		/// <summary>
		/// Refreshes the Listboxes used in the field
		/// </summary>
		private void Refresh()
		{
			if (_listboxSelected == null || _listboxSource == null)
				return;

			Sitecore.Context.ClientPage.ClientResponse.Refresh(_listboxSelected);
			Sitecore.Context.ClientPage.ClientResponse.Refresh(_listboxSource);
		}

		/// <summary>
		/// This method populates the Listbox controls with their initial values on load
		/// </summary>
		private void RestoreState()
		{
			string controlValue = Value;
			
			if (!string.IsNullOrEmpty(controlValue))
			{
				ListString ls = new ListString(controlValue);
				foreach (string s in ls)
				{
					Item item = Sitecore.Context.ContentDatabase.GetItem(s);
					if (item == null)
						continue;

					ListItem listItem = new ListItem { ID = GetUniqueID("I"), Value = s, Header = item.DisplayName };
					_listboxSelected.Controls.Add(listItem);
				}	
			}

			Item sourceRoot = Sitecore.Context.ContentDatabase.GetItem(DataSourcePath);
			if (sourceRoot != null)
			{
				ChildList children = sourceRoot.GetChildren();
				foreach (Item child in children)
				{
					if (!string.IsNullOrEmpty(controlValue) && controlValue.Contains(child.ID.ToString()))
						continue;

					ListItem li = new ListItem { ID = GetUniqueID("I"), Header = child.DisplayName, Value = child.ID.ToString() };
					_listboxSource.Controls.Add(li);
				}
			}

			Refresh();
		}

		/// <summary>
		/// Set the page as modified. Used by the Content Editor to prompt users to 'Save' before navigating away from an item.
		/// </summary>
		protected void SetModified()
		{
			Sitecore.Context.ClientPage.Modified = true;
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
					Listbox listbox = FindControl(fieldId + SourceListboxIDSuffix) as Listbox;
					if (listbox == null)
						return;

					//de-select any selected items
					Sitecore.Context.ClientPage.ClientResponse.Eval("scForm.browser.getControl('" + listbox.ID + "').selectedIndex=-1");

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
		/// This event responds to the client 'Edit Item' button. It basically just kicks off the EditItem pipeline/method.
		/// </summary>
		protected void Edit()
		{
			if (Disabled) 
				return;

			//get the currently selected item from either listbox
			ListItem selectedItem = GetSelectedItem();
			if (selectedItem == null || string.IsNullOrEmpty(selectedItem.Value))
				return;
			
			//run the EditItem pipeline/method, passing in the selected item's ID
			NameValueCollection nvc = new NameValueCollection();
			nvc.Add("itemid", selectedItem.Value);
			Sitecore.Context.ClientPage.Start(this, "EditItem", nvc);
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
		/// This event responds to the client 'Remove Item' button and kicks off the RemoveItem pipeline/method 
		/// </summary>
		protected void Remove()
		{
			if (Disabled)
				return;
			
			//run the RemoveItem pipeline/method
			Sitecore.Context.ClientPage.Start(this, "RemoveItem");
		}

		/// <summary>
		/// This event responds to the client 'SelectAll' button 
		/// </summary>
		protected void SelectAll()
		{
			if (Disabled)
				return;

			string fieldId = GetViewStateString("ID");
			Listbox listbox1 = FindControl(fieldId + SourceListboxIDSuffix) as Listbox;
			Listbox listbox2 = FindControl(fieldId + SelectedListboxIDSuffix) as Listbox;

			Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox2);

			foreach (ListItem item in listbox1.Items)
			{
				listbox2.Controls.Add(item);
			}

			Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox1);
			Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox2);
		}

		protected void DeSelectAll()
		{
			if (Disabled)
				return;

			string fieldId = GetViewStateString("ID");
			Listbox listbox1 = FindControl(fieldId + SourceListboxIDSuffix) as Listbox;
			Listbox listbox2 = FindControl(fieldId + SelectedListboxIDSuffix) as Listbox;

			Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox1);

			foreach (ListItem item in listbox2.Items)
			{
				listbox1.Controls.Add(item);
			}

			Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox1);
			Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox2);
		}


		/// <summary>
		/// This method removes the currently selected item from it's respective Listbox and 
		/// also moves the item to the Recycle Bin (or deletes the item if the Recycle Bin is not active)
		/// </summary>
		/// <param name="args"></param>
		protected void RemoveItem(ClientPipelineArgs args)
		{
			ListItem selectedItem = GetSelectedItem();
			if (selectedItem == null)
				return;
			
			Item item = Sitecore.Context.ContentDatabase.GetItem(selectedItem.Value);
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
					Listbox listbox = selectedItem.Parent as Listbox;
					if (listbox == null)
						return;

					item.Recycle();

					listbox.Controls.Remove(selectedItem);
					Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox);
					SetModified();
				}
			}
		}

		/// <summary>
		/// In order for the Edit command to work properly, the user can only have one item selected at a item
		/// So when one Listbox is clicked, be sure to un-select any items in the other Listbox
		/// </summary>
		/// <param name="sender"></param>
		protected void Click(string sender)
		{
			if (Disabled)
				return;

			string id = GetViewStateString("ID");
			Listbox listbox1 = FindControl(id + sender) as Listbox;
			if (listbox1 == null)
				return;

			string listbox2Name = sender == SourceListboxIDSuffix ? SelectedListboxIDSuffix : SourceListboxIDSuffix;
			Listbox listbox2 = FindControl(id + listbox2Name) as Listbox;
			if (listbox2 == null)
				return;
			
			ListItem[] items = listbox2.Items;
			listbox2.Controls.Clear();
			Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox2);

			foreach (ListItem item in items)
			{
				item.Selected = false;
				listbox2.Controls.Add(item);
			}

			Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox1);
			Sitecore.Context.ClientPage.ClientResponse.Refresh(listbox2);
		}

		/// <summary>
		/// Responds to the client Down command. If the currently selected item is in the 'Selected' Listbox, then move it down.
		/// </summary>
		protected void Down()
		{
			if (Disabled) 
				return;

			ListItem selectedItem = GetSelectedItem();
			if (selectedItem == null || _listboxSelected == null)
				return;

			int index = _listboxSelected.Controls.IndexOf(selectedItem);
			if (index == -1)
				return;

			if (index == _listboxSelected.Controls.Count - 1)
			{
				_listboxSelected.Controls.Remove(selectedItem);
				_listboxSelected.Controls.AddAt(0, selectedItem);
			}
			else
			{
				_listboxSelected.Controls.Remove(selectedItem);
				_listboxSelected.Controls.AddAt(index + 1, selectedItem);
			}

			Sitecore.Context.ClientPage.ClientResponse.Refresh(_listboxSelected);
			SetModified();
		}

		/// <summary>
		/// Responds to the client Up command. If the currently selected item is in the 'Selected' Listbox, then move it up.
		/// </summary>
		protected void Up()
		{
			if (Disabled)
				return;

			ListItem selectedItem = GetSelectedItem();
			if (selectedItem == null || _listboxSelected == null)
				return;

			int index = _listboxSelected.Controls.IndexOf(selectedItem);
			if (index == -1)
				return;

			_listboxSelected.Controls.Remove(selectedItem);
			if (index == 0)
			{
				_listboxSelected.Controls.Add(selectedItem);
			}
			else
			{
				_listboxSelected.Controls.AddAt(index - 1, selectedItem);	
			}

			Sitecore.Context.ClientPage.ClientResponse.Refresh(_listboxSelected);

			SetModified();
		}

		/// <summary>
		/// Responds to the client 'Left' command.
		/// If the currently selected item is in the 'Selected' Listbox, then it is moved to 'Source' Listbox.
		/// </summary>
		protected void Left()
		{
			if (Disabled)
				return;

			ListItem selectedItem = GetSelectedItem();
			if (selectedItem == null)
				return;

			if (_listboxSelected == null || _listboxSource == null)
				return;

			_listboxSelected.Controls.Remove(selectedItem);
			_listboxSource.Controls.Add(selectedItem);

			Sitecore.Context.ClientPage.ClientResponse.Refresh(_listboxSource);
			Sitecore.Context.ClientPage.ClientResponse.Refresh(_listboxSelected);

			SetModified();
		}

		/// <summary>
		/// Responds to the client 'Right' command.
		/// If the currently selected item is in the 'Right' Listbox, then it is moved to 'Selected' Listbox.
		/// </summary>
		protected void Right()
		{
			if (Disabled)
				return;

			ListItem selectedItem = GetSelectedItem();
			if (selectedItem == null)
				return;

			if (_listboxSelected == null || _listboxSource == null)
				return;

			_listboxSource.Controls.Remove(selectedItem);
			_listboxSelected.Controls.Add(selectedItem);

			Sitecore.Context.ClientPage.ClientResponse.Refresh(_listboxSource);
			Sitecore.Context.ClientPage.ClientResponse.Refresh(_listboxSelected);

			SetModified();
		}

		/// <summary>
		/// This method gets both of the Listbox controls contained in the field and 
		/// returns the first selected item found. There should be at most one item.
		/// </summary>
		/// <returns></returns>
		private ListItem GetSelectedItem()
		{
			string fieldId = GetViewStateString("ID");
			Listbox listbox1 = FindControl(fieldId + SourceListboxIDSuffix) as Listbox;
			Listbox listbox2 = FindControl(fieldId + SelectedListboxIDSuffix) as Listbox;
			if (listbox1 == null || listbox2 == null)
				return null;

			_listboxSource = listbox1;
			_listboxSelected = listbox2;

			//for some reason the Listbox.Selected and Listbox.SelectedItems properties ALWAYS return an item
			//even if no items are selected. obviously not the behavior needed here...
			ListItem selectedItem = listbox1.Items.FirstOrDefault(i => i.Selected);
			if (selectedItem == null || string.IsNullOrEmpty(selectedItem.Value))
			{
				selectedItem = listbox2.Items.FirstOrDefault(i => i.Selected);
			}

			if (selectedItem == null || string.IsNullOrEmpty(selectedItem.Value))
				return null;

			return selectedItem;
		}

		#endregion
	}
}