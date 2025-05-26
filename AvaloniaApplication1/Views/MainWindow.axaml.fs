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

        let createVisualNode (id: int) (x: float) (y: float) (label: string) (color: string) =
            Debug.WriteLine($"🎨 Creating node '{label}' at ({x},{y})")

            let radius = 60.0

            let circle = Ellipse()
            circle.Width <- radius
            circle.Height <- radius
            circle.Fill <- SolidColorBrush(Color.Parse(color))
            circle.Stroke <- Brushes.White
            circle.StrokeThickness <- 2.0
            circle.Tag <- id
            Canvas.SetLeft(circle, x)
            Canvas.SetTop(circle, y)

            let text = TextBlock()
            text.Text <- label
            text.Foreground <- Brushes.White
            text.FontSize <- 16.0
            text.FontWeight <- FontWeight.Bold

            let centerX = x + radius / 2.0
            let centerY = y + radius / 2.0

            // ⚠️ Precizno centriranje teksta (na oko)
            Canvas.SetLeft(text, centerX - 10.0)
            Canvas.SetTop(text, centerY - 10.0)

            canvas.Children.Add(circle) |> ignore
            canvas.Children.Add(text) |> ignore

        let drawConnection (x1: float) (y1: float) (x2: float) (y2: float) =
            let line = Line()
            line.SetValue(Line.StartPointProperty, Point(x1, y1)) |> ignore
            line.SetValue(Line.EndPointProperty, Point(x2, y2)) |> ignore
            line.Stroke <- Brushes.White
            line.StrokeThickness <- 2.0
            canvas.Children.Add(line) |> ignore

        let attachIfViewModel (dc: obj) =
            match dc with
            | :? MainWindowViewModel as viewModel ->
                Debug.WriteLine("✅ CalculationPerformed connected!")
                viewModel.CalculationPerformed.Add(fun (inputs, operation, result) ->
                    Debug.WriteLine($"🚨 Drawing graph with {inputs.Length} inputs")

                    canvas.Children.Clear()

                    // 🔧 Canvas dimenzije
                    let canvasWidth = 900.0
                    let canvasHeight = 500.0

                    // 🔧 X pozicije centrirano
                    let spacingX = 200.0
                    let inputX = canvasWidth / 2.0 - spacingX
                    let operatorX = canvasWidth / 2.0
                    let resultX = canvasWidth / 2.0 + spacingX

                    // 🔧 Y raspored
                    let spacingY = 100.0
                    let baseY = canvasHeight / 2.0

                    let totalHeight = float (inputs.Length - 1) * spacingY
                    let startY = baseY - totalHeight / 2.0

                    let inputPositions =
                        inputs
                        |> List.mapi (fun i (id, _) ->
                            let y = startY + float i * spacingY

                            let valueStr =
                                viewModel.Nodes
                                |> Seq.tryFind (fun n -> n.Id = id)
                                |> Option.bind (fun n -> n.Value)
                                |> Option.map string
                                |> Option.defaultValue "?"

                            let label = valueStr
                            createVisualNode id inputX y label "#3498DB"
                            (inputX + 60.0, y + 30.0)
                        )

                    let centerY =
                        inputPositions
                        |> List.map snd
                        |> List.average

                    // 🔣 Simbol operacije
                    let opSymbol =
                        match operation with
                        | "Zbrajanje" -> "+"
                        | "Oduzimanje" -> "-"
                        | "Množenje" -> "×"
                        | "Dijeljenje" -> "÷"
                        | "Korijen" -> "√"
                        | _ -> operation

                    // 🟧 Operacija čvor
                    createVisualNode -1 operatorX (centerY - 30.0) opSymbol "#E67E22"

                    // ✅ Rezultat čvor (samo broj)
                    createVisualNode -2 resultX (centerY - 30.0) $"{result}" "#2ECC71"

                    for (x, y) in inputPositions do
                        drawConnection x y operatorX centerY

                    drawConnection (operatorX + 60.0) centerY resultX centerY
                )
            | _ ->
                Debug.WriteLine("⚠️ ViewModel NOT FOUND")

        attachIfViewModel this.DataContext
        this.DataContextChanged.Add(fun _ -> attachIfViewModel this.DataContext)

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
