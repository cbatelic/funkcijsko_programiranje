namespace AvaloniaApplication1.ViewModels

open System.Collections.ObjectModel
open System.Windows.Input
open CommunityToolkit.Mvvm.ComponentModel
open CommunityToolkit.Mvvm.Input
open Avalonia.Controls
open Avalonia.Controls.Shapes
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

    let mutable nextId = 6
    let adaptiveNetwork = AdaptiveNetwork()

    let initialNodes =
        [
            { Id = 1; Name = "A"; NodeType = Input; Inputs = []; Value = Some 3.0 }
            { Id = 2; Name = "B"; NodeType = Input; Inputs = []; Value = Some 2.0 }
        ]

    let nodeCollection =
        initialNodes
        |> List.map (fun n -> RuntimeNode(n.Id, n.Name, calculateNodeValue initialNodes n.Id))
        |> ObservableCollection

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

    member private this.CreateRuntimeNode (node: Node) allNodes =
        RuntimeNode(node.Id, node.Name, calculateNodeValue allNodes node.Id)

    member private this.AddNodeInternal (node: Node) allNodes =
        let runtimeNode = this.CreateRuntimeNode node allNodes
        nodeCollection.Add(runtimeNode)
        nextId <- nextId + 1

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

                this.AddNodeInternal newNode initialNodes

                let adaptiveNode = { Id = nextId; Name = newNodeName; Value = cval parsed; X = cval 50.0; Y = cval 50.0 }
                adaptiveNetwork.AddNode adaptiveNode

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

                let allNodes =
                    nodeCollection
                    |> Seq.map (fun r -> { Id = r.Id; Name = r.Name; NodeType = Input; Inputs = []; Value = r.Value })
                    |> Seq.toList
                    |> fun list -> list @ [ newNode ]

                this.AddNodeInternal newNode allNodes

                let adaptiveNode = { Id = nextId; Name = newNodeName; Value = cval 0.0; X = cval 100.0; Y = cval 100.0 }
                adaptiveNetwork.AddNode adaptiveNode

                match adaptiveNetwork.TryGetNode(n1.Id), adaptiveNetwork.TryGetNode(n2.Id) with
                | Some a1, Some a2 ->
                    adaptiveNetwork.ConnectNodes(a1, adaptiveNode)
                    adaptiveNetwork.ConnectNodes(a2, adaptiveNode)
                | _ -> ()

                this.NewNodeName <- ""
            | _ -> ()
        ) :> ICommand

    member this.RenderAll(canvas: Canvas) =
        canvas.Children.Clear()

        for node in adaptiveNetwork.Nodes do
            let ellipse = Ellipse(Width = 60.0, Height = 60.0, Fill = Brushes.LightGreen)
            Canvas.SetLeft(ellipse, node.X.Value)
            Canvas.SetTop(ellipse, node.Y.Value)
            canvas.Children.Add(ellipse) |> ignore

        for conn in adaptiveNetwork.Connections do
            let line = Line(
                StartPoint = Point(conn.FromNode.X.Value + 30.0, conn.FromNode.Y.Value + 30.0),
                EndPoint = Point(conn.ToNode.X.Value + 30.0, conn.ToNode.Y.Value + 30.0),
                Stroke = Brushes.White,
                StrokeThickness = 2.0)
            canvas.Children.Add(line) |> ignore
