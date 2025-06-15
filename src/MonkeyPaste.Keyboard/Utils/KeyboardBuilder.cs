using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using KeyboardLib;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;

namespace iosKeyboardTest {
    public static class KeyboardBuilder {
        #region Brushes
        public static SolidColorBrush FgBrush2 { get; } = new SolidColorBrush(Colors.Gainsboro);
        public static SolidColorBrush FgBrush { get; } = new SolidColorBrush(Colors.White);
        public static SolidColorBrush BgBrush { get; } = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush HoldBgBrush { get; } = new SolidColorBrush(Colors.Gold);
        public static SolidColorBrush HoldFocusBgBrush { get; } = new SolidColorBrush(Colors.Orange);
        public static SolidColorBrush HoldFgBrush { get; } = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush PressedBrush { get; } = new SolidColorBrush(Colors.Gray);
        public static SolidColorBrush ShiftBrush { get; } = new SolidColorBrush(Colors.Cyan);
        public static SolidColorBrush MenuBgBrush { get; } = new SolidColorBrush(Colors.Silver);
        public static SolidColorBrush CursorControlBgBrush { get; } = new SolidColorBrush(Color.FromArgb(150,20,20,20));
        public static SolidColorBrush CursorControlFgBrush { get; } = new SolidColorBrush(Colors.White);
        public static SolidColorBrush DefaultKeyBgBrush { get; } = new SolidColorBrush(Colors.Silver);
        public static LinearGradientBrush DefaultKeyGradBgBrush { get; } = new LinearGradientBrush() {
            TransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            StartPoint = new RelativePoint(0.15, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.15, 1, RelativeUnit.Relative),
            GradientStops = [
                new GradientStop(Colors.Silver,0),
                new GradientStop(Colors.DimGray,0.09),
                new GradientStop(Color.FromRgb(68,68,68),0.8),
                new GradientStop(Color.FromRgb(68,68,68),1)
                ]
        };
        public static SolidColorBrush SpecialKeyBgBrush { get; } = new SolidColorBrush(Colors.DimGray);
        public static LinearGradientBrush SpecialKeyGradBgBrush { get; } = new LinearGradientBrush() {
            TransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            StartPoint = new RelativePoint(0.15, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.15, 1, RelativeUnit.Relative),
            GradientStops = [
                new GradientStop(Colors.DimGray,0),
                new GradientStop(Color.FromRgb(51,51,51),0.08),
                new GradientStop(Color.FromRgb(51,51,51),0.8),
                new GradientStop(Color.FromRgb(34,34,34),1)
                ]
        };


        #endregion

        public static Control Build(IKeyboardInputConnection conn, Size desiredSize, double scale, out Size unscaledSize) {
            var kbvm = new KeyboardViewModel(conn, desiredSize, scale);
            var kb = CreateKeyboardView(kbvm);
            KeyboardRenderer.KeyboardView = kb;
            unscaledSize = new Size(kbvm.TotalWidth * scale, kbvm.TotalHeight * scale);
            return kb;
        }

        static Control CreateKeyboardView(KeyboardViewModel kbvm) {
            #region Sytles

            // key fg
            var styles = new List<Style>{
                new Style(x => x.OfType<BuilderKeyView>().Descendant().OfType<TextBlock>()) {
                    Setters = { new Setter(TextBlock.ForegroundProperty, FgBrush) },
                },
                new Style(x => x.OfType<BuilderKeyView>().Descendant().OfType<TextBlock>().Name("SecondaryKeyTextBlock")) {
                    Setters = { new Setter(TextBlock.ForegroundProperty, FgBrush2) }
                },
                new Style(x => x.OfType<BuilderKeyView>().Class("shift").Descendant().OfType<TextBlock>()) {
                    Setters = { new Setter(TextBlock.ForegroundProperty, ShiftBrush) }
                },
                new Style(x => x.OfType<BuilderKeyView>().Class("shift-lock").Not(x = x.Class("down")).Descendant().OfType<TextBlock>()) {
                    Setters = { new Setter(TextBlock.ForegroundProperty, HoldFgBrush) }
                },

                // key bg
                new Style(x => x.OfType<BuilderKeyView>().Descendant().OfType<Border>().Name("KeyBgRect")) {
                    Setters = { new Setter(TextBlock.BackgroundProperty, DefaultKeyBgBrush) }
                },
                new Style(x => x.OfType<BuilderKeyView>().Class("special").Descendant().OfType<Border>().Name("KeyBgRect")) {
                    Setters = { new Setter(TextBlock.BackgroundProperty, SpecialKeyBgBrush) }
                },
                new Style(x => x.OfType<BuilderKeyView>().Class("down").Descendant().OfType<Border>().Name("KeyBgRect")) {
                    Setters = { new Setter(TextBlock.BackgroundProperty, PressedBrush) }
                },
                new Style(x => x.OfType<BuilderKeyView>().Class("shift-lock").Descendant().OfType<Border>().Name("KeyBgRect")) {
                    Setters = { new Setter(TextBlock.BackgroundProperty, HoldBgBrush) }
                },
                new Style(x => x.OfType<BuilderKeyView>().Class("hold-focus").Descendant().OfType<Border>().Name("KeyBgRect")) {
                    Setters = { new Setter(TextBlock.BackgroundProperty, HoldFocusBgBrush) }
                },

                // text layouts
                new Style(x => x.OfType<Canvas>().Descendant().OfType<TextBlock>().Name("KeyTextBlock")) {
                    Setters = {
                        new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center) ,
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center) },
                },

