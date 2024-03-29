﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.Views {
	public class ListViewTests {
		[Fact]
		public void Constructors_Defaults ()
		{
			var lv = new ListView ();
			Assert.Null (lv.Source);
			Assert.True (lv.CanFocus);

			lv = new ListView (new List<string> () { "One", "Two", "Three" });
			Assert.NotNull (lv.Source);

			lv = new ListView (new NewListDataSource ());
			Assert.NotNull (lv.Source);

			lv = new ListView (new Rect (0, 1, 10, 20), new List<string> () { "One", "Two", "Three" });
			Assert.NotNull (lv.Source);
			Assert.Equal (new Rect (0, 1, 10, 20), lv.Frame);

			lv = new ListView (new Rect (0, 1, 10, 20), new NewListDataSource ());
			Assert.NotNull (lv.Source);
			Assert.Equal (new Rect (0, 1, 10, 20), lv.Frame);
		}

		private class NewListDataSource : IListDataSource {
			public int Count => throw new NotImplementedException ();

			public int Length => throw new NotImplementedException ();

			public bool IsMarked (int item)
			{
				throw new NotImplementedException ();
			}

			public void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
			{
				throw new NotImplementedException ();
			}

			public void SetMark (int item, bool value)
			{
				throw new NotImplementedException ();
			}

			public IList ToList ()
			{
				throw new NotImplementedException ();
			}
		}

		[Fact]
		public void KeyBindings_Command ()
		{
			List<string> source = new List<string> () { "One", "Two", "Three" };
			ListView lv = new ListView (source) { Height = 2, AllowsMarking = true };
			Assert.Equal (0, lv.SelectedItem);
			Assert.True (lv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (1, lv.SelectedItem);
			Assert.True (lv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (0, lv.SelectedItem);
			Assert.True (lv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (2, lv.SelectedItem);
			Assert.Equal (2, lv.TopItem);
			Assert.True (lv.ProcessKey (new KeyEvent (Key.PageUp, new KeyModifiers ())));
			Assert.Equal (0, lv.SelectedItem);
			Assert.Equal (0, lv.TopItem);
			Assert.False (lv.Source.IsMarked (lv.SelectedItem));
			Assert.True (lv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ())));
			Assert.True (lv.Source.IsMarked (lv.SelectedItem));
			var opened = false;
			lv.OpenSelectedItem += (_) => opened = true;
			Assert.True (lv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (opened);
			Assert.True (lv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (2, lv.SelectedItem);
			Assert.True (lv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (0, lv.SelectedItem);
		}

		[Fact]
		[AutoInitShutdown]
		public void RowRender_Event ()
		{
			var rendered = false;
			var source = new List<string> () { "one", "two", "three" };
			var lv = new ListView () { Width = Dim.Fill (), Height = Dim.Fill () };
			lv.RowRender += _ => rendered = true;
			Application.Top.Add (lv);
			Application.Begin (Application.Top);
			Assert.False (rendered);

			lv.SetSource (source);
			lv.Redraw (lv.Bounds);
			Assert.True (rendered);
		}

		[Fact]
		[AutoInitShutdown]
		public void EnsuresVisibilitySelectedItem_Top ()
		{
			var source = new List<string> () { "First", "Second" };
			ListView lv = new ListView (source) { Width = Dim.Fill (), Height = 1 };
			lv.SelectedItem = 1;
			Application.Top.Add (lv);
			Application.Begin (Application.Top);

			Assert.Equal ("Second ", GetContents (0));
			Assert.Equal (new (' ', 7), GetContents (1));

			lv.MoveUp ();
			lv.Redraw (lv.Bounds);

			Assert.Equal ("First  ", GetContents (0));
			Assert.Equal (new (' ', 7), GetContents (1));

			string GetContents (int line)
			{
				var item = "";
				for (int i = 0; i < 7; i++) {
					item += (char)Application.Driver.Contents [line, i, 0];
				}
				return item;
			}
		}
	}
}
