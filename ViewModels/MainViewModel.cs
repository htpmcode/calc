using System;
using System.Collections.ObjectModel;
using Calc.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Calc.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ExpressionEvaluator _evaluator = new();

    [ObservableProperty]
    public partial string Expression { get; set; } = "";

    [ObservableProperty]
    public partial string Result { get; set; } = "0";

    [ObservableProperty]
    public partial bool DegreeMode { get; set; } = false;

    [ObservableProperty]
    public partial string ModeText { get; set; } = "RAD";

    partial void OnDegreeModeChanged(bool value) => ModeText = value ? "DEG" : "RAD";

    public ObservableCollection<string> History { get; } = new();

    /// <summary>按钮输入：插入文本到表达式末尾</summary>
    [RelayCommand]
    private void Insert(string token)
    {
        Expression += token;
    }

    /// <summary>退格：删除表达式末尾一个字符</summary>
    [RelayCommand]
    private void Backspace()
    {
        if (Expression.Length > 0)
            Expression = Expression[..^1];
    }

    /// <summary>清空全部</summary>
    [RelayCommand]
    private void Clear()
    {
        Expression = "";
        Result = "0";
    }

    /// <summary>切换 DEG/RAD</summary>
    [RelayCommand]
    private void ToggleAngle()
    {
        DegreeMode = !DegreeMode;
        _evaluator.DegreeMode = DegreeMode;
    }

    /// <summary>计算表达式</summary>
    [RelayCommand]
    private void Equals()
    {
        if (string.IsNullOrWhiteSpace(Expression)) return;
        try
        {
            _evaluator.DegreeMode = DegreeMode;
            double value = _evaluator.Evaluate(Expression);
            Result = FormatNumber(value);
            History.Insert(0, $"{Expression} = {Result}");
            if (History.Count > 50) History.RemoveAt(History.Count - 1);
        }
        catch (Exception ex)
        {
            Result = "错误";
            History.Insert(0, $"{Expression} → {ex.Message}");
            if (History.Count > 50) History.RemoveAt(History.Count - 1);
        }
    }

    /// <summary>点击历史记录回填表达式</summary>
    [RelayCommand]
    private void UseHistory(string entry)
    {
        int idx = entry.IndexOf(" = ", StringComparison.Ordinal);
        Expression = idx > 0 ? entry[..idx] : entry;
        Result = idx > 0 ? entry[(idx + 3)..] : "0";
    }

    private static string FormatNumber(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v))
            return v.ToString().ToUpperInvariant();
        // 超大/超小或非整数时用 G15，普通整数直接显示
        if (v == Math.Floor(v) && Math.Abs(v) < 1e15)
            return ((long)v).ToString("N0");
        return v.ToString("G15");
    }
}
