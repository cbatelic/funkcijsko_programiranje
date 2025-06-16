namespace AvaloniaApplication1.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Markup.Xaml
open Avalonia.Media
open AvaloniaApplication1.ViewModels
open System.Diagnostics

type MainWindow() as this =
    inherit Window()

    do
        this.InitializeComponent()

        let canvas = this.FindControl<Canvas>("NodeCanvas")

        let createVisualNode (x: float) (y: float) (label: string) (color: string) =
            let radius = 50.0

            let circle = Ellipse()
            circle.Width <- radius
            circle.Height <- radius
            circle.Fill <- SolidColorBrush(Color.Parse(color))
            circle.Stroke <- Brushes.White
            circle.StrokeThickness <- 2.0
            Canvas.SetLeft(circle, x)
            Canvas.SetTop(circle, y)

            let text = TextBlock()
            text.Text <- label
            text.Foreground <- Brushes.White
            text.FontSize <- 14.0
            Canvas.SetLeft(text, x + radius / 4.0)
            Canvas.SetTop(text, y + radius / 4.0)

            canvas.Children.Add(circle) |> ignore
            canvas.Children.Add(text) |> ignore

        let drawConnection (x1: float) (y1: float) (x2: float) (y2: float) =
            let line = Line()
            line.StartPoint <- Point(x1, y1)
            line.EndPoint <- Point(x2, y2)
            line.Stroke <- Brushes.Gray
            line.StrokeThickness <- 2.0
            canvas.Children.Add(line) |> ignore

        let attachIfViewModel (dc: obj) =
            match dc with
            | :? MainWindowViewModel as viewModel ->
                viewModel.CalculationPerformed.Add(fun args ->
                    canvas.Children.Clear()

                    if List.isEmpty args.Inputs then
                        Debug.WriteLine("⚠️ Lista čvorova je prazna, preskačem crtanje.")
                    else
                        let inputPositions =
                            args.Inputs
                            |> List.mapi (fun i (_, valueLabel) ->
                                let x = 50.0
                                let y = 100.0 + float i * 100.0
                                createVisualNode x y valueLabel "#2980B9"
                                (x + 50.0, y + 25.0)
                            )

                        let centerY = inputPositions |> List.map snd |> List.average
                        let opX = 300.0
                        let resultX = 500.0

                        createVisualNode opX (centerY - 25.0) args.Operation "#E67E22"
                        createVisualNode resultX (centerY - 25.0) args.Result "#27AE60"

                        for (x, y) in inputPositions do
                            drawConnection x y opX centerY

                        drawConnection (opX + 50.0) centerY resultX centerY
                )

                viewModel.ClearAllRequested.Add(fun () ->
                    canvas.Children.Clear()
                )
            | _ -> ()

        attachIfViewModel this.DataContext
        this.DataContextChanged.Add(fun _ -> attachIfViewModel this.DataContext)

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
