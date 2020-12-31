﻿using NStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Describes how to render a given column in  a <see cref="TableView"/> including <see cref="Alignment"/> and textual representation of cells (e.g. date formats)
	/// </summary>
	public class ColumnStyle {
		
		/// <summary>
		/// Defines the default alignment for all values rendered in this column.  For custom alignment based on cell contents use <see cref="AlignmentGetter"/>.
		/// </summary>
		public TextAlignment Alignment {get;set;}
	
		/// <summary>
		/// Defines a delegate for returning custom alignment per cell based on cell values.  When specified this will override <see cref="Alignment"/>
		/// </summary>
		public Func<object,TextAlignment> AlignmentGetter;

		/// <summary>
		/// Defines a delegate for returning custom representations of cell values.  If not set then <see cref="object.ToString()"/> is used.  Return values from your delegate may be truncated e.g. based on <see cref="MaxWidth"/>
		/// </summary>
		public Func<object,string> RepresentationGetter;

		/// <summary>
		/// Defines the format for values e.g. "yyyy-MM-dd" for dates
		/// </summary>
		public string Format{get;set;}

		/// <summary>
		/// Set the maximum width of the column in characters.  This value will be ignored if more than the tables <see cref="TableView.MaxCellWidth"/>.  Defaults to <see cref="TableView.DefaultMaxCellWidth"/>
		/// </summary>
		public int MaxWidth {get;set;} = TableView.DefaultMaxCellWidth;

		/// <summary>
		/// Set the minimum width of the column in characters.  This value will be ignored if more than the tables <see cref="TableView.MaxCellWidth"/> or the <see cref="MaxWidth"/>
		/// </summary>
		public int MinWidth {get;set;}

		/// <summary>
		/// Returns the alignment for the cell based on <paramref name="cellValue"/> and <see cref="AlignmentGetter"/>/<see cref="Alignment"/>
		/// </summary>
		/// <param name="cellValue"></param>
		/// <returns></returns>
		public TextAlignment GetAlignment(object cellValue)
		{
			if(AlignmentGetter != null)
				return AlignmentGetter(cellValue);

			return Alignment;
		}

		/// <summary>
		/// Returns the full string to render (which may be truncated if too long) that the current style says best represents the given <paramref name="value"/>
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public string GetRepresentation (object value)
		{
			if(!string.IsNullOrWhiteSpace(Format)) {

				if(value is IFormattable f)
					return f.ToString(Format,null);
			}
				

			if(RepresentationGetter != null)
				return RepresentationGetter(value);

			return value?.ToString();
		}
	}
	/// <summary>
	/// Defines rendering options that affect how the table is displayed
	/// </summary>
	public class TableStyle {
		
		/// <summary>
		/// When scrolling down always lock the column headers in place as the first row of the table
		/// </summary>
		public bool AlwaysShowHeaders {get;set;} = false;

		/// <summary>
		/// True to render a solid line above the headers
		/// </summary>
		public bool ShowHorizontalHeaderOverline {get;set;} = true;

		/// <summary>
		/// True to render a solid line under the headers
		/// </summary>
		public bool ShowHorizontalHeaderUnderline {get;set;} = true;

		/// <summary>
		/// True to render a solid line vertical line between cells
		/// </summary>
		public bool ShowVerticalCellLines {get;set;} = true;

		/// <summary>
		/// True to render a solid line vertical line between headers
		/// </summary>
		public bool ShowVerticalHeaderLines {get;set;} = true;

		/// <summary>
		/// Collection of columns for which you want special rendering (e.g. custom column lengths, text alignment etc)
		/// </summary>
		public Dictionary<DataColumn,ColumnStyle> ColumnStyles {get;set; }  = new Dictionary<DataColumn, ColumnStyle>();

		/// <summary>
		/// Returns the entry from <see cref="ColumnStyles"/> for the given <paramref name="col"/> or null if no custom styling is defined for it
		/// </summary>
		/// <param name="col"></param>
		/// <returns></returns>
		public ColumnStyle GetColumnStyleIfAny (DataColumn col)
		{
			return ColumnStyles.TryGetValue(col,out ColumnStyle result) ? result : null;
		}
	}
	
	/// <summary>
	/// View for tabular data based on a <see cref="DataTable"/>
	/// </summary>
	public class TableView : View {

		private int columnOffset;
		private int rowOffset;
		private int selectedRow;
		private int selectedColumn;
		private DataTable table;
		private TableStyle style = new TableStyle();

		/// <summary>
		/// The default maximum cell width for <see cref="TableView.MaxCellWidth"/> and <see cref="ColumnStyle.MaxWidth"/>
		/// </summary>
		public const int DefaultMaxCellWidth = 100;

		/// <summary>
		/// The data table to render in the view.  Setting this property automatically updates and redraws the control.
		/// </summary>
		public DataTable Table { get => table; set {table = value; Update(); } }
		
		/// <summary>
		/// Contains options for changing how the table is rendered
		/// </summary>
		public TableStyle Style { get => style; set {style = value; Update(); } }
						
		/// <summary>
		/// Horizontal scroll offset.  The index of the first column in <see cref="Table"/> to display when when rendering the view.
		/// </summary>
		/// <remarks>This property allows very wide tables to be rendered with horizontal scrolling</remarks>
		public int ColumnOffset {
			get => columnOffset;

			//try to prevent this being set to an out of bounds column
			set => columnOffset = Table == null ? 0 :Math.Max (0,Math.Min (Table.Columns.Count - 1,  value));
		}

		/// <summary>
		/// Vertical scroll offset.  The index of the first row in <see cref="Table"/> to display in the first non header line of the control when rendering the view.
		/// </summary>
		public int RowOffset {
			get => rowOffset;
			set => rowOffset = Table == null ? 0 : Math.Max (0,Math.Min (Table.Rows.Count - 1, value));
		}

		/// <summary>
		/// The index of <see cref="DataTable.Columns"/> in <see cref="Table"/> that the user has currently selected
		/// </summary>
		public int SelectedColumn {
			get => selectedColumn;

			set {
				var oldValue = selectedColumn;

				//try to prevent this being set to an out of bounds column
				selectedColumn = Table == null ? 0 :  Math.Min (Table.Columns.Count - 1, Math.Max (0, value));

				if(oldValue != selectedColumn)
					OnSelectedCellChanged(new SelectedCellChangedEventArgs(Table,oldValue,SelectedColumn,SelectedRow,SelectedRow));
			} 
		}

		/// <summary>
		/// The index of <see cref="DataTable.Rows"/> in <see cref="Table"/> that the user has currently selected
		/// </summary>
		public int SelectedRow {
			get => selectedRow;
			set {

				var oldValue = selectedRow;

				selectedRow =  Table == null ? 0 : Math.Min (Table.Rows.Count - 1, Math.Max (0, value));

				if(oldValue != selectedRow)
					OnSelectedCellChanged(new SelectedCellChangedEventArgs(Table,SelectedColumn,SelectedColumn,oldValue,selectedRow));
			}
		}

		/// <summary>
		/// The maximum number of characters to render in any given column.  This prevents one long column from pushing out all the others
		/// </summary>
		public int MaxCellWidth { get; set; } = DefaultMaxCellWidth;

		/// <summary>
		/// The text representation that should be rendered for cells with the value <see cref="DBNull.Value"/>
		/// </summary>
		public string NullSymbol { get; set; } = "-";

		/// <summary>
		/// The symbol to add after each cell value and header value to visually seperate values (if not using vertical gridlines)
		/// </summary>
		public char SeparatorSymbol { get; set; } = ' ';

		/// <summary>
		/// This event is raised when the selected cell in the table changes.
		/// </summary>
		public event Action<SelectedCellChangedEventArgs> SelectedCellChanged;

		/// <summary>
		/// Initialzies a <see cref="TableView"/> class using <see cref="LayoutStyle.Computed"/> layout. 
		/// </summary>
		/// <param name="table">The table to display in the control</param>
		public TableView (DataTable table) : this ()
		{
			this.Table = table;
		}

		/// <summary>
		/// Initialzies a <see cref="TableView"/> class using <see cref="LayoutStyle.Computed"/> layout. Set the <see cref="Table"/> property to begin editing
		/// </summary>
		public TableView () : base ()
		{
			CanFocus = true;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			var frame = Frame;

			// What columns to render at what X offset in viewport
			var columnsToRender = CalculateViewport(bounds).ToArray();

			Driver.SetAttribute (ColorScheme.Normal);
			
			//invalidate current row (prevents scrolling around leaving old characters in the frame
			Driver.AddStr (new string (' ', bounds.Width));

			int line = 0;

			if(ShouldRenderHeaders()){
				// Render something like:
				/*
					┌────────────────────┬──────────┬───────────┬──────────────┬─────────┐
					│ArithmeticComparator│chi       │Healthboard│Interpretation│Labnumber│
					└────────────────────┴──────────┴───────────┴──────────────┴─────────┘
				*/
				if(Style.ShowHorizontalHeaderOverline){
					RenderHeaderOverline(line,bounds.Width,columnsToRender);
					line++;
				}

				RenderHeaderMidline(line,columnsToRender);
				line++;

				if(Style.ShowHorizontalHeaderUnderline){
					RenderHeaderUnderline(line,bounds.Width,columnsToRender);
					line++;
				}
			}
					
			int headerLinesConsumed = line;

			//render the cells
			for (; line < frame.Height; line++) {

				ClearLine(line,bounds.Width);

				//work out what Row to render
				var rowToRender = RowOffset + (line - headerLinesConsumed);

				//if we have run off the end of the table
				if ( Table == null || rowToRender >= Table.Rows.Count || rowToRender < 0)
					continue;

				RenderRow(line,rowToRender,columnsToRender);
			}
		}

		/// <summary>
		/// Clears a line of the console by filling it with spaces
		/// </summary>
		/// <param name="row"></param>
		/// <param name="width"></param>
		private void ClearLine(int row, int width)
		{            
			Move (0, row);
			Driver.SetAttribute (ColorScheme.Normal);
			Driver.AddStr (new string (' ', width));
		}

		/// <summary>
		/// Returns the amount of vertical space required to display the header
		/// </summary>
		/// <returns></returns>
		private int GetHeaderHeight()
		{
			int heightRequired = 1;
			
			if(Style.ShowHorizontalHeaderOverline)
				heightRequired++;

			if(Style.ShowHorizontalHeaderUnderline)
				heightRequired++;
			
			return heightRequired;
		}

		private void RenderHeaderOverline(int row,int availableWidth, ColumnToRender[] columnsToRender)
		{
			// Renders a line above table headers (when visible) like:
			// ┌────────────────────┬──────────┬───────────┬──────────────┬─────────┐

			for(int c = 0;c< availableWidth;c++) {

				var rune = Driver.HLine;

				if (Style.ShowVerticalHeaderLines){
							
					if(c == 0){
						rune = Driver.ULCorner;
					}	
					// if the next column is the start of a header
					else if(columnsToRender.Any(r=>r.X == c+1)){
						rune = Driver.TopTee;
					}
					else if(c == availableWidth -1){
						rune = Driver.URCorner;
					}
				}

				AddRuneAt(Driver,c,row,rune);
			}
		}

		private void RenderHeaderMidline(int row, ColumnToRender[] columnsToRender)
		{
			// Renders something like:
			// │ArithmeticComparator│chi       │Healthboard│Interpretation│Labnumber│
						
			ClearLine(row,Bounds.Width);

			//render start of line
			if(style.ShowVerticalHeaderLines)
				AddRune(0,row,Driver.VLine);

			for(int i =0 ; i<columnsToRender.Length;i++) {
				
				var current =  columnsToRender[i];
				var availableWidthForCell = GetCellWidth(columnsToRender,i);

				var colStyle = Style.GetColumnStyleIfAny(current.Column);
				var colName = current.Column.ColumnName;

				RenderSeparator(current.X-1,row,true);
									
				Move (current.X, row);
				
				Driver.AddStr(TruncateOrPad(colName,colName,availableWidthForCell ,colStyle));

			}

			//render end of line
			if(style.ShowVerticalHeaderLines)
				AddRune(Bounds.Width-1,row,Driver.VLine);
		}

		/// <summary>
		/// Calculates how much space is available to render index <paramref name="i"/> of the <paramref name="columnsToRender"/> given the remaining horizontal space
		/// </summary>
		/// <param name="columnsToRender"></param>
		/// <param name="i"></param>
		private int GetCellWidth (ColumnToRender [] columnsToRender, int i)
		{
			var current =  columnsToRender[i];
			var next = i+1 < columnsToRender.Length ? columnsToRender[i+1] : null;

			if(next == null) {
				// cell can fill to end of the line
				return Bounds.Width - current.X;
			}
			else {
				// cell can fill up to next cell start				
				return next.X - current.X;
			}

		}

		private void RenderHeaderUnderline(int row,int availableWidth, ColumnToRender[] columnsToRender)
		{
			// Renders a line below the table headers (when visible) like:
			// ├──────────┼───────────┼───────────────────┼──────────┼────────┼─────────────┤
								
			for(int c = 0;c< availableWidth;c++) {

				var rune = Driver.HLine;

				if (Style.ShowVerticalHeaderLines){
					if(c == 0){
						rune = Style.ShowVerticalCellLines ? Driver.LeftTee : Driver.LLCorner;
					}	
					// if the next column is the start of a header
					else if(columnsToRender.Any(r=>r.X == c+1)){
					
						/*TODO: is ┼ symbol in Driver?*/ 
						rune = Style.ShowVerticalCellLines ? '┼' :Driver.BottomTee;
					}
					else if(c == availableWidth -1){
						rune = Style.ShowVerticalCellLines ? Driver.RightTee : Driver.LRCorner;
					}
				}

				AddRuneAt(Driver,c,row,rune);
			}
			
		}
		private void RenderRow(int row, int rowToRender, ColumnToRender[] columnsToRender)
		{
			//render start of line
			if(style.ShowVerticalCellLines)
				AddRune(0,row,Driver.VLine);

			// Render cells for each visible header for the current row
			for(int i=0;i< columnsToRender.Length ;i++) {

				var current = columnsToRender[i];
				var availableWidthForCell = GetCellWidth(columnsToRender,i);

				var colStyle = Style.GetColumnStyleIfAny(current.Column);

				// move to start of cell (in line with header positions)
				Move (current.X, row);

				// Set color scheme based on whether the current cell is the selected one
				bool isSelectedCell = rowToRender == SelectedRow && current.Column.Ordinal == SelectedColumn;
				Driver.SetAttribute (isSelectedCell ? ColorScheme.HotFocus : ColorScheme.Normal);

				var val = Table.Rows [rowToRender][current.Column];

				// Render the (possibly truncated) cell value
				var representation = GetRepresentation(val,colStyle);
				
				Driver.AddStr (TruncateOrPad(val,representation,availableWidthForCell,colStyle));
				
				// Reset color scheme to normal and render the vertical line (or space) at the end of the cell
				Driver.SetAttribute (ColorScheme.Normal);
				RenderSeparator(current.X-1,row,false);
			}

			//render end of line
			if(style.ShowVerticalCellLines)
				AddRune(Bounds.Width-1,row,Driver.VLine);
		}
		
		private void RenderSeparator(int col, int row,bool isHeader)
		{
			if(col<0)
				return;
				
			var renderLines = isHeader ? style.ShowVerticalHeaderLines : style.ShowVerticalCellLines;

			Rune symbol =  renderLines ? Driver.VLine : SeparatorSymbol;
			AddRune(col,row,symbol);
		}

		void AddRuneAt (ConsoleDriver d,int col, int row, Rune ch)
		{
			Move (col, row);
			d.AddRune (ch);
		}

		/// <summary>
		/// Truncates or pads <paramref name="representation"/> so that it occupies a exactly <paramref name="availableHorizontalSpace"/> using the alignment specified in <paramref name="colStyle"/> (or left if no style is defined)
		/// </summary>
		/// <param name="originalCellValue">The object in this cell of the <see cref="Table"/></param>
		/// <param name="representation">The string representation of <paramref name="originalCellValue"/></param>
		/// <param name="availableHorizontalSpace"></param>
		/// <param name="colStyle">Optional style indicating custom alignment for the cell</param>
		/// <returns></returns>
		private string TruncateOrPad (object originalCellValue,string representation, int availableHorizontalSpace, ColumnStyle colStyle)
		{
			if (string.IsNullOrEmpty (representation))
				return representation;

			// if value is not wide enough
			if(representation.Sum(c=>Rune.ColumnWidth(c)) < availableHorizontalSpace) {
				
				// pad it out with spaces to the given alignment
				int toPad = availableHorizontalSpace - (representation.Sum(c=>Rune.ColumnWidth(c)) +1 /*leave 1 space for cell boundary*/);

				switch(colStyle?.GetAlignment(originalCellValue) ?? TextAlignment.Left) {

					case TextAlignment.Left : 
						return representation + new string(' ',toPad);
					case TextAlignment.Right : 
						return new string(' ',toPad) + representation;
					
					// TODO: With single line cells, centered and justified are the same right?
					case TextAlignment.Centered : 
					case TextAlignment.Justified : 
						return 
							new string(' ',(int)Math.Floor(toPad/2.0)) + // round down
							representation +
							 new string(' ',(int)Math.Ceiling(toPad/2.0)) ; // round up
				}
			}

			// value is too wide
			return new string(representation.TakeWhile(c=>(availableHorizontalSpace-= Rune.ColumnWidth(c))>0).ToArray());
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
			default:
				// Not a keystroke we care about
				return false;
			}
			PositionCursor ();
			return true;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp &&
				me.Flags != MouseFlags.WheeledLeft && me.Flags != MouseFlags.WheeledRight)
				return false;

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}

			if (Table == null) {
				return false;
			}

			// Scroll wheel flags
			switch(me.Flags)
			{
				case MouseFlags.WheeledDown: 
					RowOffset++;
					EnsureValidScrollOffsets();
					SetNeedsDisplay();
					return true;

				case MouseFlags.WheeledUp:
					RowOffset--;
					EnsureValidScrollOffsets();
					SetNeedsDisplay();
					return true;

				case MouseFlags.WheeledRight:
					ColumnOffset++;
					EnsureValidScrollOffsets();
					SetNeedsDisplay();
					return true;

				case  MouseFlags.WheeledLeft:
					ColumnOffset--;
					EnsureValidScrollOffsets();
					SetNeedsDisplay();
					return true;
			}

			if(me.Flags == MouseFlags.Button1Clicked) {
				
				var viewPort = CalculateViewport(Bounds);
				
				var headerHeight = ShouldRenderHeaders()? GetHeaderHeight():0;

				var col = viewPort.LastOrDefault(c=>c.X <= me.OfX);
				
				// Click is on the header section of rendered UI
				if(me.OfY < headerHeight)
					return false;

				var rowIdx = RowOffset - headerHeight + me.OfY;

				if(col != null && rowIdx >= 0) {
					
					SelectedRow = rowIdx;
					SelectedColumn = col.Column.Ordinal;
				
					Update();
				}
			}

			return false;
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

			EnsureValidScrollOffsets();
			EnsureValidSelection();

			EnsureSelectedCellIsVisible();

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Updates <see cref="ColumnOffset"/> and <see cref="RowOffset"/> where they are outside the bounds of the table (by adjusting them to the nearest existing cell).  Has no effect if <see cref="Table"/> has not been set.
		/// </summary>
		/// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDisplay()"/></remarks>
		public void EnsureValidScrollOffsets ()
		{
			if(Table == null){
				return;
			}

			ColumnOffset = Math.Max(Math.Min(ColumnOffset,Table.Columns.Count -1),0);
			RowOffset = Math.Max(Math.Min(RowOffset,Table.Rows.Count -1),0);
		}


		/// <summary>
		/// Updates <see cref="SelectedColumn"/> and <see cref="SelectedRow"/> where they are outside the bounds of the table (by adjusting them to the nearest existing cell).  Has no effect if <see cref="Table"/> has not been set.
		/// </summary>
		/// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDisplay()"/></remarks>
		public void EnsureValidSelection ()
		{
			if(Table == null){
				return;
			}

			SelectedColumn = Math.Max(Math.Min(SelectedColumn,Table.Columns.Count -1),0);
			SelectedRow = Math.Max(Math.Min(SelectedRow,Table.Rows.Count -1),0);
		}

		/// <summary>
		/// Updates scroll offsets to ensure that the selected cell is visible.  Has no effect if <see cref="Table"/> has not been set.
		/// </summary>
		/// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDisplay()"/></remarks>
		public void EnsureSelectedCellIsVisible ()
		{
			if(Table == null || Table.Columns.Count <= 0){
				return;
			}

			var columnsToRender = CalculateViewport (Bounds).ToArray();
			var headerHeight = ShouldRenderHeaders()? GetHeaderHeight() : 0;

			//if we have scrolled too far to the left 
			if (SelectedColumn < columnsToRender.Min (r => r.Column.Ordinal)) {
				ColumnOffset = SelectedColumn;
			}

			//if we have scrolled too far to the right
			if (SelectedColumn > columnsToRender.Max (r=> r.Column.Ordinal)) {
				ColumnOffset = SelectedColumn;
			}

			//if we have scrolled too far down
			if (SelectedRow >= RowOffset + (Bounds.Height - headerHeight)) {
				RowOffset = SelectedRow;
			}
			//if we have scrolled too far up
			if (SelectedRow < RowOffset) {
				RowOffset = SelectedRow;
			}
		}

		/// <summary>
		/// Invokes the <see cref="SelectedCellChanged"/> event
		/// </summary>
		protected virtual void OnSelectedCellChanged(SelectedCellChangedEventArgs args)
		{
			SelectedCellChanged?.Invoke(args);
		}

		/// <summary>
		/// Calculates which columns should be rendered given the <paramref name="bounds"/> in which to display and the <see cref="ColumnOffset"/>
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="padding"></param>
		/// <returns></returns>
		private IEnumerable<ColumnToRender> CalculateViewport (Rect bounds, int padding = 1)
		{
			if(Table == null)
				yield break;
			
			int usedSpace = 0;

			//if horizontal space is required at the start of the line (before the first header)
			if(Style.ShowVerticalHeaderLines || Style.ShowVerticalCellLines)
				usedSpace+=1;
			
			int availableHorizontalSpace = bounds.Width;
			int rowsToRender = bounds.Height;

			// reserved for the headers row
			if(ShouldRenderHeaders())
				rowsToRender -= GetHeaderHeight(); 

			bool first = true;

			foreach (var col in Table.Columns.Cast<DataColumn>().Skip (ColumnOffset)) {

				int startingIdxForCurrentHeader = usedSpace;
				var colStyle = Style.GetColumnStyleIfAny(col);

				// is there enough space for this column (and it's data)?
				usedSpace += CalculateMaxCellWidth (col, rowsToRender,colStyle) + padding;

				// no (don't render it) unless its the only column we are render (that must be one massively wide column!)
				if (!first && usedSpace > availableHorizontalSpace)
					yield break;

				// there is space
				yield return new ColumnToRender(col, startingIdxForCurrentHeader);
				first=false;
			}
		}

		private bool ShouldRenderHeaders()
		{
			if(Table == null || Table.Columns.Count == 0)
				return false;

		    return Style.AlwaysShowHeaders || rowOffset == 0;
		}

		/// <summary>
		/// Returns the maximum of the <paramref name="col"/> name and the maximum length of data that will be rendered starting at <see cref="RowOffset"/> and rendering <paramref name="rowsToRender"/>
		/// </summary>
		/// <param name="col"></param>
		/// <param name="rowsToRender"></param>
		/// <param name="colStyle"></param>
		/// <returns></returns>
		private int CalculateMaxCellWidth(DataColumn col, int rowsToRender,ColumnStyle colStyle)
		{
			int spaceRequired = col.ColumnName.Sum(c=>Rune.ColumnWidth(c));

			// if table has no rows
			if(RowOffset < 0)
				return spaceRequired;


			for (int i = RowOffset; i < RowOffset + rowsToRender && i < Table.Rows.Count; i++) {

				//expand required space if cell is bigger than the last biggest cell or header
				spaceRequired = Math.Max (spaceRequired, GetRepresentation(Table.Rows [i][col],colStyle).Sum(c=>Rune.ColumnWidth(c)));
			}

			// Don't require more space than the style allows
			if(colStyle != null){

				// enforce maximum cell width based on style
				if(spaceRequired > colStyle.MaxWidth) {
					spaceRequired = colStyle.MaxWidth;
				}

				// enforce minimum cell width based on style
				if(spaceRequired < colStyle.MinWidth) {
					spaceRequired = colStyle.MinWidth;
				}
			}
			
			// enforce maximum cell width based on global table style
			if(spaceRequired > MaxCellWidth)
				spaceRequired = MaxCellWidth;


			return spaceRequired;
		}

		/// <summary>
		/// Returns the value that should be rendered to best represent a strongly typed <paramref name="value"/> read from <see cref="Table"/>
		/// </summary>
		/// <param name="value"></param>
		/// <param name="colStyle">Optional style defining how to represent cell values</param>
		/// <returns></returns>
		private string GetRepresentation(object value,ColumnStyle colStyle)
		{
			if (value == null || value == DBNull.Value) {
				return NullSymbol;
			}

			return colStyle != null ? colStyle.GetRepresentation(value): value.ToString();
		}
	}

	/// <summary>
	/// Describes a desire to render a column at a given horizontal position in the UI
	/// </summary>
	internal class ColumnToRender {

		/// <summary>
		/// The column to render
		/// </summary>
		public DataColumn Column {get;set;}

		/// <summary>
		/// The horizontal position to begin rendering the column at
		/// </summary>
		public int X{get;set;}

		public ColumnToRender (DataColumn col, int x)
		{
			Column = col;
			X = x;
		}
	}

	/// <summary>
	/// Defines the event arguments for <see cref="TableView.SelectedCellChanged"/> 
	/// </summary>
	public class SelectedCellChangedEventArgs : EventArgs
	{
		/// <summary>
		/// The current table to which the new indexes refer.  May be null e.g. if selection change is the result of clearing the table from the view
		/// </summary>
		/// <value></value>
		public DataTable Table {get;}


		/// <summary>
		/// The previous selected column index.  May be invalid e.g. when the selection has been changed as a result of replacing the existing Table with a smaller one
		/// </summary>
		/// <value></value>
		public int OldCol {get;}


		/// <summary>
		/// The newly selected column index.
		/// </summary>
		/// <value></value>
		public int NewCol {get;}


		/// <summary>
		/// The previous selected row index.  May be invalid e.g. when the selection has been changed as a result of deleting rows from the table
		/// </summary>
		/// <value></value>
		public int OldRow {get;}


		/// <summary>
		/// The newly selected row index.
		/// </summary>
		/// <value></value>
		public int NewRow {get;}

		/// <summary>
		/// Creates a new instance of arguments describing a change in selected cell in a <see cref="TableView"/>
		/// </summary>
		/// <param name="t"></param>
		/// <param name="oldCol"></param>
		/// <param name="newCol"></param>
		/// <param name="oldRow"></param>
		/// <param name="newRow"></param>
		public SelectedCellChangedEventArgs(DataTable t, int oldCol, int newCol, int oldRow, int newRow)
		{
			Table = t;
			OldCol = oldCol;
			NewCol = newCol;
			OldRow = oldRow;
			NewRow = newRow;
		}
	}
}
