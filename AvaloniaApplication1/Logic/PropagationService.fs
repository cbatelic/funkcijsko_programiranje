module AvaloniaApplication1.Logic.PropagationService

open AvaloniaApplication1.Models.Node

let getOperation (node: Node) : float list -> float option =
    match node.NodeType with
    | Sum -> fun inputs -> Some (List.sum inputs)
    | Subtract -> fun inputs ->
        match inputs with
        | h::t -> Some (List.fold (-) h t)
        | _ -> None
    | Multiply -> fun inputs -> Some (List.fold (*) 1.0 inputs)
    | Divide -> fun inputs ->
        match inputs with
        | h::t when not (List.exists ((=) 0.0) t) -> Some (List.fold (/) h t)
        | _ -> None
    | Sqrt -> fun inputs ->
        match inputs with
        | [x] when x >= 0.0 -> Some (sqrt x)
        | _ -> None
    | _ -> fun _ -> None

let propagate (nodes: Node list) (targetId: int) : float option =
    let rec eval (id: int) : float option =
        match nodes |> List.tryFind (fun n -> n.Id = id) with
        | Some node ->
            match node.NodeType with
            | NodeType.Input ->
                node.Value
            | NodeType.Sum ->
                node.Inputs
                |> List.map eval
                |> List.fold (fun acc v -> match acc, v with Some a, Some b -> Some(a + b) | _ -> None) (Some 0.0)
            | NodeType.Subtract ->
                match node.Inputs |> List.map eval with
                | Some a :: Some b :: _ -> Some (a - b)
                | _ -> None
            | NodeType.Multiply ->
                node.Inputs
                |> List.map eval
                |> List.fold (fun acc v -> match acc, v with Some a, Some b -> Some(a * b) | _ -> None) (Some 1.0)
            | NodeType.Divide ->
                match node.Inputs |> List.map eval with
                | Some a :: Some b :: _ when b <> 0.0 -> Some (a / b)
                | _ -> None
            | NodeType.Sqrt ->
                match node.Inputs |> List.tryHead |> Option.bind eval with
                | Some x when x >= 0.0 -> Some (sqrt x)
                | _ -> None
            | _ -> None
        | None -> None
    eval targetId