                new Style(x => x.OfType<Canvas>().Descendant().OfType<TextBlock>().Name("SecondaryKeyTextBlock")) {
                    Setters = {
                        new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right) ,
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top) },
                }
            };

            #endregion

            var outer_grid = new Grid() {
                RowDefinitions = new RowDefinitions("Auto,*,Auto")
            };
            outer_grid.Styles.AddRange(styles);

            #region Menu Strip
            var menu_strip = new Border() {
                Background = Brushes.Purple,
                [!Border.HeightProperty] = CreateBinding(kbvm, nameof(kbvm.MenuHeight))
            };
            outer_grid.Children.Add(menu_strip);
            Grid.SetRow(menu_strip, 0);
            #endregion

            #region Key Grid

            var key_items_canvas = new Canvas() {
                ClipToBounds = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Background = BgBrush,
                [!Canvas.WidthProperty] = CreateBinding(kbvm, nameof(kbvm.KeyboardWidth)),
                [!Canvas.HeightProperty] = CreateBinding(kbvm, nameof(kbvm.KeyboardHeight)),
            };
            foreach(var kvm in kbvm.Keys) {
                key_items_canvas.Children.Add(CreateKey(kvm));
            }
            outer_grid.Children.Add(key_items_canvas);
            Grid.SetRow(key_items_canvas, 1);


            #endregion

            #region Key

            BuilderKeyView CreateKey(KeyViewModel kvm) {
                var sec_key_text_block = new TextBlock {
                    Name = "SecondaryKeyTextBlock",
                    ClipToBounds = false,
                    RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    [!TextBlock.OpacityProperty] = CreateBinding(kvm, nameof(kvm.SecondaryOpacity)),
                    [!TextBlock.FontSizeProperty] = CreateBinding(kvm, nameof(kvm.SecondaryFontSize)),
                    [!TextBlock.TextProperty] = CreateBinding(kvm, nameof(kvm.SecondaryValue)),
                    RenderTransform = new TranslateTransform() {
                        [!TranslateTransform.XProperty] = CreateBinding(kvm, nameof(kvm.SecondaryTranslateOffsetX)),
                        [!TranslateTransform.YProperty] = CreateBinding(kvm, nameof(kvm.SecondaryTranslateOffsetY)),
                    }
                };
                var primary_key_text_block = new TextBlock {
                    Name = "KeyTextBlock",
                    ClipToBounds = false,
                    RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    [!TextBlock.OpacityProperty] = CreateBinding(kvm, nameof(kvm.PrimaryOpacity)),
                    [!TextBlock.FontSizeProperty] = CreateBinding(kvm, nameof(kvm.PrimaryFontSize)),
                    [!TextBlock.TextProperty] = CreateBinding(kvm, nameof(kvm.PrimaryValue)),
                    RenderTransform = new TranslateTransform() {
                        [!TranslateTransform.YProperty] = CreateBinding(kvm, nameof(kvm.PullTranslateY)),
                    }
                };
                var fg_border_grid = new Grid();
                fg_border_grid.Children.Add(sec_key_text_block);
                fg_border_grid.Children.Add(primary_key_text_block);

                var fg_border = new Border() {
                    Child = fg_border_grid
                };

                var key_bg_rect = new Border {
                    Name = "KeyBgRect",
                    [!Border.CornerRadiusProperty] = CreateBinding(kvm, nameof(kvm.CornerRadius))
                };
                var inner_grid = new Grid {
                    [!Grid.WidthProperty] = CreateBinding(kvm, nameof(kvm.InnerWidth)),
                    [!Grid.HeightProperty] = CreateBinding(kvm, nameof(kvm.InnerHeight)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                inner_grid.Children.Add(key_bg_rect);
                inner_grid.Children.Add(fg_border);

                var key_view = new BuilderKeyView {
                    DataContext = kvm,
                    [!Canvas.LeftProperty] = CreateBinding(kvm, nameof(kvm.X)),
                    [!Canvas.TopProperty] = CreateBinding(kvm, nameof(kvm.Y)),
                    [!Border.IsVisibleProperty] = CreateBinding(kvm, nameof(kvm.IsVisible)),
                    [!Border.WidthProperty] = CreateBinding(kvm, nameof(kvm.Width)),
                    [!Border.HeightProperty] = CreateBinding(kvm, nameof(kvm.Height)),
                    Child = inner_grid
                };

                void Kvm_CleanUp(object sender, EventArgs e) {
                    if (sender is not KeyViewModel disp_kvm) {
                        return;
                    }
                    disp_kvm.PropertyChanged -= Kvm_PropertyChanged;
                    disp_kvm.OnCleanup -= Kvm_CleanUp;
                }

                void Kvm_PropertyChanged(object sender, PropertyChangedEventArgs e) {
                    switch (e.PropertyName) {
                        case nameof(kvm.PrimaryValue):
                            if(primary_key_text_block == null) {
                                break;
                            }
                            if(kvm.SpecialKeyType == SpecialKeyType.Shift) {

                            }
                            primary_key_text_block.Text = kvm.PrimaryValue;
                            break;
                        case nameof(kvm.IsPressed):
                            if (kvm.IsPressed) {
                                key_view.Classes.Add("down");
                            } else {
                                key_view.Classes.Remove("down");
                            }
                            break;
                        case nameof(kvm.IsActiveKey):
                            if (kvm.IsActiveKey) {
                                key_view.Classes.Add("hold-focus-key");
                            } else {
                                key_view.Classes.Remove("hold-focus-key");
                            }
                            break;
                        case nameof(kvm.IsPopupKey):
                            if (kvm.IsPopupKey) {
                                key_view.Classes.Add("popup-key");
                            } else {
                                key_view.Classes.Remove("popup-key");
                            }
                            break;
                        case nameof(kvm.IsSpecial):
                            if (kvm.IsSpecial) {
                                key_view.Classes.Add("special");
                            } else {
                                key_view.Classes.Remove("special");
                            }
                            break;
                        case nameof(kvm.IsShiftKeyAndOnTemp):
                            if (kvm.IsShiftKeyAndOnTemp) {
                                key_view.Classes.Add("shift");
                            } else {
                                key_view.Classes.Remove("shift");
                            }
                            break;
                        case nameof(kvm.IsShiftKeyAndOnLock):
                            if (kvm.IsShiftKeyAndOnLock) {
                                key_view.Classes.Add("shift-lock");
                            } else {
                                key_view.Classes.Remove("shift-lock");
                            }
                            break;
                    }
                }

                kvm.PropertyChanged += Kvm_PropertyChanged;
                kvm.OnCleanup += Kvm_CleanUp;


                return key_view;
            }

            void Keys_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
                if (e.NewItems != null) {
                    foreach (KeyViewModel kvm in e.NewItems) {
                        key_items_canvas.Children.Add(CreateKey(kvm));
                    }
                }
                if (e.OldItems != null) {
                    foreach (KeyViewModel kvm in e.OldItems) {
                        if (key_items_canvas.Children.FirstOrDefault(x => x.DataContext == kvm) is not { } to_remove) {
                            continue;
                        }
                        kvm.Cleanup();
                        key_items_canvas.Children.Remove(to_remove);
                    }
                }
            }
            kbvm.Keys.CollectionChanged += Keys_CollectionChanged;
            #endregion

            #region Next Keyboard Panel
            var next_kb_panel = new StackPanel {
                Spacing = 10,
                Orientation = Orientation.Horizontal,
                [!StackPanel.IsVisibleProperty] = CreateBinding(kbvm, nameof(kbvm.NeedsNextKeyboardButton))
            };

            var next_kb_btn = new Button {
                //[!Button.CommandProperty] = CreateBinding(kbvm, nameof(kbvm.NextKeyboardCommand)),
                Content = new TextBlock {
                    Foreground = CursorControlFgBrush,
                    FontSize = 24,
                    Text = "⌨"
                }
            };
            next_kb_panel.Children.Add(next_kb_btn);

            var test_kb_btn = new Button {
                //[!Button.CommandProperty] = CreateBinding(kbvm, nameof(kbvm.Test1Command)),
                Content = new TextBlock {
                    Foreground = CursorControlFgBrush,
                    FontSize = 24,
                    Text = "Test1"
                }
            };
            next_kb_panel.Children.Add(test_kb_btn);

            var status_text_block = new TextBlock {
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                [!TextBlock.TextProperty] = CreateBinding(kbvm, nameof(kbvm.ErrorText))
            };
            next_kb_panel.Children.Add(status_text_block);

            #endregion

            #region Cursor Control

            #endregion
            var outer_border = new BuilderKeyView() {
                DataContext = kbvm,
                Child = outer_grid,
                [!BuilderKeyView.WidthProperty] = CreateBinding(kbvm, nameof(kbvm.TotalWidth)),
                [!BuilderKeyView.HeightProperty] = CreateBinding(kbvm, nameof(kbvm.TotalHeight)),
            };

            kbvm.UpdateKeyboardState();
            return outer_border;
        }


        static IBinding CreateBinding(ViewModelBase kbvm, string propName) {
            var binding = new Binding() {
                Source = kbvm,
                Path = propName
            };
            return binding;
        }

        class BuilderKeyView : Border { }
    }
}
