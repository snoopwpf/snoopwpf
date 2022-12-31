namespace Snoop.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

[TemplatePart(Name = "PART_ItemsControl", Type = typeof(ItemsControl))]
public class NoSelectionComboBox : ComboBox
{
    static NoSelectionComboBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NoSelectionComboBox), new FrameworkPropertyMetadata(typeof(NoSelectionComboBox)));
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(NoSelectionComboBox), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty GroupHeaderTemplateProperty = DependencyProperty.Register(
        nameof(GroupHeaderTemplate), typeof(DataTemplate), typeof(NoSelectionComboBox), new PropertyMetadata(default(DataTemplate), OnGroupHeaderTemplateChanged));

    private static void OnGroupHeaderTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NoSelectionComboBox)d;
        control.UpdateGroupStyle();
    }

    public static readonly DependencyProperty ResetSettingsCommandProperty = DependencyProperty.Register(
        nameof(ResetSettingsCommand), typeof(ICommand), typeof(NoSelectionComboBox), new PropertyMetadata(default(ICommand)));

    public static readonly DependencyProperty ResetSettingsToolTipProperty = DependencyProperty.Register(
        nameof(ResetSettingsToolTip), typeof(string), typeof(NoSelectionComboBox), new PropertyMetadata("Reset to default settings"));

    private ItemsControl? itemsControl;

    public string? Header
    {
        get => (string?)this.GetValue(HeaderProperty);
        set => this.SetValue(HeaderProperty, value);
    }

    public DataTemplate? GroupHeaderTemplate
    {
        get => (DataTemplate?)this.GetValue(GroupHeaderTemplateProperty);
        set => this.SetValue(GroupHeaderTemplateProperty, value);
    }

    public ICommand? ResetSettingsCommand
    {
        get => (ICommand?)this.GetValue(ResetSettingsCommandProperty);
        set => this.SetValue(ResetSettingsCommandProperty, value);
    }

    public string ResetSettingsToolTip
    {
        get => (string)this.GetValue(ResetSettingsToolTipProperty);
        set => this.SetValue(ResetSettingsToolTipProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        this.itemsControl = this.GetTemplateChild("PART_ItemsControl") as ItemsControl;

        this.UpdateGroupStyle();
    }

    private void UpdateGroupStyle()
    {
        if (this.itemsControl is not null
            && this.GroupHeaderTemplate is not null)
        {
            this.itemsControl.GroupStyle.Clear();
            this.itemsControl.GroupStyle.Add(new GroupStyle { HeaderTemplate = this.GroupHeaderTemplate, Panel = defaultItemsPanelTemplate });
        }
    }

    private static readonly ItemsPanelTemplate defaultItemsPanelTemplate = CreateDefaultItemsPanelTemplate();

    private static ItemsPanelTemplate CreateDefaultItemsPanelTemplate()
    {
        var frameworkElementFactory = new FrameworkElementFactory(typeof(StackPanel));
        frameworkElementFactory.SetValue(StyleProperty, new Style(typeof(StackPanel), null));

        var template = new ItemsPanelTemplate(frameworkElementFactory);
        template.Seal();

        return template;
    }
}