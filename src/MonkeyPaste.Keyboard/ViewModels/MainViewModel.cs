using System;

namespace MonkeyPaste.Keyboard;

public class MainViewModel : MonkeyPaste.Keyboard.Common.ViewModelBase {
    public static bool IsMockKeyboardVisible =>
         OperatingSystem.IsWindows();
    public static string ErrorText { get; set; } //= Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string Test => "Yoooo dude";
#pragma warning disable CA1822 // Mark members as static
    public static string Greeting { get; set; } =
        @"
14.0: 🫠, 🫱🏼‍🫲🏿, 🫰🏽
13.1: 😶‍🌫️, 🧔🏻‍♀️, 🧑🏿‍❤️‍🧑🏾
13.0: 🥲, 🥷🏿, 🐻‍❄️
12.1: 🧑🏻‍🦰, 🧑🏿‍🦯, 👩🏻‍🤝‍👩🏼
12.0: 🦩, 🦻🏿, 👩🏼‍🤝‍👩🏻";
#pragma warning restore CA1822 // Mark members as static
}
