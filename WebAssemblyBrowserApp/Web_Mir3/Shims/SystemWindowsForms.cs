// System.Windows.Forms 兼容壳（仅覆盖原版客户端用到的类型/成员）。
// 浏览器无 WinForms，这里提供最小可用的类型定义让代码编过；
// 真实鼠标/键盘交互后续由 Canvas 输入层接管。
namespace System.Windows.Forms
{
    using System;
    using System.Drawing;

    public enum MouseButtons
    {
        None = 0,
        Left = 0x100000,
        Right = 0x200000,
        Middle = 0x400000,
        XButton1 = 0x800000,
        XButton2 = 0x1000000,
    }

    [Flags]
    public enum Keys : int
    {
        None = 0,
        // 修饰键
        ShiftKey = 0x10,
        ControlKey = 0x11,
        Menu = 0x12,
        Shift = 0x10000,
        Control = 0x20000,
        Alt = 0x40000,
        LShiftKey = 0xA0,
        RShiftKey = 0xA1,
        LControlKey = 0xA2,
        RControlKey = 0xA3,
        LMenu = 0xA4,
        RMenu = 0x20012,
        // 控制键
        Back = 0x08,
        Tab = 0x09,
        LineFeed = 0x0A,
        Clear = 0x0C,
        Enter = 0x0D,
        Return = 0x0D,
        Escape = 0x1B,
        Space = 0x20,
        Prior = 0x21,
        PageUp = 0x21,
        Next = 0x22,
        PageDown = 0x22,
        End = 0x23,
        Home = 0x24,
        Left = 0x25,
        Up = 0x26,
        Right = 0x27,
        Down = 0x28,
        Select = 0x29,
        Print = 0x2A,
        Execute = 0x2B,
        PrintScreen = 0x2C,
        Insert = 0x2D,
        Delete = 0x2E,
        Help = 0x2F,
        Sleep = 0x5F,
        LWin = 0x5B,
        RWin = 0x5C,
        Apps = 0x5D,
        Capital = 0x14,
        CapsLock = 0x14,
        NumLock = 0x90,
        Scroll = 0x91,
        Pause = 0x13,
        Cancel = 0x03,
        Snapshot = 0x2C,
        // 鼠标按键
        LButton = 0x01,
        RButton = 0x02,
        MButton = 0x04,
        XButton1 = 0x80,
        XButton2 = 0x81,
        // 数字
        D0 = 0x30, D1 = 0x31, D2 = 0x32, D3 = 0x33, D4 = 0x34, D5 = 0x35, D6 = 0x36, D7 = 0x37, D8 = 0x38, D9 = 0x39,
        // 字母
        A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45, F = 0x46, G = 0x47, H = 0x48, I = 0x49,
        J = 0x4A, K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E, O = 0x4F,
        P = 0x50, Q = 0x51, R = 0x52, S = 0x53, T = 0x54, U = 0x55, V = 0x56, W = 0x57, X = 0x58, Y = 0x59, Z = 0x5A,
        // 功能键
        F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73, F5 = 0x74, F6 = 0x75, F7 = 0x76, F8 = 0x77, F9 = 0x78, F10 = 0x79, F11 = 0x7A, F12 = 0x7B,
        F13 = 0x7C, F14 = 0x7D, F15 = 0x7E, F16 = 0x7F, F17 = 0x80, F18 = 0x81, F19 = 0x82, F20 = 0x83, F21 = 0x84, F22 = 0x85, F23 = 0x86, F24 = 0x87,
        // 小键盘
        NumPad0 = 0x60, NumPad1 = 0x61, NumPad2 = 0x62, NumPad3 = 0x63, NumPad4 = 0x64, NumPad5 = 0x65, NumPad6 = 0x66, NumPad7 = 0x67, NumPad8 = 0x68, NumPad9 = 0x69,
        // 小键盘运算符
        Multiply = 0x6A, Add = 0x6B, Separator = 0x6C, Subtract = 0x6D, Decimal = 0x6E, Divide = 0x6F,
        // OEM 键
        OemSemicolon = 0xBA, Oem1 = 0xBA, Oemplus = 0xBB, Oemcomma = 0xBC, OemMinus = 0xBD, OemPeriod = 0xBE, OemQuestion = 0xBF,
        Oemtilde = 0xC0, Oem3 = 0xC0, OemOpenBrackets = 0xDB, Oem4 = 0xDB, OemPipe = 0xDC, Oem5 = 0xDC,
        OemCloseBrackets = 0xDD, Oem6 = 0xDD, OemQuotes = 0xDE, Oem7 = 0xDE, Oem8 = 0xDF, Oem102 = 0xE2, OemBackslash = 0xE2,
        // 多媒体 / 浏览器
        BrowserBack = 0xA6, BrowserForward = 0xA7, BrowserRefresh = 0xA8, BrowserStop = 0xA9,
        BrowserSearch = 0xAA, BrowserFavorites = 0xAB, BrowserHome = 0xAC,
        VolumeMute = 0xAD, VolumeDown = 0xAE, VolumeUp = 0xAF,
        MediaNextTrack = 0xB0, MediaPreviousTrack = 0xB1, MediaStop = 0xB2, MediaPlayPause = 0xB3,
        LaunchMail = 0xB4, SelectMedia = 0xB5, LaunchApplication1 = 0xB6, LaunchApplication2 = 0xB7,
        // IME
        KanaMode = 0x15, JunjaMode = 0x17, FinalMode = 0x18, HanjaMode = 0x19,
        IMEConvert = 0x1C, IMENonconvert = 0x1D, IMEAccept = 0x1E, IMEModeChange = 0x1F,
        // 杂项
        ProcessKey = 0xE5, Packet = 0xE7, Attn = 0xF6, Crsel = 0xF7, Exsel = 0xF8,
        EraseEof = 0xF9, Play = 0xFA, Zoom = 0xFB, NoName = 0xFC, Pa1 = 0xFD, OemClear = 0xFE, KeyLock = 0x90,
        KeyCode = 0xFFFF,
        Modifiers = unchecked((int)0xFFFF0000),
    }

