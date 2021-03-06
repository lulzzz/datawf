﻿/*
 TemplateParcer.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using DataWF.Common;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Word = DocumentFormat.OpenXml.Wordprocessing;

//using DataControl;

namespace DataWF.Data
{
    public class DocxParser : DocumentParser
    {
        public override string Parse(Stream stream, string fileName, ExecuteArgs param)
        {
            stream.Position = 0;
            using (var wd = WordprocessingDocument.Open(stream, true))
            {
                ParseDocxPart(wd.MainDocumentPart.Document, param);
                foreach (var header in wd.MainDocumentPart.HeaderParts)
                {
                    ParseDocxPart(header.Header, param);
                }
                stream.Flush();
            }
            return stream is FileStream fileStream ? fileStream.Name : null;
        }

        public void ParseDocxPart(OpenXmlPartRootElement doc, ExecuteArgs param)
        {
            var list = new List<Word.SdtElement>();
            Find<Word.SdtElement>(doc, list);
            foreach (var item in list)
            {
                OpenXmlElement stdContent = FindChildByName(item, "sdtContent");
                var element = stdContent.FirstChild;
                var prop = item.Descendants<Word.SdtProperties>().FirstOrDefault();
                var temp = prop.Descendants<Word.TemporarySdt>().FirstOrDefault();
                var tag = prop.Descendants<Word.Tag>().FirstOrDefault();
                if (tag == null)
                    tag = stdContent.Descendants<Word.Tag>().FirstOrDefault();

                if (tag != null)
                {
                    object val = ParseString(param, tag.Val.ToString());
                    if (val != null)
                    {
                        if (temp != null)
                        {
                            element.Remove();
                            item.Parent.ReplaceChild(element, item);
                        }
                        if (val is QResult)
                            FillTable(item, (QResult)val);
                        else
                            ReplaceString(element, val.ToString());
                    }
                }
            }
            doc.Save();
        }

        private OpenXmlElement FindChildByName(OpenXmlElement element, string v)
        {
            foreach (var child in element)
                if (child.LocalName == v)
                    return child;
            return null;
        }

        public void FillTable(OpenXmlElement element, QResult query)
        {
            var row = FindParent<Word.TableRow>(element);
            var prg = row.Descendants<Word.Paragraph>().FirstOrDefault();
            //element.Remove();
            Word.TableRow prow = null;
            foreach (object[] data in query.Values)
            {
                Word.TableCell cell = row.GetFirstChild<Word.TableCell>();
                foreach (object value in data)
                {
                    if (cell != null)
                    {
                        var paragraph = cell.Descendants<Word.Paragraph>().FirstOrDefault();
                        if (paragraph == null)
                        {
                            paragraph = (Word.Paragraph)prg.Clone();
                            cell.Append(paragraph);
                        }
                        Word.Run run = paragraph.Descendants<Word.Run>().FirstOrDefault();
                        if (run != null)
                        {
                            ReplaceString(run, value.ToString());
                        }
                        else
                        {
                            Word.Text text = new Word.Text() { Text = value.ToString(), Space = SpaceProcessingModeValues.Preserve };
                            run = new Word.Run();
                            run.Append(text);
                            paragraph.Append(run);
                        }

                        cell = cell.NextSibling<Word.TableCell>();
                    }
                }
                if (row.Parent == null)
                    prow.InsertAfterSelf<Word.TableRow>(row);
                prow = row;
                row = (Word.TableRow)row.Clone();
            }
        }

        public T FindParent<T>(OpenXmlElement element) where T : OpenXmlElement
        {
            while (!(element is T) && element.Parent != null)
                element = element.Parent;
            return element as T;
        }

        public void ReplaceString(OpenXmlElement element, string val)
        {
            var text = element.Descendants<Word.Text>().FirstOrDefault();
            var run = text == null ? element.Descendants<Word.Run>().FirstOrDefault() : FindParent<Word.Run>(text);
            var runp = run.Parent;
            var paragraph = FindParent<Word.Paragraph>(runp);
            run.RsidRunProperties = null;
            run.RemoveAllChildren<Word.Text>();
            run.RemoveAllChildren<Word.RunProperties>();
            runp.RemoveAllChildren<Word.Run>();
            runp.RemoveAllChildren<Word.Break>();

            if (paragraph.ParagraphProperties?.ParagraphMarkRunProperties != null)
            {
                run.RunProperties = new Word.RunProperties();
                foreach (var item in paragraph.ParagraphProperties.ParagraphMarkRunProperties)
                {
                    run.RunProperties.AppendChild(item.CloneNode(true));
                }
            }
            if (text == null)
                text = new Word.Text();
            else if (text.Parent != null)
                text.Remove();
            string[] pagesplit = val.TrimEnd("\r\n".ToCharArray()).Split("\f".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int p = 0; p < pagesplit.Length; p++)
            {
                var lineSpit = pagesplit[p].Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                if (lineSpit.Length > 0)
                {
                    for (int i = 0; i < lineSpit.Length; i++)
                    {
                        if (p == 0 && i == 0)
                        {
                            text.Text = lineSpit[0];
                            text.Space = SpaceProcessingModeValues.Preserve;
                            run.Append(text);
                            runp.Append(run);
                            continue;
                        }
                        Word.Run r = run.Clone() as Word.Run;
                        r.RemoveAllChildren<Word.Text>();
                        r.Append(new Word.Text(lineSpit[i]) { Space = SpaceProcessingModeValues.Preserve });

                        Word.Paragraph pr = (Word.Paragraph)paragraph.Clone();
                        pr.RemoveAllChildren<Word.Run>();
                        pr.RemoveAllChildren<Word.Break>();
                        pr.RemoveAllChildren<Word.SdtBlock>();
                        pr.RemoveAllChildren<Word.SdtRun>();

                        pr.Append(r);

                        paragraph.Parent.InsertAfter<Word.Paragraph>(pr, paragraph);
                        paragraph = pr;
                    }
                }
                if (p < pagesplit.Length - 1)
                {
                    var bp = new Word.Break() { Type = Word.BreakValues.Page };
                    paragraph.AppendChild(bp);
                }
            }
        }

        public Word.TableCell GenerateCell(string val, string w, int span = 0, bool bold = false, string sz = "12", string s = "style22", string f = "Courier New", Word.JustificationValues halign = Word.JustificationValues.Left)
        {
            Word.LeftMargin margin = new Word.LeftMargin { Type = new EnumValue<Word.TableWidthUnitValues>(Word.TableWidthUnitValues.Dxa), Width = new StringValue("10") };
            Word.TableCellWidth width = new Word.TableCellWidth { Type = new EnumValue<Word.TableWidthUnitValues>(Word.TableWidthUnitValues.Dxa), Width = w };
            Word.Shading shading = new Word.Shading { Fill = "auto", Val = new EnumValue<Word.ShadingPatternValues>(Word.ShadingPatternValues.Clear) };
            Word.TableCellMargin cellmargin = new Word.TableCellMargin(margin);
            Word.VerticalTextAlignmentOnPage align = new Word.VerticalTextAlignmentOnPage { Val = new EnumValue<Word.VerticalJustificationValues>(Word.VerticalJustificationValues.Center) };
            Word.GridSpan gspan = new Word.GridSpan { Val = span };

            Word.TableCellProperties props = new Word.TableCellProperties(width, shading, cellmargin, align, gspan);

            Word.Paragraph paragraph = GenerateParagraph(val, bold, sz, s, f, halign);

            Word.TableCell cell = new Word.TableCell(props, paragraph);
            return cell;
        }

        public Word.Paragraph GenerateParagraph(string val, bool bold = false, string sz = "12", string s = "style22", string f = "Courier New", Word.JustificationValues align = Word.JustificationValues.Left)
        {
            Word.ParagraphStyleId pstyle = new Word.ParagraphStyleId { Val = s };
            Word.Justification jut = new Word.Justification { Val = new EnumValue<Word.JustificationValues>(align) };
            Word.ParagraphProperties pprop = new Word.ParagraphProperties(pstyle, jut);

            Word.RunProperties rprop = new Word.RunProperties(
                new Word.RunFonts { Ascii = f, ComplexScript = f, HighAnsi = f },
                new Word.Bold { Val = new OnOffValue(bold) },
                new Word.BoldComplexScript { Val = new OnOffValue(bold) },
                new Word.FontSize { Val = sz });

            Word.Text text = new Word.Text(val);
            Word.Run run = new Word.Run(rprop, text);

            return new Word.Paragraph(pprop, run);
        }

        public void FindFields(OpenXmlElement documentPart, Type t, List<OpenXmlElement> results)
        {
            foreach (var child in documentPart.Elements())
            {
                if (child.GetType() == t)
                    results.Add(child);
                else
                    FindFields(child, t, results);
            }
        }

        private void Find<T>(OpenXmlElement documentPart, List<T> list) where T : OpenXmlElement
        {
            foreach (var child in documentPart)
            {
                if (child is T)
                    list.Add((T)child);
                else
                    Find<T>(child, list);
            }
        }

        private void FindFields(OpenXmlElement documentPart, Dictionary<Word.FieldChar, Word.FieldChar> results, ref Word.FieldChar lch)
        {
            foreach (var child in documentPart.Elements())
            {
                if (child is Word.FieldChar fch)
                {
                    if (fch.FieldCharType == Word.FieldCharValues.Begin)
                    {
                        lch = fch;
                        results.Add(fch, null);
                    }
                    else if (fch.FieldCharType == Word.FieldCharValues.End)
                        results[lch] = fch;
                }
                FindFields(child, results, ref lch);
            }
        }

    }


}