namespace AvaloniaApplication1.ViewModels

open System.Collections.ObjectModel
open System.Collections.Generic
open CommunityToolkit.Mvvm.ComponentModel
open CommunityToolkit.Mvvm.Input

type OperatorNodeType =
    | Sum
    | Subtract
    | Multiply
    | Divide
    | Sqrt

type NodeType =
    | Input of float option
    | Output of float option
    | Operator of OperatorNodeType

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

    // Mutable polje za NewValue
    let mutable newValue = ""

    // Flag za kontrolu automatske propagacije
    let mutable allowAutoPropagation = false

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

    member this.AddOperator(op: OperatorNodeType) =
        let inputIds =
            nodes
            |> Seq.filter (fun n -> match n.NodeType with Input (Some _) -> true | _ -> false)
            |> Seq.map (fun n -> n.Id)
            |> Seq.toList

        let id = System.Guid.NewGuid().GetHashCode()
        let node = {
            Id = id
            Name =
                match op with
                | Sum -> "+"
                | Subtract -> "-"
                | Multiply -> "*"
                | Divide -> "/"
                | Sqrt -> "√"
            NodeType = Operator op
            Position = (300.0, 250.0)
            Inputs = inputIds
            IsEditing = false
        }

        for fromId in inputIds do
            connections.Add((fromId, id))

        nodes.Add(node)

        let outputId = System.Guid.NewGuid().GetHashCode()
        let outputNode = {
            Id = outputId
            Name = ""
            NodeType = Output None
            Position = (node.Position |> fun (x,y) -> (x + 150.0, y))
            Inputs = [id]
            IsEditing = false
        }

        connections.Add((id, outputId))
        nodes.Add(outputNode)

        nodesChanged.Trigger()

    member this.AddOperatorCommand =
        RelayCommand<OperatorNodeType>(fun op ->
            this.AddOperator(op)
        )

    member this.GetNodeValue(id: int) : float option =
        nodes
        |> Seq.tryFind (fun n -> n.Id = id)
        |> Option.bind (fun n -> match n.NodeType with Input v | Output v -> v | _ -> None)

    member this.propagate (operator: OperatorNodeType) (inputs: float option list) (output: float option) : Result<(float option list * float option), string> =
        let knownInputs = inputs |> List.choose id
        let unknownInputs = inputs |> List.filter Option.isNone
        let n = List.length inputs

        match knownInputs.Length, unknownInputs.Length, output with
        | k, 0, Some outVal ->
            let result =
                match operator with
                | Sum -> List.sum knownInputs
                | Multiply -> List.reduce (*) knownInputs
                | Subtract -> List.reduce (-) knownInputs
                | Divide -> List.reduce (/) knownInputs
                | Sqrt -> sqrt (List.head knownInputs)
            if abs (result - outVal) < 0.0001 then
                Ok (inputs, Some outVal)
            else
                Error "Vrijednosti ne odgovaraju rezultatu"
        | k, 0, None ->
            let result =
                match operator with
                | Sum -> List.sum knownInputs
                | Multiply -> List.reduce (*) knownInputs
                | Subtract -> List.reduce (-) knownInputs
                | Divide -> List.reduce (/) knownInputs
                | Sqrt -> sqrt (List.head knownInputs)
            Ok (inputs, Some result)
        | k, 1, Some outVal when n > 1 ->
            let missingValue =
                match operator with
                | Sum -> outVal - List.sum knownInputs
                | Multiply -> outVal / List.fold (*) 1.0 knownInputs
                | Subtract -> knownInputs.Head - outVal
                | Divide -> knownInputs.Head / outVal
                | Sqrt -> outVal ** 2.0
            let filled = inputs |> List.map (fun i -> if i.IsNone then Some missingValue else i)
            Ok (filled, Some outVal)
        | _, x, _ when x > 1 -> Error "Premalo poznatih vrijednosti"
        | _ -> Error "Nepodržana kombinacija inputa/outputa"

    member this.TryPropagateNode(node: Node) : bool =
        match node.NodeType with
        | Operator op ->
            let inputValues = node.Inputs |> List.map this.GetNodeValue
            printfn $"🔁 TryPropagateNode: NodeId={node.Id}, Operator={op}, Inputs={inputValues}"

            let outputNodeOpt =
                nodes
                |> Seq.tryFind (fun n ->
                    match n.NodeType with
                    | Output _ -> List.contains node.Id n.Inputs
                    | _ -> false)

            let outputValue =
                outputNodeOpt |> Option.bind (fun o ->
                    match o.NodeType with Output v -> v | _ -> None)

            match this.propagate op inputValues outputValue with
            | Ok (newInputs, newOutput) ->
                let updated = ref false

                for (inputId, newValOpt) in List.zip node.Inputs newInputs do
                    match newValOpt with
                    | Some newVal ->
                        let i = nodes |> Seq.tryFindIndex (fun n -> n.Id = inputId)
                        match i with
                        | Some idx ->
                            let n = nodes.[idx]
                            match n.NodeType with
                            | Input oldVal ->
                                if oldVal <> Some newVal then
                                    printfn $"📝 Updating Input Node {inputId}: {oldVal} → {newVal}"
                                    nodes.[idx] <- { n with NodeType = Input (Some newVal); Name = newVal.ToString("0.##") }
                                    updated := true
                            | _ -> ()
                        | None -> ()
                    | None -> ()

                match outputNodeOpt, newOutput with
                | Some outNode, Some result ->
                    let idx = nodes |> Seq.findIndex (fun n -> n.Id = outNode.Id)
                    match outNode.NodeType with
                    | Output oldVal ->
                        if oldVal <> Some result then
                            printfn $"📝 Updating Output Node {outNode.Id}: {oldVal} → {result}"
                            nodes.[idx] <- { outNode with NodeType = Output (Some result); Name = result.ToString("0.##") }
                            updated := true
                    | _ -> ()
                | _ -> ()

                !updated
            | Error msg ->
                printfn $"⚠️ Propagation error at node {node.Id}: {msg}"
                false
        | _ -> false

    member this.PropagateAll() =
        let mutable anyUpdated = true
        while anyUpdated do
            anyUpdated <- false
            for node in Seq.toList nodes do
                match node.NodeType with
                | Operator _ ->
                    let updated = this.TryPropagateNode(node)
                    if updated then anyUpdated <- true
                | _ -> ()
        nodesChanged.Trigger()

    member this.PropagateCommand =
        RelayCommand(fun () ->
            allowAutoPropagation <- true
            this.PropagateAll()
            allowAutoPropagation <- false
        )

    member this.UpdateNodeValue(nodeId: int, newValue: string option) =
        printfn $"UpdateNodeValue called for nodeId={nodeId} with newValue={newValue}"
        let index = nodes |> Seq.tryFindIndex (fun n -> n.Id = nodeId)
        match index with
        | Some i ->
            let node = nodes.[i]

            let parsed =
                match newValue with
                | Some s when System.Double.TryParse(s) |> fst -> Some (double s)
                | Some s when System.String.IsNullOrWhiteSpace(s) -> None
                | None -> None
                | _ -> None

            let newName =
                match parsed with
                | None -> ""
                | Some v -> v.ToString("0.##")

            printfn $"FINAL: parsed = {parsed}, newName = '{newName}'"

            let updatedNode =
                match node.NodeType with
                | Input _ ->
                    { node with NodeType = Input parsed; Name = newName; IsEditing = false }
                | Output _ ->
                    { node with NodeType = Output parsed; Name = newName; IsEditing = false }
                | _ -> node

            let hasChanged =
                match node.NodeType, updatedNode.NodeType with
                | Input oldVal, Input newVal -> oldVal <> newVal || node.Name <> updatedNode.Name
                | Output oldVal, Output newVal -> oldVal <> newVal || node.Name <> updatedNode.Name
                | _ -> true

            if hasChanged then
                nodes.[i] <- updatedNode
                nodesChanged.Trigger()

                if allowAutoPropagation && parsed.IsSome then
                    this.PropagateAll()
            else
                printfn $"⚠️ No update needed for node {node.Id}"
        | None ->
            printfn "❌ Node not found"

    member this.RemoveNode(nodeId: int) =
        let toRemove =
            connections
            |> Seq.filter (fun (fromId, toId) -> fromId = nodeId || toId = nodeId)
            |> Seq.toList

        for conn in toRemove do
            connections.Remove(conn) |> ignore

        let maybeNode =
            nodes
            |> Seq.tryFind (fun n -> n.Id = nodeId)

        match maybeNode with
        | Some node ->
            nodes.Remove(node) |> ignore
            nodesChanged.Trigger()
            this.PropagateAll()
        | None -> ()

    member this.RemoveNodeCommand =
        RelayCommand<int>(fun id ->
            this.RemoveNode(id)
        )

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
