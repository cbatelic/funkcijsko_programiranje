namespace AvaloniaApplication1.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Markup.Xaml
open Avalonia.Media
open AvaloniaApplication1.ViewModels

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
            Canvas.SetLeft(text, x + 10.0)
            Canvas.SetTop(text, y + 15.0)

            canvas.Children.Add(circle) |> ignore
            canvas.Children.Add(text) |> ignore

        let drawConnection (x1: float) (y1: float) (x2: float) (y2: float) =
            let line = Line()
            line.StartPoint <- Point(x1, y1)
            line.EndPoint <- Point(x2, y2)
            line.Stroke <- Brushes.Gray
            line.StrokeThickness <- 2.0
            canvas.Children.Add(line) |> ignore

        let renderCanvas (viewModel: MainWindowViewModel) =
            canvas.Children.Clear()

            let nodePositions =
                viewModel.Nodes
                |> Seq.map (fun n ->
                    let (x, y) = n.Position
                    let color =
                        match n.NodeType with
                        | Input _ -> "#2980B9"
                        | Sum -> "#E67E22"
                        | Output _ -> "#27AE60"
                    createVisualNode x y n.Name color
                    (n.Id, (x + 25.0, y + 25.0))
                )
                |> Map.ofSeq

            for (fromId, toId) in viewModel.Connections do
                match Map.tryFind fromId nodePositions, Map.tryFind toId nodePositions with
                | Some (x1, y1), Some (x2, y2) -> drawConnection x1 y1 x2 y2
                | _ -> ()

        let attachIfViewModel (dc: obj) =
            match dc with
            | :? MainWindowViewModel as viewModel ->
                viewModel.CalculationPerformed.Add(fun _ -> renderCanvas viewModel)
                viewModel.ClearAllRequested.Add(fun () -> canvas.Children.Clear())
                viewModel.NodesChanged.Add(fun () -> renderCanvas viewModel)
            | _ -> ()

        attachIfViewModel this.DataContext
        this.DataContextChanged.Add(fun _ -> attachIfViewModel this.DataContext)

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
