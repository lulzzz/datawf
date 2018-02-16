﻿using DataWF.Common;
using System;
using System.Runtime.InteropServices;
using Xwt;
using Xwt.Drawing;
using System.Timers;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataWF.Gui
{
    public class ToolWindow : Window
    {
        protected Timer timerHide = new Timer();
        protected Timer timerStart = new Timer();
        protected ToolShowMode mode = ToolShowMode.Default;
        protected Widget senderWidget;
        protected WindowFrame senderWindow;
        protected Widget target;
        protected Label toolLabel;
        protected Button toolAccept;
        protected Button toolClose;
        protected VBox vbox;
        protected HBox hbox;
        protected ScrollView panel;
        private Point movePoint;
        private LayoutAlignType sizeAlign = LayoutAlignType.None;
        private bool byDeactivate;
        private bool closeOnAccept = true;
        private bool closeOnClose = true;
        private Widget tempSender;
        private Point tempLocation;
        private PointerButton moveButton;
        private Point moveBounds;

        public ToolWindow()// : base(PopupType.Menu)
        {
            var p = 6;

            panel = new ScrollView
            {
                Name = "panel",
                BackgroundColor = Colors.White
            };

            toolLabel = new Label
            {
                Name = "toolLabel",
                Text = "Label",
                Cursor = CursorType.Arrow
            };
            toolLabel.ButtonPressed += OnContentMouseDown;
            toolLabel.ButtonReleased += OnContentMouseUp;
            toolLabel.MouseEntered += OnContentMouseEntered;
            toolLabel.MouseExited += OnContentMouseExited;
            toolLabel.MouseMoved += OnContentMouseMove;

            toolClose = new Button()
            {
                Name = "toolClose",
                Label = "Close",
                WidthRequest = 80
            };
            toolClose.Clicked += OnCloseClick;

            toolAccept = new Button()
            {
                Name = "toolAccept",
                Label = "Ok",
                WidthRequest = 60
            };
            toolAccept.Clicked += OnAcceptClick;

            hbox = new HBox();
            hbox.PackStart(toolLabel, true, true);
            hbox.PackStart(toolClose, false);
            hbox.PackStart(toolAccept, false);
            //hbox.Margin = new WidgetSpacing(padding, 0, padding, padding);

            vbox = new VBox
            {
                //Margin = new WidgetSpacing(padding, padding, padding, padding),
                Name = "tools"
            };
            vbox.PackStart(panel, true, true);
            vbox.PackStart(hbox, false, false);
            vbox.KeyPressed += OnContentKeyPress;

            BackgroundColor = Colors.LightSlateGray;

            Content = vbox;
            Decorated = false;
            Name = "ToolWindow";
            Resizable = true;
            Size = new Size(360, 320);
            Padding = new WidgetSpacing(p, p, p, p);

            timerHide.Interval = 2000;
            timerHide.Elapsed += TimerHideTick;

            timerStart.Interval = 500;
            timerStart.Elapsed += TimerStartTick;
        }

        public bool HeaderVisible
        {
            get { return hbox.Visible; }
            set
            {
                if (hbox.Visible != value)
                {
                    hbox.Visible = value;
                }
            }
        }

        public ToolShowMode Mode
        {
            get { return mode; }
            set { mode = value; }
        }

        public ToolWindow OwnerToolForm
        {
            get { return senderWindow as ToolWindow; }
        }

        public Timer StartTimer
        {
            get { return timerStart; }
        }

        public double TimerInterval
        {
            get { return timerHide.Interval; }
            set { timerHide.Interval = value; }
        }

        public virtual Widget Target
        {
            get { return target; }
            set
            {
                if (target == value)
                    return;
                target = value;
                panel.Content = target;
                var size = target.Surface.GetPreferredSize();
                if (size.Width > Size.Width || size.Height > Size.Height)
                    Size = new Size(Math.Max(size.Width, Size.Width) + 35,
                                   Math.Max(size.Height, Size.Width) + 70);
            }
        }

        public Command DResult { get; set; }

        public Label Label
        {
            get { return toolLabel; }
        }

        public Button ButtonAccept
        {
            get { return toolAccept; }
        }

        public string ButtonAcceptText
        {
            get { return toolAccept.Label; }
            set { toolAccept.Label = value; }
        }

        public bool ButtonAcceptEnabled
        {
            get { return toolAccept.Sensitive; }
            set { toolAccept.Sensitive = value; }
        }

        public event EventHandler ButtonAcceptClick
        {
            add { toolAccept.Clicked += value; }
            remove { toolAccept.Clicked -= value; }
        }

        public Button ButtonClose
        {
            get { return toolClose; }
        }

        public string ButtonCloseText
        {
            get { return toolClose.Label; }
            set { toolClose.Label = value; }
        }

        public event EventHandler ButtonCloseClick
        {
            add { toolClose.Clicked += value; }
            remove { toolClose.Clicked -= value; }
        }

        public Widget Sender
        {
            get { return senderWidget; }
            set
            {
                byDeactivate = false;
                senderWidget = value;
                Owner = senderWidget?.ParentWindow as WindowFrame;
            }
        }

        public WindowFrame Owner
        {
            get { return senderWindow; }
            set
            {
                senderWindow = value;
                if (value is WindowFrame)
                    TransientFor = (WindowFrame)value;
            }
        }

        protected void OnSenderClick(object sender, ButtonEventArgs e)
        {
            Hide();
        }

        protected void OnContentMouseDown(object sender, ButtonEventArgs e)
        {
            if (e.Button == PointerButton.Left)
            {
                movePoint = Desktop.MouseLocation;
                moveButton = e.Button;
            }
        }

        protected void OnContentMouseUp(object sender, ButtonEventArgs e)
        {
            moveButton = 0;
            moveBounds = Location;
        }

        private void OnContentMouseExited(object sender, EventArgs e)
        {
            if (Mode == ToolShowMode.ToolTip)
            {
                Debug.WriteLine($"Handle Mouse Exited!");
                timerHide.Start();
            }
        }

        private void OnContentMouseEntered(object sender, EventArgs e)
        {
            if (Mode == ToolShowMode.ToolTip)
            {
                Debug.WriteLine($"Handle Mouse Entered!");
                timerHide.Stop();
            }
        }

        protected void OnContentMouseMove(object sender, MouseMovedEventArgs e)
        {
            if (moveButton == PointerButton.Left)
            {
                var location = Desktop.MouseLocation;
                var diff = new Point(location.X - movePoint.X, location.Y - movePoint.Y);
                Debug.WriteLine($"Location Diff:{diff} Bound:{moveBounds}");

                if (Label.Cursor == CursorType.Move)
                {
                    Location = new Point(moveBounds.X + diff.X, moveBounds.Y + diff.Y);
                }

            }
        }

        public virtual void Show(Widget sender, Point location)
        {
            if (mode == ToolShowMode.ToolTip)
            {
                tempSender = sender;
                tempLocation = location;

                if (!timerStart.Enabled)
                {
                    timerStart.Start();
                    return;
                }

            }
            Sender = sender;

            CheckLocation(sender?.ConvertToScreenCoordinates(location) ?? location);

            //if (Owner != null) Owner.Show();
            base.Show();//Position.Bottom, sender, ScreenBounds);

            if (mode == ToolShowMode.AutoHide || mode == ToolShowMode.ToolTip)
            {
                if (timerHide.Enabled)
                    timerHide.Stop();
                timerHide.Start();
            }

            byDeactivate = false;
        }

        public void ShowCancel()
        {
            timerStart.Stop();
            Hide();
        }

        private void CheckLocation(Point location)
        {
            Rectangle screen = Desktop.PrimaryScreen.VisibleBounds;
            if (location.Y + Height > screen.Height)
            {
                location.Y -= (location.Y + Height) - screen.Height;
                //Left += 10;
            }
            if (location.X + Width > screen.Right)
            {
                location.X -= (location.X + Width) - screen.Right;
            }
            Location = moveBounds = location;
            //moveBounds.Size = Size = Content.Surface.GetPreferredSize();
        }

        public IEnumerable<ToolWindow> GetOwners()
        {
            var window = this;
            while (window.OwnerToolForm != null)
            {
                yield return window.OwnerToolForm;
                window = window.OwnerToolForm;
            }
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            Visible = false;
            moveButton = 0;
            var temp = Owner;
            Owner = null;

            if (temp != null)
            {
                if (temp is ToolWindow)
                {
                    if (!byDeactivate)
                        ((ToolWindow)temp).byDeactivate = true;
                }
                //owner.Show();
            }

            Hide();

            if (timerHide.Enabled)
                timerHide.Stop();
            byDeactivate = false;
        }

        protected void OnContentKeyPress(object sender, KeyEventArgs e)
        {
            //prevent alt from closing it and allow alt+menumonic to work
            if (Keyboard.CurrentModifiers == ModifierKeys.Alt)
                e.Handled = true;
            if (e.Key == Key.Escape)
                Hide();
        }

        private void TimerHideTick(object sender, EventArgs e)
        {
            Application.Invoke(() =>
            {
                Hide();
                timerHide.Stop();
            });
        }

        private void TimerStartTick(object sender, EventArgs e)
        {
            Application.Invoke(() =>
            {
                Show(tempSender, tempLocation);
                timerStart.Stop();
            });
        }

        protected virtual void OnCloseClick(object sender, EventArgs e)
        {
            if (CloseOnClose)
                Hide();
            DResult = Command.Cancel;
        }

        protected virtual void OnAcceptClick(object sender, EventArgs e)
        {
            if (CloseOnAccept)
                Hide();
            DResult = Command.Ok;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Mode == ToolShowMode.ToolTip)
                    return;
                //if (target != null)
                //    target.Dispose();
                if (timerHide != null)
                    timerHide.Dispose();
                if (timerStart != null)
                    timerStart.Dispose();
            }
            base.Dispose(disposing);
        }


        public void AddButton(string text, EventHandler click)
        {
            var button = new Button();
            button.Label = text;
            button.Clicked += click;
            hbox.PackStart(button, false, false);
        }

        public static ToolWindow InitEditor(string label, object obj, bool dispose = true)
        {
            var list = new LayoutList();
            list.EditMode = EditModes.ByClick;
            list.FieldSource = obj;

            var window = new ToolWindow();
            window.Mode = ToolShowMode.Dialog;
            window.HeaderVisible = true;
            window.Target = list;
            window.Label.Text = label;
            if (dispose)
                window.Closed += (s, e) => window.Dispose();

            return window;
        }

        public bool CloseOnAccept
        {
            get { return closeOnAccept; }
            set { closeOnAccept = value; }
        }

        public bool CloseOnClose
        {
            get { return closeOnClose; }
            set { closeOnClose = value; }
        }


    }

    [Flags]
    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 4,
        No = 8
    }
}