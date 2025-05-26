module AvaloniaApplication1.Logic.Propagation

open FSharp.Data.Adaptive
open AvaloniaApplication1.Models.Node

let rec calculateNodeValue (nodes: Node list) (nodeId: int) : aval<float option> =
    let node = nodes |> List.find (fun n -> n.Id = nodeId)

    let inputValues = node.Inputs |> List.map (calculateNodeValue nodes)

    match node.NodeType with
    | Input -> AVal.constant node.Value

    | Output -> AVal.constant node.Value

    | Sum ->
        if inputValues.IsEmpty then AVal.constant None
        else
            AVal.custom (fun _ ->
                inputValues
                |> List.map AVal.force
                |> List.fold (fun acc opt ->
                    match acc, opt with
                    | Some a, Some b -> Some (a + b)
                    | _ -> None) (Some 0.0))

    | Multiply ->
        if inputValues.IsEmpty then AVal.constant None
        else
            AVal.custom (fun _ ->
                inputValues
                |> List.map AVal.force
                |> List.fold (fun acc opt ->
                    match acc, opt with
                    | Some a, Some b -> Some (a * b)
                    | _ -> None) (Some 1.0))

    | Subtract ->
        AVal.custom (fun _ ->
            match inputValues |> List.map AVal.force with
            | h :: t when t.Length > 0 ->
                t |> List.fold (fun acc opt ->
                    match acc, opt with
                    | Some a, Some b -> Some (a - b)
                    | _ -> None) h
            | _ -> None)

    | Divide ->
        AVal.custom (fun _ ->
            match inputValues |> List.map AVal.force with
            | h :: t when t.Length > 0 ->
                t |> List.fold (fun acc opt ->
                    match acc, opt with
                    | Some a, Some 0.0 -> None
                    | Some a, Some b -> Some (a / b)
                    | _ -> None) h
            | _ -> None)

    | Sqrt ->
        AVal.custom (fun _ ->
            match inputValues |> List.map AVal.force with
            | h :: _ when h.IsSome && h.Value >= 0.0 -> Some (sqrt h.Value)
            | _ -> None)