namespace AvaloniaApplication1.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Controls.Shapes
open Avalonia.Media
open Avalonia.Input
open Avalonia

type MainWindow() as this =
    inherit Window()

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        this.AddSampleNode()

    member private this.AddSampleNode() =
        let canvas = this.FindControl<Canvas>("GraphCanvas")

        let node = Ellipse(Width = 60.0, Height = 60.0, Fill = Brushes.Orange)
        Canvas.SetLeft(node, 100.0)
        Canvas.SetTop(node, 100.0)

        let node2 = Ellipse(Width = 60.0, Height = 60.0, Fill = Brushes.LightBlue)
        Canvas.SetLeft(node2, 300.0)
        Canvas.SetTop(node2, 200.0)

        let line = Line(Stroke = Brushes.White, StrokeThickness = 2.0)

        let updateLine () =
            let node1X = Canvas.GetLeft(node) + node.Width / 2.0
            let node1Y = Canvas.GetTop(node) + node.Height / 2.0
            let node2X = Canvas.GetLeft(node2) + node2.Width / 2.0
            let node2Y = Canvas.GetTop(node2) + node2.Height / 2.0

            line.StartPoint <- Point(node1X, node1Y)
            line.EndPoint <- Point(node2X, node2Y)

        updateLine() 

        let setupDragDrop (element: Ellipse) =
            let mutable isDragging = false
            let mutable lastPoint = Point()

            element.PointerPressed.Add(fun e ->
                isDragging <- true
                lastPoint <- e.GetPosition(canvas)
                e.Pointer.Capture(element) |> ignore
            )

            element.PointerMoved.Add(fun e ->
                if isDragging then
                    let currentPos = e.GetPosition(canvas)
                    let dx = currentPos.X - lastPoint.X
                    let dy = currentPos.Y - lastPoint.Y

                    let newLeft = Canvas.GetLeft(element) + dx
                    let newTop = Canvas.GetTop(element) + dy

                    Canvas.SetLeft(element, newLeft)
                    Canvas.SetTop(element, newTop)

                    lastPoint <- currentPos
                    updateLine()
            )

            element.PointerReleased.Add(fun e ->
                isDragging <- false
                e.Pointer.Capture(null) |> ignore
            )

        setupDragDrop node
        setupDragDrop node2

        canvas.Children.Add(line) |> ignore
        canvas.Children.Add(node) |> ignore
        canvas.Children.Add(node2) |> ignore
