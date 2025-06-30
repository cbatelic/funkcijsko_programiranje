namespace AvaloniaApplication1.ViewModels

open System.Collections.ObjectModel
open System.Windows.Input
open CommunityToolkit.Mvvm.ComponentModel
open CommunityToolkit.Mvvm.Input

type NodeType =
    | Input of float option
    | Sum
    | Output of float option

type Node = {
    Id: int
    Name: string
    NodeType: NodeType
    Position: float * float
    Inputs: int list
    IsEditing: bool
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
    let nodesChanged = Event<unit>()

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
                    NodeType = Input (Some value)
                    Position = (float nodes.Count * 100.0 + 50.0, 100.0)
                    Inputs = []
                    IsEditing = false
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
                |> Seq.filter (fun n -> match n.NodeType with Input (Some _) -> true | _ -> false)
                |> Seq.map (fun n -> n.Id)
                |> Seq.toList

            let id = System.Guid.NewGuid().GetHashCode()
            let node = {
                Id = id
                Name = "+"
                NodeType = Sum
                Position = (300.0, 250.0)
                Inputs = inputIds
                IsEditing = false
            }

            for fromId in inputIds do
                connections.Add((fromId, id))

            nodes.Add(node)
            nodesChanged.Trigger()
        )

    member this.CalculateCommand =
        RelayCommand(fun () ->
            let sumNodeOpt =
                nodes
                |> Seq.tryFind (fun n -> match n.NodeType with Sum -> true | _ -> false)

            match sumNodeOpt with
            | Some sumNode ->
                // očisti stare outputove povezane s ovim sum nodeom
                let oldOutputs =
                    nodes
                    |> Seq.filter (fun n ->
                        match n.NodeType with
                        | Output _ -> List.exists ((=) sumNode.Id) n.Inputs
                        | _ -> false)
                    |> Seq.toList

                for o in oldOutputs do
                    nodes.Remove(o) |> ignore

                // dohvat input nodeova
                let inputNodes =
                    sumNode.Inputs
                    |> List.choose (fun id ->
                        nodes |> Seq.tryFind (fun n -> n.Id = id)
                    )

                // nađi output povezan sa sum čvorom
                let outputNodeOpt =
                    nodes
                    |> Seq.tryFind (fun n ->
                        match n.NodeType with
                        | Output (Some _) when List.exists ((=) sumNode.Id) n.Inputs -> true
                        | _ -> false
                    )

                let knownInputs =
                    inputNodes
                    |> List.choose (fun n ->
                        match n.NodeType with
                        | Input (Some v) -> Some (n.Id, v)
                        | _ -> None
                    )

                let unknownInputs =
                    inputNodes
                    |> List.filter (fun n ->
                        match n.NodeType with Input None -> true | _ -> false
                    )

                match knownInputs.Length, unknownInputs.Length, outputNodeOpt with
                | _, 0, _ ->
                    // standardno zbrajanje: svi inputi poznati
                    let total = knownInputs |> List.map snd |> List.sum

                    let output = {
                        Id = System.Guid.NewGuid().GetHashCode()
                        Name = total.ToString()
                        NodeType = Output (Some total)
                        Position = (500.0, 250.0)
                        Inputs = [ sumNode.Id ]
                        IsEditing = false
                    }

                    connections.Add((sumNode.Id, output.Id))
                    nodes.Add(output)

                    calculationPerformed.Trigger({
                        Inputs = knownInputs |> List.map (fun (id, v) -> (id, v.ToString()))
                        Operation = "+"
                        Result = total.ToString()
                    })

                    nodesChanged.Trigger()

                | _, 1, Some outputNode ->
                    // možemo propagirati unazad – jedan input fali
                    match outputNode.NodeType with
                    | Output (Some total) ->
                        let knownSum = knownInputs |> List.map snd |> List.sum
                        let missingValue = total - knownSum

                        let missingNode = unknownInputs.Head
                        let index = nodes |> Seq.findIndex (fun n -> n.Id = missingNode.Id)

                        nodes.[index] <- {
                            missingNode with
                                NodeType = Input (Some missingValue)
                                Name = missingValue.ToString()
                        }

                        nodesChanged.Trigger()
                    | _ -> ()

                | _ -> ()
            | None -> ()
        )

    member this.UpdateNodeValue(nodeId: int, newValue: string option) =
        let index = nodes |> Seq.tryFindIndex (fun n -> n.Id = nodeId)
        match index with
        | Some i ->
            let node = nodes.[i]
            let updatedNode =
                let parsed =
                    match newValue with
                    | Some s when System.Double.TryParse(s) |> fst -> Some (double s)
                    | _ -> None

                let newName = defaultArg newValue ""

                match node.NodeType with
                | Input _ ->
                    { node with NodeType = Input parsed; Name = newName; IsEditing = false }
                | Output _ ->
                    { node with NodeType = Output parsed; Name = newName; IsEditing = false }
                | _ -> node

            nodes.[i] <- updatedNode
            nodesChanged.Trigger()
        | None -> ()

    member this.ToggleEditing(nodeId: int) =
        let index = nodes |> Seq.tryFindIndex (fun n -> n.Id = nodeId)
        match index with
        | Some i ->
            let node = nodes.[i]
            nodes.[i] <- { node with IsEditing = not node.IsEditing }
            nodesChanged.Trigger()
        | None -> ()

    member this.ClearCommand =
        RelayCommand(fun () ->
            nodes.Clear()
            connections.Clear()
            clearAllRequested.Trigger()
            nodesChanged.Trigger()
        )
