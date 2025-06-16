module AvaloniaApplication1.Logic.BidirectionalPropagation

open AvaloniaApplication1.Models.Node

let private getNodeById (nodes: Node list) id =
    nodes |> List.tryFind (fun n -> n.Id = id)

let tryBackwardSum (nodes: Node list) (outputNode: Node) : Node list =
    match outputNode.NodeType, outputNode.Value with
    | NodeType.Sum, Some total ->
        let inputs =
            outputNode.Inputs
            |> List.choose (getNodeById nodes)

        let known, unknown =
            inputs |> List.partition (fun n -> n.Value.IsSome)

        let knownSum =
            known |> List.choose (fun n -> n.Value) |> List.sum

        match unknown with
        | [missing] ->
            let missingVal = total - knownSum
            [ { missing with Value = Some missingVal } ]
        | _ -> []
    | _ -> []

let tryBackwardSubtract (nodes: Node list) (outputNode: Node) : Node list =
    match outputNode.NodeType, outputNode.Value with
    | NodeType.Subtract, Some total ->
        let inputs =
            outputNode.Inputs
            |> List.choose (getNodeById nodes)

        let known, unknown =
            inputs |> List.partition (fun n -> n.Value.IsSome)

        let knownValues = known |> List.choose (fun n -> n.Value)

        match unknown with
        | [missing] ->
            let rest = knownValues |> List.tail
            let result = total + (rest |> List.sum)
            [ { missing with Value = Some result } ]
        | _ -> []
    | _ -> []

let tryBackwardMultiply (nodes: Node list) (outputNode: Node) : Node list =
    match outputNode.NodeType, outputNode.Value with
    | NodeType.Multiply, Some total ->
        let inputs =
            outputNode.Inputs
            |> List.choose (getNodeById nodes)

        let known, unknown =
            inputs |> List.partition (fun n -> n.Value.IsSome)

        let product =
            known |> List.choose (fun n -> n.Value) |> List.fold (*) 1.0

        match unknown with
        | [missing] when product <> 0.0 ->
            let missingVal = total / product
            [ { missing with Value = Some missingVal } ]
        | _ -> []
    | _ -> []

let tryBackwardDivide (nodes: Node list) (outputNode: Node) : Node list =
    match outputNode.NodeType, outputNode.Value with
    | NodeType.Divide, Some total ->
        let inputs =
            outputNode.Inputs
            |> List.choose (getNodeById nodes)

        let known, unknown =
            inputs |> List.partition (fun n -> n.Value.IsSome)

        let knownValues = known |> List.choose (fun n -> n.Value)

        match unknown with
        | [missing] when knownValues.Length = inputs.Length - 1 ->
            let divisor = knownValues |> List.tail |> List.fold (/) 1.0
            let result =
                if known.Head.Value.IsSome then
                    Some (known.Head.Value.Value / total)
                else Some (total * divisor)
            [ { missing with Value = result } ]
        | _ -> []
    | _ -> []

let tryBackwardSqrt (nodes: Node list) (outputNode: Node) : Node list =
    match outputNode.NodeType, outputNode.Value, outputNode.Inputs with
    | NodeType.Sqrt, Some r, [ aId ] ->
        match getNodeById nodes aId with
        | Some aNode ->
            match aNode.Value with
            | None -> [ { aNode with Value = Some (r * r) } ]
            | _ -> []
        | None -> []
    | _ -> []