using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Calc.ViewModels;

namespace Calc.Views;

public partial class MainWindow : Window
{
    private const double CompactWidth = 580;
    private const double ExpandedWidth = 900;

    private MainViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm is INotifyPropertyChanged old)
            old.PropertyChanged -= OnVmPropertyChanged;

        _vm = DataContext as MainViewModel;

        if (_vm is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += OnVmPropertyChanged;
            ApplyHistoryLayout(_vm.ShowHistory);
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ShowHistory) && _vm is not null)
            ApplyHistoryLayout(_vm.ShowHistory);
    }

    /// <summary>根据历史面板开关调整列宽与窗口尺寸</summary>
    private void ApplyHistoryLayout(bool show)
    {
        HistoryPanel.IsVisible = show;
        RootGrid.ColumnDefinitions[1].Width = show ? new GridLength(300) : new GridLength(0);
        RootGrid.ColumnSpacing = show ? 18 : 0;
        Width = show ? ExpandedWidth : CompactWidth;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (DataContext is not MainViewModel vm)
            return;

        // 焦点在表达式输入框内时，由 TextBox 自行处理字符输入，避免重复插入
        if (e.Source is TextBox)
            return;

        // 功能键
        switch (e.Key)
        {
            case Key.Enter or Key.Space:
                vm.EqualsCommand.Execute(null);
                e.Handled = true;
                return;
            case Key.Back:
                vm.BackspaceCommand.Execute(null);
                e.Handled = true;
                return;
            case Key.Escape or Key.Delete:
                vm.ClearCommand.Execute(null);
                e.Handled = true;
                return;
        }

        // 字符键映射
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        string? token = e.Key switch
        {
            Key.D0 or Key.NumPad0 => shift ? ")" : "0",
            Key.D1 or Key.NumPad1 => shift ? "!" : "1",
            Key.D2 or Key.NumPad2 => "2",
            Key.D3 or Key.NumPad3 => "3",
            Key.D4 or Key.NumPad4 => "4",
            Key.D5 or Key.NumPad5 => shift ? "%" : "5",
            Key.D6 or Key.NumPad6 => shift ? "^" : "6",
            Key.D7 or Key.NumPad7 => "7",
            Key.D8 => shift ? "×" : "8",
            Key.NumPad8 => "8",
            Key.D9 => shift ? "(" : "9",
            Key.NumPad9 => "9",
            Key.Decimal or Key.OemPeriod or Key.OemComma => ".",
            Key.OemPlus or Key.Add => "+",
            Key.OemMinus or Key.Subtract => "−",
            Key.Multiply => "×",
            Key.Divide or Key.OemQuestion => "÷",
            _ => null
        };

        if (token is not null)
        {
            vm.InsertCommand.Execute(token);
            e.Handled = true;
        }
    }
}
