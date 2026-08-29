# MonoGame UI Grid Reference

XAML-style Grid layout with pixel, auto, and star column/row sizing.

## Table of Contents
1. [GridLength — sizing definition](#gridlength--sizing-definition)
2. [GridCellInfo — attached properties](#gridcellinfo--attached-properties)
3. [UIGrid — full implementation](#uigrid--full-implementation)
4. [Usage examples](#usage-examples)

---

## GridLength — Sizing Definition

```csharp
// GridLength.cs
namespace YourGame.UI
{
    public enum GridSizeMode { Pixel, Auto, Star }

    public struct GridLength
    {
        public GridSizeMode Mode  { get; }
        public float        Value { get; }  // pixels for Pixel mode; weight for Star mode

        private GridLength(GridSizeMode mode, float value) { Mode = mode; Value = value; }

        public static GridLength Pixel(float px)     => new GridLength(GridSizeMode.Pixel, px);
        public static GridLength Auto                => new GridLength(GridSizeMode.Auto,  0);
        public static GridLength Star(float weight = 1f) => new GridLength(GridSizeMode.Star, weight);

        // Computed size — set during Measure
        internal float ResolvedSize;
    }
}
```

---

## GridCellInfo — Attached Properties

Children carry their grid placement as a small struct stored in a static dictionary:

```csharp
// GridCellInfo.cs
using System.Collections.Generic;

namespace YourGame.UI
{
    public struct GridCellInfo
    {
        public int Row;     public int Col;
        public int RowSpan; public int ColSpan;

        public GridCellInfo(int row, int col, int rowSpan = 1, int colSpan = 1)
        {
            Row = row; Col = col; RowSpan = rowSpan; ColSpan = colSpan;
        }
    }

    public static class UIGridAttached
    {
        // Keyed by element reference — no allocation in the layout hot path
        private static readonly Dictionary<UIElement, GridCellInfo> _cells
            = new Dictionary<UIElement, GridCellInfo>();

        public static void SetCell(UIElement el, int row, int col,
                                   int rowSpan = 1, int colSpan = 1)
            => _cells[el] = new GridCellInfo(row, col, rowSpan, colSpan);

        public static GridCellInfo GetCell(UIElement el)
            => _cells.TryGetValue(el, out var c) ? c : new GridCellInfo(0, 0);

        public static void Remove(UIElement el) => _cells.Remove(el);
    }
}
```

---

## UIGrid — Full Implementation

```csharp
// UIGrid.cs
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace YourGame.UI
{
    public class UIGrid : UIContainer
    {
        private readonly List<GridLength> _cols = new List<GridLength>();
        private readonly List<GridLength> _rows = new List<GridLength>();

        // Resolved pixel origins after Measure/Arrange
        private int[] _colX;
        private int[] _colW;
        private int[] _rowY;
        private int[] _rowH;

        public void AddColumn(GridLength def) { _cols.Add(def); LayoutDirty = true; }
        public void AddRow(GridLength def)    { _rows.Add(def); LayoutDirty = true; }

        // Shorthand for UIGridAttached.SetCell
        public static void SetCell(UIElement el, int row, int col,
                                   int rowSpan = 1, int colSpan = 1)
            => UIGridAttached.SetCell(el, row, col, rowSpan, colSpan);

        public override void Measure(Point available)
        {
            int colCount = _cols.Count;
            int rowCount = _rows.Count;
            if (colCount == 0 || rowCount == 0) { DesiredSize = Point.Zero; return; }

            // --- Step 1: Measure Auto columns ---
            float[] colSizes = new float[colCount];
            float[] rowSizes = new float[rowCount];

            for (int c = 0; c < colCount; c++)
                colSizes[c] = (_cols[c].Mode == GridSizeMode.Pixel) ? _cols[c].Value : 0;
            for (int r = 0; r < rowCount; r++)
                rowSizes[r] = (_rows[r].Mode == GridSizeMode.Pixel) ? _rows[r].Value : 0;

            // Pass 1: measure children to resolve Auto sizes
            for (int i = 0; i < Children.Count; i++)
            {
                var info = UIGridAttached.GetCell(Children[i]);
                int c = System.Math.Min(info.Col, colCount - 1);
                int r = System.Math.Min(info.Row, rowCount - 1);
                Children[i].Measure(available);
                Point ds = Children[i].DesiredSize;

                if (_cols[c].Mode == GridSizeMode.Auto && info.ColSpan == 1)
                    if (ds.X > colSizes[c]) colSizes[c] = ds.X;
                if (_rows[r].Mode == GridSizeMode.Auto && info.RowSpan == 1)
                    if (ds.Y > rowSizes[r]) rowSizes[r] = ds.Y;
            }

            // --- Step 2: Distribute star space ---
            float totalFixedAuto_W = 0; float totalStarWeight_W = 0;
            float totalFixedAuto_H = 0; float totalStarWeight_H = 0;

            for (int c = 0; c < colCount; c++)
            {
                if (_cols[c].Mode == GridSizeMode.Star) totalStarWeight_W += _cols[c].Value;
                else                                    totalFixedAuto_W  += colSizes[c];
            }
            for (int r = 0; r < rowCount; r++)
            {
                if (_rows[r].Mode == GridSizeMode.Star) totalStarWeight_H += _rows[r].Value;
                else                                    totalFixedAuto_H  += rowSizes[r];
            }

            float starW = (totalStarWeight_W > 0) ? (available.X - totalFixedAuto_W) / totalStarWeight_W : 0;
            float starH = (totalStarWeight_H > 0) ? (available.Y - totalFixedAuto_H) / totalStarWeight_H : 0;

            for (int c = 0; c < colCount; c++)
                if (_cols[c].Mode == GridSizeMode.Star) colSizes[c] = _cols[c].Value * starW;
            for (int r = 0; r < rowCount; r++)
                if (_rows[r].Mode == GridSizeMode.Star) rowSizes[r] = _rows[r].Value * starH;

            // Cache resolved sizes for Arrange
            _colW = new int[colCount]; _colX = new int[colCount];
            _rowH = new int[rowCount]; _rowY = new int[rowCount];
            for (int c = 0; c < colCount; c++) _colW[c] = (int)colSizes[c];
            for (int r = 0; r < rowCount; r++) _rowH[r] = (int)rowSizes[r];

            // Total DesiredSize
            int totalW = 0; for (int c = 0; c < colCount; c++) totalW += _colW[c];
            int totalH = 0; for (int r = 0; r < rowCount; r++) totalH += _rowH[r];

            DesiredSize = (FixedSize != Point.Zero)
                ? FixedSize
                : new Point(totalW, totalH);
        }

        public override void Arrange(Rectangle finalRect)
        {
            Bounds = finalRect;
            if (_colW == null || _rowH == null) return;

            // Compute X origins
            int cx = finalRect.X;
            for (int c = 0; c < _colW.Length; c++) { _colX[c] = cx; cx += _colW[c]; }
            // Compute Y origins
            int cy = finalRect.Y;
            for (int r = 0; r < _rowH.Length; r++) { _rowY[r] = cy; cy += _rowH[r]; }

            // Arrange children
            for (int i = 0; i < Children.Count; i++)
            {
                var info    = UIGridAttached.GetCell(Children[i]);
                int col     = System.Math.Min(info.Col,     _colW.Length - 1);
                int row     = System.Math.Min(info.Row,     _rowH.Length - 1);
                int colSpan = System.Math.Min(info.ColSpan, _colW.Length - col);
                int rowSpan = System.Math.Min(info.RowSpan, _rowH.Length - row);

                int cellX = _colX[col];
                int cellY = _rowY[row];
                int cellW = 0; for (int c = col; c < col + colSpan; c++) cellW += _colW[c];
                int cellH = 0; for (int r = row; r < row + rowSpan; r++) cellH += _rowH[r];

                Children[i].Arrange(new Rectangle(cellX, cellY, cellW, cellH));
            }
        }
    }
}
```

---

## Usage Examples

### Settings Form

```csharp
var grid = new UIGrid();
grid.AddColumn(GridLength.Pixel(140));  // label column
grid.AddColumn(GridLength.Star(1));     // input column (fills remaining width)
grid.AddRow(GridLength.Auto);           // Volume row
grid.AddRow(GridLength.Auto);           // Resolution row
grid.AddRow(GridLength.Auto);           // Fullscreen row
grid.AddRow(GridLength.Pixel(50));      // OK/Cancel buttons row

// Volume label + slider
var volLabel  = new Label(font) { Text = "Volume:" };
var volSlider = new UISlider(pixel) { Min = 0, Max = 1 };
UIGrid.SetCell(volLabel,  row: 0, col: 0);
UIGrid.SetCell(volSlider, row: 0, col: 1);
grid.Add(volLabel);  grid.Add(volSlider);

// Fullscreen checkbox spanning both columns
var fullscreenCheck = new UICheckbox(font, "Fullscreen", boxTex, checkTex);
UIGrid.SetCell(fullscreenCheck, row: 2, col: 0, colSpan: 2);
grid.Add(fullscreenCheck);
```

### Inventory Grid (Uniform Cells)

```csharp
const int SlotSize = 64;
const int Cols = 8, Rows = 4;

var grid = new UIGrid();
for (int c = 0; c < Cols; c++) grid.AddColumn(GridLength.Pixel(SlotSize));
for (int r = 0; r < Rows; r++) grid.AddRow(GridLength.Pixel(SlotSize));

// Register slots and assign gamepad IDs via AssignGridNeighbors
UIElement[,] slots = new UIElement[Rows, Cols];
for (int r = 0; r < Rows; r++)
for (int c = 0; c < Cols; c++)
{
    var slot = new InventorySlot(pixel, slotTex);
    UIGrid.SetCell(slot, r, c);
    grid.Add(slot);
    slots[r, c] = slot;
}
UIFocusGridHelper.AssignGridNeighbors(slots, startId: 100, wrapH: true);
```