    [Flags]
    public enum TextFormatFlags
    {
        Default = 0,
        Left = 0x1,
        Top = 0x0,
        HorizontalCenter = 0x2,
        Right = 0x4,
        VerticalCenter = 0x8,
        Bottom = 0x10,
        WordBreak = 0x20,
        SingleLine = 0x40,
        ExpandTabs = 0x80,
        NoClipping = 0x100,
        ExternalLeading = 0x200,
        NoPrefix = 0x400,
        Internal = 0x800,
        TextBoxControl = 0x2000,
        PathEllipsis = 0x4000,
        EndEllipsis = 0x8000,
        ModifyString = 0x10000,
        RightToLeft = 0x20000,
        NoFullWidthCharacterBreak = 0x80000,
        HidePrefix = 0x100000,
        NoPadding = 0x1000000,
        LeftAndRightPadding = 0x2000000,
        PreserveGraphicsClipping = 0x4000000,
        PreserveGraphicsTranslateTransform = 0x8000000,
        NoWrap = 0x10000000,
        WordEllipsis = 0x20000000,
    }

    public enum BorderStyle
    {
        None = 0,
        FixedSingle = 1,
        Fixed3D = 2,
    }

    public class MouseEventArgs : EventArgs
    {
        public MouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta)
        {
            Button = button;
            Clicks = clicks;
            X = x;
            Y = y;
            Delta = delta;
        }

