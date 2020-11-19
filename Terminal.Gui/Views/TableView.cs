﻿using NStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Terminal.Gui.Views {

	/// <summary>
	/// View for tabular data based on a <see cref="DataTable"/>
	/// </summary>
	public class TableView : View {

		private int columnOffset;
		private int rowOffset;
		private int selectedRow;
		private int selectedColumn;
		private DataTable table;

		/// <summary>
		/// The data table to render in the view.  Setting this property automatically updates and redraws the control.
		/// </summary>
		public DataTable Table { get => table; set {table = value; Update(); } }

		/// <summary>
		/// Zero indexed offset for the upper left <see cref="DataColumn"/> to display in <see cref="Table"/>.
		/// </summary>
		/// <remarks>This property allows very wide tables to be rendered with horizontal scrolling</remarks>
		public int ColumnOffset {
			get => columnOffset;

			//try to prevent this being set to an out of bounds column
			set => columnOffset = Table == null ? 0 : Math.Min (Table.Columns.Count - 1, Math.Max (0, value));
		}

		/// <summary>
		/// Zero indexed offset for the <see cref="DataRow"/> to display in <see cref="Table"/> on line 2 of the control (first line being headers)
		/// </summary>
		/// <remarks>This property allows very wide tables to be rendered with horizontal scrolling</remarks>
		public int RowOffset {
			get => rowOffset;
			set => rowOffset = Table == null ? 0 : Math.Min (Table.Rows.Count - 1, Math.Max (0, value));
		}

		/// <summary>
		/// The index of <see cref="DataTable.Columns"/> in <see cref="Table"/> that the user has currently selected
		/// </summary>
		public int SelectedColumn {
			get => selectedColumn;

			//try to prevent this being set to an out of bounds column
			set => selectedColumn = Table == null ? 0 :  Math.Min (Table.Columns.Count - 1, Math.Max (0, value));
		}

		/// <summary>
		/// The index of <see cref="DataTable.Rows"/> in <see cref="Table"/> that the user has currently selected
		/// </summary>
		public int SelectedRow {
			get => selectedRow;
			set => selectedRow =  Table == null ? 0 : Math.Min (Table.Rows.Count - 1, Math.Max (0, value));
		}

		/// <summary>
		/// The maximum number of characters to render in any given column.  This prevents one long column from pushing out all the others
		/// </summary>
		public int MaximumCellWidth { get; set; } = 100;

		/// <summary>
		/// The text representation that should be rendered for cells with the value <see cref="DBNull.Value"/>
		/// </summary>
		public string NullSymbol { get; set; } = "-";

		/// <summary>
		/// The symbol to add after each cell value and header value to visually seperate values
		/// </summary>
		public char SeparatorSymbol { get; set; } = ' ';

		/// <summary>
		/// Initialzies a <see cref="TableView"/> class using <see cref="LayoutStyle.Computed"/> layout. 
		/// </summary>
		/// <param name="table">The table to display in the control</param>
		public TableView (DataTable table) : base ()
		{
			this.Table = table;
		}

		/// <summary>
		/// Initialzies a <see cref="TableView"/> class using <see cref="LayoutStyle.Computed"/> layout. Set the <see cref="Table"/> property to begin editing
		/// </summary>
		public TableView () : base ()
		{
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			var frame = Frame;

			// What columns to render at what X offset in viewport
			Dictionary<DataColumn, int> columnsToRender = CalculateViewport (bounds);

			Driver.SetAttribute (ColorScheme.Normal);

			//invalidate current row (prevents scrolling around leaving old characters in the frame
			Driver.AddStr (new string (' ', bounds.Width));

			// Render the headers
			foreach (var kvp in columnsToRender) {

				Move (kvp.Value, 0);
				Driver.AddStr (Truncate (kvp.Key.ColumnName + SeparatorSymbol, bounds.Width - kvp.Value));
			}

			//render the cells
			for (int line = 1; line < frame.Height; line++) {

				//invalidate current row (prevents scrolling around leaving old characters in the frame
				Move (0, line);
				Driver.SetAttribute (ColorScheme.Normal);
				Driver.AddStr (new string (' ', bounds.Width));

				//work out what Row to render
				var rowToRender = RowOffset + (line - 1);

				//if we have run off the end of the table
				if ( Table == null || rowToRender >= Table.Rows.Count)
					continue;

				foreach (var kvp in columnsToRender) {
					Move (kvp.Value, line);

					bool isSelectedCell = rowToRender == SelectedRow && kvp.Key.Ordinal == SelectedColumn;

					Driver.SetAttribute (isSelectedCell ? ColorScheme.HotFocus : ColorScheme.Normal);


					var valueToRender = GetRenderedVal (Table.Rows [rowToRender] [kvp.Key]) + SeparatorSymbol;
					Driver.AddStr (Truncate (valueToRender, bounds.Width - kvp.Value));
				}
			}

		}

		/// <summary>
		/// Truncates <paramref name="valueToRender"/> so that it occupies a maximum of <paramref name="availableHorizontalSpace"/>
		/// </summary>
		/// <param name="valueToRender"></param>
		/// <param name="availableHorizontalSpace"></param>
		/// <returns></returns>
		private ustring Truncate (string valueToRender, int availableHorizontalSpace)
		{
			if (string.IsNullOrEmpty (valueToRender) || valueToRender.Length < availableHorizontalSpace)
				return valueToRender;

			return valueToRender.Substring (0, availableHorizontalSpace);
		}

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			switch (keyEvent.Key) {
			case Key.CursorLeft:
				SelectedColumn--;
				Update ();
				break;
			case Key.CursorRight:
				SelectedColumn++;
				Update ();
				break;
			case Key.CursorDown:
				SelectedRow++;
				Update ();
				break;
			case Key.CursorUp:
				SelectedRow--;
				Update ();
				break;
			case Key.PageUp:
				SelectedRow -= Frame.Height;
				Update ();
				break;
			case Key.PageDown:
				SelectedRow += Frame.Height;
				Update ();
				break;
			case Key.Home | Key.CtrlMask:
				SelectedRow = 0;
				SelectedColumn = 0;
				Update ();
				break;
			case Key.Home:
				SelectedColumn = 0;
				Update ();
				break;
			case Key.End | Key.CtrlMask:
				//jump to end of table
				SelectedRow =  Table == null ? 0 : Table.Rows.Count - 1;
				SelectedColumn =  Table == null ? 0 : Table.Columns.Count - 1;
				Update ();
				break;
			case Key.End:
				//jump to end of row
				SelectedColumn =  Table == null ? 0 : Table.Columns.Count - 1;
				Update ();
				break;
			}
			PositionCursor ();
			return true;
		}

		/// <summary>
		/// Updates the view to reflect changes to <see cref="Table"/> and to (<see cref="ColumnOffset"/> / <see cref="RowOffset"/>) etc
		/// </summary>
		/// <remarks>This always calls <see cref="View.SetNeedsDisplay()"/></remarks>
		public void Update()
		{
			if(Table == null) {
				SetNeedsDisplay ();
				return;
			}

			//if user opened a large table scrolled down a lot then opened a smaller table (or API deleted a bunch of columns without telling anyone)
			ColumnOffset = Math.Max(Math.Min(ColumnOffset,Table.Columns.Count -1),0);
			RowOffset = Math.Max(Math.Min(RowOffset,Table.Rows.Count -1),0);
			SelectedColumn = Math.Max(Math.Min(SelectedColumn,Table.Columns.Count -1),0);
			SelectedRow = Math.Max(Math.Min(SelectedRow,Table.Rows.Count -1),0);

			Dictionary<DataColumn, int> columnsToRender = CalculateViewport (Bounds);

			//if we have scrolled too far to the left 
			if (SelectedColumn < columnsToRender.Keys.Min (col => col.Ordinal)) {
				ColumnOffset = SelectedColumn;
			}

			//if we have scrolled too far to the right
			if (SelectedColumn > columnsToRender.Keys.Max (col => col.Ordinal)) {
				ColumnOffset = SelectedColumn;
			}

			//if we have scrolled too far down
			if (SelectedRow > RowOffset + Bounds.Height - 1) {
				RowOffset = SelectedRow;
			}
			//if we have scrolled too far up
			if (SelectedRow < RowOffset) {
				RowOffset = SelectedRow;
			}

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Calculates which columns should be rendered given the <paramref name="bounds"/> in which to display and the <see cref="ColumnOffset"/>
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="padding"></param>
		/// <returns></returns>
		private Dictionary<DataColumn, int> CalculateViewport (Rect bounds, int padding = 1)
		{
			Dictionary<DataColumn, int> toReturn = new Dictionary<DataColumn, int> ();

			if(Table == null)
				return toReturn;

			int usedSpace = 0;
			int availableHorizontalSpace = bounds.Width;
			int rowsToRender = bounds.Height - 1; //1 reserved for the headers row

			foreach (var col in Table.Columns.Cast<DataColumn> ().Skip (ColumnOffset)) {

				toReturn.Add (col, usedSpace);
				usedSpace += CalculateMaxRowSize (col, rowsToRender) + padding;

				if (usedSpace > availableHorizontalSpace)
					return toReturn;

			}

			return toReturn;
		}

		/// <summary>
		/// Returns the maximum of the <paramref name="col"/> name and the maximum length of data that will be rendered starting at <see cref="RowOffset"/> and rendering <paramref name="rowsToRender"/>
		/// </summary>
		/// <param name="col"></param>
		/// <param name="rowsToRender"></param>
		/// <returns></returns>
		private int CalculateMaxRowSize (DataColumn col, int rowsToRender)
		{
			int spaceRequired = col.ColumnName.Length;

			for (int i = RowOffset; i < RowOffset + rowsToRender && i < Table.Rows.Count; i++) {

				//expand required space if cell is bigger than the last biggest cell or header
				spaceRequired = Math.Max (spaceRequired, GetRenderedVal (Table.Rows [i] [col]).Length);
			}

			return spaceRequired;
		}

		/// <summary>
		/// Returns the value that should be rendered to best represent a strongly typed <paramref name="value"/> read from <see cref="Table"/>
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private string GetRenderedVal (object value)
		{
			if (value == null || value == DBNull.Value) {
				return NullSymbol;
			}

			var representation = value.ToString ();

			//if it is too long to fit
			if (representation.Length > MaximumCellWidth)
				return representation.Substring (0, MaximumCellWidth);

			return representation;
		}
	}
}
