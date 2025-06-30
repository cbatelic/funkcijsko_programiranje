namespace AvaloniaApplication1.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Input
open Avalonia.Markup.Xaml
open Avalonia.Media
open AvaloniaApplication1.ViewModels

type MainWindow() as this =
    inherit Window()

    do
        this.InitializeComponent()
        let canvas = this.FindControl<Canvas>("NodeCanvas")

        let createVisualNode (viewModel: MainWindowViewModel) (x: float) (y: float) (node: Node) =
            let radius = 50.0

            let circle = Ellipse()
            circle.Width <- radius
            circle.Height <- radius
            circle.Fill <-
                SolidColorBrush(
                    match node.NodeType with
                    | Input _ -> Color.Parse("#2980B9")
                    | Sum -> Color.Parse("#E67E22")
                    | Output _ -> Color.Parse("#27AE60")
                )
            circle.Stroke <- Brushes.White
            circle.StrokeThickness <- 2.0
            Canvas.SetLeft(circle, x)
            Canvas.SetTop(circle, y)
            canvas.Children.Add(circle) |> ignore

            if node.IsEditing then
                let textBox = TextBox()
                textBox.Width <- 40.0
                textBox.Text <- node.Name
                Canvas.SetLeft(textBox, x + 5.0)
                Canvas.SetTop(textBox, y + 15.0)

                // Save on LostFocus
                textBox.LostFocus.Add(fun _ ->
                    let newValue =
                        if System.String.IsNullOrWhiteSpace(textBox.Text) then
                            None
                        else Some textBox.Text
                    viewModel.UpdateNodeValue(node.Id, newValue)
                )

                // Save on Enter
                textBox.KeyDown.Add(fun args ->
                    if args.Key = Key.Enter then
                        textBox.Focusable <- false
                        this.Focus() |> ignore
                )

                canvas.Children.Add(textBox) |> ignore
            else
                let text = TextBlock()
                text.Text <- node.Name
                text.Foreground <- Brushes.White
                text.FontSize <- 14.0
                Canvas.SetLeft(text, x + 10.0)
                Canvas.SetTop(text, y + 15.0)

                text.PointerPressed.Add(fun _ ->
                    viewModel.ToggleEditing(node.Id)
                )

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
                    createVisualNode viewModel x y n
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