        public MouseButtons Button { get; }
        public int Clicks { get; }
        public int X { get; }
        public int Y { get; }
        public int Delta { get; }
        public Point Location => new Point(X, Y);
    }

    public class KeyEventArgs : EventArgs
    {
        public KeyEventArgs(Keys keyData) { KeyData = keyData; }

        public Keys KeyData { get; }
        public Keys KeyCode => (Keys)((int)KeyData & 0xFFFF);
        public bool Alt => (KeyData & Keys.Alt) != 0;
        public bool Control => (KeyData & Keys.Control) != 0;
        public bool Shift => (KeyData & Keys.Shift) != 0;
        public bool Handled { get; set; }
        public bool SuppressKeyPress { get; set; }
    }

    public class KeyPressEventArgs : EventArgs
    {
        public KeyPressEventArgs(char keyChar) { KeyChar = keyChar; }

        public char KeyChar { get; }
        public bool Handled { get; set; }
    }

    public class PreviewKeyDownEventArgs : EventArgs
    {
        public PreviewKeyDownEventArgs(Keys keyData) { KeyData = keyData; }

        public Keys KeyData { get; }
        public Keys KeyCode => (Keys)((int)KeyData & 0xFFFF);
        public bool Alt => (KeyData & Keys.Alt) != 0;
        public bool Control => (KeyData & Keys.Control) != 0;
        public bool Shift => (KeyData & Keys.Shift) != 0;
        public bool IsInputKey { get; set; }
    }

    public struct Message
    {
        public IntPtr HWnd { get; set; }
        public int Msg { get; set; }
        public IntPtr WParam { get; set; }
        public IntPtr LParam { get; set; }
        public IntPtr Result { get; set; }
    }

    public class Cursor
    {
        public static Cursor Current { get; set; } = new Cursor();
    }

    public static class Cursors
    {
        public static Cursor Arrow { get; } = new Cursor();
        public static Cursor Default { get; } = new Cursor();
        public static Cursor IBeam { get; } = new Cursor();
        public static Cursor WaitCursor { get; } = new Cursor();
        public static Cursor Cross { get; } = new Cursor();
        public static Cursor Hand { get; } = new Cursor();
        public static Cursor SizeAll { get; } = new Cursor();
        public static Cursor SizeWE { get; } = new Cursor();
        public static Cursor SizeNWSE { get; } = new Cursor();
        public static Cursor SizeNESW { get; } = new Cursor();
        public static Cursor SizeNS { get; } = new Cursor();
        public static Cursor No { get; } = new Cursor();
        public static Cursor Help { get; } = new Cursor();
    }

    public class TextBox : IDisposable
    {
        public MirEngine.Font Font { get; set; }
        public int MaxLength { get; set; }
        public bool UseSystemPasswordChar { get; set; }
        public bool ReadOnly { get; set; }
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
        public Point Location { get; set; }
        public Size Size { get; set; }
        public bool Visible { get; set; }
        public string Text { get; set; }
        public int SelectionStart { get; set; }
        public int SelectionLength { get; set; }
        public int TextLength => (Text ?? string.Empty).Length;
        public IntPtr Handle => IntPtr.Zero;
        public bool Multiline { get; set; }
        public bool AcceptsReturn { get; set; }
        public bool AcceptsTab { get; set; }
        public BorderStyle BorderStyle { get; set; }
        public object Parent { get; set; }
        public bool IsDisposed { get; } = false;

        public event EventHandler TextChanged;
        public event EventHandler GotFocus;
        public event EventHandler LostFocus;
        public event EventHandler<KeyPressEventArgs> KeyPress;
        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;
        public event EventHandler<MouseEventArgs> MouseDown;
        public event EventHandler<MouseEventArgs> MouseMove;
        public event EventHandler<MouseEventArgs> MouseUp;
        public event EventHandler<PreviewKeyDownEventArgs> PreviewKeyDown;
        public event EventHandler<MouseEventArgs> MouseWheel;

        public virtual void DrawToBitmap(Image image, Rectangle rectangle) { }
        protected virtual void OnMouseUp(MouseEventArgs e) { }
        protected virtual void WndProc(ref Message m) { }
        protected virtual void OnMouseClick(MouseEventArgs e) { }
        protected virtual void OnKeyDown(KeyEventArgs e) { }
        protected virtual void OnKeyUp(KeyEventArgs e) { }
        protected virtual void OnPreviewKeyDown(PreviewKeyDownEventArgs e) { }
        protected virtual void OnTextChanged(EventArgs e) { }
        protected virtual void OnSizeChanged(EventArgs e) { }

        public int GetLineFromCharIndex(int index) => 0;
        public void SelectAll() { }
        public void Invalidate() { }
        public Graphics CreateGraphics() => null;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
    }

    public class Timer
    {
        public Timer() { }

        public event EventHandler Tick;
        public int Interval { get; set; }
        public bool Enabled { get; set; }

        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }

    public static class SystemInformation
    {
        public static int MouseButtons => 3;
        public static int VerticalScrollBarWidth => 17;
        public static int HorizontalScrollBarHeight => 17;
        public static int Border3DSize => 2;
        public static int CaptionHeight => 23;
        public static int FrameBorderSize => 4;
        public static int DoubleClickTime => 500;
        public static Size PrimaryMonitorSize => new Size(1024, 768);
        public static Rectangle VirtualScreen => new Rectangle(0, 0, 1024, 768);
        public static int MouseWheelScrollDelta => 120;
    }

    public static class Application
    {
        public static string ExecutablePath => string.Empty;
        public static void DoEvents() { }
        public static void Exit() { }
        public static void Run() { }
    }

    public static class TextRenderer
    {
        public static Size MeasureText(string text, MirEngine.Font font) => Size.Empty;
        public static Size MeasureText(string text, MirEngine.Font font, Size proposedSize) => Size.Empty;
        public static Size MeasureText(string text, MirEngine.Font font, TextFormatFlags flags) => Size.Empty;
        public static Size MeasureText(string text, MirEngine.Font font, Size proposedSize, TextFormatFlags flags) => Size.Empty;

        public static void DrawText(Graphics g, string text, MirEngine.Font font, Point pt, Color foreColor) { }
        public static void DrawText(Graphics g, string text, MirEngine.Font font, Point pt, Color foreColor, TextFormatFlags flags) { }
        public static void DrawText(Graphics g, string text, MirEngine.Font font, Rectangle bounds, Color foreColor) { }
        public static void DrawText(Graphics g, string text, MirEngine.Font font, Rectangle bounds, Color foreColor, TextFormatFlags flags) { }
    }
}
