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
        |> Option.bind (fun n ->
            match n.NodeType with
            | Input v | Output v -> v
            | _ -> None)

    member this.propagate (operator: OperatorNodeType) (inputs: float option list) (output: float option) : Result<(float option list * float option), string> =
        let knownInputs = inputs |> List.choose id
        let unknownInputs = inputs |> List.filter Option.isNone
        let n = List.length inputs

        match knownInputs.Length, unknownInputs.Length, output with
        | k, 0, Some outVal ->
            let result =
                match operator with
                | Sum -> knownInputs |> List.sum
                | Multiply -> knownInputs |> List.reduce (*)
                | Subtract -> knownInputs |> List.reduce (-)
                | Divide -> knownInputs |> List.reduce (/)
                | Sqrt -> sqrt (knownInputs.Head)

            if abs (result - outVal) < 0.0001 then
                Ok (inputs, Some outVal)
            else
                Error "ERROR: Values do not match output"
        | k, 0, None ->
            let result =
                match operator with
                | Sum -> knownInputs |> List.sum
                | Multiply -> knownInputs |> List.reduce (*)
                | Subtract -> knownInputs |> List.reduce (-)
                | Divide -> knownInputs |> List.reduce (/)
                | Sqrt -> sqrt (knownInputs.Head)

            Ok (inputs, Some result)
        | _, 1, Some outVal when n > 1 ->
            let knownSum = 
                match operator with
                | Sum -> knownInputs |> List.sum
                | Multiply -> knownInputs |> List.fold (*) 1.0
                | Subtract -> knownInputs.Head
                | Divide -> knownInputs.Head
                | Sqrt -> outVal

            let missingValue =
                match operator with
                | Sum -> outVal - knownSum
                | Multiply -> outVal / knownSum
                | Subtract -> outVal + knownSum
                | Divide -> outVal * knownSum
                | Sqrt -> outVal ** 2.0

            let filled = inputs |> List.map (fun i -> if i.IsNone then Some missingValue else i)
            Ok (filled, Some outVal)
        | _, x, _ when x > 1 -> Error "Underdetermined"
        | _ -> Error "Unsupported combination"

    member this.TryPropagateNode(node: Node) : bool =
        match node.NodeType with
        | Operator op ->
            let inputValues = node.Inputs |> List.map this.GetNodeValue
            let outputNodeOpt =
                nodes
                |> Seq.tryFind (fun n -> match n.NodeType with Output _ -> List.exists ((=) node.Id) n.Inputs | _ -> false)

            let outputValue = outputNodeOpt |> Option.bind (fun o -> match o.NodeType with Output v -> v | _ -> None)

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
                            | Input None ->
                                nodes.[idx] <- { n with NodeType = Input (Some newVal); Name = newVal.ToString("0.##") }
                                updated := true
                            | _ -> ()
                        | _ -> ()
                    | None -> ()

                match outputNodeOpt, newOutput with
                | Some outNode, Some result ->
                    let idx = nodes |> Seq.findIndex (fun n -> n.Id = outNode.Id)
                    match outNode.NodeType with
                    | Output None ->
                        nodes.[idx] <- { outNode with NodeType = Output (Some result); Name = result.ToString("0.##") }
                        updated := true
                    | _ -> ()
                | _ -> ()

                !updated

            | Error _ -> false
        | _ -> false

    member this.PropagateAll() =
        let visited = HashSet<int>()

        let rec propagateFrom nodeId =
            if visited.Contains(nodeId) then () else
                visited.Add(nodeId) |> ignore

                let affectedNodes =
                    nodes
                    |> Seq.filter (fun n -> List.exists ((=) nodeId) n.Inputs)
                    |> Seq.toList

                for node in affectedNodes do
                    let updated = this.TryPropagateNode(node)
                    if updated then
                        propagateFrom node.Id

        let startingNodes =
            nodes
            |> Seq.filter (fun n -> match n.NodeType with Input (Some _) -> true | _ -> false)
            |> Seq.map (fun n -> n.Id)
            |> Seq.toList

        for id in startingNodes do
            propagateFrom id

        nodesChanged.Trigger()

    member this.PropagateCommand =
        RelayCommand(fun () ->
            this.PropagateAll()
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
                | Input _ -> { node with NodeType = Input parsed; Name = newName; IsEditing = false }
                | Output _ -> { node with NodeType = Output parsed; Name = newName; IsEditing = false }
                | _ -> node
            nodes.[i] <- updatedNode
            nodesChanged.Trigger()
            this.PropagateAll()
        | None -> ()

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