namespace AvaloniaApplication1.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Media
open Avalonia.Markup.Xaml
open AvaloniaApplication1.ViewModels

type MainWindow() as this =
    inherit Window()

    let mutable canvas : Canvas = null

    do
        AvaloniaXamlLoader.Load(this)
        canvas <- this.FindControl<Canvas>("GraphCanvas")

        this.DataContextChanged.Add(fun _ ->
            match this.DataContext with
            | :? MainWindowViewModel as vm ->
                vm.RenderRequested.Add(fun c -> vm.RenderAll(canvas))
                vm.RequestRender() 
            | _ -> ()
        )
