namespace Fluent.Automation.Peers;

using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

/// <summary>
/// Automation peer for <see cref="RibbonGroupBox"/>.
/// </summary>
public class RibbonGroupBoxAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IScrollItemProvider
{
    private RibbonGroupHeaderAutomationPeer? headerPeer;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public RibbonGroupBoxAutomationPeer(RibbonGroupBox owner)
        : base(owner)
    {
        this.OwningGroup = owner;
    }

    private RibbonGroupBox OwningGroup { get; }

    private RibbonGroupHeaderAutomationPeer? HeaderPeer
    {
        get
        {
            if (this.headerPeer is null
                || !this.headerPeer.Owner.IsDescendantOf(this.OwningGroup))
            {
                if (this.OwningGroup.State == RibbonGroupBoxState.Collapsed)
                {
                    if (this.OwningGroup.CollapsedHeaderContentControl is not null)
                    {
                        this.headerPeer = new RibbonGroupHeaderAutomationPeer(this.OwningGroup.CollapsedHeaderContentControl);
                    }
                }
                else if (this.OwningGroup.Header is not null
                         && this.OwningGroup.HeaderContentControl is not null)
                {
                    this.headerPeer = new RibbonGroupHeaderAutomationPeer(this.OwningGroup.HeaderContentControl);
                }
            }

            return this.headerPeer;
        }
    }

    /// <inheritdoc />
    protected override List<AutomationPeer> GetChildrenCore()
    {
        var list = base.GetChildrenCore();

        if (this.HeaderPeer is not null)
        {
            if (list is null)
            {
                list = new List<AutomationPeer>(1);
            }

            list.Add(this.HeaderPeer);
        }

        return list;
    }

    /// <inheritdoc />
    protected override string GetClassNameCore()
    {
        return this.Owner.GetType().Name;
    }

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();

        if (string.IsNullOrEmpty(name))
        {
            name = (this.Owner as IHeaderedControl)?.Header as string;
        }

        return name ?? string.Empty;
    }

    /// <inheritdoc />
    public override object GetPattern(PatternInterface patternInterface)
    {
        switch (patternInterface)
        {
            case PatternInterface.ExpandCollapse:
                return this.IsCollapseOrExpandValid ? this : base.GetPattern(patternInterface);

            case PatternInterface.Scroll:
                return base.GetPattern(patternInterface);

            default:
                return base.GetPattern(patternInterface);
        }
    }

    /// <inheritdoc />
    protected override void SetFocusCore()
    {
    }

    #region IExpandCollapseProvider Members

    /// <inheritdoc />
    void IExpandCollapseProvider.Expand()
    {
        if (this.IsCollapseOrExpandValid == false)
        {
            return;
        }

        this.OwningGroup.IsDropDownOpen = true;
    }

    /// <inheritdoc />
    void IExpandCollapseProvider.Collapse()
    {
        if (this.IsCollapseOrExpandValid == false)
        {
            return;
        }

        this.OwningGroup.IsDropDownOpen = false;
    }

    /// <inheritdoc />
    ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState => this.IsCollapseOrExpandValid
        ? ExpandCollapseState.Collapsed
        : ExpandCollapseState.Expanded;

    private bool IsCollapseOrExpandValid => this.OwningGroup.State == RibbonGroupBoxState.Collapsed || this.OwningGroup.State == RibbonGroupBoxState.QuickAccess;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
    {
        this.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
            newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }

    #endregion

    #region IScrollItemProvider Members

    /// <inheritdoc />
    void IScrollItemProvider.ScrollIntoView()
    {
        this.OwningGroup.BringIntoView();
    }

    #endregion
}