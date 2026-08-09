using System.Collections;
using Finora.Application;
using Microsoft.Maui.Graphics;

namespace Finora.App;

public sealed class ReportBarChartView : GraphicsView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ReportBarChartView), null, propertyChanged: static (bindable, _, _) => ((ReportBarChartView)bindable).Invalidate());
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public ReportBarChartView() { Drawable = new BarChartDrawable(this); HeightRequest = 240; MinimumHeightRequest = 180; }

    private sealed class BarChartDrawable(ReportBarChartView owner) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var items = owner.ItemsSource?.Cast<object>().OfType<ReportPoint>().Take(12).ToList() ?? [];
            canvas.SaveState(); canvas.FontSize = 11;
            if (items.Count == 0) { canvas.FontColor = Colors.Gray; canvas.DrawString("No data for this period.", dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center); canvas.RestoreState(); return; }
            var max = Math.Max(1L, items.Max(x => Math.Abs(x.ValueMinor))); const float top = 12f; const float bottom = 46f; const float left = 18f; const float right = 10f; var plotHeight = Math.Max(20f, dirtyRect.Height - top - bottom); var plotWidth = Math.Max(20f, dirtyRect.Width - left - right); var slot = plotWidth / items.Count; var barWidth = Math.Max(5f, slot * 0.58f);
            var fill = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#71C4B8") : Color.FromArgb("#176B65"); var text = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Color.FromArgb("#102A43");
            canvas.FontColor = text; canvas.StrokeColor = Color.FromArgb("#829AB1"); canvas.StrokeSize = 1; canvas.DrawLine(left, top + plotHeight, dirtyRect.Width - right, top + plotHeight);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i]; var magnitude = Math.Abs(item.ValueMinor); var height = (float)((decimal)magnitude / max * (decimal)plotHeight); var x = left + i * slot + (slot - barWidth) / 2f; var y = top + plotHeight - height;
                canvas.FillColor = fill; canvas.FillRoundedRectangle(x, y, barWidth, Math.Max(1f, height), 4f); var label = item.Label.Length > 11 ? item.Label[..10] + "…" : item.Label; canvas.FontSize = 9; canvas.DrawString(label, x - slot * .2f, top + plotHeight + 6f, barWidth + slot * .4f, 34f, HorizontalAlignment.Center, VerticalAlignment.Top, TextFlow.OverflowBounds);
            }
            canvas.RestoreState();
        }
    }
}
