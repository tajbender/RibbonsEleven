// *********************************
// Message from Original Author:
//
// 2008 Jose Menendez Poo
// Please give me credit if you use this code. It's all I ask.
// Contact me for more info: menendezpoo@gmail.com
// *********************************
//
// Original project from http://ribbon.codeplex.com/
// Continue to support and maintain by http://officeribbon.codeplex.com/


using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms.Design;
using System.Windows.Forms.Design.Behavior;
using System.Windows.Forms.RibbonHelpers;

namespace System.Windows.Forms
{
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
    public class RibbonDesigner
      : ControlDesigner
    {
        #region Static

        internal static RibbonDesigner Current;
        internal static readonly string MessageBoxCaption = "RibbonWinForms - Designer";

        #endregion

        #region Fields

        private IRibbonElement _selectedElement;
        private Adorner _quickAccessAdorner;
//        private Adorner _orbAdorner;  // tajbender 15/08/19 Isn't used afaik
        private Adorner _tabAdorner;

        #endregion

        #region Ctor
        public RibbonDesigner()
        {
            Current = this;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (Current == this)
            {
                Current = null;
            }
        }
        //Finalize is called by base class "System.ComponentModel.Design.ComponentDesigner"

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the currently selected RibbonElement
        /// </summary>
        public IRibbonElement SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (Ribbon == null) return;
                _selectedElement = value;

                if (GetService(typeof(ISelectionService)) is ISelectionService selector && value != null)
                    selector.SetSelectedComponents(new[] { value as Component }, SelectionTypes.Primary);

                if (value is RibbonButton button)
                {
                    button.ShowDropDown();
                }

                Ribbon.Refresh();
            }
        }

        /// <summary>
        /// Gets the Ribbon of the designer
        /// </summary>
        public Ribbon Ribbon => Control as Ribbon;

        #endregion

        #region Methods

        public virtual void CreateItem(Ribbon ribbon, RibbonItemCollection collection, Type t)
        {
            if (GetService(typeof(IDesignerHost)) is IDesignerHost host && collection != null && ribbon != null)
            {
                DesignerTransaction transaction = host.CreateTransaction("AddRibbonItem_" + Component.Site.Name);

                MemberDescriptor member = TypeDescriptor.GetProperties(Component)["Items"];
                RaiseComponentChanging(member);

                RibbonItem item = host.CreateComponent(t) as RibbonItem;

                if (!(item is RibbonSeparator)) item.Text = item.Site.Name;

                collection.Add(item);
                ribbon.OnRegionsChanged();

                RaiseComponentChanged(member, null, null);
                transaction.Commit();
            }
        }

        private void CreateOrbItem(string collectionName, RibbonItemCollection collection, Type t)
        {
            if (Ribbon == null) return;

            IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
            DesignerTransaction transaction = host.CreateTransaction("AddRibbonOrbItem_" + Component.Site.Name);
            MemberDescriptor member = TypeDescriptor.GetProperties(Ribbon.OrbDropDown)[collectionName];
            RaiseComponentChanging(member);

            RibbonItem item = host.CreateComponent(t) as RibbonItem;

            if (!(item is RibbonSeparator)) item.Text = item.Site.Name;

            collection.Add(item);
            Ribbon.OrbDropDown.OnRegionsChanged();

            RaiseComponentChanged(member, null, null);
            transaction.Commit();

            Ribbon.OrbDropDown.SelectOnDesigner(item);
            Ribbon.OrbDropDown.WrappedDropDown.Size = Ribbon.OrbDropDown.Size;
        }

        /// <summary>
        /// Creates an Orb's MenuItem
        /// </summary>
        /// <param name="t"></param>
        public void CreateOrbMenuItem(Type t)
        {
            CreateOrbItem("MenuItems", Ribbon.OrbDropDown.MenuItems, t);
        }

        /// <summary>
        /// Creates an Orb's RecentItem
        /// </summary>
        /// <param name="t"></param>
        public void CreateOrbRecentItem(Type t)
        {
            CreateOrbItem("RecentItems", Ribbon.OrbDropDown.RecentItems, t);
        }

        /// <summary>
        /// Creates an Orb's OptionItem
        /// </summary>
        /// <param name="t"></param>
        public void CreateOrbOptionItem(Type t)
        {
            CreateOrbItem("OptionItems", Ribbon.OrbDropDown.OptionItems, t);
        }

        private void AssignEventHandler()
        {
            //TODO: Didn't work
            //if (SelectedElement == null) return;

            //IEventBindingService binder = GetService(typeof(IEventBindingService)) as IEventBindingService;

            //EventDescriptorCollection evts = TypeDescriptor.GetEvents(SelectedElement);

            ////string id = binder.CreateUniqueMethodName(SelectedElement as Component, evts["Click"]);

            //binder.ShowCode(SelectedElement as Component, evts["Click"]);
        }

        private void SelectRibbon()
        {
            if (this.GetService(typeof(ISelectionService)) is ISelectionService selector)
            {
                selector.SetSelectedComponents(new Component[] { this.Ribbon }, SelectionTypes.Primary);
            }
        }

        public override DesignerVerbCollection Verbs
        {
            get
            {
                DesignerVerbCollection verbs = new DesignerVerbCollection
                {
                    new DesignerVerb("Add Tab", this.AddTabVerb),
                    new DesignerVerb("Add Context", this.AddContextVerb),
                    new DesignerVerb("About RibbonWinForms", this.AboutRibbonWinFormsVerb)
                };

                return verbs;
            }
        }

        public void AddTabVerb(object sender, EventArgs e)
        {
            if (Control is Ribbon r)
            {
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;

                RibbonTab tab = host?.CreateComponent(typeof(RibbonTab)) as RibbonTab;

                if (tab == null) return;

                tab.Text = tab.Site.Name;

                Ribbon.Tabs.Add(tab);

                r.Refresh();
            }
        }

        public void AddContextVerb(object sender, EventArgs e)
        {
            if (Control is Ribbon r)
            {
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;

                RibbonContext context = host?.CreateComponent(typeof(RibbonContext)) as RibbonContext;

                if (context == null) return;

                context.Text = context.Site.Name;
                Random rnd1 = new Random();

                context.GlowColor = Color.FromArgb(rnd1.Next(155, 255), rnd1.Next(155, 255), rnd1.Next(155, 255));

                Ribbon.Contexts.Add(context);

                r.Refresh();
            }
        }

        public void AboutRibbonWinFormsVerb(object sender, EventArgs e)
        {
            if (Control is Ribbon)
            {
                MessageBox.Show("This is our about text for further copyright notices.",
                    "RibbonWinForms - Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.HWnd == Control.Handle)
            {
                switch (m.Msg)
                {
                    case WinApi.WM_LBUTTONDBLCLK:
                        AssignEventHandler();
                        break;
                    case WinApi.WM_LBUTTONDOWN:
                    case WinApi.WM_RBUTTONDOWN:
                        return;
                    case WinApi.WM_LBUTTONUP:
                    case WinApi.WM_RBUTTONUP:
                        HitOn(WinApi.LoWord((int)m.LParam), WinApi.HiWord((int)m.LParam));
                        return;
                    default:
                        break;
                }
            }

            base.WndProc(ref m);
        }

        private void HitOn(int x, int y)
        {
            if (Ribbon.Tabs.Count == 0 || Ribbon.ActiveTab == null)
            {
                SelectRibbon();
                return;
            }

            if (Ribbon != null)
            {
                if (Ribbon.TabHitTest(x, y))
                {
                    SelectedElement = Ribbon.ActiveTab;
                }
                else if (Ribbon.ContextHitTest(x, y))
                {
                    foreach (RibbonContext context in Ribbon.Contexts)
                    {
                        if (context.Bounds.Contains(x, y))
                        {
                            SelectedElement = context;
                            break;
                        }
                    }
                }
                else
                {
                    #region Tab ScrollTest

                    if (Ribbon.ActiveTab.TabContentBounds.Contains(x, y))
                    {
                        if (Ribbon.ActiveTab.ScrollLeftBounds.Contains(x, y) && Ribbon.ActiveTab.ScrollLeftVisible)
                        {
                            Ribbon.ActiveTab.ScrollLeft();
                            SelectedElement = Ribbon.ActiveTab;
                            return;
                        }

                        if (Ribbon.ActiveTab.ScrollRightBounds.Contains(x, y) && Ribbon.ActiveTab.ScrollRightVisible)
                        {
                            Ribbon.ActiveTab.ScrollRight();
                            SelectedElement = Ribbon.ActiveTab;
                            return;
                        }
                    }

                    #endregion

                    //Check Panel
                    if (Ribbon.ActiveTab.TabContentBounds.Contains(x, y))
                    {
                        RibbonPanel hittedPanel = null;

                        foreach (RibbonPanel panel in Ribbon.ActiveTab.Panels)
                            if (panel.Bounds.Contains(x, y))
                            {
                                hittedPanel = panel;
                                break;
                            }

                        if (hittedPanel != null)
                        {
                            //Check item
                            RibbonItem hittedItem = null;

                            foreach (RibbonItem item in hittedPanel.Items)
                                if (item.Bounds.Contains(x, y))
                                {
                                    hittedItem = item;
                                    break;
                                }

                            if (hittedItem != null && hittedItem is IContainsSelectableRibbonItems)
                            {
                                //Check subitem
                                RibbonItem hittedSubItem = null;

                                foreach (RibbonItem subItem in (hittedItem as IContainsSelectableRibbonItems).GetItems())
                                    if (subItem.Bounds.Contains(x, y))
                                    {
                                        hittedSubItem = subItem;
                                        break;
                                    }

                                if (hittedSubItem != null)
                                {
                                    SelectedElement = hittedSubItem;
                                }
                                else
                                {
                                    SelectedElement = hittedItem;
                                }
                            }
                            else if (hittedItem != null)
                            {
                                SelectedElement = hittedItem;
                            }
                            else
                            {
                                SelectedElement = hittedPanel;
                            }
                        }
                        else
                        {
                            SelectedElement = Ribbon.ActiveTab;
                        }
                    }
                    else if (Ribbon.QuickAccessToolbar.SuperBounds.Contains(x, y))
                    {
                        bool itemHitted = false;

                        foreach (RibbonItem item in Ribbon.QuickAccessToolbar.Items)
                        {
                            if (item.Bounds.Contains(x, y))
                            {
                                itemHitted = true;
                                SelectedElement = item;
                                break;
                            }
                        }
                        if (!itemHitted)
                            SelectedElement = Ribbon.QuickAccessToolbar;
                    }
                    else if (Ribbon.OrbBounds.Contains(x, y))
                    {
                        Ribbon.OrbMouseDown();
                    }
                    else
                    {
                        SelectRibbon();

                        Ribbon.ForceOrbMenu = false;
                        if (Ribbon.OrbDropDown.Visible) Ribbon.OrbDropDown.Close();
                    }
                }
            }
        }

        protected override void OnPaintAdornments(PaintEventArgs pe)
        {
            base.OnPaintAdornments(pe);

            using (Pen p = new Pen(Color.Black))
            {
                p.DashStyle = DashStyle.Dot;

                if (GetService(typeof(ISelectionService)) is ISelectionService host)
                {
                    foreach (IComponent comp in host.GetSelectedComponents())
                    {
                        if (comp is RibbonContext)
                        {
                            if (comp is RibbonContext item)
                            {
                                Rectangle selection = (item.ContextualTabsCount > 0) ? item.HeaderBounds : item.Bounds;
                                selection.Inflate(-1, -1);

                                pe.Graphics.DrawRectangle(p, selection);
                            }
                        }
                        else if (comp is RibbonTab)
                        {
                            if (comp is RibbonTab item)
                            {

                                Rectangle selection = item.Bounds;
                                selection.Inflate(-1, -1);

                                pe.Graphics.DrawRectangle(p, selection);
                            }
                        }
                        else if (comp is RibbonPanel)
                        {
                            if (comp is RibbonPanel item)
                            {
                                Rectangle selection = item.Bounds;
                                selection.Inflate(-1, -1);

                                pe.Graphics.DrawRectangle(p, selection);
                            }
                        }
                        else if (comp is RibbonItem)
                        {
                            if (comp is RibbonItem item && !Ribbon.OrbDropDown.AllItems.Contains(item))
                            {
                                Rectangle selection = item.Bounds;
                                selection.Inflate(1, 1);

                                pe.Graphics.DrawRectangle(p, selection);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Site

        public BehaviorService GetBehaviorService()
        {
            return BehaviorService;
        }

        public override void Initialize(IComponent component)
        {
            base.Initialize(component);

            // 15/08/19: tajbender: desigerEvt isn't in use, afaik
            //IDesignerEventService desigerEvt = this.GetService(typeof(IDesignerEventService)) as IDesignerEventService;

            if (this.GetService(typeof(IComponentChangeService)) is IComponentChangeService changeService)
            {
                changeService.ComponentRemoved += this.ChangeService_ComponentRemoved;
            }

            // this._orbAdorner = new Adorner(); // 15/08/19 tajbender Isn't used, afaik
            this._tabAdorner = new Adorner();

            if (BehaviorService != null)
            {
                //BehaviorService.Adorners.AddRange(new[] { _orbAdorner, _tabAdorner });    // 15/08/19 tajbender _orbAdorner: Isn't used, afaik
                BehaviorService.Adorners.Add(this._tabAdorner);

                if (Ribbon.QuickAccessToolbar.Visible)
                {
                    _quickAccessAdorner = new Adorner();
                    BehaviorService.Adorners.Add(_quickAccessAdorner);
                    _quickAccessAdorner.Glyphs.Add(new RibbonQuickAccessToolbarGlyph(BehaviorService, this, Ribbon));
                }
                else
                {
                    _quickAccessAdorner = null;
                }

                //orbAdorner.Glyphs.Add(new RibbonOrbAdornerGlyph(BehaviorService, this, Ribbon));
                _tabAdorner.Glyphs.Add(new RibbonTabGlyph(BehaviorService, this, Ribbon));
            }
        }

        /// <summary>
        /// Catches the event of a component on the ribbon being removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ChangeService_ComponentRemoved(object sender, ComponentEventArgs e)
        {
            IDesignerHost designerHost = GetService(typeof(IDesignerHost)) as IDesignerHost;

            RemoveRecursive(e.Component as IContainsRibbonComponents, designerHost);

            if (e.Component is RibbonTab tab && Ribbon != null)
            {
                Ribbon.Tabs.Remove(tab);                                    // Remove RibbonTab
            }
            else if (e.Component is RibbonContext context)
            {
                Ribbon.Contexts.Remove(context);                            // Remove RibbonContext
            }
            else if (e.Component is RibbonPanel panel)
            {
                panel.OwnerTab.Panels.Remove(panel);                        // Remove RibbonPanel
            }
            else if (e.Component is RibbonItem item)
            {
                if (item.Canvas is RibbonOrbDropDown)
                {
                    Ribbon.OrbDropDown.HandleDesignerItemRemoved(item);     // Remove RibbonItem from RibbonOrbDropDown
                }
                else if (item.OwnerItem is RibbonItemGroup group)
                {
                    group.Items.Remove(item);                               // Remove RibbonItem from RibbonItemGroup
                }
                else if (item.OwnerPanel != null)
                {
                    item.OwnerPanel.Items.Remove(item);                     // Remove RibbonItem from RibbonPanel
                }
                else if (Ribbon != null && Ribbon.QuickAccessToolbar.Items.Contains(item))
                {
                    Ribbon.QuickAccessToolbar.Items.Remove(item);           // Remove RibbonItem from RibbonQuickAccessToolbar
                }
            }

            SelectedElement = null;

            Ribbon?.OnRegionsChanged();
        }

        public void RemoveRecursive(IContainsRibbonComponents item, IDesignerHost service)
        {
            if (item == null || service == null) return;

            foreach (Component c in item.GetAllChildComponents())
            {
                if (c is IContainsRibbonComponents components)
                {
                    RemoveRecursive(components, service);
                }
                service.DestroyComponent(c);
            }
        }

        #endregion
    }
}