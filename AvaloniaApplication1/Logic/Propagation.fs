module AvaloniaApplication1.Logic.Propagation
open FSharp.Data.Adaptive
open AvaloniaApplication1.Models.Node

let rec calculateNodeValue (nodes: Node list) (nodeId: int) : aval<float option> =
    let node = nodes |> List.find (fun n -> n.Id = nodeId)
    
    match node.NodeType with
    | Input -> 
        AVal.constant node.Value
    | Sum ->
        let inputValues = node.Inputs |> List.map (fun id -> calculateNodeValue nodes id)
        
        aval.Bind2 (inputValues.[0], inputValues.[1], fun (v1, v2) ->
        match (v1, v2) with
        | (Some x, Some y) -> 
            AVal.constant (Some (x + y)) 
        | _ -> 
            AVal.constant None  
        )

    | Multiply ->
        let inputValues = node.Inputs |> List.map (fun id -> calculateNodeValue nodes id)
        
        aval.Bind2 (inputValues.[0], inputValues.[1], fun (v1, v2) ->
        match (v1, v2) with
        | (Some x, Some y) -> 
            AVal.constant (Some (x * y))  
        | _ -> 
            AVal.constant None  
        )
    | Output -> 
        AVal.constant node.Value