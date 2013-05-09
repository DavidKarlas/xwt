// 
// LabelBackend.cs
//  
// Author:
//       Carlos Alberto Cortez <calberto.cortez@gmail.com>
// 
// Copyright (c) 2012 Carlos Alberto Cortez
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using SWC = System.Windows.Controls;
using SWM = System.Windows.Media;

using Xwt.Backends;
using System.Globalization;
using System.Windows.Media;

namespace Xwt.WPFBackend
{
	public class LabelBackend : WidgetBackend, ILabelBackend
	{
		public LabelBackend ()
		{
			Widget = new WpfLabel ();
		}

		WpfLabel Label {
			get { return (WpfLabel)Widget; }
		}

		public string Text {
			get {
				if (Label.FormattedText != null)
					return Label.FormattedText.Text;
				return Label.TextBlock.Text;
			}
			set {
				Label.FormattedText = null;
				formattedTextUsedForUpdate = null;
				Label.Content = Label.TextBlock;
				Label.TextBlock.Text = value;
				Widget.InvalidateMeasure();
			}
		}

		private FormattedText formattedTextUsedForUpdate = null;

		private void UpdateFormattedText()
		{
			if (formattedTextUsedForUpdate != null)
				SetFormattedText(formattedTextUsedForUpdate);
		}

		public void SetFormattedText (FormattedText text)
		{
			formattedTextUsedForUpdate = text;
			var wpfTextLayoutHandler = new WpfTextLayoutBackendHandler();
			var wpfTextLayout = new TextLayoutBackend();
			wpfTextLayoutHandler.SetText(wpfTextLayout, text.Text);
			wpfTextLayout.FormattedText.SetFontSize(Label.FontSize);
			wpfTextLayout.FormattedText.SetFontTypeface(new Typeface(Label.FontFamily, Label.FontStyle, Label.FontWeight, Label.FontStretch));
			wpfTextLayout.FormattedText.SetForegroundBrush(Label.Foreground);
			wpfTextLayout.FormattedText.TextAlignment = Label.TextBlock.TextAlignment;
			wpfTextLayout.FormattedText.Trimming = Label.TextBlock.TextTrimming;
			//Wraping is handled in OnRender...
			//if (Label.TextBlock.TextWrapping != TextWrapping.NoWrap && Label.MaxWidth != double.PositiveInfinity)
			//    wpfTextLayout.FormattedText.MaxTextWidth = Label.MaxWidth;
			//else
			//    wpfTextLayout.FormattedText.MaxTextWidth = 0;

			foreach (var ha in text.Attributes)
				wpfTextLayoutHandler.AddAttribute(wpfTextLayout, ha);
			Label.FormattedText = wpfTextLayout.FormattedText;
			Label.Content = null;
			Widget.InvalidateMeasure();
		}

		public override object Font
		{
			get
			{
				return base.Font;
			}
			set
			{
				base.Font = value;
				UpdateFormattedText();
			}
		}

		public Xwt.Drawing.Color TextColor {
			get {
				SWM.Color color = SystemColors.ControlColor;

				if (Label.Foreground != null)
					color = ((SWM.SolidColorBrush) Label.Foreground).Color;

				return DataConverter.ToXwtColor (color);
			}
			set {
				Label.Foreground = ResPool.GetSolidBrush (value);
				UpdateFormattedText();
			}
		}

		public Alignment TextAlignment {
			get { return DataConverter.ToXwtAlignment(Label.TextBlock.TextAlignment); }
			set {
				Label.TextBlock.TextAlignment = DataConverter.ToTextAlignment(value);
				UpdateFormattedText();
			}
		}

		public EllipsizeMode Ellipsize {
			get {
				if (Label.TextBlock.TextTrimming == TextTrimming.None)
					return Xwt.EllipsizeMode.None;
				else
					return Xwt.EllipsizeMode.End;
			}
			set {
				if (value == EllipsizeMode.None)
					Label.TextBlock.TextTrimming = TextTrimming.None;
				else
					Label.TextBlock.TextTrimming = TextTrimming.CharacterEllipsis;
				UpdateFormattedText();
			}
		}

		public WrapMode Wrap {
			get {
				if (Label.TextBlock.TextWrapping == TextWrapping.NoWrap)
					return WrapMode.None;
				else
					return WrapMode.Word;
			} set {
				if (value == WrapMode.None)
					Label.TextBlock.TextWrapping = TextWrapping.NoWrap;
				else
					Label.TextBlock.TextWrapping = TextWrapping.Wrap;
			}
		}

		public override WidgetSize GetPreferredWidth ()
		{
			if (Label.TextBlock.TextWrapping == TextWrapping.Wrap)
				return new WidgetSize (0);
			else if (Label.FormattedText != null)
				return new WidgetSize(Label.FormattedText.Width);
			else
				return base.GetPreferredWidth ();
		}
	}

	class WpfLabel : SWC.Label, IWpfWidget
	{
		public WpfLabel ()
		{
			TextBlock = new SWC.TextBlock ();
			Content = TextBlock;
			Padding = new Thickness (0);
		}

		public WidgetBackend Backend { get; set; }

		protected override System.Windows.Size MeasureOverride (System.Windows.Size constraint)
		{
			var s = base.MeasureOverride (constraint);
			if (FormattedText != null)
				s.Height = Math.Max (FormattedText.Height, s.Height);
			return Backend.MeasureOverride (constraint, s);
		}

		protected override void OnRender(SWM.DrawingContext drawingContext)
		{
			if (FormattedText != null)
			{
				if (TextBlock.TextWrapping != TextWrapping.NoWrap)
					FormattedText.MaxTextWidth = RenderSize.Width;
				drawingContext.DrawText(FormattedText, new System.Windows.Point(0, 0));
			}
			else
			{
				base.OnRender(drawingContext);
			}
		}

		public SWC.TextBlock TextBlock {
			get;
			set;
		}

		public SWM.FormattedText FormattedText { get; set; }
	}
}
