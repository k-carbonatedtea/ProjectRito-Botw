﻿using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.ViewModels;

namespace GLFrameworkEngine
{
    //For rendering objects in the tree
    public class EditableObjectNode : NodeBase
    {
        private string header = $"Object";
        public override string Header
        {
            get { return header; }
            set { header = value; }
        }

        public List<string> Icons = new List<string>();
        public EditableObject Object;

        public EventHandler UIProperyDrawer;

        public override bool IsChecked
        {
            get => Object.IsVisible;
            set
            {
                if (Object.IsVisible != value)
                {
                    Object.IsVisible = value;
                    this.OnChecked?.Invoke(value, EventArgs.Empty);
                }
            }
        }

        public override bool IsSelected
        {
            get => Object.IsSelected;
            set
            {
                if (Object.IsSelected != value)
                {
                    Object.IsSelected = value;
                    GLContext.ActiveContext.Scene.OnSelectionChanged(GLContext.ActiveContext);
                }
            }
        }

        public EditableObjectNode(EditableObject obj)
        {
            Object = obj;
            HasCheckBox = true;
        }

        public override void OnDoubleClicked()
        {
            //Focus the camera on the double clicked object attached to the node
            var context = GLContext.ActiveContext;
            context.Camera.FocusOnObject(Object.Transform);
        }
    }
}
