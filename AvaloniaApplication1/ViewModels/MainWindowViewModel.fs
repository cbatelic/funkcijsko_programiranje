namespace AvaloniaApplication1.ViewModels

open System.Collections.ObjectModel
open System.Windows.Input
open CommunityToolkit.Mvvm.ComponentModel
open CommunityToolkit.Mvvm.Input
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Input
open Avalonia.Media
open AvaloniaApplication1.Models.RuntimeNode
open AvaloniaApplication1.Models.Node
open AvaloniaApplication1.Logic.Propagation
open FSharp.Data.Adaptive
open System.Collections.Generic
open Avalonia

type AdaptiveNetwork() =
    let adaptiveNodes = clist()
    let adaptiveConnections = clist()
    let nodeMap = Dictionary<int, AdaptiveNode>()

    member val Nodes = adaptiveNodes with get
    member val Connections = adaptiveConnections with get

    member _.AddNode(node: AdaptiveNode) =
        nodeMap[node.Id] <- node
        adaptiveNodes.Add(node) |> ignore

    member _.ConnectNodes(fromNode: AdaptiveNode, toNode: AdaptiveNode) =
        adaptiveConnections.Add({ FromNode = fromNode; ToNode = toNode }) |> ignore

    member _.TryGetNode(id: int) =
        match nodeMap.TryGetValue(id) with
        | true, node -> Some node
        | _ -> None

