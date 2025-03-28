namespace AvaloniaApplication1.ViewModels

open System.Collections.ObjectModel
open System.Windows.Input
open CommunityToolkit.Mvvm.ComponentModel
open CommunityToolkit.Mvvm.Input
open AvaloniaApplication1.Models.RuntimeNode
open AvaloniaApplication1.Models.Node
open AvaloniaApplication1.Logic.Propagation
open FSharp.Data.Adaptive

type MainWindowViewModel() =
    inherit ObservableObject()

    let mutable nextId = 6

    let initialNodes =
        [
            { Id = 1; Name = "A"; NodeType = Input; Inputs = []; Value = Some 3.0 }
            { Id = 2; Name = "B"; NodeType = Input; Inputs = []; Value = Some 2.0 }
            { Id = 3; Name = "Sum1"; NodeType = Sum; Inputs = [1; 2]; Value = None }
            { Id = 4; Name = "Multiply1"; NodeType = Multiply; Inputs = [1; 3]; Value = None }
            { Id = 5; Name = "Output1"; NodeType = Output; Inputs = [4]; Value = None }
        ]

    let nodeCollection =
        initialNodes
        |> List.map (fun n -> RuntimeNode(n.Id, n.Name, calculateNodeValue initialNodes n.Id))
        |> fun list -> ObservableCollection<RuntimeNode>(list)

    let mutable newNodeName = ""
    let mutable newNodeValue = ""

    let mutable selectedInput1: RuntimeNode option = None
    let mutable selectedInput2: RuntimeNode option = None

    let mutable selectedOperation = "Sum"
    let operationOptions = [ "Sum"; "Multiply" ]

    member this.Nodes = nodeCollection

    member this.NewNodeName
        with get() = newNodeName
        and set value = ignore (this.SetProperty(&newNodeName, value))

    member this.NewNodeValue
        with get() = newNodeValue
        and set value = ignore (this.SetProperty(&newNodeValue, value))

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
        and set value = ignore (this.SetProperty(&selectedOperation, value))

    member this.OperationOptions = operationOptions

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

                let runtime = RuntimeNode(newNode.Id, newNode.Name, AVal.constant newNode.Value)
                nodeCollection.Add(runtime)
                nextId <- nextId + 1

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
                    |> Seq.map (fun r ->
                        {
                            Id = r.Id
                            Name = r.Name
                            NodeType = Input
                            Inputs = []
                            Value = r.Value 
                        } : Node
                    )
                    |> Seq.toList
                    |> fun list -> list @ [ newNode ]

                let runtime = RuntimeNode(newNode.Id, newNode.Name, calculateNodeValue allNodes newNode.Id)
                nodeCollection.Add(runtime)
                nextId <- nextId + 1
                this.NewNodeName <- ""
            | _ -> ()
        ) :> ICommand

