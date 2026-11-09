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

    /// <summary>等号确认后的最终结果（大字显示）</summary>
    [ObservableProperty]
    public partial string Result { get; set; } = "";

    /// <summary>输入过程中的实时预览结果</summary>
    [ObservableProperty]
    public partial string Preview { get; set; } = "";

    /// <summary>详细错误信息（表达式非法时显示）</summary>
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool DegreeMode { get; set; } = false;

    [ObservableProperty]
    public partial string ModeText { get; set; } = "RAD";

    /// <summary>上次有效计算结果，对应 Ans 键</summary>
    [ObservableProperty]
    public partial string AnsValue { get; set; } = "0";

    /// <summary>是否显示右侧历史记录面板（收起时窗口更紧凑）</summary>
    [ObservableProperty]
    public partial bool ShowHistory { get; set; } = false;

    partial void OnDegreeModeChanged(bool value)
    {
        ModeText = value ? "DEG" : "RAD";
        UpdatePreview();
    }

    partial void OnExpressionChanged(string value) => UpdatePreview();

    public ObservableCollection<string> History { get; } = new();

    /// <summary>按钮输入：插入文本到表达式末尾</summary>
    [RelayCommand]
    private void Insert(string token)
    {
        ErrorMessage = null;
        Expression += token;
    }

    /// <summary>退格：删除表达式末尾一个字符</summary>
    [RelayCommand]
    private void Backspace()
    {
        if (Expression.Length > 0)
            Expression = Expression[..^1];
    }

    /// <summary>清空当前表达式与结果</summary>
    [RelayCommand]
    private void Clear()
    {
        Expression = "";
        Result = "";
        Preview = "";
        ErrorMessage = null;
    }

    /// <summary>切换 DEG/RAD</summary>
    [RelayCommand]
    private void ToggleAngle()
    {
        DegreeMode = !DegreeMode;
    }

    /// <summary>开/关右侧历史记录面板</summary>
    [RelayCommand]
    private void ToggleHistory() => ShowHistory = !ShowHistory;

    /// <summary>计算表达式（等号）</summary>
    [RelayCommand]
    private void Equals()
    {
        if (string.IsNullOrWhiteSpace(Expression))
        {
            Result = "";
            return;
        }

        try
        {
            _evaluator.DegreeMode = DegreeMode;
            _evaluator.LastAnswer = double.TryParse(AnsValue, out var a) ? a : 0;
            double value = _evaluator.Evaluate(Expression);

            _evaluator.LastAnswer = value;
            AnsValue = FormatNumber(value);
            Result = FormatNumber(value);
            Preview = "";
            ErrorMessage = null;
            History.Insert(0, $"{Expression} = {Result}");
            if (History.Count > 50) History.RemoveAt(History.Count - 1);
        }
        catch (Exception ex)
        {
            Result = "错误";
            Preview = "";
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>点击历史记录回填表达式</summary>
    [RelayCommand]
    private void UseHistory(string entry)
    {
        int idx = entry.LastIndexOf(" = ", StringComparison.Ordinal);
        if (idx > 0)
        {
            Expression = entry[..idx];
            Result = entry[(idx + 3)..];
            Preview = "";
            ErrorMessage = null;
        }
    }

    /// <summary>清空历史记录</summary>
    [RelayCommand]
    private void ClearHistory() => History.Clear();

    /// <summary>输入时实时求值，形成预览（不弹错误，仅静默失败）</summary>
    private void UpdatePreview()
    {
        if (string.IsNullOrWhiteSpace(Expression))
        {
            Preview = "";
            return;
        }

        try
        {
            _evaluator.DegreeMode = DegreeMode;
            _evaluator.LastAnswer = double.TryParse(AnsValue, out var a) ? a : 0;
            double v = _evaluator.Evaluate(Expression);
            // 表达式不完整（如以运算符结尾）时结果与输入一致，不显示预览
            if (!string.Equals(FormatNumber(v), Expression.TrimEnd(), StringComparison.OrdinalIgnoreCase))
                Preview = FormatNumber(v);
            else
                Preview = "";
        }
        catch
        {
            Preview = ""; // 输入中途，静默忽略
        }
    }

    private static string FormatNumber(double v)
    {
        if (double.IsNaN(v)) return "NaN";
        if (double.IsPositiveInfinity(v)) return "∞";
        if (double.IsNegativeInfinity(v)) return "-∞";
        // 整数带千分位；小数/超大数用 G15
        if (v == Math.Floor(v) && Math.Abs(v) < 1e15)
            return ((long)v).ToString("N0");
        return v.ToString("G15");
    }
}
