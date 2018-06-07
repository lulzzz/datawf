﻿using DataWF.Common;
using DataWF.Gui;
using DataWF.Module.Flow;

namespace DataWF.Module.FlowGui
{
    public class DocumentTypedView : DocumentListView
    {
        public DocumentTypedView()
        {
            DockType = Gui.DockType.Top;
            toolCreateFrom.Visible = false;
            FilterView.Box.Map["Document Type"].Visible = false;
            Filter.IsWork = CheckedState.Checked;
            FilterVisible = false;
            HideOnClose = true;
        }

        public override Template FilterTemplate
        {
            get => base.FilterTemplate;
            set { base.FilterTemplate = value; }
        }

        public override void Localize()
        {
            base.Localize();
            if (FilterTemplate != null)
            {
                Text = FilterTemplate.ToString();
            }
        }

        public override void ShowDocument(Document document)
        {
            base.ShowDocument(document);
            this.GetParent<DockBox>()?.ClosePage(this);
        }

        public override void Serialize(ISerializeWriter writer)
        { }

        public override void Deserialize(ISerializeReader reader)
        { }
    }
}
