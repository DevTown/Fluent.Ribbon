﻿// ReSharper disable once CheckNamespace
namespace Fluent;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Fluent.Extensibility;
using Fluent.Internal;
using Fluent.Internal.KnownBoxes;

/// <summary>
/// Represent panel for group box panel
/// </summary>
[ContentProperty(nameof(Children))]
[StyleTypedProperty(Property = nameof(SeparatorStyle), StyleTargetType = typeof(Separator))]
public class RibbonToolBar : RibbonControl, IRibbonSizeChangedSink, ISimplifiedStateControl
{
    #region Fields

    // User defined children
    // User defined layout definitions

    // Actual children
    private readonly List<FrameworkElement> actualChildren = new();
    // Designates that rebuilding of visual & logical children is required
    private bool rebuildVisualAndLogicalChildren = true;

    #endregion

    #region Properties

    #region Separator Style

    /// <summary>
    /// Gets or sets style for the separator
    /// </summary>
    public Style? SeparatorStyle
    {
        get { return (Style?)this.GetValue(SeparatorStyleProperty); }
        set { this.SetValue(SeparatorStyleProperty, value); }
    }

    /// <summary>Identifies the <see cref="SeparatorStyle"/> dependency property.</summary>
    public static readonly DependencyProperty SeparatorStyleProperty =
        DependencyProperty.Register(nameof(SeparatorStyle), typeof(Style),
            typeof(RibbonToolBar), new PropertyMetadata(OnSeparatorStyleChanged));

