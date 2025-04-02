namespace AvaloniaApplication1.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open AvaloniaApplication1.ViewModels

type MainWindow() as this =
    inherit Window()

    let mutable canvas : Canvas = null

    do
        AvaloniaXamlLoader.Load(this)
        canvas <- this.FindControl<Canvas>("GraphCanvas")

        let vm = MainWindowViewModel()
        this.DataContext <- vm
        vm.RenderRequested.Add(fun () -> vm.RenderAll(canvas))
        vm.RequestRender()