type MainWindowViewModel() =
    inherit ObservableObject()

    let mutable nextId = 1
    let adaptiveNetwork = AdaptiveNetwork()
    let mutable selectedConnectionStart : AdaptiveNode option = None
    let mutable allNodes : Node list = []
    let mutable draggedNode : AdaptiveNode option = None

    let nodeCollection = ObservableCollection<RuntimeNode>()
    let mutable currentX = 100.0
    let mutable currentY = 100.0

    let mutable newNodeName = ""
    let mutable newNodeValue = ""
    let mutable selectedInput1 : RuntimeNode option = None
    let mutable selectedInput2 : RuntimeNode option = None
    let mutable selectedOperation = "Sum"
    let operationOptions = [ "Sum"; "Multiply" ]

    let renderRequested = Event<unit>()

    member this.Nodes = nodeCollection
    member this.AdaptiveNetwork = adaptiveNetwork

    member this.NewNodeName
        with get() = newNodeName
        and set value = this.SetProperty(&newNodeName, value) |> ignore

    member this.NewNodeValue
        with get() = newNodeValue
        and set value = this.SetProperty(&newNodeValue, value) |> ignore

    member this.Input1
        with get() = selectedInput1 |> Option.toObj
        and set v =
            selectedInput1 <- Option.ofObj v
            this.OnPropertyChanged(nameof this.Input1)

    member this.Input2
        with get() = selectedInput2 |> Option.toObj
        and set v =
            selectedInput2 <- Option.ofObj v
            this.OnPropertyChanged(nameof this.Input2)

    member this.SelectedOperation
        with get() = selectedOperation
        and set value = this.SetProperty(&selectedOperation, value) |> ignore

    member this.OperationOptions = operationOptions

    member this.RenderRequested = renderRequested.Publish
    member this.RequestRender() = renderRequested.Trigger()

    member private this.CreateRuntimeNode (node: Node) (allNodes: Node list) =
        RuntimeNode(node.Id, node.Name, calculateNodeValue allNodes node.Id)

    member private this.AddNodeInternal (node: Node) =
        allNodes <- allNodes @ [node]
        let runtimeNode = this.CreateRuntimeNode node allNodes
        nodeCollection.Add(runtimeNode)
        let adaptiveNode = { Id = node.Id; Name = node.Name; Value = cval (defaultArg node.Value 0.0); X = cval currentX; Y = cval currentY }
        adaptiveNetwork.AddNode adaptiveNode
        nextId <- nextId + 1
        this.RequestRender()

        currentX <- currentX + 100.0 
        if currentX > 600.0 then
            currentX <- 100.0
            currentY <- currentY + 100.0  

    member this.AddNodeCommand : ICommand =
        RelayCommand(fun () -> 
            match System.Double.TryParse(newNodeValue) with
            | true, parsed -> 
                let newNode = {
                    Id = nextId
                    Name = newNodeName
                    NodeType = Input
                    Inputs = []
                    Value = Some parsed
                }
                this.AddNodeInternal newNode
                this.NewNodeName <- ""
                this.NewNodeValue <- ""
            | _ -> ()
        ) :> ICommand

    member this.AddConnectedNodeCommand : ICommand =
        RelayCommand(fun () -> 
            match selectedInput1, selectedInput2 with
            | Some n1, Some n2 -> 
                let inputIds = [ n1.Id; n2.Id ]
                let nodeType = 
                    match selectedOperation with
                    | "Sum" -> NodeType.Sum
                    | "Multiply" -> NodeType.Multiply
                    | _ -> NodeType.Sum

                let newNode = {
                    Id = nextId
                    Name = newNodeName
                    NodeType = nodeType
                    Inputs = inputIds
                    Value = None
                }

                allNodes <- allNodes @ [newNode]
                let runtimeNode = this.CreateRuntimeNode newNode allNodes
                nodeCollection.Add(runtimeNode)

                let adaptiveNode = { Id = newNode.Id; Name = newNode.Name; Value = cval 0.0; X = cval currentX; Y = cval currentY }
                adaptiveNetwork.AddNode adaptiveNode

                // Povezivanje čvorova
                match adaptiveNetwork.TryGetNode(n1.Id), adaptiveNetwork.TryGetNode(n2.Id) with
                | Some a1, Some a2 -> 
                    adaptiveNetwork.ConnectNodes(a1, adaptiveNode)
                    adaptiveNetwork.ConnectNodes(a2, adaptiveNode)
                | _ -> ()

                nextId <- nextId + 1
                this.NewNodeName <- ""
                this.RequestRender()
            | _ -> ()
        ) :> ICommand

    member this.SyncNodesCommand : ICommand =
        RelayCommand(fun () -> 
            match selectedInput1, selectedInput2 with
            | Some n1, Some n2 -> 
                // Povezivanje selektiranih čvorova
                match adaptiveNetwork.TryGetNode(n1.Id), adaptiveNetwork.TryGetNode(n2.Id) with
                | Some a1, Some a2 -> 
                    adaptiveNetwork.ConnectNodes(a1, a2)
                    this.RequestRender()
            | _ -> ()
        ) :> ICommand

    member this.RenderAll(canvas: Canvas) =
        canvas.Children.Clear()

        for node in adaptiveNetwork.Nodes do
            let isSelected =
                match selectedConnectionStart with
                | Some n when n.Id = node.Id -> true
                | _ -> false

            let fillBrush = if isSelected then Brushes.Orange else Brushes.LightGreen

            let ellipse = Ellipse(Width = 60.0, Height = 60.0, Fill = fillBrush, Stroke = Brushes.Black, StrokeThickness = 2.0)
            Canvas.SetLeft(ellipse, node.X.Value)
            Canvas.SetTop(ellipse, node.Y.Value)
            canvas.Children.Add(ellipse) |> ignore

            ellipse.PointerPressed.Add(fun args ->
                if args.GetCurrentPoint(ellipse).Properties.IsLeftButtonPressed then
                    draggedNode <- Some node
                    args.Pointer.Capture(ellipse) |> ignore
            )

            ellipse.PointerMoved.Add(fun args ->
                match draggedNode with
                | Some dn when args.Pointer.Captured <> null -> 
                    let capturedElement = args.Pointer.Captured
                    let p = args.GetPosition(canvas)
                    dn.Y.Value <- p.Y - 30.0
                    dn.X.Value <- p.X - 30.0
                    this.RequestRender()
                | _ -> ()
            )

            ellipse.PointerReleased.Add(fun args ->
                draggedNode <- None
                args.Pointer.Capture(canvas :> IInputElement) |> ignore
            )

            let label = TextBlock(Text = node.Name, Foreground = Brushes.White)
            Canvas.SetLeft(label, node.X.Value + 10.0)
            Canvas.SetTop(label, node.Y.Value + 20.0)
            canvas.Children.Add(label) |> ignore

            let valueText =
                let v = AVal.force node.Value
                TextBlock(Text = $"%.2f{v}", Foreground = Brushes.Yellow)
            Canvas.SetLeft(valueText, node.X.Value + 10.0)
            Canvas.SetTop(valueText, node.Y.Value + 35.0)
            canvas.Children.Add(valueText) |> ignore

        for conn in adaptiveNetwork.Connections do
            let line = Line(
                StartPoint = Point(conn.FromNode.X.Value + 30.0, conn.FromNode.Y.Value + 30.0),
                EndPoint = Point(conn.ToNode.X.Value + 30.0, conn.ToNode.Y.Value + 30.0),
                Stroke = Brushes.White,
                StrokeThickness = 2.0)
            canvas.Children.Add(line) |> ignore
