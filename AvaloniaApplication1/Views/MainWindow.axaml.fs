namespace AvaloniaApplication1.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml

type MainWindow() as this =
    inherit Window()
    do
        this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
