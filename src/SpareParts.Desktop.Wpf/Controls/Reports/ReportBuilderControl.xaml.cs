using SpareParts.Desktop.Wpf.ViewModels;
using SpareParts.Domain.Reports;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Linq;

namespace SpareParts.Desktop.Wpf
{
    public partial class ReportBuilderControl : UserControl
    {
        private INotifyPropertyChanged? _dataContextNotifier;
        private Border? _draggingCard;
        private UIElement? _dragHandle;
        private Point _dragOffset;
        private Point? _columnDragStartPoint;
        private ReportBuilderColumnDragPayload? _pendingColumnDragPayload;
        private bool _isUpdatingRelationConnector;
        private bool _isRelationConnectorQueued;

        public ReportBuilderControl()
        {
            InitializeComponent();

            SourceColumnsListBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(RelationViewer_ScrollChanged));
            TargetColumnsListBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(RelationViewer_ScrollChanged));

            Loaded += ReportBuilderControl_Loaded;
            DataContextChanged += ReportBuilderControl_DataContextChanged;
            Unloaded += ReportBuilderControl_Unloaded;
        }

        private void ReportBuilderControl_Loaded(object sender, RoutedEventArgs e)
        {
            AttachToDataContext();
            RebuildResultGrid();
            RebuildChartVisuals();
            RebuildJoinGraph();
            QueueRelationConnectorUpdate();
        }

        private void ReportBuilderControl_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachFromDataContext();
            ResetCardDrag();
            ResetColumnDrag();
        }

        private void ReportBuilderControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachFromDataContext();
            AttachToDataContext();
            RebuildResultGrid();
            RebuildChartVisuals();
            RebuildJoinGraph();
            QueueRelationConnectorUpdate();
        }

        private void AttachToDataContext()
        {
            if (DataContext is INotifyPropertyChanged notifier)
            {
                _dataContextNotifier = notifier;
                _dataContextNotifier.PropertyChanged += DataContext_PropertyChanged;
            }
        }

        private void DetachFromDataContext()
        {
            if (_dataContextNotifier != null)
            {
                _dataContextNotifier.PropertyChanged -= DataContext_PropertyChanged;
                _dataContextNotifier = null;
            }
        }

        private void DataContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReportBuilderViewModel.SelectedDesignerSourceTable)
                || e.PropertyName == nameof(ReportBuilderViewModel.SelectedDesignerTargetTable)
                || e.PropertyName == nameof(ReportBuilderViewModel.SelectedDesignerSourceColumn)
                || e.PropertyName == nameof(ReportBuilderViewModel.SelectedDesignerTargetColumn)
                || e.PropertyName == nameof(ReportBuilderViewModel.DesignerRelationPreviewText)
                || e.PropertyName == nameof(ReportBuilderViewModel.IsDesignerPopupOpen))
            {
                if (DataContext is ReportBuilderViewModel viewModel
                    && e.PropertyName == nameof(ReportBuilderViewModel.IsDesignerPopupOpen)
                    && viewModel.IsDesignerPopupOpen)
                {
                    ResetRelationWorkspace();
                }

                QueueRelationConnectorUpdate();
            }

            if (e.PropertyName == nameof(ReportBuilderViewModel.ResultView)
                || e.PropertyName == nameof(ReportBuilderViewModel.HasResult))
            {
                RebuildResultGrid();
            }

            if (e.PropertyName == nameof(ReportBuilderViewModel.ChartStatusText)
                || e.PropertyName == nameof(ReportBuilderViewModel.SelectedChartType)
                || e.PropertyName == nameof(ReportBuilderViewModel.SelectedChartCategoryColumnName)
                || e.PropertyName == nameof(ReportBuilderViewModel.SelectedChartValueColumnName))
            {
                RebuildChartVisuals();
            }

            if (e.PropertyName == nameof(ReportBuilderViewModel.JoinGraph))
            {
                RebuildJoinGraph();
            }
        }

        private void RelationCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueRelationConnectorUpdate();
        }

        private void RelationVisual_Loaded(object sender, RoutedEventArgs e)
        {
            QueueRelationConnectorUpdate();
        }

        private void RelationVisual_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueRelationConnectorUpdate();
        }

        private void RelationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            QueueRelationConnectorUpdate();
        }

        private void OpenTableDesignerTab_Click(object sender, RoutedEventArgs e)
        {
            ReportBuilderTabs.SelectedIndex = 1;
        }

        private void CustomLinksGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ReportBuilderViewModel viewModel
                || sender is not DataGrid grid
                || grid.SelectedItem is not ReportTableLinkDto link)
            {
                return;
            }

            if (viewModel.EditLinkCommand.CanExecute(link))
            {
                viewModel.EditLinkCommand.Execute(link);
            }
        }

        private void ResultDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ReportBuilderViewModel viewModel
                || !viewModel.OpenRowDetailsCommand.CanExecute(null))
            {
                return;
            }

            viewModel.OpenRowDetailsCommand.Execute(null);
        }

        private void RelationViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            QueueRelationConnectorUpdate();
        }

        private void RelationCardHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not UIElement handle)
            {
                return;
            }

            var card = FindRelationCard(sender as DependencyObject);
            if (card == null)
            {
                return;
            }

            _draggingCard = card;
            _dragHandle = handle;
            _dragOffset = e.GetPosition(card);
            _dragHandle.CaptureMouse();
            e.Handled = true;
        }

        private void RelationCardHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingCard == null
                || _dragHandle == null
                || !ReferenceEquals(sender, _dragHandle)
                || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var pointer = e.GetPosition(RelationCanvas);
            var leftLimit = Math.Max(12d, RelationCanvas.ActualWidth - _draggingCard.ActualWidth - 12d);
            var topLimit = Math.Max(12d, RelationCanvas.ActualHeight - _draggingCard.ActualHeight - 12d);
            var nextLeft = Math.Clamp(pointer.X - _dragOffset.X, 12d, leftLimit);
            var nextTop = Math.Clamp(pointer.Y - _dragOffset.Y, 12d, topLimit);

            Canvas.SetLeft(_draggingCard, nextLeft);
            Canvas.SetTop(_draggingCard, nextTop);
            UpdateRelationConnector();
            e.Handled = true;
        }

        private void RelationCardHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragHandle != null && ReferenceEquals(sender, _dragHandle))
            {
                _dragHandle.ReleaseMouseCapture();
            }

            ResetCardDrag();
            QueueRelationConnectorUpdate();
            e.Handled = true;
        }

        private void RelationCardHeader_LostMouseCapture(object sender, MouseEventArgs e)
        {
            ResetCardDrag();
            QueueRelationConnectorUpdate();
        }

        private void RelationColumnItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBoxItem item || item.DataContext is not ReportColumnDto column)
            {
                ResetColumnDrag();
                return;
            }

            var listBox = FindAncestor<ListBox>(item);
            var role = listBox?.Tag as string;
            if (string.IsNullOrWhiteSpace(role))
            {
                ResetColumnDrag();
                return;
            }

            if (listBox != null)
            {
                listBox.SelectedItem = column;
            }

            _columnDragStartPoint = e.GetPosition(this);
            _pendingColumnDragPayload = new ReportBuilderColumnDragPayload
            {
                Role = role,
                Column = column
            };

            QueueRelationConnectorUpdate();
        }

        private void RelationColumnItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_columnDragStartPoint == null
                || _pendingColumnDragPayload == null
                || e.LeftButton != MouseButtonState.Pressed
                || sender is not ListBoxItem item)
            {
                return;
            }

            var currentPoint = e.GetPosition(this);
            if (Math.Abs(currentPoint.X - _columnDragStartPoint.Value.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(currentPoint.Y - _columnDragStartPoint.Value.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            var dragData = new DataObject(typeof(ReportBuilderColumnDragPayload), _pendingColumnDragPayload);
            DragDrop.DoDragDrop(item, dragData, DragDropEffects.Link);
            ResetColumnDrag();
        }

        private void RelationColumnItem_DragOver(object sender, DragEventArgs e)
        {
            if (!TryGetDropContext(sender, e, out var payload, out var targetRole, out _))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = string.Equals(payload.Role, targetRole, StringComparison.OrdinalIgnoreCase)
                ? DragDropEffects.None
                : DragDropEffects.Link;
            e.Handled = true;
        }

        private void RelationColumnItem_Drop(object sender, DragEventArgs e)
        {
            if (!TryGetDropContext(sender, e, out var payload, out var targetRole, out var droppedColumn))
            {
                return;
            }

            if (string.Equals(payload.Role, targetRole, StringComparison.OrdinalIgnoreCase)
                || DataContext is not ReportBuilderViewModel viewModel)
            {
                e.Handled = true;
                return;
            }

            viewModel.ApplyDesignerColumnDrop(payload.Role, payload.Column, targetRole, droppedColumn);
            QueueRelationConnectorUpdate();
            e.Handled = true;
        }

        private void UpdateRelationConnector()
        {
            if (_isUpdatingRelationConnector)
            {
                return;
            }

            _isUpdatingRelationConnector = true;

            try
            {
                if (!IsLoaded || DataContext is not ReportBuilderViewModel viewModel)
                {
                    CollapseRelationConnector();
                    return;
                }

                if (!viewModel.IsDesignerPopupOpen)
                {
                    CollapseRelationConnector();
                    return;
                }

                var sourceItem = GetSelectedColumnItem(SourceColumnsListBox, viewModel.SelectedDesignerSourceColumn);
                var targetItem = GetSelectedColumnItem(TargetColumnsListBox, viewModel.SelectedDesignerTargetColumn);
                if (sourceItem == null || targetItem == null)
                {
                    CollapseRelationConnector();
                    return;
                }

                var sourcePoint = GetAnchorPoint(sourceItem, isSource: true);
                var targetPoint = GetAnchorPoint(targetItem, isSource: false);
                if (sourcePoint == null || targetPoint == null)
                {
                    CollapseRelationConnector();
                    return;
                }

                var horizontalOffset = Math.Max(72d, Math.Abs(targetPoint.Value.X - sourcePoint.Value.X) * 0.45d);
                var segment = new BezierSegment(
                    new Point(sourcePoint.Value.X + horizontalOffset, sourcePoint.Value.Y),
                    new Point(targetPoint.Value.X - horizontalOffset, targetPoint.Value.Y),
                    targetPoint.Value,
                    true);

                RelationConnectorPath.Data = new PathGeometry(new[]
                {
                    new PathFigure(sourcePoint.Value, new PathSegment[] { segment }, false)
                });
                RelationConnectorPath.Visibility = Visibility.Visible;

                PositionRelationDot(SourceRelationDot, sourcePoint.Value);
                PositionRelationDot(TargetRelationDot, targetPoint.Value);
            }
            finally
            {
                _isUpdatingRelationConnector = false;
            }
        }

        private void CollapseRelationConnector()
        {
            RelationConnectorPath.Data = null;
            RelationConnectorPath.Visibility = Visibility.Collapsed;
            SourceRelationDot.Visibility = Visibility.Collapsed;
            TargetRelationDot.Visibility = Visibility.Collapsed;
        }

        private void PositionRelationDot(FrameworkElement dot, Point point)
        {
            Canvas.SetLeft(dot, point.X - (dot.Width / 2d));
            Canvas.SetTop(dot, point.Y - (dot.Height / 2d));
            dot.Visibility = Visibility.Visible;
        }

        private ListBoxItem? GetSelectedColumnItem(ListBox listBox, ReportColumnDto? selectedColumn)
        {
            if (selectedColumn == null)
            {
                return null;
            }

            listBox.ScrollIntoView(selectedColumn);
            listBox.UpdateLayout();

            return listBox.ItemContainerGenerator.ContainerFromItem(selectedColumn) as ListBoxItem;
        }

        private Point? GetAnchorPoint(FrameworkElement element, bool isSource)
        {
            try
            {
                var relativePoint = isSource
                    ? new Point(element.ActualWidth, element.ActualHeight / 2d)
                    : new Point(0d, element.ActualHeight / 2d);

                return element.TransformToAncestor(RelationCanvas).Transform(relativePoint);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private void QueueRelationConnectorUpdate()
        {
            if (_isRelationConnectorQueued)
            {
                return;
            }

            _isRelationConnectorQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _isRelationConnectorQueued = false;
                UpdateRelationConnector();
            }), DispatcherPriority.Loaded);
        }

        private bool TryGetDropContext(object sender, DragEventArgs e, out ReportBuilderColumnDragPayload payload, out string targetRole, out ReportColumnDto droppedColumn)
        {
            payload = e.Data.GetData(typeof(ReportBuilderColumnDragPayload)) as ReportBuilderColumnDragPayload ?? new ReportBuilderColumnDragPayload();
            targetRole = string.Empty;
            droppedColumn = new ReportColumnDto();

            if (sender is not ListBoxItem item || item.DataContext is not ReportColumnDto targetColumn)
            {
                return false;
            }

            var listBox = FindAncestor<ListBox>(item);
            targetRole = listBox?.Tag as string ?? string.Empty;
            droppedColumn = targetColumn;

            return !string.IsNullOrWhiteSpace(payload.Role) && !string.IsNullOrWhiteSpace(targetRole);
        }

        private void ResetCardDrag()
        {
            _draggingCard = null;
            _dragHandle = null;
        }

        private void ResetColumnDrag()
        {
            _columnDragStartPoint = null;
            _pendingColumnDragPayload = null;
        }

        private void ResetRelationWorkspace()
        {
            if (!IsLoaded)
            {
                return;
            }

            Canvas.SetLeft(SourceTableCard, 36d);
            Canvas.SetTop(SourceTableCard, 32d);
            Canvas.SetLeft(TargetTableCard, Math.Max(356d, RelationCanvas.ActualWidth - TargetTableCard.Width - 40d));
            Canvas.SetTop(TargetTableCard, 128d);
            QueueRelationConnectorUpdate();
        }

        private void RebuildResultGrid()
        {
            if (!IsLoaded)
            {
                return;
            }

            ResultDataGrid.Columns.Clear();

            if (DataContext is not ReportBuilderViewModel viewModel || viewModel.ResultView?.Table == null)
            {
                ResultDataGrid.ItemsSource = null;
                return;
            }

            ResultDataGrid.ItemsSource = viewModel.ResultView;

            foreach (DataColumn column in viewModel.ResultView.Table.Columns)
            {
                var columnName = string.IsNullOrWhiteSpace(column.ColumnName) ? "Column" : column.ColumnName;
                ResultDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = columnName,
                    Binding = new Binding($"[{columnName}]")
                    {
                        Mode = BindingMode.OneWay,
                        TargetNullValue = string.Empty,
                        FallbackValue = string.Empty
                    },
                    Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells)
                });
            }
        }

        private void RebuildChartVisuals()
        {
            if (!IsLoaded || DataContext is not ReportBuilderViewModel viewModel)
            {
                return;
            }

            RebuildLineChart(viewModel);
            RebuildPieChart(viewModel);
        }

        private void RebuildLineChart(ReportBuilderViewModel viewModel)
        {
            LineChartCanvas.Children.Clear();

            if (viewModel.ChartPoints.Count == 0)
            {
                return;
            }

            var width = Math.Max(620d, LineChartCanvas.ActualWidth == 0 ? 620d : LineChartCanvas.ActualWidth);
            var height = Math.Max(300d, LineChartCanvas.ActualHeight == 0 ? 300d : LineChartCanvas.ActualHeight);
            LineChartCanvas.Width = width;
            LineChartCanvas.Height = height;

            var axisBrush = GetBrush("BorderBrush", Brushes.Gray);
            var accentBrush = GetBrush("AccentBrush", Brushes.Orange);
            var secondaryBrush = GetBrush("TextSecondaryBrush", Brushes.LightGray);
            var chartPoints = viewModel.ChartPoints.ToList();
            var maxValue = chartPoints.Max(point => point.Value);
            var drawableWidth = width - 80d;
            var drawableHeight = height - 70d;
            var left = 48d;
            var top = 20d;
            var bottom = top + drawableHeight;
            var step = chartPoints.Count <= 1 ? drawableWidth : drawableWidth / (chartPoints.Count - 1d);

            LineChartCanvas.Children.Add(new Line
            {
                X1 = left,
                Y1 = top,
                X2 = left,
                Y2 = bottom,
                Stroke = axisBrush,
                StrokeThickness = 1.2
            });
            LineChartCanvas.Children.Add(new Line
            {
                X1 = left,
                Y1 = bottom,
                X2 = left + drawableWidth,
                Y2 = bottom,
                Stroke = axisBrush,
                StrokeThickness = 1.2
            });

            var polyline = new Polyline
            {
                Stroke = accentBrush,
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            };

            for (var index = 0; index < chartPoints.Count; index++)
            {
                var point = chartPoints[index];
                var ratio = maxValue == 0 ? 0 : (double)(point.Value / maxValue);
                var x = left + (step * index);
                var y = bottom - (drawableHeight * ratio);
                polyline.Points.Add(new Point(x, y));

                var dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = accentBrush,
                    Stroke = Brushes.White,
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(dot, x - 5d);
                Canvas.SetTop(dot, y - 5d);
                LineChartCanvas.Children.Add(dot);

                var label = new TextBlock
                {
                    Text = point.Label,
                    Foreground = secondaryBrush,
                    FontSize = 10,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Width = Math.Max(54d, step),
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(label, x - (label.Width / 2d));
                Canvas.SetTop(label, bottom + 8d);
                LineChartCanvas.Children.Add(label);
            }

            LineChartCanvas.Children.Add(polyline);
        }

        private void RebuildPieChart(ReportBuilderViewModel viewModel)
        {
            PieChartCanvas.Children.Clear();

            var points = viewModel.ChartPoints.ToList();
            if (points.Count == 0)
            {
                return;
            }

            var width = Math.Max(360d, PieChartCanvas.ActualWidth == 0 ? 360d : PieChartCanvas.ActualWidth);
            var height = Math.Max(300d, PieChartCanvas.ActualHeight == 0 ? 300d : PieChartCanvas.ActualHeight);
            PieChartCanvas.Width = width;
            PieChartCanvas.Height = height;

            var center = new Point(150d, 150d);
            var radius = 104d;
            var palette = BuildChartPalette();
            var total = points.Sum(point => point.Value);
            if (total == 0)
            {
                return;
            }

            double angle = -90d;
            for (var index = 0; index < points.Count; index++)
            {
                var point = points[index];
                var sweep = (double)(point.Value / total) * 360d;
                var slice = BuildPieSlice(center, radius, angle, sweep, palette[index % palette.Length]);
                PieChartCanvas.Children.Add(slice);

                var midAngle = angle + (sweep / 2d);
                var radians = midAngle * Math.PI / 180d;
                var label = new TextBlock
                {
                    Text = $"{point.Label} ({point.Percentage:P0})",
                    Foreground = GetBrush("TextSecondaryBrush", Brushes.LightGray),
                    FontSize = 10,
                    Width = 160,
                    TextWrapping = TextWrapping.Wrap
                };
                Canvas.SetLeft(label, center.X + Math.Cos(radians) * (radius + 16d));
                Canvas.SetTop(label, center.Y + Math.Sin(radians) * (radius + 16d));
                PieChartCanvas.Children.Add(label);
                angle += sweep;
            }
        }

        private void RebuildJoinGraph()
        {
            if (!IsLoaded || DataContext is not ReportBuilderViewModel viewModel)
            {
                return;
            }

            JoinGraphCanvas.Children.Clear();
            if (viewModel.JoinGraph == null || viewModel.JoinGraph.Nodes.Count == 0)
            {
                return;
            }

            var nodePositions = new Dictionary<string, Rect>(StringComparer.OrdinalIgnoreCase);
            var groups = viewModel.JoinGraph.Nodes
                .OrderBy(node => node.SchemaName)
                .ThenBy(node => node.DisplayName)
                .GroupBy(node => node.SchemaName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var x = 36d;
            var yPadding = 26d;
            var nodeWidth = 190d;
            var nodeHeight = 86d;
            var maxBottom = 0d;

            foreach (var group in groups)
            {
                var y = 42d;
                foreach (var node in group)
                {
                    var rect = new Rect(x, y, nodeWidth, nodeHeight);
                    nodePositions[node.TableKey] = rect;
                    y += nodeHeight + yPadding;
                    maxBottom = Math.Max(maxBottom, rect.Bottom);
                }

                x += nodeWidth + 90d;
            }

            JoinGraphCanvas.Width = Math.Max(860d, x);
            JoinGraphCanvas.Height = Math.Max(420d, maxBottom + 60d);

            foreach (var edge in viewModel.JoinGraph.Edges)
            {
                if (!nodePositions.TryGetValue(edge.SourceTableKey, out var source)
                    || !nodePositions.TryGetValue(edge.TargetTableKey, out var target))
                {
                    continue;
                }

                var sourcePoint = new Point(source.Right, source.Top + (source.Height / 2d));
                var targetPoint = new Point(target.Left, target.Top + (target.Height / 2d));
                var midX = sourcePoint.X + ((targetPoint.X - sourcePoint.X) / 2d);
                var path = new Path
                {
                    Stroke = edge.MayDuplicateRows ? Brushes.OrangeRed : GetBrush("AccentBrush", Brushes.Orange),
                    StrokeThickness = edge.MayDuplicateRows ? 2.6 : 2.2,
                    Data = new PathGeometry(new[]
                    {
                        new PathFigure(sourcePoint, new PathSegment[]
                        {
                            new BezierSegment(
                                new Point(midX, sourcePoint.Y),
                                new Point(midX, targetPoint.Y),
                                targetPoint,
                                true)
                        }, false)
                    })
                };
                JoinGraphCanvas.Children.Add(path);

                var label = new TextBlock
                {
                    Text = edge.CardinalityLabel,
                    Foreground = edge.MayDuplicateRows ? Brushes.OrangeRed : GetBrush("TextSecondaryBrush", Brushes.LightGray),
                    Background = GetBrush("CardBackgroundBrush", Brushes.Transparent),
                    FontSize = 10,
                    Padding = new Thickness(4, 2, 4, 2)
                };
                Canvas.SetLeft(label, midX - 24d);
                Canvas.SetTop(label, ((sourcePoint.Y + targetPoint.Y) / 2d) - 12d);
                JoinGraphCanvas.Children.Add(label);
            }

            foreach (var node in viewModel.JoinGraph.Nodes)
            {
                if (!nodePositions.TryGetValue(node.TableKey, out var rect))
                {
                    continue;
                }

                var border = new Border
                {
                    Width = rect.Width,
                    Height = rect.Height,
                    Background = GetBrush("CardBackgroundBrush", Brushes.DimGray),
                    BorderBrush = GetBrush("BorderBrush", Brushes.Gray),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(12)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock
                {
                    Text = node.DisplayName,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = GetBrush("TextPrimaryBrush", Brushes.White),
                    FontSize = 13
                });
                stack.Children.Add(new TextBlock
                {
                    Text = node.TableKey,
                    Foreground = GetBrush("TextSecondaryBrush", Brushes.LightGray),
                    FontSize = 10,
                    Margin = new Thickness(0, 4, 0, 0)
                });
                stack.Children.Add(new TextBlock
                {
                    Text = $"{node.ColumnCount:N0} column(s)",
                    Foreground = GetBrush("AccentBrush", Brushes.Orange),
                    FontSize = 10,
                    Margin = new Thickness(0, 8, 0, 0)
                });
                border.Child = stack;
                Canvas.SetLeft(border, rect.Left);
                Canvas.SetTop(border, rect.Top);
                JoinGraphCanvas.Children.Add(border);
            }
        }

        private Path BuildPieSlice(Point center, double radius, double startAngle, double sweepAngle, Brush fill)
        {
            var startRadians = startAngle * Math.PI / 180d;
            var endRadians = (startAngle + sweepAngle) * Math.PI / 180d;
            var startPoint = new Point(center.X + Math.Cos(startRadians) * radius, center.Y + Math.Sin(startRadians) * radius);
            var endPoint = new Point(center.X + Math.Cos(endRadians) * radius, center.Y + Math.Sin(endRadians) * radius);

            var figure = new PathFigure { StartPoint = center };
            figure.Segments.Add(new LineSegment(startPoint, true));
            figure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), 0, sweepAngle > 180d, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(center, true));

            return new Path
            {
                Fill = fill,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Data = new PathGeometry(new[] { figure })
            };
        }

        private Brush[] BuildChartPalette()
        {
            return new[]
            {
                GetBrush("AccentBrush", Brushes.Orange),
                new SolidColorBrush(Color.FromRgb(78, 176, 211)),
                new SolidColorBrush(Color.FromRgb(76, 160, 114)),
                new SolidColorBrush(Color.FromRgb(214, 120, 66)),
                new SolidColorBrush(Color.FromRgb(161, 112, 194)),
                new SolidColorBrush(Color.FromRgb(206, 87, 123))
            };
        }

        private Brush GetBrush(string resourceKey, Brush fallback)
        {
            return TryFindResource(resourceKey) as Brush ?? fallback;
        }

        private Border? FindRelationCard(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is Border border
                    && (ReferenceEquals(border, SourceTableCard) || ReferenceEquals(border, TargetTableCard)))
                {
                    return border;
                }

                element = GetParent(element);
            }

            return null;
        }

        private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T match)
                {
                    return match;
                }

                element = GetParent(element);
            }

            return null;
        }

        private static DependencyObject? GetParent(DependencyObject element)
        {
            if (element is Visual || element is Visual3D)
            {
                return VisualTreeHelper.GetParent(element);
            }

            return LogicalTreeHelper.GetParent(element);
        }
    }
}
