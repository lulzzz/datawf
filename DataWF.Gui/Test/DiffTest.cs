﻿using DataWF.Gui;
using DataWF.Common;
using System;
using Xwt;

namespace DataWF.TestGui
{
    public class DiffTest : VPanel
    {
        private Label textALabel;
        private Label textBLabel;
        private TextEntry textA;
        private TextEntry textB;
        private Button testChar;
        private Button testWord;
        private LayoutList result;
        private GroupBoxItem itemParam;
        private GroupBoxItem itemResult;
        private GroupBox map;
        private Table panel;
        public DiffTest()
        {
            textALabel = new Label() { Text = "Param1:", TextAlignment = Alignment.Center };
            textBLabel = new Label() { Text = "Param2:", TextAlignment = Alignment.Center };
            textA = new TextEntry() { Text = "Sequential Read: Up to 550 MB/s " };
            textB = new TextEntry() { Text = "Sequential Write: Up to 510 MB/s " };

            testChar = new Button() { Label = "Test Char" };
            testChar.Clicked += TestCharOnClick;

            testWord = new Button() { Label = "Test Word" };
            testWord.Clicked += TestWordOnClick;

            result = new LayoutList() { GenerateToString = false };
            result.SelectionChanged += result_SelectionChanged;

            panel = new Table();
            panel.Add(textALabel, 0, 0);
            panel.Add(textA, 1, 0, colspan: 2, hexpand: true);
            panel.Add(textBLabel, 0, 1);
            panel.Add(textB, 1, 1, colspan: 2, hexpand: true);
            panel.Add(testChar, 0, 2);
            panel.Add(testWord, 1, 2, hexpand: false);
            //test.RowSpan = 

            itemParam = new GroupBoxItem()
            {
                Widget = panel,
                FillWidth = true,
                Name = "Params"
            };

            itemResult = new GroupBoxItem()
            {
                Widget = result,
                Row = 1,
                FillHeight = true,
                Name = "Result"
            };

            map = new GroupBox(itemParam, itemResult);

            PackStart(map, true, true);
            Text = "Diff Test";
        }

        private void result_SelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (result.SelectedItem != null)
            {
                var rez = (DiffResult)result.SelectedItem;
                var box = rez.Type == DiffType.Deleted ? textA : textB;
                box.SetFocus();
                box.SelectionStart = rez.Index;
                box.SelectionLength = rez.Length;
            }
        }

        private void TestCharOnClick(object sender, EventArgs e)
        {
            result.ListSource = DiffResult.Diff(textA.Text, textB.Text, false);
        }

        private void TestWordOnClick(object sender, EventArgs e)
        {
            result.ListSource = DiffResult.Diff(textA.Text.Split(' '), textB.Text.Split(' '));
        }
    }
}