    private static void OnSeparatorStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var toolBar = (RibbonToolBar)d;
        toolBar.rebuildVisualAndLogicalChildren = true;
        toolBar.InvalidateMeasure();
    }

    #endregion

    #region IsSimplified

    /// <summary>
    /// Gets or sets whether or not the ribbon is in Simplified mode
    /// </summary>
    public bool IsSimplified
    {
        get { return (bool)this.GetValue(IsSimplifiedProperty); }
        private set { this.SetValue(IsSimplifiedPropertyKey, BooleanBoxes.Box(value)); }
    }

    private static readonly DependencyPropertyKey IsSimplifiedPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IsSimplified), typeof(bool), typeof(RibbonToolBar), new PropertyMetadata(BooleanBoxes.FalseBox, OnIsSimplifiedChanged));

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty = IsSimplifiedPropertyKey.DependencyProperty;

    /// <summary>
    /// Called when <see cref="IsSimplified"/> changes.
    /// </summary>
    private static void OnIsSimplifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonToolBar ribbonToolBar)
        {
            ribbonToolBar.rebuildVisualAndLogicalChildren = true;

            var isSimplified = (bool)e.NewValue;
            ribbonToolBar.UpdateChildIsSimplified(isSimplified);
        }
    }

    private void UpdateChildIsSimplified(bool isSimplified)
    {
        foreach (var element in this.Children)
        {
            this.UpdateIsSimplifiedOfUIElement(element, isSimplified);
        }

        // Use SizeDefinition or SimplifiedSizeDefinition depending on IsSimplified property to determine the child size.
        this.InvalidateMeasure();
    }

    private void UpdateIsSimplifiedOfUIElement(DependencyObject? element, bool isSimplified)
    {
        if (element is null)
        {
            return;
        }

        if (element is Panel panel)
        {
            for (var i = 0; i < panel.Children.Count; i++)
            {
                this.UpdateIsSimplifiedOfUIElement(panel.Children[i], isSimplified);
            }
        }

        if (element is ContentPresenter)
        {
            element = UIHelper.GetFirstVisualChild(element) ?? element;
        }

        if (element is ISimplifiedStateControl simplifiedStateControl)
        {
            simplifiedStateControl.UpdateSimplifiedState(isSimplified);
        }
    }
    #endregion

    /// <summary>
    /// Gets children
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public ObservableCollection<FrameworkElement> Children { get; } = new();

    /// <summary>
    /// Gets particular rules  for layout in this group box panel
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public ObservableCollection<RibbonToolBarLayoutDefinition> LayoutDefinitions { get; } = new();

    #endregion

    #region Logical & Visual Tree

    /// <inheritdoc />
    protected override int VisualChildrenCount
    {
        get
        {
            if (this.LayoutDefinitions.Count == 0)
            {
                return this.Children.Count;
            }

            if (this.rebuildVisualAndLogicalChildren)
            {
                //TODO: Exception during theme changing
                // UpdateLayout();
                this.InvalidateMeasure();
            }

            return this.actualChildren.Count;
        }
    }

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index)
    {
        if (this.LayoutDefinitions.Count == 0)
        {
            return this.Children[index];
        }

        if (this.rebuildVisualAndLogicalChildren)
        {
            // UpdateLayout();
            this.InvalidateMeasure();
        }

        return this.actualChildren[index];
    }

    /// <inheritdoc />
    protected override IEnumerator LogicalChildren
    {
        get
        {
            var baseEnumerator = base.LogicalChildren;
            while (baseEnumerator?.MoveNext() == true)
            {
                yield return baseEnumerator.Current;
            }

            if (this.Icon is not null)
            {
                yield return this.Icon;
            }

            foreach (var child in this.Children)
            {
                yield return child;
            }
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Static constructor
    /// </summary>
    static RibbonToolBar()
    {
        // Override default style
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonToolBar), new FrameworkPropertyMetadata(typeof(RibbonToolBar)));
        // Disable QAT for this control
        CanAddToQuickAccessToolBarProperty.OverrideMetadata(typeof(RibbonToolBar), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public RibbonToolBar()
    {
        this.Children.CollectionChanged += this.OnChildrenCollectionChanged;
        this.LayoutDefinitions.CollectionChanged += this.OnLayoutDefinitionsChanged;
    }

    private void OnLayoutDefinitionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.rebuildVisualAndLogicalChildren = true;
        this.InvalidateMeasure();
    }

    private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Children have changed, reset layouts
        this.rebuildVisualAndLogicalChildren = true;
        this.InvalidateMeasure();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets current used layout definition (or null if no present definitions)
    /// </summary>
    /// <returns>Layout definition or null</returns>
    internal RibbonToolBarLayoutDefinition? GetCurrentLayoutDefinition()
    {
        if (this.LayoutDefinitions.Count == 0)
        {
            return null;
        }

        if (this.LayoutDefinitions.Count == 1)
        {
            return this.LayoutDefinitions[0];
        }

        var size = this.Size;
        var map = new Dictionary<RibbonControlSize, RibbonToolBarLayoutDefinition>();
        foreach (var definition in this.LayoutDefinitions)
        {
            if (definition.ForSimplified != this.IsSimplified)
            {
                continue;
            }

            var definitionSize = definition.Size;
            if (definitionSize == size)
            {
                return definition;
            }

            map[definitionSize] = definition;
        }

        if (map.Count == 0)
        {
            return null;
        }

        // find the closest definition if no matching definition exists
        return size switch
        {
            RibbonControlSize.Large => map.ContainsKey(RibbonControlSize.Middle)
                ? map[RibbonControlSize.Middle]
                : (map.ContainsKey(RibbonControlSize.Small)
                    ? map[RibbonControlSize.Small]
                    : this.LayoutDefinitions[0]),

            RibbonControlSize.Middle => map.ContainsKey(RibbonControlSize.Small)
                ? map[RibbonControlSize.Small]
                : (map.ContainsKey(RibbonControlSize.Large)
                    ? map[RibbonControlSize.Large]
                    : this.LayoutDefinitions[0]),

            RibbonControlSize.Small => map.ContainsKey(RibbonControlSize.Middle)
                ? map[RibbonControlSize.Middle]
                : (map.ContainsKey(RibbonControlSize.Large)
                    ? map[RibbonControlSize.Large]
                    : this.LayoutDefinitions[0]),

            _ => this.LayoutDefinitions[0]
        };
    }

    #endregion

    #region Size Property Changing

    /// <inheritdoc />
    public void OnSizePropertyChanged(RibbonControlSize previous, RibbonControlSize current)
    {
        this.rebuildVisualAndLogicalChildren = true;
        this.InvalidateMeasure();
    }

    #endregion

    #region Implementation of ISimplifiedStateControl

    /// <inheritdoc />
    void ISimplifiedStateControl.UpdateSimplifiedState(bool isSimplified)
    {
        this.IsSimplified = isSimplified;
    }

    #endregion

    #region Layout Overriding

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var layoutDefinition = this.GetCurrentLayoutDefinition();

        // Rebuilding actual children (visual & logical)
        if (this.rebuildVisualAndLogicalChildren)
        {
            // Clear previous children
            foreach (var child in this.actualChildren)
            {
                var controlGroup = child as RibbonToolBarControlGroup;
                controlGroup?.Items.Clear();

                this.RemoveVisualChild(child);
                this.RemoveLogicalChild(child);
            }

            this.actualChildren.Clear();
            this.cachedControlGroups.Clear();
        }

        if (layoutDefinition is null)
        {
            if (this.rebuildVisualAndLogicalChildren)
            {
                // If default layout is used add all children
                foreach (var child in this.Children)
                {
                    this.actualChildren.Add(child);
                    this.AddVisualChild(child);
                    this.AddLogicalChild(child);
                }

                this.rebuildVisualAndLogicalChildren = false;
            }

            return this.WrapPanelLayuot(availableSize, true);
        }
        else
        {
            var result = this.CustomLayout(layoutDefinition, availableSize, true, this.rebuildVisualAndLogicalChildren);
            this.rebuildVisualAndLogicalChildren = false;
            return result;
        }
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var layoutDefinition = this.GetCurrentLayoutDefinition();

        if (layoutDefinition is null)
        {
            return this.WrapPanelLayuot(finalSize, false);
        }

        return this.CustomLayout(layoutDefinition, finalSize, false, false);
    }

    #region Wrap Panel Layout

    /// <summary>
    /// Unified method for wrap panel logic
    /// </summary>
    /// <param name="availableSize">Available or final size</param>
    /// <param name="measure">Pass true if measure required; pass false if arrange required</param>
    /// <returns>Final size</returns>
    private Size WrapPanelLayuot(Size availableSize, bool measure)
    {
        var arrange = !measure;
        var availableHeight = double.IsPositiveInfinity(availableSize.Height)
            ? 0
            : availableSize.Height;

        double currentheight = 0;
        double columnWidth = 0;

        double resultWidth = 0;
        double resultHeight = 0;

        var ribbonToolBarSize = RibbonProperties.GetSize(this);
        foreach (var child in this.Children)
        {
            // Measuring
            if (measure)
            {
                // Apply Control Definition Properties
                RibbonProperties.SetAppropriateSize(child, ribbonToolBarSize);
                child.Measure(SizeConstants.Infinite);
            }

            if (currentheight + child.DesiredSize.Height > availableHeight)
            {
                // Move to the next column
                resultHeight = Math.Max(resultHeight, currentheight);
                resultWidth += columnWidth;
                currentheight = 0;
                columnWidth = 0;
            }

            // Arranging
            if (arrange)
            {
                child.Arrange(new Rect(resultWidth, currentheight, child.DesiredSize.Width, child.DesiredSize.Height));
            }

            columnWidth = Math.Max(columnWidth, child.DesiredSize.Width);
            currentheight += child.DesiredSize.Height;
            resultHeight = Math.Max(resultHeight, currentheight);
        }

        return new Size(resultWidth + columnWidth, resultHeight);
    }

    #endregion

    #region Control and Group Creation from a Definition

    private FrameworkElement? GetControl(RibbonToolBarControlDefinition controlDefinition)
    {
        var name = controlDefinition.Target;
        return this.Children.FirstOrDefault(x => x.Name == name);
    }

    private readonly Dictionary<object, RibbonToolBarControlGroup> cachedControlGroups = new();

    private RibbonToolBarControlGroup GetControlGroup(RibbonToolBarControlGroupDefinition controlGroupDefinition)
    {
        if (this.cachedControlGroups.TryGetValue(controlGroupDefinition, out var controlGroup))
        {
            return controlGroup;
        }

        controlGroup = new RibbonToolBarControlGroup();

        // Add items to the group
        foreach (var child in controlGroupDefinition.Children)
        {
            controlGroup.Items.Add(this.GetControl(child));
        }

        this.cachedControlGroups.Add(controlGroupDefinition, controlGroup);
        return controlGroup;
    }

    #endregion

    #region Custom Layout

    // Cached separators (clear & set in Measure pass)
    private readonly Dictionary<int, Separator> separatorCache = new();

    /// <summary>
    /// Layout logic for the given layout definition
    /// </summary>
    /// <param name="layoutDefinition">Current layout definition</param>
    /// <param name="availableSize">Available or final size</param>
    /// <param name="measure">Pass true if measure required; pass false if arrange required</param>
    /// <param name="addchildren">Determines whether we have to add children to the logical and visual tree</param>
    /// <returns>Final size</returns>
    private Size CustomLayout(RibbonToolBarLayoutDefinition layoutDefinition, Size availableSize, bool measure, bool addchildren)
    {
        var arrange = !measure;
        var availableHeight = double.IsPositiveInfinity(availableSize.Height)
            ? 0
            : availableSize.Height;

        // Clear separator cahce
        if (addchildren)
        {
            this.separatorCache.Clear();
        }

        // Get the first control and measure, its height accepts as row height
        var rowHeight = this.GetRowHeight(layoutDefinition);

        // Calculate whitespace
        var rowCountInColumn = Math.Min(layoutDefinition.RowCount, layoutDefinition.Rows.Count);
        var whitespace = (availableHeight - (rowCountInColumn * rowHeight)) / (rowCountInColumn + 1);

        double y = 0;
        double currentRowBegin = 0;
        double currentMaxX = 0;
        double maxy = 0;

        for (var rowIndex = 0; rowIndex < layoutDefinition.Rows.Count; rowIndex++)
        {
            var row = layoutDefinition.Rows[rowIndex];

            var x = currentRowBegin;

            if (rowIndex % rowCountInColumn == 0)
            {
                // Reset vars at new column
                x = currentRowBegin = currentMaxX;
                y = 0;

                if (rowIndex != 0)
                {
                    #region Add separator

                    if (this.separatorCache.TryGetValue(rowIndex, out var separator) == false)
                    {
                        separator = new Separator
                        {
                            Style = this.SeparatorStyle
                        };
                        this.separatorCache.Add(rowIndex, separator);
                    }

                    if (measure)
                    {
                        separator.Height = availableHeight - separator.Margin.Bottom - separator.Margin.Top;
                        separator.Measure(availableSize);
                    }

                    if (arrange)
                    {
                        separator.Arrange(new Rect(x, y, separator.DesiredSize.Width, separator.DesiredSize.Height));
                    }

                    x += separator.DesiredSize.Width;

                    if (addchildren)
                    {
                        // Add control in the children
                        this.AddVisualChild(separator);
                        this.AddLogicalChild(separator);
                        this.actualChildren.Add(separator);
                    }

                    #endregion
                }
            }

            y += whitespace;

            // Measure & arrange new row
            for (var i = 0; i < row.Children.Count; i++)
            {
                // Control Definition Case
                if (row.Children[i] is RibbonToolBarControlDefinition ribbonToolBarControlDefinition)
                {
                    var control = this.GetControl(ribbonToolBarControlDefinition);

                    if (control is null)
                    {
                        continue;
                    }

                    if (addchildren)
                    {
                        // Add control in the children
                        this.AddVisualChild(control);
                        this.AddLogicalChild(control);
                        this.actualChildren.Add(control);
                    }

                    if (measure)
                    {
                        // Apply Control Definition Properties
                        RibbonProperties.SetAppropriateSize(control, RibbonProperties.GetSize(ribbonToolBarControlDefinition));
                        control.Width = ribbonToolBarControlDefinition.Width;
                        control.Measure(availableSize);
                    }

                    if (arrange)
                    {
                        control.Arrange(new Rect(x, y, control.DesiredSize.Width, control.DesiredSize.Height));
                    }

                    x += control.DesiredSize.Width;
                }

                // Control Group Definition Case
                if (row.Children[i] is RibbonToolBarControlGroupDefinition ribbonToolBarControlGroupDefinition)
                {
                    var control = this.GetControlGroup(ribbonToolBarControlGroupDefinition);

                    if (addchildren)
                    {
                        // Add control in the children
                        this.AddVisualChild(control);
                        this.AddLogicalChild(control);
                        this.actualChildren.Add(control);
                    }

                    if (measure)
                    {
                        // Apply Control Definition Properties
                        control.IsFirstInRow = i == 0;
                        control.IsLastInRow = i == row.Children.Count - 1;
                        control.Measure(availableSize);
                    }

                    if (arrange)
                    {
                        control.Arrange(new Rect(x, y, control.DesiredSize.Width, control.DesiredSize.Height));
                    }

                    x += control.DesiredSize.Width;
                }
            }

            y += rowHeight;

            if (currentMaxX < x)
            {
                currentMaxX = x;
            }

            if (maxy < y)
            {
                maxy = y;
            }
        }

        return new Size(currentMaxX, Math.Max(maxy + whitespace, 0));
    }

    // Get the first control and measure, its height accepts as row height
    private double GetRowHeight(RibbonToolBarLayoutDefinition layoutDefinition)
    {
        const double defaultRowHeight = 0;

        foreach (var row in layoutDefinition.Rows)
        {
            foreach (var item in row.Children)
            {
                var controlDefinition = item as RibbonToolBarControlDefinition;
                var controlGroupDefinition = item as RibbonToolBarControlGroupDefinition;
                FrameworkElement? control = null;

                if (controlDefinition is not null)
                {
                    control = this.GetControl(controlDefinition);
                }
                else if (controlGroupDefinition is not null)
                {
                    control = this.GetControlGroup(controlGroupDefinition);
                }

                if (control is null)
                {
                    return defaultRowHeight;
                }

                control.Measure(SizeConstants.Infinite);

                return control.DesiredSize.Height;
            }
        }

        return defaultRowHeight;
    }

    #endregion

    #endregion

    #region QAT Support

    /// <inheritdoc />
    public override FrameworkElement CreateQuickAccessItem()
    {
        throw new NotImplementedException();
    }

    #endregion
}