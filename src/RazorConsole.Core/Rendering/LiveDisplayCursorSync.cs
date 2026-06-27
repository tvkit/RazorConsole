// Copyright (c) RazorConsole. All rights reserved.

using RazorConsole.Core.Focus;
using RazorConsole.Core.Input;
using static RazorConsole.Core.Utilities.AnsiSequences;

namespace RazorConsole.Core.Rendering;

/// <summary>
/// Coordinates end-of-frame hardware cursor placement for focused text inputs.
/// </summary>
internal static class LiveDisplayCursorSync
{
    private static FocusManager? _focusManager;
    private static KeyboardEventManager? _keyboardEventManager;
    private static bool _forceHideInputCursor;

    internal static void Attach(FocusManager focusManager, KeyboardEventManager keyboardEventManager)
    {
        _focusManager = focusManager;
        _keyboardEventManager = keyboardEventManager;
    }

    internal static void Detach()
    {
        _forceHideInputCursor = false;
        _focusManager = null;
        _keyboardEventManager = null;
    }

    internal static void SetForceHideInputCursor(bool hide) => _forceHideInputCursor = hide;

    /// <summary>
    /// Builds the ANSI control sequence to emit at the end of a diff frame.
    /// </summary>
    /// <param name="maxWidth">Line width used to clamp the caret column.</param>
    /// <remarks>
    /// DiffRenderable finishes with the hardware cursor on the line below the bottom border (col 1).
    /// Bottom-mounted TextInput content is two lines above that position.
    /// </remarks>
    internal static string BuildEndOfFrameCursorControl(int maxWidth)
    {
        if (_focusManager is null || _keyboardEventManager is null)
        {
            return SM(DECTCEM);
        }

        if (!_focusManager.TryGetFocusedTarget(out var target) || target is null)
        {
            return RM(DECTCEM);
        }

        if (!IsTextInput(target))
        {
            return RM(DECTCEM);
        }

        if (ShouldHideCursor(target))
        {
            return RM(DECTCEM);
        }

        var column = Math.Clamp(_keyboardEventManager.GetTextInputCaretColumn(target), 1, Math.Max(1, maxWidth));
        return CUU(2) + CUF(column - 1) + SetCursorStyle(CursorBlinkBlock) + SM(DECTCEM);
    }

    /// <summary>Restores the terminal default cursor shape after live display exits.</summary>
    internal static string BuildRestoreCursorStyleControl() => SetCursorStyle(CursorDefault);

    private static bool IsTextInput(FocusManager.FocusTarget target) =>
        target.Attributes.TryGetValue("data-text-input", out var flag)
        && string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldHideCursor(FocusManager.FocusTarget target) =>
        _forceHideInputCursor
        || (target.Attributes.TryGetValue("data-hide-cursor", out var hide)
            && string.Equals(hide, "true", StringComparison.OrdinalIgnoreCase));
}