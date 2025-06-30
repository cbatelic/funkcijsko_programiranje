namespace AvaloniaApplication1.ViewModels

open System.Collections.ObjectModel
open System.Windows.Input
open CommunityToolkit.Mvvm.ComponentModel
open CommunityToolkit.Mvvm.Input

type NodeType =
    | Input of float
    | Sum
    | Output of float option

type Node = {
    Id: int
    Name: string
    NodeType: NodeType
    Position: float * float
    Inputs: int list
}

type CalculationPerformedArgs = {
    Inputs: (int * string) list
    Operation: string
    Result: string
}

type MainWindowViewModel() =
    inherit ObservableObject()

    let nodes = ObservableCollection<Node>()
    let connections = ObservableCollection<(int * int)>()

    let calculationPerformed = Event<CalculationPerformedArgs>()
    let clearAllRequested = Event<unit>()
    let nodesChanged = Event<unit>() // Nova notifikacija

    let mutable newValue = ""

    member this.Nodes = nodes
    member this.Connections = connections

    member this.CalculationPerformed = calculationPerformed.Publish
    member this.ClearAllRequested = clearAllRequested.Publish
    member this.NodesChanged = nodesChanged.Publish

    member this.NewValue
        with get() = newValue
        and set v = this.SetProperty(&newValue, v) |> ignore

    member this.AddInputCommand =
        RelayCommand(fun () ->
            match System.Double.TryParse(newValue) with
            | true, value ->
                let id = System.Guid.NewGuid().GetHashCode()
                let node = {
                    Id = id
                    Name = value.ToString()
                    NodeType = Input value
                    Position = (float nodes.Count * 100.0 + 50.0, 100.0)
                    Inputs = []
                }
                nodes.Add(node)
                this.NewValue <- ""
                nodesChanged.Trigger()
            | _ -> ()
        )

    member this.AddSumOperationCommand =
        RelayCommand(fun () ->
            let inputIds =
                nodes
                |> Seq.filter (fun n -> match n.NodeType with Input _ -> true | _ -> false)
                |> Seq.map (fun n -> n.Id)
                |> Seq.toList

            let id = System.Guid.NewGuid().GetHashCode()
            let node = {
                Id = id
                Name = "+"
                NodeType = Sum
                Position = (300.0, 250.0)
                Inputs = inputIds
            }

            for fromId in inputIds do
                connections.Add((fromId, id))

            nodes.Add(node)
            nodesChanged.Trigger()
        )

    member this.CalculateCommand =
        RelayCommand(fun () ->
            let sumNode =
                nodes
                |> Seq.tryFind (fun n -> match n.NodeType with Sum -> true | _ -> false)

            match sumNode with
            | Some sum ->
                let inputs =
                    sum.Inputs
                    |> List.choose (fun id ->
                        nodes
                        |> Seq.tryFind (fun n -> n.Id = id)
                        |> Option.bind (fun n -> match n.NodeType with Input v -> Some (id, v) | _ -> None))

                let total = inputs |> List.map snd |> List.sum

                let outputId = System.Guid.NewGuid().GetHashCode()
                let output = {
                    Id = outputId
                    Name = total.ToString()
                    NodeType = Output (Some total)
                    Position = (500.0, 250.0)
                    Inputs = [ sum.Id ]
                }

                connections.Add((sum.Id, outputId))
                nodes.Add(output)

                calculationPerformed.Trigger({
                    Inputs = inputs |> List.map (fun (id, v) -> (id, v.ToString()))
                    Operation = "+"
                    Result = total.ToString()
                })

                nodesChanged.Trigger()
            | None -> ()
        )

    member this.ClearCommand =
        RelayCommand(fun () ->
            nodes.Clear()
            connections.Clear()
            clearAllRequested.Trigger()
            nodesChanged.Trigger()
        )
