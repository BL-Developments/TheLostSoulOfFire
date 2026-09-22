---
name: monogame-ui-grid
description: MonoGame UI XAML-style Grid layout — row and column definitions with pixel (fixed), auto (content-sized), and star (*) proportional sizing. Children are placed at specific Grid.Row / Grid.Column coordinates with optional span. Use this skill whenever the user asks about a grid layout, table layout, XAML Grid, rows and columns in UI, grid with star sizing, proportional columns, "how do I arrange UI elements in a table", or any 2D grid layout in MonoGame UI.
---

# MonoGame UI Grid Layout

The `GridLayout` container positions children at explicit row/column coordinates — identical to XAML's `Grid`. It is the most flexible layout container for complex UIs (settings screens, inventory tables, form layouts). For complete code templates, read `references/ui-grid.md`.

## Assumed Contract

Extends `UIContainer` from `monogame-ui-core`. Implements `Measure`/`Arrange` from `monogame-ui-layout`. Follows the same no-LINQ, no-allocation rules inside layout methods.

## Column and Row Definitions

Three sizing modes, same as XAML. Use the static shorthand methods on `GridTrack`:

| Mode | Syntax | Behavior |
|------|--------|---------|
| Fixed | `GridTrack.Fixed(n)` | Exact pixel size |
| Auto | `GridTrack.Auto()` | Sized to the widest/tallest child in that column/row |
| Star | `GridTrack.Star(n)` | Proportional share of remaining space after fixed + auto |

Add tracks to `ColumnDefinitions` / `RowDefinitions` before arranging children:

```csharp
var grid = new GridLayout();
grid.ColumnDefinitions.Add(GridTrack.Fixed(120));   // fixed 120px label column
grid.ColumnDefinitions.Add(GridTrack.Star(1));      // remaining width → inputs
grid.RowDefinitions.Add(GridTrack.Auto());          // as tall as the tallest child
grid.RowDefinitions.Add(GridTrack.Auto());
grid.RowDefinitions.Add(GridTrack.Fixed(40));       // fixed footer row
```

## Placing Children

Children are placed with `SetCell(child, row, col)` — an **instance method** on `GridLayout`:

```csharp
var nameLabel = new Label { Text = "Name:" };
grid.SetCell(nameLabel, row: 0, col: 0);
grid.Add(nameLabel);

var nameInput = new TextBox(font, pixel, Window);
grid.SetCell(nameInput, row: 0, col: 1);
grid.Add(nameInput);
```

`SetCell` stores `row`, `col`, `rowSpan`, `colSpan` in an internal dictionary keyed by child.

## Column/Row Span

```csharp
grid.SetCell(titleLabel, row: 0, col: 0, rowSpan: 1, colSpan: 2);  // spans both columns
```

## Measure Algorithm

1. Measure all **Auto** columns: for each auto column `c`, measure every child in column `c`, take the max `DesiredSize.X`.
2. Measure all **Auto** rows similarly (max `DesiredSize.Y` per row).
3. Sum fixed + auto column widths. Remaining width → distribute among star columns proportionally.
4. Same for rows.
5. Re-measure children with their final cell size as `available` (for children that expand to fill their cell).

No LINQ — use indexed loops over `ColumnDefinitions`, `RowDefinitions`, and `Children`.

## Arrange Algorithm

1. Compute each column's X origin and width (from the resolved sizes above).
2. Compute each row's Y origin and height.
3. For each child: look up its `(row, col, rowSpan, colSpan)`, compute its cell rectangle as the union of its spanned cells, call `child.Arrange(cellRect)`.

## Common Patterns

**Settings form (label + input pairs):**
```
Col 0 (Auto): Labels
Col 1 (Star): Inputs fill the rest
Row per setting (Auto each)
```

**Inventory grid (fixed cells):**
```
All columns: Pixel(64)
All rows:    Pixel(64)
Each slot: UIElement at [row, col]
```

**HUD corners (2×2 grid):**
```
Col 0 (Star), Col 1 (Star)
Row 0 (Star), Row 1 (Star)
Top-left: health display
Top-right: minimap
Bottom-left: ability bar
Bottom-right: score
```

## Cell Alignment

After the cell rectangle is computed, children can align within it using `CellHAlign` (`HAlign`) and `CellVAlign` (`VAlign`) properties on the `GridLayout`:

```csharp
grid.CellHAlign = HAlign.Center;
grid.CellVAlign = VAlign.Middle;
```

Default is `HAlign.Left` / `VAlign.Top` which stretches children to fill their cell.

## Anti-Patterns

- Never use LINQ or `foreach` inside `Measure` or `Arrange` — indexed `for` loops only.
- Never manually position children by pixel offset when the grid would handle it — let Arrange assign `Bounds`.
- Do not rebuild `ColumnDefinitions` or `RowDefinitions` every frame — modify them only when the layout needs to change, then call `Invalidate()`.
- Do not span a child past the grid's column/row count — clamp spans in `SetCell`.

## Reference

Complete `GridLayout`, `GridTrack`, `GridSizeMode`, and `Measure`/`Arrange` implementations: `references/ui-grid.md`.
